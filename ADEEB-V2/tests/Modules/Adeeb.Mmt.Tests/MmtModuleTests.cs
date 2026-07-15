using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Application.Abstractions.AcademicCatalog;
using Adeeb.Modules.Mmt.Application;
using Adeeb.Modules.Mmt.Application.Import;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Adeeb.Mmt.Tests;

public sealed class MmtModuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Duplicate_cluster_code_is_rejected_after_normalization()
    {
        await using var db = Db(); var service = Catalog(db);
        Assert.True((await service.CreateClusterAsync(new("Cluster 2", " c2 ", null), default)).IsSuccess);
        var duplicate = await service.CreateClusterAsync(new("Other", "C2", null), default);
        Assert.Equal(MmtErrors.DuplicateCluster.Code, duplicate.Error?.Code);
    }

    [Fact]
    public async Task Catalog_reads_resolve_the_current_request_language()
    {
        await using var db = Db();
        var service = Catalog(db);
        var created = await service.CreateClusterAsync(new("Кластери 2", "C2", "Тавсиф",
            NameTg: "Кластери 2", NameRu: "Кластер 2", DescriptionTg: "Тавсиф", DescriptionRu: "Описание"), default);

        var result = await service.GetClusterAsync(created.Value!.Id, SupportedLanguage.Russian, default);
        Assert.Equal("Кластер 2", result.Value!.Name);
        Assert.Equal("Описание", result.Value.Description);
        Assert.Equal("Кластери 2", result.Value.NameTg);
        Assert.Equal("Кластер 2", result.Value.NameRu);
    }

    [Fact]
    public async Task Duplicate_specialty_code_is_rejected_after_normalization()
    {
        await using var db = Db(); var service = Catalog(db);
        Assert.True((await service.CreateSpecialtyAsync(new("law", "Law", null), default)).IsSuccess);
        var duplicate = await service.CreateSpecialtyAsync(new(" LAW ", "Other", null), default);
        Assert.Equal(MmtErrors.DuplicateSpecialty.Code, duplicate.Error?.Code);
    }

    [Fact]
    public async Task Cluster_subjects_are_saved_and_localized_for_reads()
    {
        await using var db = Db();
        var subjectId = Guid.NewGuid();
        var service = Catalog(db);
        var created = await service.CreateClusterAsync(new("Cluster", "C1", null, SubjectIds: [subjectId]), default);

        var result = await service.GetClusterAsync(created.Value!.Id, SupportedLanguage.Russian, default);

        var subject = Assert.Single(result.Value!.Subjects!);
        Assert.Equal(subjectId, subject.Id);
        Assert.Equal("Russian Subject", subject.Name);
    }

    [Fact]
    public async Task Duplicate_admission_program_is_rejected()
    {
        await using var db = Db(); var refs = SeedReferences(db); await db.SaveChangesAsync();
        var service = Programs(db);
        var request = new CreateAdmissionProgramDto(refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, 0, 0, 0, 2026, 20, false);
        Assert.True((await service.CreateProgramAsync(request, default)).IsSuccess);
        Assert.Equal(MmtErrors.DuplicateProgram.Code, (await service.CreateProgramAsync(request, default)).Error?.Code);
    }

    [Fact]
    public async Task Duplicate_score_for_program_year_and_round_is_rejected()
    {
        await using var db = Db(); var program = await SeedProgram(db); var service = Programs(db);
        Assert.True((await service.AddScoreAsync(program.Id, new(2025, 250, null, null, null), default)).IsSuccess);
        Assert.Equal(MmtErrors.DuplicateScore.Code, (await service.AddScoreAsync(program.Id, new(2025, 260, null, null, null), default)).Error?.Code);
    }

    [Fact]
    public async Task Same_program_and_year_accepts_main_and_repeat_scores()
    {
        await using var db = Db(); var program = await SeedProgram(db); var service = Programs(db);
        Assert.True((await service.AddScoreAsync(program.Id, new(2025, 250, null, null, null, (int)DistributionRound.Main), default)).IsSuccess);
        Assert.True((await service.AddScoreAsync(program.Id, new(2025, 240, null, null, null, (int)DistributionRound.Repeat), default)).IsSuccess);
        Assert.Equal(2, await db.PassingScores.CountAsync());
    }

    [Fact]
    public void Conservative_threshold_is_max_of_latest_and_three_year_average()
    {
        var analytics = AdmissionProgramService.Analytics([250m, 300m, 290m]);
        Assert.Equal(250m, analytics.LatestPassingScore);
        Assert.Equal(280m, analytics.AverageLast3Years);
        Assert.Equal(280m, analytics.ConservativeThreshold);
    }

    [Fact]
    public async Task Import_preview_detects_invalid_and_duplicate_rows()
    {
        await using var db = Db(); var service = Import(db);
        var bytes = Workbook(Row(clusterCode: ""), Row(), Row());
        var result = await service.PreviewAsync(new MmtImportPreviewRequestDto { File = File(bytes), CreateMissingReferences = true }, default);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalRows);
        Assert.Equal(2, result.Value.InvalidRowsCount);
        Assert.Equal(1, result.Value.DuplicateRowsCount);
    }

    [Fact]
    public async Task Import_confirm_creates_references_program_and_score()
    {
        await using var db = Db(); var service = Import(db); var file = File(Workbook(Row()));
        var result = await service.ConfirmAsync(new MmtImportConfirmRequestDto { File = file, CreateMissingReferences = true, PublishAdmissionPrograms = true }, default);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, await db.Clusters.CountAsync()); Assert.Equal(1, await db.Universities.CountAsync()); Assert.Equal(1, await db.Specialties.CountAsync());
        Assert.Equal(1, await db.AdmissionPrograms.CountAsync()); Assert.Equal(1, await db.PassingScores.CountAsync());
        Assert.True((await db.AdmissionPrograms.SingleAsync()).IsPublished);
    }

    [Fact]
    public async Task Import_without_distribution_round_column_defaults_to_main()
    {
        await using var db = Db();
        var legacyHeaders = MmtSpreadsheet.Headers.Where(x => x != "DistributionRound").ToArray();
        var legacyRow = Row().Where((_, index) => index != 14).ToArray();

        var result = await Import(db).ConfirmAsync(new MmtImportConfirmRequestDto
        {
            File = File(WorkbookWithHeaders(legacyHeaders, legacyRow)),
            CreateMissingReferences = true
        }, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(DistributionRound.Main, (await db.PassingScores.SingleAsync()).DistributionRound);
    }

    [Fact]
    public async Task Student_read_returns_only_active_published_current_year_programs()
    {
        await using var db = Db(); var refs = SeedReferences(db);
        db.AdmissionPrograms.AddRange(
            new(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, 2026, null, true, Now),
            new(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, AdmissionType.Contract, StudyForm.FullTime, StudyLanguage.Tajik, 2026, null, false, Now),
            new(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, AdmissionType.Budget, StudyForm.PartTime, StudyLanguage.Tajik, 2025, null, true, Now));
        await db.SaveChangesAsync();
        var result = await Programs(db).GetProgramsAsync(new AdmissionProgramFilter(), false, SupportedLanguage.Tajik, default);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task Mmt_pagination_defaults_to_ten_and_clamps_to_fifty()
    {
        await using var db = Db();
        var refs = SeedReferences(db);
        for (var i = 0; i < 55; i++)
            db.AdmissionPrograms.Add(new AdmissionProgram(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id,
                AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, 2026, null, true, Now));
        await db.SaveChangesAsync();

        var defaultPage = await Programs(db).GetProgramsAsync(new AdmissionProgramFilter(), true, SupportedLanguage.Tajik, default);
        var clampedPage = await Programs(db).GetProgramsAsync(new AdmissionProgramFilter(Page: 0, PageSize: 100), true, SupportedLanguage.Tajik, default);

        Assert.Equal(10, defaultPage.Value!.Items.Count);
        Assert.Equal(10, defaultPage.Value.PageSize);
        Assert.Equal(1, clampedPage.Value!.Page);
        Assert.Equal(50, clampedPage.Value.PageSize);
        Assert.Equal(50, clampedPage.Value.Items.Count);
    }

    [Fact]
    public async Task Dashboard_missing_score_count_is_not_limited_to_first_page()
    {
        await using var db = Db();
        var refs = SeedReferences(db);
        for (var i = 0; i < 101; i++)
        {
            var program = new AdmissionProgram(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id,
                AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, 2026, null, true, Now);
            db.AdmissionPrograms.Add(program);
            if (i < 100)
                db.PassingScores.Add(new PassingScoreHistory(Guid.NewGuid(), program.Id, 2025, 250m, null, null, null, Now));
        }
        await db.SaveChangesAsync();

        var stats = await new MmtDashboardService(db, new Clock(), Options.Create(new MmtOptions { CurrentAdmissionYear = 2027 }))
            .GetAsync(default);

        Assert.Equal(101, stats.PublishedProgramsCount);
        Assert.Equal(1, stats.ProgramsMissingLatestScoreCount);
        Assert.Equal(2027, stats.CurrentAdmissionYear);
    }

    [Fact]
    public async Task Student_read_uses_configured_admission_year()
    {
        await using var db = Db(); var refs = SeedReferences(db);
        db.AdmissionPrograms.AddRange(
            new(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, 2027, null, true, Now),
            new(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, AdmissionType.Contract, StudyForm.FullTime, StudyLanguage.Tajik, 2026, null, true, Now));
        await db.SaveChangesAsync();

        var result = await Programs(db, 2027).GetProgramsAsync(new AdmissionProgramFilter(), false, SupportedLanguage.Tajik, default);

        var item = Assert.Single(result.Value!.Items);
        Assert.Equal(2027, item.AdmissionYear);
    }

    [Fact]
    public async Task Program_reads_use_the_explicit_requested_language()
    {
        await using var db = Db();
        var refs = SeedReferences(db);
        refs.University.UpdateTranslation(SupportedLanguage.Russian, "Russian University", "RU", "Russian City", UniversityType.Public, null, true, Now);
        refs.Specialty.UpdateTranslation(SupportedLanguage.Russian, "LAW", "Russian Specialty", null, true, Now);
        refs.Cluster.UpdateTranslation(SupportedLanguage.Russian, "Russian Cluster", null, "C2", true, Now);
        db.AdmissionPrograms.Add(new AdmissionProgram(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id,
            AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Russian, 2026, null, true, Now));
        await db.SaveChangesAsync();

        var result = await Programs(db).GetProgramsAsync(new AdmissionProgramFilter(), false, SupportedLanguage.Russian, default);

        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("Russian University", item.UniversityName);
        Assert.Equal("Russian Specialty", item.SpecialtyName);
        Assert.Equal("Russian Cluster", item.ClusterName);
    }

    [Fact]
    public async Task Import_update_existing_changes_score_explicitly()
    {
        await using var db = Db(); var service = Import(db); var bytes = Workbook(Row());
        await service.ConfirmAsync(new MmtImportConfirmRequestDto { File = File(bytes), CreateMissingReferences = true }, default);
        var updatedBytes = Workbook(Row(score: "301.25"));
        var result = await service.ConfirmAsync(new MmtImportConfirmRequestDto { File = File(updatedBytes), CreateMissingReferences = true, ExistingScoreMode = (int)ExistingScoreMode.UpdateExisting }, default);
        Assert.Equal(1, result.Value!.UpdatedScores);
        Assert.Equal(301.25m, (await db.PassingScores.SingleAsync()).PassingScore);
    }

    [Fact]
    public async Task Import_updates_score_by_program_year_and_distribution_round()
    {
        await using var db = Db(); var service = Import(db);
        await service.ConfirmAsync(new MmtImportConfirmRequestDto { File = File(Workbook(Row(round: "Main"))), CreateMissingReferences = true }, default);
        await service.ConfirmAsync(new MmtImportConfirmRequestDto { File = File(Workbook(Row(score: "240", round: "Repeat"))), CreateMissingReferences = true }, default);
        var result = await service.ConfirmAsync(new MmtImportConfirmRequestDto { File = File(Workbook(Row(score: "235", round: "Repeat"))), CreateMissingReferences = true, ExistingScoreMode = (int)ExistingScoreMode.UpdateExisting }, default);

        Assert.Equal(1, result.Value!.UpdatedScores);
        Assert.Equal(2, await db.PassingScores.CountAsync());
        Assert.Equal(287.50m, (await db.PassingScores.SingleAsync(x => x.DistributionRound == DistributionRound.Main)).PassingScore);
        Assert.Equal(235m, (await db.PassingScores.SingleAsync(x => x.DistributionRound == DistributionRound.Repeat)).PassingScore);
    }

    [Fact]
    public async Task Malformed_workbook_is_rejected_as_validation_error()
    {
        await using var db = Db();
        var result = await Import(db).PreviewAsync(new MmtImportPreviewRequestDto { File = File([1, 2, 3, 4]) }, default);
        Assert.True(result.IsFailure);
        Assert.Contains("file", result.ValidationErrors!.Keys);
    }

    private static MmtDbContext Db() => new(new DbContextOptionsBuilder<MmtDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static AdmissionProgramService Programs(MmtDbContext db, int? year = null) =>
        new(db, new Clock(), Options.Create(new MmtOptions { CurrentAdmissionYear = year }));
    private static MmtCatalogService Catalog(MmtDbContext db) => new(db, new Clock(), new CatalogLookup());
    private static MmtImportService Import(MmtDbContext db) => new(db, new Clock(), new MmtSpreadsheet());
    private static (University University, Specialty Specialty, MmtCluster Cluster) SeedReferences(MmtDbContext db)
    {
        var cluster = new MmtCluster(Guid.NewGuid(), "Cluster 2", "C2", null, Now);
        var university = new University(Guid.NewGuid(), "Tajik National University", "TNU", "Dushanbe", UniversityType.Public, null, Now);
        var specialty = new Specialty(Guid.NewGuid(), "LAW", "Law", null, Now);
        db.AddRange(cluster, university, specialty); return (university, specialty, cluster);
    }
    private static async Task<AdmissionProgram> SeedProgram(MmtDbContext db)
    {
        var refs = SeedReferences(db); var program = new AdmissionProgram(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, 2026, 20, true, Now);
        db.Add(program); await db.SaveChangesAsync(); return program;
    }
    private static IFormFile File(byte[] bytes) => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", "mmt.xlsx") { Headers = new HeaderDictionary(), ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };
    private static string[] Row(string clusterCode = "C2", string score = "287.50", string round = "Main") => ["2026", clusterCode, "Cluster 2", "Tajik National University", "TNU", "Dushanbe", "Public", "LAW", "Law", "Budget", "FullTime", "Tajik", "50", score, round, "MMT", ""];
    private static byte[] Workbook(params string[][] rows)
        => WorkbookWithHeaders(MmtSpreadsheet.Headers, rows);
    private static byte[] WorkbookWithHeaders(string[] headers, params string[][] rows)
    {
        using var stream = new MemoryStream();
        using (var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Create(stream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook, true))
        {
            var wb = document.AddWorkbookPart(); wb.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook(); var ws = wb.AddNewPart<DocumentFormat.OpenXml.Packaging.WorksheetPart>();
            var data = new DocumentFormat.OpenXml.Spreadsheet.SheetData(); data.Append(MakeRow(headers)); foreach (var row in rows) data.Append(MakeRow(row));
            ws.Worksheet = new DocumentFormat.OpenXml.Spreadsheet.Worksheet(data); var sheets = wb.Workbook.AppendChild(new DocumentFormat.OpenXml.Spreadsheet.Sheets()); sheets.Append(new DocumentFormat.OpenXml.Spreadsheet.Sheet { Id = wb.GetIdOfPart(ws), SheetId = 1, Name = "Import" }); wb.Workbook.Save();
        }
        return stream.ToArray();
    }
    private static DocumentFormat.OpenXml.Spreadsheet.Row MakeRow(IEnumerable<string> values) => new(values.Select(v => new DocumentFormat.OpenXml.Spreadsheet.Cell { DataType = DocumentFormat.OpenXml.Spreadsheet.CellValues.InlineString, InlineString = new DocumentFormat.OpenXml.Spreadsheet.InlineString(new DocumentFormat.OpenXml.Spreadsheet.Text(v)) }));
    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
        public DateTimeOffset DushanbeNow => Now.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
    private sealed class CatalogLookup : IAcademicSubjectLookup
    {
        public Task<IReadOnlyList<AcademicSubjectLookupItem>> GetActiveSubjectsAsync(IReadOnlyCollection<Guid> subjectIds, SupportedLanguage language, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AcademicSubjectLookupItem>>(subjectIds.Select(id => new AcademicSubjectLookupItem(id, "SUB", language == SupportedLanguage.Russian ? "Russian Subject" : "Tajik Subject")).ToList());
    }
}
