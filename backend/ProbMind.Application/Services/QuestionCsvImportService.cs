using System.Text.Json;
using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class QuestionCsvImportService : IQuestionCsvImportService
{
    private static readonly char[] OptionLetters = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public QuestionCsvImportService(IUnitOfWork uow, IClock clock)
    {
        _uow = uow;
        _clock = clock;
    }

    public async Task<QuestionCsvImportResultDto> ImportAsync(
        Guid actorId,
        string csv,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(csv))
            throw new InvalidOperationException("CSV-файл пуст.");

        var rows = Parse(csv);
        if (rows.Count < 2)
            throw new InvalidOperationException("В CSV должен быть заголовок и хотя бы одна задача.");

        var headers = rows[0]
            .Select((value, index) => new { Name = value.Trim().TrimStart('\uFEFF').ToLowerInvariant(), Index = index })
            .Where(x => x.Name.Length > 0)
            .ToDictionary(x => x.Name, x => x.Index, StringComparer.OrdinalIgnoreCase);

        string Read(IReadOnlyList<string> row, string column, bool required = false)
        {
            if (!headers.TryGetValue(column, out var index) || index >= row.Count)
            {
                if (required) throw new InvalidOperationException($"В CSV отсутствует обязательная колонка '{column}'.");
                return string.Empty;
            }

            var value = row[index].Trim();
            if (required && value.Length == 0)
                throw new InvalidOperationException($"Обязательная колонка '{column}' содержит пустое значение.");
            return value;
        }

        foreach (var required in new[] { "code", "topic", "difficulty", "prompt", "option_a", "option_b", "correct_option", "explanation" })
        {
            if (!headers.ContainsKey(required))
                throw new InvalidOperationException($"В CSV отсутствует обязательная колонка '{required}'.");
        }

        var topics = await _uow.Topics.ListAsync(ct);
        var topicLookup = topics
            .SelectMany(topic => new[]
            {
                new KeyValuePair<string, Topic>(topic.Code, topic),
                new KeyValuePair<string, Topic>(topic.NameRu, topic),
                new KeyValuePair<string, Topic>(topic.NameEn, topic)
            })
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().Value, StringComparer.OrdinalIgnoreCase);

        var existingCodes = (await _uow.Questions.ListAsync(ct))
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var parsed = new List<ParsedQuestion>();
        var incomingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace))
                continue;

            try
            {
                var code = Read(row, "code", true);
                if (code.Length > 120)
                    throw new InvalidOperationException("Код задачи длиннее 120 символов.");
                if (existingCodes.Contains(code) || !incomingCodes.Add(code))
                    throw new InvalidOperationException($"Код '{code}' уже существует.");

                var topicValue = Read(row, "topic", true);
                if (!topicLookup.TryGetValue(topicValue, out var topic))
                    throw new InvalidOperationException($"Неизвестная тема '{topicValue}'.");

                var difficulty = ParseDifficulty(Read(row, "difficulty", true));
                var prompt = Read(row, "prompt", true);
                var explanation = Read(row, "explanation", true);
                if (prompt.Length > 4000)
                    throw new InvalidOperationException("Условие задачи длиннее 4000 символов.");
                if (explanation.Length > 6000)
                    throw new InvalidOperationException("Объяснение длиннее 6000 символов.");

                var options = new List<ParsedOption>();
                var optionGap = false;
                foreach (var letter in OptionLetters)
                {
                    var text = Read(row, $"option_{letter}");
                    if (text.Length == 0)
                    {
                        optionGap = true;
                        continue;
                    }
                    if (optionGap)
                        throw new InvalidOperationException("Варианты ответа должны заполняться подряд: A, B, C и далее без пропусков.");
                    if (text.Length > 1200)
                        throw new InvalidOperationException($"Вариант {char.ToUpperInvariant(letter)} длиннее 1200 символов.");
                    options.Add(new ParsedOption(
                        letter,
                        text,
                        Read(row, $"feedback_{letter}")));
                }

                if (options.Count < 2)
                    throw new InvalidOperationException("У задачи должно быть минимум два варианта ответа.");

                var correctRaw = Read(row, "correct_option", true).Trim().ToLowerInvariant();
                if (correctRaw.Length != 1 || !OptionLetters.Contains(correctRaw[0]))
                    throw new InvalidOperationException("correct_option должен содержать букву A-H.");
                if (options.All(x => x.Letter != correctRaw[0]))
                    throw new InvalidOperationException("Правильный вариант отсутствует среди заполненных option_* колонок.");

                parsed.Add(new ParsedQuestion(
                    code,
                    topic.Id,
                    prompt,
                    explanation,
                    difficulty,
                    options,
                    correctRaw[0]));
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException($"Строка {rowIndex + 1}: {ex.Message}");
            }
        }

        if (parsed.Count == 0)
            throw new InvalidOperationException("В CSV нет задач для импорта.");

        var importedIds = new List<Guid>();
        var importedCodes = new List<string>();

        foreach (var item in parsed)
        {
            var question = new Question
            {
                TopicId = item.TopicId,
                Code = item.Code,
                Kind = QuestionKind.Diagnostic,
                Status = ContentStatus.Published,
                CurrentVersionNumber = 1,
                PublishedAt = _clock.UtcNow
            };
            var version = new QuestionVersion
            {
                QuestionId = question.Id,
                VersionNumber = 1,
                Prompt = item.Prompt,
                CorrectExplanation = item.Explanation,
                Difficulty = item.Difficulty,
                IsTransferQuestion = item.Difficulty == QuestionDifficulty.Transfer,
                EstimatedSeconds = item.Difficulty is QuestionDifficulty.Advanced or QuestionDifficulty.Transfer ? 150 : 90,
                AuthorNotes = "Импортировано из CSV."
            };

            await _uow.Questions.AddAsync(question, ct);
            await _uow.QuestionVersions.AddAsync(version, ct);

            var sortOrder = 0;
            foreach (var option in item.Options)
            {
                sortOrder++;
                var isCorrect = option.Letter == item.CorrectLetter;
                await _uow.AnswerOptions.AddAsync(new AnswerOption
                {
                    QuestionVersionId = version.Id,
                    Text = option.Text,
                    IsCorrect = isCorrect,
                    Feedback = option.Feedback.Length > 0
                        ? option.Feedback
                        : isCorrect
                            ? "Верно."
                            : "Ответ неверный. Сверьтесь с объяснением после ответа.",
                    SortOrder = sortOrder
                }, ct);
            }

            await _uow.AuditLogs.AddAsync(new AuditLog
            {
                ActorUserId = actorId,
                Action = AuditAction.Created,
                EntityType = nameof(Question),
                EntityId = question.Id,
                OldValueJson = "{}",
                NewValueJson = JsonSerializer.Serialize(new { source = "csv", question.Code }),
                RequestId = Guid.NewGuid().ToString("N")
            }, ct);

            importedIds.Add(question.Id);
            importedCodes.Add(question.Code);
        }

        await _uow.SaveChangesAsync(ct);
        return new QuestionCsvImportResultDto(importedIds.Count, importedIds, importedCodes);
    }

    private static QuestionDifficulty ParseDifficulty(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "introductory" or "intro" or "вводная" => QuestionDifficulty.Introductory,
            "basic" or "базовая" or "легкая" or "лёгкая" => QuestionDifficulty.Basic,
            "intermediate" or "medium" or "средняя" => QuestionDifficulty.Intermediate,
            "advanced" or "hard" or "сложная" => QuestionDifficulty.Advanced,
            "transfer" or "перенос" => QuestionDifficulty.Transfer,
            _ => throw new InvalidOperationException($"Неизвестная сложность '{value}'. Используйте Introductory, Basic, Intermediate, Advanced или Transfer.")
        };
    }

    private static IReadOnlyList<IReadOnlyList<string>> Parse(string csv)
    {
        var headerEnd = csv.IndexOfAny(['\r', '\n']);
        var header = headerEnd >= 0 ? csv[..headerEnd] : csv;
        var delimiter = header.Count(x => x == ';') > header.Count(x => x == ',') ? ';' : ',';
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var field = new System.Text.StringBuilder();
        var quoted = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var ch = csv[i];
            if (quoted)
            {
                if (ch == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                    }
                }
                else
                {
                    field.Append(ch);
                }
                continue;
            }

            if (ch == '"')
            {
                quoted = true;
            }
            else if (ch == delimiter)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if (ch == '\n')
            {
                row.Add(field.ToString().TrimEnd('\r'));
                field.Clear();
                rows.Add(row);
                row = new List<string>();
            }
            else
            {
                field.Append(ch);
            }
        }

        if (quoted)
            throw new InvalidOperationException("В CSV есть незакрытая кавычка.");

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString().TrimEnd('\r'));
            rows.Add(row);
        }

        return rows;
    }

    private sealed record ParsedQuestion(
        string Code,
        Guid TopicId,
        string Prompt,
        string Explanation,
        QuestionDifficulty Difficulty,
        IReadOnlyList<ParsedOption> Options,
        char CorrectLetter);

    private sealed record ParsedOption(char Letter, string Text, string Feedback);
}
