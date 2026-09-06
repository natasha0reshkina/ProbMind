using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ProbMind.Application.Abstractions.Security;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;
using ProbMind.Infrastructure.Persistence;

namespace ProbMind.Infrastructure.Seeding;

public sealed class DatabaseSeeder
{
    private readonly ProbMindDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public DatabaseSeeder(ProbMindDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _db.Database.EnsureCreatedAsync(ct);
        await EnsureCompatibilityIndexesAsync(ct);

        if (await _db.Set<Topic>().AnyAsync(ct))
        {
            await SeedExtendedQuestionBankAsync(ct);
            return;
        }

        var topics = BuildTopics();
        var misconceptions = BuildMisconceptions(topics);
        var users = BuildUsers();

        await _db.Set<Topic>().AddRangeAsync(topics, ct);
        await _db.Set<Misconception>().AddRangeAsync(misconceptions, ct);
        await _db.Set<User>().AddRangeAsync(users, ct);
        await _db.SaveChangesAsync(ct);

        var topicByCode = topics.ToDictionary(x => x.Code);
        var misconceptionByCode = misconceptions.ToDictionary(x => x.Code);
        var definitions = ConditionalProbabilityQuestionSeed.All
            .Concat(IndependenceQuestionSeed.All)
            .Concat(PValueQuestionSeed.All)
            .Concat(LawLargeNumbersQuestionSeed.All)
            .Concat(RandomnessQuestionSeed.All)
            .Concat(ExtendedQuestionSeed.All)
            .Concat(LargeQuestionBankSeed.All)
            .ToArray();

        foreach (var definition in definitions)
        {
            var question = new Question
            {
                Id = StableGuid("question:" + definition.Code),
                TopicId = topicByCode[definition.TopicCode].Id,
                Code = definition.Code,
                Kind = definition.Kind,
                Status = ContentStatus.Published,
                CurrentVersionNumber = 1,
                PublishedAt = DateTimeOffset.UtcNow
            };

            var version = new QuestionVersion
            {
                Id = StableGuid("version:" + definition.Code + ":1"),
                QuestionId = question.Id,
                VersionNumber = 1,
                Prompt = definition.Prompt,
                CorrectExplanation = definition.CorrectExplanation,
                Difficulty = definition.Difficulty,
                IsTransferQuestion = definition.IsTransfer,
                EstimatedSeconds = definition.IsTransfer ? 150 : 90,
                AuthorNotes = "Начальная версия банка заданий."
            };

            await _db.Set<Question>().AddAsync(question, ct);
            await _db.Set<QuestionVersion>().AddAsync(version, ct);

            var optionIndex = 0;
            foreach (var option in definition.Options)
            {
                optionIndex++;
                await _db.Set<AnswerOption>().AddAsync(new AnswerOption
                {
                    Id = StableGuid($"option:{definition.Code}:{optionIndex}"),
                    QuestionVersionId = version.Id,
                    Text = option.Text,
                    IsCorrect = option.IsCorrect,
                    MisconceptionId = option.MisconceptionCode is null
                        ? null
                        : misconceptionByCode[option.MisconceptionCode].Id,
                    Feedback = option.Feedback,
                    SortOrder = optionIndex
                }, ct);
            }

            foreach (var mcCode in definition.TestedMisconceptionCodes.Distinct())
            {
                await _db.Set<QuestionMisconceptionMap>().AddAsync(new QuestionMisconceptionMap
                {
                    Id = StableGuid($"map:{definition.Code}:{mcCode}"),
                    QuestionId = question.Id,
                    MisconceptionId = misconceptionByCode[mcCode].Id,
                    RelevanceWeight = definition.IsTransfer ? 1.15d : 1d,
                    CanDisconfirm = true
                }, ct);
            }
        }

        foreach (var misconception in misconceptions)
        {
            var module = new CorrectiveModule
            {
                Id = StableGuid("module:" + misconception.Code),
                MisconceptionId = misconception.Id,
                Title = "Коррекция: " + misconception.Title,
                TheoryMarkdown = misconception.CorrectiveExplanation,
                WorkedExampleMarkdown =
                    $"### Разбор\nСначала сформулируйте ошибочное правило, связанное с «{misconception.Title}». " +
                    "Затем сравните его с корректным определением и проверьте на контрпримере.",
                Revision = 1,
                Status = ContentStatus.Published
            };
            await _db.Set<CorrectiveModule>().AddAsync(module, ct);
        }

        await _db.SaveChangesAsync(ct);
        await SeedDemoLearnerHistoryAsync(users.Single(x => x.Email == "student@probmind.local"), topics, misconceptions, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedExtendedQuestionBankAsync(CancellationToken ct)
    {
        var topics = await _db.Set<Topic>().ToListAsync(ct);
        var misconceptions = await _db.Set<Misconception>().ToListAsync(ct);
        var existingQuestions = (await _db.Set<Question>().ToListAsync(ct))
            .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var existingVersions = await _db.Set<QuestionVersion>().ToListAsync(ct);

        var topicByCode = topics.ToDictionary(x => x.Code);
        var misconceptionByCode = misconceptions.ToDictionary(x => x.Code);
        var changed = false;

        foreach (var definition in ExtendedQuestionSeed.All.Concat(LargeQuestionBankSeed.All))
        {
            if (existingQuestions.TryGetValue(definition.Code, out var existingQuestion))
            {
                var currentVersion = existingVersions.SingleOrDefault(x =>
                    x.QuestionId == existingQuestion.Id &&
                    x.VersionNumber == existingQuestion.CurrentVersionNumber);
                if (currentVersion is not null)
                {
                    currentVersion.Difficulty = definition.Difficulty;
                    currentVersion.IsTransferQuestion = definition.IsTransfer;
                    currentVersion.EstimatedSeconds = definition.IsTransfer
                        ? 180
                        : definition.Difficulty == QuestionDifficulty.Advanced ? 150 : 90;
                }
                changed = true;
                continue;
            }

            var question = new Question
            {
                Id = StableGuid("question:" + definition.Code),
                TopicId = topicByCode[definition.TopicCode].Id,
                Code = definition.Code,
                Kind = definition.Kind,
                Status = ContentStatus.Published,
                CurrentVersionNumber = 1,
                PublishedAt = DateTimeOffset.UtcNow
            };

            var version = new QuestionVersion
            {
                Id = StableGuid("version:" + definition.Code + ":1"),
                QuestionId = question.Id,
                VersionNumber = 1,
                Prompt = definition.Prompt,
                CorrectExplanation = definition.CorrectExplanation,
                Difficulty = definition.Difficulty,
                IsTransferQuestion = definition.IsTransfer,
                EstimatedSeconds = definition.IsTransfer ? 180 : definition.Difficulty == QuestionDifficulty.Advanced ? 150 : 90,
                AuthorNotes = "Расширенная версия банка заданий."
            };

            await _db.Set<Question>().AddAsync(question, ct);
            await _db.Set<QuestionVersion>().AddAsync(version, ct);

            var optionIndex = 0;
            foreach (var option in definition.Options)
            {
                optionIndex++;
                await _db.Set<AnswerOption>().AddAsync(new AnswerOption
                {
                    Id = StableGuid($"option:{definition.Code}:{optionIndex}"),
                    QuestionVersionId = version.Id,
                    Text = option.Text,
                    IsCorrect = option.IsCorrect,
                    MisconceptionId = option.MisconceptionCode is null
                        ? null
                        : misconceptionByCode[option.MisconceptionCode].Id,
                    Feedback = option.Feedback,
                    SortOrder = optionIndex
                }, ct);
            }

            foreach (var mcCode in definition.TestedMisconceptionCodes.Distinct())
            {
                await _db.Set<QuestionMisconceptionMap>().AddAsync(new QuestionMisconceptionMap
                {
                    Id = StableGuid($"map:{definition.Code}:{mcCode}"),
                    QuestionId = question.Id,
                    MisconceptionId = misconceptionByCode[mcCode].Id,
                    RelevanceWeight = definition.IsTransfer ? 1.15d : definition.Difficulty == QuestionDifficulty.Advanced ? 1.08d : 1d,
                    CanDisconfirm = true
                }, ct);
            }

            existingQuestions[definition.Code] = question;
            changed = true;
        }

        if (changed)
            await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureCompatibilityIndexesAsync(CancellationToken ct)
    {
        await _db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_diagnostic_answers_SessionId_QuestionId\" ON diagnostic_answers (\"SessionId\", \"QuestionId\");",
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_practice_attempts_PracticeSessionId_QuestionId\" ON practice_attempts (\"PracticeSessionId\", \"QuestionId\");",
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE diagnostic_answers ADD COLUMN IF NOT EXISTS \"StudentNote\" character varying(2000) NULL;",
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE practice_attempts ADD COLUMN IF NOT EXISTS \"StudentNote\" character varying(2000) NULL;",
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE users ADD COLUMN IF NOT EXISTS \"LeaderboardOptIn\" boolean NOT NULL DEFAULT FALSE;",
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS \"IX_users_LeaderboardOptIn\" ON users (\"LeaderboardOptIn\");",
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS gamification_settings (
                "Id" uuid NOT NULL,
                "LeaderboardEnabled" boolean NOT NULL DEFAULT TRUE,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "Version" bigint NOT NULL,
                CONSTRAINT "PK_gamification_settings" PRIMARY KEY ("Id")
            );
            """,
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS study_items (
                "Id" uuid NOT NULL,
                "CreatedByUserId" uuid NOT NULL,
                "StudentId" uuid NULL,
                "Kind" integer NOT NULL,
                "Visibility" integer NOT NULL,
                "Title" character varying(300) NOT NULL,
                "Body" character varying(10000) NOT NULL,
                "TeacherResponse" character varying(5000) NOT NULL DEFAULT '',
                "DiscussInClass" boolean NOT NULL DEFAULT FALSE,
                "TeacherRespondedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "Version" bigint NOT NULL,
                CONSTRAINT "PK_study_items" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_study_items_StudentId" ON study_items ("StudentId");
            CREATE INDEX IF NOT EXISTS "IX_study_items_CreatedByUserId" ON study_items ("CreatedByUserId");
            CREATE INDEX IF NOT EXISTS "IX_study_items_Visibility" ON study_items ("Visibility");
            CREATE INDEX IF NOT EXISTS "IX_study_items_CreatedAt" ON study_items ("CreatedAt");
            """,
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS study_item_notes (
                "Id" uuid NOT NULL,
                "StudyItemId" uuid NOT NULL,
                "StudentId" uuid NOT NULL,
                "Body" character varying(5000) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "Version" bigint NOT NULL,
                CONSTRAINT "PK_study_item_notes" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_study_item_notes_StudyItemId" ON study_item_notes ("StudyItemId");
            CREATE INDEX IF NOT EXISTS "IX_study_item_notes_StudentId" ON study_item_notes ("StudentId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_study_item_notes_StudyItemId_StudentId" ON study_item_notes ("StudyItemId", "StudentId");
            """,
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            """ALTER TABLE diagnostic_sessions ADD COLUMN IF NOT EXISTS "DiagnosticTemplateId" uuid NULL;""",
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            """CREATE INDEX IF NOT EXISTS "IX_diagnostic_sessions_DiagnosticTemplateId" ON diagnostic_sessions ("DiagnosticTemplateId");""",
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS diagnostic_templates (
                "Id" uuid NOT NULL,
                "CreatedByUserId" uuid NOT NULL,
                "GroupId" uuid NULL,
                "StudentId" uuid NULL,
                "Title" character varying(300) NOT NULL,
                "Description" character varying(2000) NOT NULL DEFAULT '',
                "IsPublished" boolean NOT NULL DEFAULT FALSE,
                "PublishedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "Version" bigint NOT NULL,
                CONSTRAINT "PK_diagnostic_templates" PRIMARY KEY ("Id")
            );
            """,
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE diagnostic_templates ADD COLUMN IF NOT EXISTS "GroupId" uuid NULL;
            ALTER TABLE diagnostic_templates ADD COLUMN IF NOT EXISTS "StudentId" uuid NULL;
            CREATE INDEX IF NOT EXISTS "IX_diagnostic_templates_CreatedByUserId" ON diagnostic_templates ("CreatedByUserId");
            CREATE INDEX IF NOT EXISTS "IX_diagnostic_templates_GroupId" ON diagnostic_templates ("GroupId");
            CREATE INDEX IF NOT EXISTS "IX_diagnostic_templates_StudentId" ON diagnostic_templates ("StudentId");
            CREATE INDEX IF NOT EXISTS "IX_diagnostic_templates_IsPublished" ON diagnostic_templates ("IsPublished");
            CREATE INDEX IF NOT EXISTS "IX_diagnostic_templates_CreatedAt" ON diagnostic_templates ("CreatedAt");
            """,
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS diagnostic_template_questions (
                "Id" uuid NOT NULL,
                "DiagnosticTemplateId" uuid NOT NULL,
                "QuestionId" uuid NOT NULL,
                "Position" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                "Version" bigint NOT NULL,
                CONSTRAINT "PK_diagnostic_template_questions" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_diagnostic_template_questions_DiagnosticTemplateId" ON diagnostic_template_questions ("DiagnosticTemplateId");
            CREATE INDEX IF NOT EXISTS "IX_diagnostic_template_questions_QuestionId" ON diagnostic_template_questions ("QuestionId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_diagnostic_template_questions_DiagnosticTemplateId_Position" ON diagnostic_template_questions ("DiagnosticTemplateId", "Position");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_diagnostic_template_questions_DiagnosticTemplateId_QuestionId" ON diagnostic_template_questions ("DiagnosticTemplateId", "QuestionId");
            """,
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE diagnostic_answers ADD COLUMN IF NOT EXISTS "ConfidenceLevel" integer NULL;
            ALTER TABLE diagnostic_answers ADD COLUMN IF NOT EXISTS "Reasoning" character varying(4000) NULL;
            ALTER TABLE practice_attempts ADD COLUMN IF NOT EXISTS "ConfidenceLevel" integer NULL;
            ALTER TABLE practice_attempts ADD COLUMN IF NOT EXISTS "Reasoning" character varying(4000) NULL;
            """,
            ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS spaced_review_items (
                "Id" uuid NOT NULL, "UserId" uuid NOT NULL, "TopicId" uuid NOT NULL,
                "NextReviewAt" timestamp with time zone NOT NULL, "LastReviewedAt" timestamp with time zone NULL,
                "IntervalDays" integer NOT NULL, "EaseFactor" double precision NOT NULL, "Repetitions" integer NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL, "Version" bigint NOT NULL,
                CONSTRAINT "PK_spaced_review_items" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_spaced_review_items_UserId_TopicId" ON spaced_review_items ("UserId", "TopicId");
            CREATE INDEX IF NOT EXISTS "IX_spaced_review_items_NextReviewAt" ON spaced_review_items ("NextReviewAt");
            """, ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS student_groups (
                "Id" uuid NOT NULL, "CreatedByUserId" uuid NOT NULL, "Name" character varying(200) NOT NULL,
                "Description" character varying(2000) NOT NULL DEFAULT '', "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL, "Version" bigint NOT NULL,
                CONSTRAINT "PK_student_groups" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_student_groups_CreatedByUserId" ON student_groups ("CreatedByUserId");
            CREATE TABLE IF NOT EXISTS student_group_members (
                "Id" uuid NOT NULL, "GroupId" uuid NOT NULL, "StudentId" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL, "Version" bigint NOT NULL,
                CONSTRAINT "PK_student_group_members" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_student_group_members_GroupId_StudentId" ON student_group_members ("GroupId", "StudentId");
            CREATE INDEX IF NOT EXISTS "IX_student_group_members_StudentId" ON student_group_members ("StudentId");
            """, ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS teacher_interventions (
                "Id" uuid NOT NULL, "CreatedByUserId" uuid NOT NULL, "StudentId" uuid NOT NULL,
                "Title" character varying(300) NOT NULL, "Body" character varying(5000) NOT NULL DEFAULT '',
                "Kind" character varying(80) NOT NULL DEFAULT 'Practice', "IsCompleted" boolean NOT NULL DEFAULT FALSE,
                "DueAt" timestamp with time zone NULL, "CompletedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL, "Version" bigint NOT NULL,
                CONSTRAINT "PK_teacher_interventions" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_teacher_interventions_StudentId" ON teacher_interventions ("StudentId");
            CREATE INDEX IF NOT EXISTS "IX_teacher_interventions_CreatedByUserId" ON teacher_interventions ("CreatedByUserId");
            CREATE INDEX IF NOT EXISTS "IX_teacher_interventions_IsCompleted" ON teacher_interventions ("IsCompleted");
            """, ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS exam_definitions (
                "Id" uuid NOT NULL, "CreatedByUserId" uuid NOT NULL, "DiagnosticTemplateId" uuid NOT NULL, "GroupId" uuid NULL, "StudentId" uuid NULL,
                "Title" character varying(300) NOT NULL, "Description" character varying(2000) NOT NULL DEFAULT '',
                "TimeLimitMinutes" integer NOT NULL, "IsPublished" boolean NOT NULL DEFAULT FALSE,
                "AvailableFrom" timestamp with time zone NULL, "AvailableUntil" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL, "Version" bigint NOT NULL,
                CONSTRAINT "PK_exam_definitions" PRIMARY KEY ("Id")
            );
            """, ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE exam_definitions ADD COLUMN IF NOT EXISTS "GroupId" uuid NULL;
            ALTER TABLE exam_definitions ADD COLUMN IF NOT EXISTS "StudentId" uuid NULL;
            CREATE INDEX IF NOT EXISTS "IX_exam_definitions_DiagnosticTemplateId" ON exam_definitions ("DiagnosticTemplateId");
            CREATE INDEX IF NOT EXISTS "IX_exam_definitions_GroupId" ON exam_definitions ("GroupId");
            CREATE INDEX IF NOT EXISTS "IX_exam_definitions_StudentId" ON exam_definitions ("StudentId");
            CREATE INDEX IF NOT EXISTS "IX_exam_definitions_IsPublished" ON exam_definitions ("IsPublished");
            CREATE TABLE IF NOT EXISTS exam_attempts (
                "Id" uuid NOT NULL, "ExamId" uuid NOT NULL, "StudentId" uuid NOT NULL, "DiagnosticSessionId" uuid NOT NULL,
                "StartedAt" timestamp with time zone NOT NULL, "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL, "Version" bigint NOT NULL,
                CONSTRAINT "PK_exam_attempts" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_exam_attempts_ExamId_StudentId" ON exam_attempts ("ExamId", "StudentId");
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_exam_attempts_DiagnosticSessionId" ON exam_attempts ("DiagnosticSessionId");
            """, ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS student_achievements (
                "Id" uuid NOT NULL, "UserId" uuid NOT NULL, "Code" character varying(120) NOT NULL,
                "Progress" integer NOT NULL DEFAULT 0, "Target" integer NOT NULL DEFAULT 1,
                "UnlockedAt" timestamp with time zone NULL,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL, "Version" bigint NOT NULL,
                CONSTRAINT "PK_student_achievements" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_student_achievements_UserId_Code" ON student_achievements ("UserId", "Code");
            CREATE INDEX IF NOT EXISTS "IX_student_achievements_UserId" ON student_achievements ("UserId");
            CREATE INDEX IF NOT EXISTS "IX_student_achievements_UnlockedAt" ON student_achievements ("UnlockedAt");
            """, ct);
        await _db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS material_study_cycles (
                "Id" uuid NOT NULL, "CreatedByUserId" uuid NOT NULL, "PreDiagnosticTemplateId" uuid NOT NULL,
                "PostDiagnosticTemplateId" uuid NOT NULL, "GroupId" uuid NULL, "Title" character varying(300) NOT NULL,
                "MaterialText" character varying(20000) NOT NULL, "IsPublished" boolean NOT NULL DEFAULT FALSE,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL, "Version" bigint NOT NULL,
                CONSTRAINT "PK_material_study_cycles" PRIMARY KEY ("Id")
            );
            CREATE INDEX IF NOT EXISTS "IX_material_study_cycles_GroupId" ON material_study_cycles ("GroupId");
            CREATE INDEX IF NOT EXISTS "IX_material_study_cycles_IsPublished" ON material_study_cycles ("IsPublished");
            CREATE TABLE IF NOT EXISTS material_study_progress (
                "Id" uuid NOT NULL, "MaterialStudyCycleId" uuid NOT NULL, "StudentId" uuid NOT NULL,
                "PreSessionId" uuid NULL, "MaterialOpenedAt" timestamp with time zone NULL, "PostSessionId" uuid NULL,
                "CreatedAt" timestamp with time zone NOT NULL, "UpdatedAt" timestamp with time zone NOT NULL, "Version" bigint NOT NULL,
                CONSTRAINT "PK_material_study_progress" PRIMARY KEY ("Id")
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_material_study_progress_MaterialStudyCycleId_StudentId" ON material_study_progress ("MaterialStudyCycleId", "StudentId");
            CREATE INDEX IF NOT EXISTS "IX_material_study_progress_StudentId" ON material_study_progress ("StudentId");
            """, ct);
    }

    private List<User> BuildUsers()
    {
        var users = new List<User>
        {
            new()
            {
                Id = StableGuid("user:student"),
                Email = "student@probmind.local",
                DisplayName = "Демо студент",
                PasswordHash = _passwordHasher.Hash("Student123!"),
                Role = UserRole.Student,
                IsActive = true
            },
            new()
            {
                Id = StableGuid("user:teacher"),
                Email = "teacher@probmind.local",
                DisplayName = "Преподаватель",
                PasswordHash = _passwordHasher.Hash("Teacher123!"),
                Role = UserRole.Teacher,
                IsActive = true
            },
            new()
            {
                Id = StableGuid("user:admin"),
                Email = "admin@probmind.local",
                DisplayName = "Администратор",
                PasswordHash = _passwordHasher.Hash("Admin123!"),
                Role = UserRole.Admin,
                IsActive = true
            }
        };

        for (var i = 1; i <= 18; i++)
        {
            users.Add(new User
            {
                Id = StableGuid($"user:cohort:{i}"),
                Email = $"student{i:00}@demo.probmind.local",
                DisplayName = $"Студент группы {i:00}",
                PasswordHash = _passwordHasher.Hash("DemoStudent123!"),
                Role = UserRole.Student,
                IsActive = true
            });
        }

        return users;
    }

    private static List<Topic> BuildTopics() =>
    [
        new()
        {
            Id = StableGuid("topic:conditional_probability"),
            Code = "conditional_probability",
            NameRu = "Условная вероятность",
            NameEn = "Conditional probability",
            Description = "Условная вероятность, формула Байеса и влияние базовых частот.",
            SortOrder = 1,
            Status = ContentStatus.Published
        },
        new()
        {
            Id = StableGuid("topic:independence"),
            Code = "independence",
            NameRu = "Независимость событий",
            NameEn = "Independence",
            Description = "Независимость, несовместность, корреляция и совместные распределения.",
            SortOrder = 2,
            Status = ContentStatus.Published
        },
        new()
        {
            Id = StableGuid("topic:p_value"),
            Code = "p_value",
            NameRu = "p-value и проверка гипотез",
            NameEn = "p-value and hypothesis testing",
            Description = "Корректная интерпретация p-value, статистической и практической значимости.",
            SortOrder = 3,
            Status = ContentStatus.Published
        },
        new()
        {
            Id = StableGuid("topic:law_large_numbers"),
            Code = "law_large_numbers",
            NameRu = "Закон больших чисел",
            NameEn = "Law of large numbers",
            Description = "Долгосрочная стабилизация частот и ограничения вывода для коротких серий.",
            SortOrder = 4,
            Status = ContentStatus.Published
        },
        new()
        {
            Id = StableGuid("topic:randomness"),
            Code = "randomness",
            NameRu = "Интерпретация случайности",
            NameEn = "Interpretation of randomness",
            Description = "Случайный механизм, серии, визуальная регулярность и неравномерные распределения.",
            SortOrder = 5,
            Status = ContentStatus.Published
        }
    ];

    private static List<Misconception> BuildMisconceptions(IReadOnlyList<Topic> topics)
    {
        var topic = topics.ToDictionary(x => x.Code);

        return
        [
            Mc(topic["conditional_probability"], "conditional_reverse",
                "Подмена P(A|B) на P(B|A)",
                "Студент меняет местами условное и обусловливающее события.",
                "Явно выпишите, какое событие известно, а какое оценивается. Сравните P(A|B) и P(B|A) на таблице частот.",
                "Диагноз поддерживают дистракторы с обратным направлением условия."),
            Mc(topic["conditional_probability"], "base_rate_neglect",
                "Игнорирование базовой частоты",
                "При обратной вероятности студент не учитывает априорную распространённость события.",
                "Постройте дерево вероятностей или таблицу натуральных частот и включите базовую частоту до анализа теста.",
                "Диагноз поддерживают ответы, использующие только чувствительность/специфичность без учёта исходной распространённости."),
            Mc(topic["independence"], "independence_vs_exclusive",
                "Независимость как несовместность",
                "Студент считает, что независимые события не могут происходить совместно.",
                "Сопоставьте определения: несовместность означает P(A∩B)=0, независимость — P(A∩B)=P(A)P(B).",
                "Диагноз поддерживают дистракторы, отождествляющие независимость и взаимоисключение."),
            Mc(topic["independence"], "zero_corr_independent",
                "Нулевая корреляция как независимость",
                "Студент считает отсутствие линейной корреляции доказательством полной независимости.",
                "Рассмотрите нелинейный пример Y=X² для симметричного X: корреляция может быть нулевой при зависимости.",
                "Диагноз поддерживают выводы о независимости только из коэффициента корреляции."),
            Mc(topic["p_value"], "pvalue_probability_null",
                "p-value как вероятность истинности H₀",
                "Студент интерпретирует p-value как P(H₀|данные).",
                "Переформулируйте p-value как вероятность экстремальных данных при условии H₀; не меняйте направление условности.",
                "Диагноз поддерживают ответы вида «вероятность, что H₀ истинна»."),
            Mc(topic["p_value"], "significance_effect",
                "Статистическая значимость как большой эффект",
                "Студент приравнивает малое p-value к практически большой величине эффекта.",
                "Разделяйте статистическую значимость, размер эффекта и доверительный интервал.",
                "Диагноз поддерживают ответы, делающие вывод о величине эффекта только по p-value."),
            Mc(topic["law_large_numbers"], "lln_short_run",
                "Закон больших чисел для короткой серии",
                "Студент ожидает почти точного совпадения частоты с вероятностью уже после нескольких испытаний.",
                "Закон больших чисел асимптотический: оцените вариативность конечной выборки и не требуйте мгновенного выравнивания.",
                "Диагноз поддерживают утверждения, что короткая серия обязана отражать теоретическую частоту."),
            Mc(topic["law_large_numbers"], "gamblers_fallacy",
                "Ошибка игрока",
                "Студент ожидает компенсации предыдущей серии в следующем независимом испытании.",
                "Отделите долгосрочную частоту от условной вероятности следующего независимого исхода.",
                "Диагноз поддерживают ответы «после серии неудач успех теперь вероятнее»."),
            Mc(topic["randomness"], "random_must_look_messy",
                "Случайность должна выглядеть беспорядочно",
                "Студент отвергает регулярные фрагменты и длинные серии как «неслучайные».",
                "Сравните вероятность конкретных последовательностей: регулярная последовательность может быть столь же вероятна, как любая другая фиксированная.",
                "Диагноз поддерживают эвристические оценки случайности по визуальной «хаотичности»."),
            Mc(topic["randomness"], "random_equal_probability",
                "Случайность означает равновероятность",
                "Студент считает любой случайный процесс равномерным распределением.",
                "Разделите непредсказуемость исхода и форму распределения: случайные исходы могут иметь разные вероятности.",
                "Диагноз поддерживают утверждения «случайный = все исходы одинаково вероятны».")
        ];
    }

    private static Misconception Mc(
        Topic topic,
        string code,
        string title,
        string description,
        string corrective,
        string rationale) =>
        new()
        {
            Id = StableGuid("mc:" + code),
            TopicId = topic.Id,
            Code = code,
            Title = title,
            Description = description,
            CorrectiveExplanation = corrective,
            DiagnosticRationale = rationale,
            Status = ContentStatus.Published
        };

    private async Task SeedDemoLearnerHistoryAsync(
        User demo,
        IReadOnlyList<Topic> topics,
        IReadOnlyList<Misconception> misconceptions,
        CancellationToken ct)
    {
        var random = new Random(42);
        foreach (var topic in topics)
        {
            var mastery = 0.38d + random.NextDouble() * 0.48d;
            await _db.Set<TopicMastery>().AddAsync(new TopicMastery
            {
                UserId = demo.Id,
                TopicId = topic.Id,
                Mastery = mastery,
                Uncertainty = 0.22d,
                ObservationCount = random.Next(8, 25),
                LastObservedAt = DateTimeOffset.UtcNow.AddDays(-random.Next(0, 10))
            }, ct);

            for (var point = 5; point >= 0; point--)
            {
                await _db.Set<TopicMasteryHistory>().AddAsync(new TopicMasteryHistory
                {
                    UserId = demo.Id,
                    TopicId = topic.Id,
                    Mastery = Math.Clamp(mastery - 0.16d + (5 - point) * 0.032d, 0.1d, 0.95d),
                    Uncertainty = 0.45d - (5 - point) * 0.04d,
                    Reason = "Демонстрационная история прогресса.",
                    RecordedAt = DateTimeOffset.UtcNow.AddDays(-point * 7)
                }, ct);
            }
        }

        foreach (var mc in misconceptions.Take(4))
        {
            var confidence = 0.48d + random.NextDouble() * 0.35d;
            await _db.Set<UserMisconception>().AddAsync(new UserMisconception
            {
                UserId = demo.Id,
                MisconceptionId = mc.Id,
                Confidence = confidence,
                Status = confidence >= 0.62d ? MisconceptionStatus.Detected : MisconceptionStatus.Suspected,
                EvidenceCount = 4,
                PositiveEvidenceCount = 3,
                NegativeEvidenceCount = 1,
                FirstDetectedAt = DateTimeOffset.UtcNow.AddDays(-20),
                LastDetectedAt = DateTimeOffset.UtcNow.AddDays(-2)
            }, ct);

            for (var i = 0; i < 4; i++)
            {
                await _db.Set<MisconceptionEvidence>().AddAsync(new MisconceptionEvidence
                {
                    UserId = demo.Id,
                    MisconceptionId = mc.Id,
                    Kind = i == 3 ? EvidenceKind.CorrectAnswer : EvidenceKind.Distractor,
                    RawWeight = i == 3 ? -0.55d : 1d,
                    RelevanceWeight = 1d,
                    Explanation = i == 3 ? "Правильный ответ ослабляет диагностическую гипотезу." : "Выбран вариант, характерный для этого типа ошибки.",
                    ObservedAt = DateTimeOffset.UtcNow.AddDays(-15 + i * 4)
                }, ct);
            }
        }
    }

    public static Guid StableGuid(string value)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash);
    }
}
