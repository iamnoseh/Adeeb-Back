using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Application;
using Adeeb.Modules.Mmt.Application.Import;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Mmt.Tests;

public sealed class MmtModuleTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Duplicate_cluster_code_is_rejected_after_normalization()
    {
        await using var db = Db(); var service = new MmtCatalogService(db, new Clock());
        Assert.True((await service.CreateClusterAsync(new("Cluster 2", " c2 ", null), default)).IsSuccess);
        var duplicate = await service.CreateClusterAsync(new("Other", "C2", null), default);
        Assert.Equal(MmtErrors.DuplicateCluster.Code, duplicate.Error?.Code);
    }

    [Fact]
    public async Task Duplicate_specialty_code_is_rejected_after_normalization()
    {
        await using var db = Db(); var service = new MmtCatalogService(db, new Clock());
        Assert.True((await service.CreateSpecialtyAsync(new("law", "Law", null), default)).IsSuccess);
        var duplicate = await service.CreateSpecialtyAsync(new(" LAW ", "Other", null), default);
        Assert.Equal(MmtErrors.DuplicateSpecialty.Code, duplicate.Error?.Code);
    }

    [Fact]
    public async Task Duplicate_admission_program_is_rejected()
    {
        await using var db = Db(); var refs = SeedReferences(db); await db.SaveChangesAsync();
        var service = new AdmissionProgramService(db, new Clock());
        var request = new CreateAdmissionProgramDto(refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, 0, 0, 0, 2026, 20, false);
        Assert.True((await service.CreateProgramAsync(request, default)).IsSuccess);
        Assert.Equal(MmtErrors.DuplicateProgram.Code, (await service.CreateProgramAsync(request, default)).Error?.Code);
    }

    [Fact]
    public async Task Duplicate_score_for_program_and_year_is_rejected()
    {
        await using var db = Db(); var program = await SeedProgram(db); var service = new AdmissionProgramService(db, new Clock());
        Assert.True((await service.AddScoreAsync(program.Id, new(2025, 250, null, null, null), default)).IsSuccess);
        Assert.Equal(MmtErrors.DuplicateScore.Code, (await service.AddScoreAsync(program.Id, new(2025, 260, null, null, null), default)).Error?.Code);
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
    public async Task Student_read_returns_only_active_published_current_year_programs()
    {
        await using var db = Db(); var refs = SeedReferences(db);
        db.AdmissionPrograms.AddRange(
            new(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, 2026, null, true, Now),
            new(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, AdmissionType.Contract, StudyForm.FullTime, StudyLanguage.Tajik, 2026, null, false, Now),
            new(Guid.NewGuid(), refs.University.Id, refs.Specialty.Id, refs.Cluster.Id, AdmissionType.Budget, StudyForm.PartTime, StudyLanguage.Tajik, 2025, null, true, Now));
        await db.SaveChangesAsync();
        var result = await new AdmissionProgramService(db, new Clock()).GetProgramsAsync(new AdmissionProgramFilter(), false, default);
        Assert.Single(result.Value!.Items);
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
    public async Task Malformed_workbook_is_rejected_as_validation_error()
    {
        await using var db = Db();
        var result = await Import(db).PreviewAsync(new MmtImportPreviewRequestDto { File = File([1, 2, 3, 4]) }, default);
        Assert.True(result.IsFailure);
        Assert.Contains("file", result.ValidationErrors!.Keys);
    }

    private static MmtDbContext Db() => new(new DbContextOptionsBuilder<MmtDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
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
    private static string[] Row(string clusterCode = "C2", string score = "287.50") => ["2026", clusterCode, "Cluster 2", "Tajik National University", "TNU", "Dushanbe", "Public", "LAW", "Law", "Budget", "FullTime", "Tajik", "50", score, "MMT", ""];
    private static byte[] Workbook(params string[][] rows)
    {
        using var stream = new MemoryStream();
        using (var document = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Create(stream, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook, true))
        {
            var wb = document.AddWorkbookPart(); wb.Workbook = new DocumentFormat.OpenXml.Spreadsheet.Workbook(); var ws = wb.AddNewPart<DocumentFormat.OpenXml.Packaging.WorksheetPart>();
            var data = new DocumentFormat.OpenXml.Spreadsheet.SheetData(); data.Append(MakeRow(MmtSpreadsheet.Headers)); foreach (var row in rows) data.Append(MakeRow(row));
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
}
