using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Application.Import;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Mmt.Tests;

public sealed class MmtCatalogImportTests
{
    [Fact]
    public void Final_catalog_fixture_maps_all_rows_and_aggregates()
    {
        var rows = new MmtCatalogSpreadsheet().Parse(new MemoryStream(Workbook(FixtureRows())));

        Assert.Equal(17, rows.Count);
        Assert.All(rows, row => Assert.True(row.IsValid, string.Join(", ", row.ValidationErrors)));
        Assert.Equal(404, rows.Sum(row => row.Values!.SeatsCount));
        Assert.Equal(2, rows.Select(row => row.Values!.SpecialtyCode).Distinct().Count());
        Assert.Equal(8, rows.Select(row => row.Values!.UniversityNameRu).Distinct().Count());
        Assert.Equal(14, rows.Count(row => row.Values!.AdmissionType == (int)AdmissionType.Contract));
        Assert.Equal(3, rows.Count(row => row.Values!.AdmissionType == (int)AdmissionType.Budget));
        Assert.Equal(6, rows.Count(row => row.Values!.StudyLanguage == (int)StudyLanguage.Bilingual));
        Assert.All(rows.Where(row => row.Values!.AdmissionType == (int)AdmissionType.Budget), row => Assert.Null(row.Values!.TuitionFeeTjs));
    }

    [Fact]
    public async Task Confirm_is_russian_only_unpublished_and_idempotent()
    {
        await using var db = Db();
        var cluster = new MmtCluster(Guid.NewGuid(), "Кластери 1", "C1", null, Clock.Now);
        db.Clusters.Add(cluster);
        await db.SaveChangesAsync();
        var service = new MmtCatalogImportService(db, new Clock(), new MmtCatalogSpreadsheet());
        var bytes = Workbook(FixtureRows());
        var request = new MmtCatalogImportRequestDto
        {
            File = File(bytes),
            MmtClusterId = cluster.Id,
            AdmissionYear = 2026,
            DefaultUniversityType = (int)UniversityType.Public
        };

        var first = await service.ConfirmAsync(request, default);
        request = new MmtCatalogImportRequestDto
        {
            File = File(bytes),
            MmtClusterId = cluster.Id,
            AdmissionYear = 2026,
            DefaultUniversityType = (int)UniversityType.Public
        };
        var second = await service.ConfirmAsync(request, default);

        Assert.True(first.IsSuccess);
        Assert.Equal(17, first.Value!.ImportedPrograms);
        Assert.Equal(8, first.Value.CreatedUniversities);
        Assert.Equal(2, first.Value.CreatedSpecialties);
        Assert.All(await db.AdmissionPrograms.Include(x => x.University).Include(x => x.Specialty).ToListAsync(), program =>
        {
            Assert.False(program.IsPublished);
            Assert.True(program.NeedsTranslation);
            Assert.NotEmpty(program.StudyLocationRu);
        });
        Assert.Equal(8, (await db.Universities.ToListAsync()).Select(x => x.NormalizedFullName).Distinct().Count());
        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.Value!.ImportedPrograms);
        Assert.Equal(17, second.Value.SkippedPrograms);
    }

    [Theory]
    [InlineData("Evening", "платный (100)", "русский")]
    [InlineData("дневная", "контракт", "русский")]
    [InlineData("дневная", "платный (100)", "немецкий")]
    public void Parser_rejects_unknown_catalog_values(string form, string admission, string language)
    {
        var row = new[] { "1", "100 - Test", "University", "Dushanbe", form, admission, language, "10" };
        var result = new MmtCatalogSpreadsheet().Parse(new MemoryStream(Workbook([row])));
        Assert.False(result.Single().IsValid);
    }

    [Fact]
    public void Parser_rejects_invalid_headers_specialty_fee_seats_and_duplicate_identity()
    {
        var invalidHeader = MmtCatalogSpreadsheet.Headers.ToArray();
        invalidHeader[0] = "SourceId";
        Assert.Throws<InvalidDataException>(() => new MmtCatalogSpreadsheet().Parse(
            new MemoryStream(Workbook([["1", "100 - Test", "University", "Dushanbe", "дневная", "бесплатный", "русский", "10"]], invalidHeader))));

        var valid = new[] { "1", "100 - Test", "University", "Dushanbe", "дневная", "платный (100)", "русский", "10" };
        var result = new MmtCatalogSpreadsheet().Parse(new MemoryStream(Workbook([
            ["1", "malformed", "University", "Dushanbe", "дневная", "бесплатный", "русский", "10"],
            ["2", "100 - Test", "University", "Dushanbe", "дневная", "платный (-1)", "русский", "10"],
            ["3", "100 - Test", "University", "Dushanbe", "дневная", "бесплатный", "русский", "-2"],
            valid,
            valid,
        ])));

        Assert.All(result.Take(3), row => Assert.False(row.IsValid));
        Assert.True(result[3].IsValid);
        Assert.Contains("Duplicate program identity in file.", result[^1].ValidationErrors);
    }

    private static IReadOnlyList<string[]> FixtureRows()
    {
        int[] seats = [20, 25, 50, 25, 5, 14, 10, 25, 20, 8, 16, 16, 50, 20, 8, 67, 25];
        var rows = new List<string[]>();
        for (var index = 0; index < seats.Length; index++)
        {
            var budget = index is 4 or 9 or 14;
            var language = index is 2 or 9 or 10 or 11 or 14 or 15 ? "таджикский, русский" : index % 4 == 0 ? "русский" : "таджикский";
            var form = index == 13 ? "заочная" : index is 3 or 6 or 8 or 11 or 15 ? "дистанционная" : "дневная";
            var specialty = index < 14 ? "131030408 - WEB-дизайн и компьютерная графика" : "153010403 - Автоматизация и релейная защита электроустановок";
            rows.Add([(index + 1).ToString(), specialty, $"Университет {index % 8 + 1}", $"Место обучения {index + 1}",
                form, budget ? "бесплатный" : $"платный ({2700 + index * 125})", language, seats[index].ToString()]);
        }
        return rows;
    }

    private static byte[] Workbook(IEnumerable<string[]> rows, IReadOnlyList<string>? headers = null)
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbook = document.AddWorkbookPart(); workbook.Workbook = new Workbook();
            var worksheet = workbook.AddNewPart<WorksheetPart>();
            var data = new SheetData(); data.Append(Row(headers ?? MmtCatalogSpreadsheet.Headers));
            foreach (var values in rows) data.Append(Row(values));
            worksheet.Worksheet = new Worksheet(data);
            var sheets = workbook.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet { Id = workbook.GetIdOfPart(worksheet), SheetId = 1, Name = "Programs" });
            workbook.Workbook.Save();
        }
        return stream.ToArray();
    }

    private static Row Row(IEnumerable<string> values) => new(values.Select(value => new Cell
    {
        DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(value))
    }));

    private static FormFile File(byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "File", "mmt_programs.xlsx");
    }

    private static MmtDbContext Db() => new(new DbContextOptionsBuilder<MmtDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class Clock : IDateTimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 7, 17, 0, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
        public DateTimeOffset DushanbeNow => Now.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
