using System.Globalization;
using System.Text.RegularExpressions;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Adeeb.Modules.Mmt.Application.Import;

public sealed partial class MmtCatalogSpreadsheet
{
    public const int MaxRows = 5000;
    public static readonly string[] Headers =
    [
        "ID", "Specialty", "UniversityNameRu", "StudyLocationRu", "StudyFormRu",
        "AdmissionTypeRu", "StudyLanguageRu", "SeatsCount"
    ];

    public byte[] CreateTemplate()
    {
        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var data = new SheetData();
            data.Append(new Row(Headers.Select(Cell)));
            data.Append(new Row(new[]
            {
                "1", "131030408 - WEB-дизайн и компьютерная графика", "Таджикский государственный университет коммерции",
                "Душанбе", "дневная", "платный (6950)", "таджикский, русский", "20"
            }.Select(Cell)));
            sheetPart.Worksheet = new Worksheet(data);
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(sheetPart), SheetId = 1, Name = "Programs" });
            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    public IReadOnlyList<MmtCatalogImportRowPreviewDto> Parse(Stream stream)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbook = document.WorkbookPart ?? throw new InvalidDataException("Workbook is missing.");
        var sheet = workbook.Workbook.Sheets?.Elements<Sheet>()
            .SingleOrDefault(x => string.Equals(x.Name?.Value, "Programs", StringComparison.Ordinal))
            ?? throw new InvalidDataException("Worksheet 'Programs' is required.");
        var part = (WorksheetPart)workbook.GetPartById(sheet.Id!);
        var rows = part.Worksheet.GetFirstChild<SheetData>()?.Elements<Row>().ToList() ?? [];
        if (rows.Count == 0) throw new InvalidDataException("Header row is missing.");
        var actualHeaders = CellsByColumn(rows[0], workbook);
        if (actualHeaders.Count != Headers.Length || Headers.Where((header, index) =>
                !string.Equals(actualHeaders.GetValueOrDefault(index + 1), header, StringComparison.Ordinal)).Any())
            throw new InvalidDataException("Headers must exactly match: " + string.Join(", ", Headers));
        if (rows.Count - 1 > MaxRows) throw new InvalidDataException($"At most {MaxRows} data rows are allowed.");

        var result = new List<MmtCatalogImportRowPreviewDto>();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows.Skip(1))
        {
            var values = CellsByColumn(row, workbook);
            string V(int column) => values.GetValueOrDefault(column, string.Empty).Trim();
            if (Enumerable.Range(1, Headers.Length).All(column => string.IsNullOrWhiteSpace(V(column)))) continue;

            var errors = new List<string>();
            var sourceId = Required(V(1), "ID", errors);
            var specialty = ParseSpecialty(V(2), errors);
            var university = NormalizeRequired(V(3), "UniversityNameRu", 300, errors);
            var location = NormalizeRequired(V(4), "StudyLocationRu", 160, errors);
            var form = ParseStudyForm(V(5), errors);
            var admission = ParseAdmissionType(V(6), errors);
            var language = ParseStudyLanguage(V(7), errors);
            var seats = ParseSeats(V(8), errors);
            MmtCatalogImportNormalizedRowDto? normalized = null;
            if (errors.Count == 0 && specialty is not null)
            {
                normalized = new(sourceId, specialty.Value.Code, specialty.Value.Name, university, location,
                    (int)form.Form, (int)admission.Type, (int)language, seats, admission.Fee);
                var key = string.Join('|', MmtNormalization.NameKey(university), specialty.Value.Code,
                    MmtNormalization.NameKey(location), (int)admission.Type, (int)form.Form, (int)language);
                if (!identities.Add(key)) errors.Add("Duplicate program identity in file.");
            }

            result.Add(new((int)(row.RowIndex?.Value ?? (uint)(result.Count + 2)), normalized,
                errors.Count == 0, false, true, errors, []));
        }

        return result;
    }

    private static (string Code, string Name)? ParseSpecialty(string value, List<string> errors)
    {
        var separator = value.IndexOf(" - ", StringComparison.Ordinal);
        if (separator <= 0 || separator >= value.Length - 3)
        {
            errors.Add("Specialty must use the format 'code - name'.");
            return null;
        }

        var code = MmtNormalization.Code(value[..separator]);
        var name = MmtNormalization.Name(value[(separator + 3)..]);
        if (code.Length > 60 || name.Length > 240) errors.Add("Specialty code or name is too long.");
        return (code, name);
    }

    private static (AdmissionType Type, decimal? Fee) ParseAdmissionType(string value, List<string> errors)
    {
        var normalized = NormalizeToken(value);
        if (normalized == "бесплатный") return (AdmissionType.Budget, null);
        var match = PaidRegex().Match(normalized);
        if (!match.Success)
        {
            errors.Add("AdmissionTypeRu must be 'бесплатный' or 'платный (amount)'.");
            return (AdmissionType.Contract, null);
        }

        var feeText = match.Groups[1].Value.Replace(',', '.');
        if (!decimal.TryParse(feeText, NumberStyles.Number, CultureInfo.InvariantCulture, out var fee) || fee < 0 || MmtValidation.Scale(fee) > 2)
        {
            errors.Add("Tuition fee is invalid.");
            return (AdmissionType.Contract, null);
        }
        return (AdmissionType.Contract, fee);
    }

    private static (StudyForm Form, string Value) ParseStudyForm(string value, List<string> errors)
    {
        var normalized = NormalizeToken(value);
        return normalized switch
        {
            "дневная" => (StudyForm.FullTime, normalized),
            "заочная" => (StudyForm.PartTime, normalized),
            "дистанционная" => (StudyForm.Distance, normalized),
            _ => InvalidForm(normalized, errors)
        };
    }

    private static StudyLanguage ParseStudyLanguage(string value, List<string> errors)
    {
        var parts = NormalizeToken(value).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1 && parts[0] == "таджикский") return StudyLanguage.Tajik;
        if (parts.Length == 1 && parts[0] == "русский") return StudyLanguage.Russian;
        if (parts.Length == 2 && parts.Order(StringComparer.Ordinal).SequenceEqual(new[] { "русский", "таджикский" })) return StudyLanguage.Bilingual;
        errors.Add("StudyLanguageRu must be 'таджикский', 'русский', or 'таджикский, русский'.");
        return StudyLanguage.Other;
    }

    private static int ParseSeats(string value, List<string> errors)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seats) && seats >= 0) return seats;
        errors.Add("SeatsCount must be a non-negative integer.");
        return 0;
    }

    private static string NormalizeRequired(string value, string name, int maxLength, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add($"{name} is required.");
        var normalized = MmtNormalization.Name(value);
        if (normalized.Length > maxLength) errors.Add($"{name} is too long.");
        return normalized;
    }

    private static string Required(string value, string name, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add($"{name} is required.");
        return value;
    }

    private static (StudyForm Form, string Value) InvalidForm(string value, List<string> errors)
    {
        errors.Add("StudyFormRu must be 'дневная', 'заочная', or 'дистанционная'.");
        return (StudyForm.Other, value);
    }

    private static string NormalizeToken(string value) => MmtNormalization.Name(value).ToLowerInvariant().Replace('ё', 'е');

    private static Dictionary<int, string> CellsByColumn(Row row, WorkbookPart workbook)
    {
        var result = new Dictionary<int, string>();
        var position = 1;
        foreach (var cell in row.Elements<Cell>())
        {
            var column = cell.CellReference is null ? position : CellColumn(cell);
            result[column] = CellValue(cell, workbook);
            position = column + 1;
        }
        return result;
    }

    private static int CellColumn(Cell cell)
    {
        var result = 0;
        foreach (var ch in (cell.CellReference?.Value ?? "A1").TakeWhile(char.IsLetter))
            result = result * 26 + char.ToUpperInvariant(ch) - 'A' + 1;
        return result;
    }

    private static string CellValue(Cell cell, WorkbookPart workbook)
    {
        if (cell.DataType?.Value == CellValues.InlineString) return cell.InlineString?.Text?.Text ?? cell.InnerText;
        var value = cell.CellValue?.Text ?? string.Empty;
        if (cell.DataType?.Value == CellValues.SharedString && int.TryParse(value, out var index))
            return workbook.SharedStringTablePart?.SharedStringTable?.Elements<SharedStringItem>().ElementAtOrDefault(index)?.InnerText ?? string.Empty;
        return value;
    }

    private static Cell Cell(string value) => new()
    {
        DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(value))
    };

    [GeneratedRegex(@"^платный\s*\(\s*([0-9]+(?:[.,][0-9]{1,2})?)\s*\)$", RegexOptions.CultureInvariant)]
    private static partial Regex PaidRegex();
}
