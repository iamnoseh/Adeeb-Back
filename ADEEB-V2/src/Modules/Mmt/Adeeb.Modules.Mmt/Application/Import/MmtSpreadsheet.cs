using System.Globalization;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Adeeb.Modules.Mmt.Application.Import;

public sealed class MmtSpreadsheet
{
    public const int MaxRows = 5000;
    public static readonly string[] Headers = ["Year", "ClusterCode", "ClusterName", "UniversityFullName", "UniversityShortName", "UniversityCity", "UniversityType", "SpecialtyCode", "SpecialtyName", "AdmissionType", "StudyForm", "StudyLanguage", "SeatsCount", "PassingScore", "Source", "Note"];

    public byte[] CreateTemplate()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart(); workbookPart.Workbook = new Workbook();
            var sheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var data = new SheetData();
            data.Append(new Row(Headers.Select(HeaderCell)));
            data.Append(new Row(new[] { "2026", "C2", "Cluster 2", "Tajik National University", "TNU", "Dushanbe", "Public", "LAW", "Law", "Budget", "FullTime", "Tajik", "50", "287.50", "MMT 2025", "Example row" }.Select(TextCell)));
            sheetPart.Worksheet = new Worksheet(data);
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(sheetPart), SheetId = 1, Name = "MMT Import" });
            workbookPart.Workbook.Save();
        }
        return stream.ToArray();
    }

    public IReadOnlyList<MmtImportRowPreviewDto> Parse(Stream stream, int? defaultYear)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbook = document.WorkbookPart ?? throw new InvalidDataException("Workbook is missing.");
        var sheet = workbook.Workbook.Sheets?.Elements<Sheet>().FirstOrDefault() ?? throw new InvalidDataException("Worksheet is missing.");
        var part = (WorksheetPart)workbook.GetPartById(sheet.Id!);
        var rows = part.Worksheet.GetFirstChild<SheetData>()?.Elements<Row>().ToList() ?? [];
        if (rows.Count == 0) throw new InvalidDataException("Header row is missing.");
        var headerMap = CellsByColumn(rows[0], workbook).ToDictionary(x => x.Value.Trim(), x => x.Key, StringComparer.OrdinalIgnoreCase);
        var missing = Headers.Where(x => !headerMap.ContainsKey(x)).ToArray();
        if (missing.Length > 0) throw new InvalidDataException("Missing columns: " + string.Join(", ", missing));
        if (rows.Count - 1 > MaxRows) throw new InvalidDataException($"At most {MaxRows} data rows are allowed.");

        var previews = new List<MmtImportRowPreviewDto>();
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.Skip(1))
        {
            var cells = CellsByColumn(row, workbook);
            string V(string name) => cells.GetValueOrDefault(headerMap[name], string.Empty).Trim();
            if (Headers.All(x => string.IsNullOrWhiteSpace(V(x)))) continue;
            var errors = new List<string>();
            var year = ParseInt(V("Year"), defaultYear, "Year", errors);
            var clusterCode = Required(V("ClusterCode"), "ClusterCode", errors);
            var clusterName = Required(V("ClusterName"), "ClusterName", errors);
            var universityName = Required(V("UniversityFullName"), "UniversityFullName", errors);
            var universityCity = Required(V("UniversityCity"), "UniversityCity", errors);
            var specialtyCode = Required(V("SpecialtyCode"), "SpecialtyCode", errors);
            var specialtyName = Required(V("SpecialtyName"), "SpecialtyName", errors);
            var universityType = ParseEnum<UniversityType>(V("UniversityType"), "UniversityType", errors);
            var admissionType = ParseEnum<AdmissionType>(V("AdmissionType"), "AdmissionType", errors);
            var form = ParseEnum<StudyForm>(V("StudyForm"), "StudyForm", errors);
            var language = ParseEnum<StudyLanguage>(V("StudyLanguage"), "StudyLanguage", errors);
            var seats = ParseNullableInt(V("SeatsCount"), "SeatsCount", errors);
            var score = ParseDecimal(V("PassingScore"), "PassingScore", errors);
            if (year is < 2000 or > 2100) errors.Add("Year must be between 2000 and 2100.");
            if (seats < 0) errors.Add("SeatsCount cannot be negative.");
            if (score <= 0 || score > 1000 || MmtValidation.Scale(score) > 2) errors.Add("PassingScore must be positive, at most 1000, and have at most two decimal places.");
            MmtImportNormalizedRowDto? values = null;
            var duplicate = false;
            if (errors.Count == 0)
            {
                values = new(year, MmtNormalization.Code(clusterCode), MmtNormalization.Name(clusterName), MmtNormalization.Name(universityName), Null(V("UniversityShortName")), MmtNormalization.Name(universityCity), universityType,
                    MmtNormalization.Code(specialtyCode), MmtNormalization.Name(specialtyName), admissionType, form, language, seats, score, Null(V("Source")), Null(V("Note")));
                var key = $"{year}|{values.ClusterCode}|{MmtNormalization.NameKey(values.UniversityFullName)}|{values.SpecialtyCode}|{admissionType}|{form}|{language}";
                duplicate = !keys.Add(key);
                if (duplicate) errors.Add("Duplicate row in file.");
            }
            previews.Add(new((int)(row.RowIndex?.Value ?? (uint)(previews.Count + 2)), values, errors.Count == 0, duplicate, errors));
        }
        return previews;
    }

    private static Dictionary<int, string> CellsByColumn(Row row, WorkbookPart workbook)
    {
        var values = new Dictionary<int, string>();
        var position = 1;
        foreach (var cell in row.Elements<Cell>())
        {
            var column = cell.CellReference is null ? position : CellColumn(cell);
            values[column] = CellValue(cell, workbook);
            position = column + 1;
        }
        return values;
    }
    private static int CellColumn(Cell cell)
    {
        var reference = cell.CellReference?.Value ?? "A1"; var value = 0;
        foreach (var ch in reference.TakeWhile(char.IsLetter)) value = value * 26 + char.ToUpperInvariant(ch) - 'A' + 1;
        return value;
    }
    private static string CellValue(Cell cell, WorkbookPart workbook)
    {
        if (cell.DataType?.Value == CellValues.InlineString) return cell.InlineString?.Text?.Text ?? cell.InnerText;
        var value = cell.CellValue?.Text ?? string.Empty;
        if (cell.DataType?.Value == CellValues.SharedString && int.TryParse(value, out var index)) return workbook.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>().ElementAtOrDefault(index)?.InnerText ?? string.Empty;
        return value;
    }
    private static Cell HeaderCell(string value) => new() { DataType = CellValues.InlineString, InlineString = new InlineString(new Text(value)) };
    private static Cell TextCell(string value) => HeaderCell(value);
    private static string Required(string value, string name, List<string> errors) { if (string.IsNullOrWhiteSpace(value)) errors.Add($"{name} is required."); return value; }
    private static int ParseInt(string value, int? fallback, string name, List<string> errors) { if (string.IsNullOrWhiteSpace(value) && fallback.HasValue) return fallback.Value; if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return parsed; errors.Add($"{name} must be an integer."); return 0; }
    private static int? ParseNullableInt(string value, string name, List<string> errors) { if (string.IsNullOrWhiteSpace(value)) return null; if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return parsed; errors.Add($"{name} must be an integer."); return null; }
    private static decimal ParseDecimal(string value, string name, List<string> errors) { if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)) return parsed; errors.Add($"{name} must be numeric."); return 0; }
    private static int ParseEnum<T>(string value, string name, List<string> errors) where T : struct, Enum { if (Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed)) return Convert.ToInt32(parsed, CultureInfo.InvariantCulture); errors.Add($"{name} is invalid."); return 0; }
    private static string? Null(string value) => string.IsNullOrWhiteSpace(value) ? null : MmtNormalization.Name(value);
}
