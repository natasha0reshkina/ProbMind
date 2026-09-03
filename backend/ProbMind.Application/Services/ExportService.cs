using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class ExportService : IExportService
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");
    private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeRelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypeNs = "http://schemas.openxmlformats.org/package/2006/content-types";

    private readonly ITeacherService _teacher;
    private readonly IStatisticsService _statistics;

    public ExportService(ITeacherService teacher, IStatisticsService statistics)
    {
        _teacher = teacher;
        _statistics = statistics;
    }

    public async Task<ExportFileDto> StudentProfileXlsxAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        var student = await _teacher.StudentAsync(studentId, ct);
        var rows = new List<IReadOnlyList<string>>();

        foreach (var topic in student.Topics)
        {
            rows.Add(new[]
            {
                "Тема",
                topic.Name,
                Percent(topic.Mastery),
                $"Неопределённость: {Percent(topic.Uncertainty)}"
            });
        }

        foreach (var mc in student.Misconceptions)
        {
            rows.Add(new[]
            {
                "Типичная ошибка",
                mc.Title,
                Percent(mc.Confidence),
                Status(mc.Status)
            });
        }

        return new ExportFileDto(
            $"rezultaty-studenta-{studentId:N}.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            CreateWorkbook(
                "Результаты студента",
                new[] { "Раздел", "Название", "Значение", "Дополнительная информация" },
                rows));
    }

    public async Task<ExportFileDto> CohortMisconceptionsXlsxAsync(CancellationToken ct = default)
    {
        var items = await _statistics.PrevalenceAsync(ct);
        var rows = items.Select(item => (IReadOnlyList<string>)new[]
        {
            item.Title,
            item.StudentsAffected.ToString(Ru),
            item.TotalStudents.ToString(Ru),
            Percent(item.Prevalence),
            Percent(item.MeanConfidence),
            item.EvidenceCount.ToString(Ru)
        }).ToList();

        return new ExportFileDto(
            "tipichnye-oshibki-gruppy.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            CreateWorkbook(
                "Типичные ошибки",
                new[]
                {
                    "Типичная ошибка",
                    "Студентов с ошибкой",
                    "Студентов с результатами",
                    "Доля студентов",
                    "Средняя выраженность",
                    "Количество наблюдений"
                },
                rows));
    }

    public async Task<ExportFileDto> QuestionAnalyticsXlsxAsync(
        Guid? topicId,
        CancellationToken ct = default)
    {
        var items = await _teacher.QuestionAnalyticsAsync(topicId, ct);
        var rows = items.Select(item => (IReadOnlyList<string>)new[]
        {
            QuestionLabel(item.Code),
            item.Responses.ToString(Ru),
            Percent(item.CorrectRate),
            Number(item.Difficulty, 2),
            Number(item.Discrimination, 2),
            Quality(item.QualityBand),
            Number(item.DistractorEntropy, 2),
            Number(item.MedianResponseSeconds, 2)
        }).ToList();

        return new ExportFileDto(
            "statistika-zadaniy.xlsx",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            CreateWorkbook(
                "Статистика заданий",
                new[]
                {
                    "Код задания",
                    "Ответов",
                    "Доля правильных",
                    "Сложность",
                    "Различающая способность",
                    "Оценка качества",
                    "Разнообразие вариантов",
                    "Медианное время ответа, сек."
                },
                rows));
    }

    private static byte[] CreateWorkbook(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var allRows = new List<IReadOnlyList<string>>(rows.Count + 1) { headers };
        allRows.AddRange(rows);

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            WriteEntry(archive, "[Content_Types].xml", ContentTypesXml());
            WriteEntry(archive, "_rels/.rels", RootRelationshipsXml());
            WriteEntry(archive, "xl/workbook.xml", WorkbookXml(sheetName));
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml());
            WriteEntry(archive, "xl/styles.xml", StylesXml());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", WorksheetXml(allRows));
        }

        return stream.ToArray();
    }

    private static string ContentTypesXml() =>
        new XDocument(
            new XElement(ContentTypeNs + "Types",
                new XElement(ContentTypeNs + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(ContentTypeNs + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                new XElement(ContentTypeNs + "Override",
                    new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(ContentTypeNs + "Override",
                    new XAttribute("PartName", "/xl/worksheets/sheet1.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")),
                new XElement(ContentTypeNs + "Override",
                    new XAttribute("PartName", "/xl/styles.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"))))
        .ToString(SaveOptions.DisableFormatting);

    private static string RootRelationshipsXml() =>
        new XDocument(
            new XElement(PackageRelNs + "Relationships",
                new XElement(PackageRelNs + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "xl/workbook.xml"))))
        .ToString(SaveOptions.DisableFormatting);

    private static string WorkbookXml(string sheetName)
    {
        var safeName = SanitizeSheetName(sheetName);
        return new XDocument(
            new XElement(SpreadsheetNs + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", OfficeRelNs),
                new XElement(SpreadsheetNs + "sheets",
                    new XElement(SpreadsheetNs + "sheet",
                        new XAttribute("name", safeName),
                        new XAttribute("sheetId", "1"),
                        new XAttribute(OfficeRelNs + "id", "rId1")))))
            .ToString(SaveOptions.DisableFormatting);
    }

    private static string WorkbookRelationshipsXml() =>
        new XDocument(
            new XElement(PackageRelNs + "Relationships",
                new XElement(PackageRelNs + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                    new XAttribute("Target", "worksheets/sheet1.xml")),
                new XElement(PackageRelNs + "Relationship",
                    new XAttribute("Id", "rId2"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
                    new XAttribute("Target", "styles.xml"))))
        .ToString(SaveOptions.DisableFormatting);

    private static string StylesXml() =>
        new XDocument(
            new XElement(SpreadsheetNs + "styleSheet",
                new XElement(SpreadsheetNs + "fonts",
                    new XAttribute("count", "2"),
                    new XElement(SpreadsheetNs + "font",
                        new XElement(SpreadsheetNs + "sz", new XAttribute("val", "11")),
                        new XElement(SpreadsheetNs + "name", new XAttribute("val", "Calibri"))),
                    new XElement(SpreadsheetNs + "font",
                        new XElement(SpreadsheetNs + "b"),
                        new XElement(SpreadsheetNs + "sz", new XAttribute("val", "11")),
                        new XElement(SpreadsheetNs + "name", new XAttribute("val", "Calibri")))),
                new XElement(SpreadsheetNs + "fills",
                    new XAttribute("count", "2"),
                    new XElement(SpreadsheetNs + "fill",
                        new XElement(SpreadsheetNs + "patternFill", new XAttribute("patternType", "none"))),
                    new XElement(SpreadsheetNs + "fill",
                        new XElement(SpreadsheetNs + "patternFill", new XAttribute("patternType", "gray125")))),
                new XElement(SpreadsheetNs + "borders",
                    new XAttribute("count", "1"),
                    new XElement(SpreadsheetNs + "border",
                        new XElement(SpreadsheetNs + "left"),
                        new XElement(SpreadsheetNs + "right"),
                        new XElement(SpreadsheetNs + "top"),
                        new XElement(SpreadsheetNs + "bottom"),
                        new XElement(SpreadsheetNs + "diagonal"))),
                new XElement(SpreadsheetNs + "cellStyleXfs",
                    new XAttribute("count", "1"),
                    new XElement(SpreadsheetNs + "xf",
                        new XAttribute("numFmtId", "0"),
                        new XAttribute("fontId", "0"),
                        new XAttribute("fillId", "0"),
                        new XAttribute("borderId", "0"))),
                new XElement(SpreadsheetNs + "cellXfs",
                    new XAttribute("count", "2"),
                    new XElement(SpreadsheetNs + "xf",
                        new XAttribute("numFmtId", "0"),
                        new XAttribute("fontId", "0"),
                        new XAttribute("fillId", "0"),
                        new XAttribute("borderId", "0"),
                        new XAttribute("xfId", "0"),
                        new XAttribute("applyAlignment", "1"),
                        new XElement(SpreadsheetNs + "alignment",
                            new XAttribute("vertical", "top"),
                            new XAttribute("wrapText", "1"))),
                    new XElement(SpreadsheetNs + "xf",
                        new XAttribute("numFmtId", "0"),
                        new XAttribute("fontId", "1"),
                        new XAttribute("fillId", "0"),
                        new XAttribute("borderId", "0"),
                        new XAttribute("xfId", "0"),
                        new XAttribute("applyAlignment", "1"),
                        new XElement(SpreadsheetNs + "alignment",
                            new XAttribute("vertical", "center"),
                            new XAttribute("wrapText", "1")))),
                new XElement(SpreadsheetNs + "cellStyles",
                    new XAttribute("count", "1"),
                    new XElement(SpreadsheetNs + "cellStyle",
                        new XAttribute("name", "Normal"),
                        new XAttribute("xfId", "0"),
                        new XAttribute("builtinId", "0")))))
        .ToString(SaveOptions.DisableFormatting);

    private static string WorksheetXml(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var columnCount = rows.Count == 0 ? 1 : rows.Max(x => x.Count);
        var widths = new double[columnCount];
        for (var column = 0; column < columnCount; column++)
        {
            var maxLength = rows
                .Select(row => column < row.Count ? row[column]?.Length ?? 0 : 0)
                .DefaultIfEmpty(0)
                .Max();
            widths[column] = Math.Clamp(maxLength + 3d, 12d, 55d);
        }

        var sheetData = new XElement(SpreadsheetNs + "sheetData");
        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var rowElement = new XElement(SpreadsheetNs + "row",
                new XAttribute("r", rowIndex + 1),
                new XAttribute("ht", rowIndex == 0 ? "24" : "20"),
                new XAttribute("customHeight", "1"));

            for (var columnIndex = 0; columnIndex < rows[rowIndex].Count; columnIndex++)
            {
                var value = rows[rowIndex][columnIndex] ?? string.Empty;
                rowElement.Add(new XElement(SpreadsheetNs + "c",
                    new XAttribute("r", CellReference(columnIndex + 1, rowIndex + 1)),
                    new XAttribute("t", "inlineStr"),
                    new XAttribute("s", rowIndex == 0 ? "1" : "0"),
                    new XElement(SpreadsheetNs + "is",
                        new XElement(SpreadsheetNs + "t",
                            new XAttribute(XNamespace.Xml + "space", "preserve"),
                            value))));
            }

            sheetData.Add(rowElement);
        }

        var lastCell = CellReference(columnCount, Math.Max(1, rows.Count));
        return new XDocument(
            new XElement(SpreadsheetNs + "worksheet",
                new XElement(SpreadsheetNs + "dimension", new XAttribute("ref", $"A1:{lastCell}")),
                new XElement(SpreadsheetNs + "sheetViews",
                    new XElement(SpreadsheetNs + "sheetView",
                        new XAttribute("workbookViewId", "0"),
                        new XElement(SpreadsheetNs + "pane",
                            new XAttribute("ySplit", "1"),
                            new XAttribute("topLeftCell", "A2"),
                            new XAttribute("activePane", "bottomLeft"),
                            new XAttribute("state", "frozen")))),
                new XElement(SpreadsheetNs + "sheetFormatPr", new XAttribute("defaultRowHeight", "20")),
                new XElement(SpreadsheetNs + "cols",
                    widths.Select((width, index) =>
                        new XElement(SpreadsheetNs + "col",
                            new XAttribute("min", index + 1),
                            new XAttribute("max", index + 1),
                            new XAttribute("width", width.ToString("0.##", CultureInfo.InvariantCulture)),
                            new XAttribute("customWidth", "1")))),
                sheetData,
                new XElement(SpreadsheetNs + "autoFilter",
                    new XAttribute("ref", $"A1:{CellReference(columnCount, 1)}"))))
            .ToString(SaveOptions.DisableFormatting);
    }

    private static string CellReference(int column, int row)
    {
        var name = string.Empty;
        var value = column;
        while (value > 0)
        {
            value--;
            name = (char)('A' + value % 26) + name;
            value /= 26;
        }
        return name + row;
    }

    private static string SanitizeSheetName(string value)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var safe = new string(value.Select(ch => invalid.Contains(ch) ? ' ' : ch).ToArray()).Trim();
        if (safe.Length == 0)
            safe = "Данные";
        return safe.Length <= 31 ? safe : safe[..31];
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new System.Text.UTF8Encoding(false));
        writer.Write(content);
    }


    private static string QuestionLabel(string code)
    {
        var lastUnderscore = code.LastIndexOf('_');
        if (lastUnderscore <= 0 || lastUnderscore == code.Length - 1)
            return "Учебное задание";

        var prefix = code[..lastUnderscore];
        var number = int.TryParse(code[(lastUnderscore + 1)..], out var parsed)
            ? parsed.ToString(Ru)
            : code[(lastUnderscore + 1)..];

        var topic = prefix switch
        {
            "conditional_probability" => "Условная вероятность",
            "independence" => "Независимость",
            "p_value" => "p-value",
            "law_large_numbers" => "Закон больших чисел",
            "randomness" => "Случайность",
            _ => "Учебное задание"
        };

        return $"{topic} · №{number}";
    }

    private static string Percent(double value) => value.ToString("P1", Ru);

    private static string Number(double value, int digits) => value.ToString($"F{digits}", Ru);

    private static string Status(MisconceptionStatus status) => status switch
    {
        MisconceptionStatus.Unknown => "Нет данных",
        MisconceptionStatus.Suspected => "Есть признаки",
        MisconceptionStatus.Detected => "Выявлено",
        MisconceptionStatus.CorrectionInProgress => "Идёт повторная работа",
        MisconceptionStatus.Corrected => "Исправлено",
        MisconceptionStatus.RecheckRequired => "Нужна повторная проверка",
        _ => "Нет данных"
    };

    private static string Quality(string value) => value switch
    {
        "excellent" => "Отличное",
        "good" => "Хорошее",
        "acceptable" => "Допустимое",
        "review" => "Требует проверки",
        "no_data" => "Нет данных",
        "insufficient_sample" => "Мало данных",
        "questionable" => "Сомнительное",
        "poor" => "Низкое",
        _ => "Не определено"
    };
}
