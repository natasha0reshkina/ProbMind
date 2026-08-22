namespace ProbMind.Domain.Learning;

public sealed record QuestionPriority(
    Guid QuestionId,
    double Score,
    double WeaknessComponent,
    double MisconceptionComponent,
    double SpacingComponent,
    double DifficultyComponent,
    double NoveltyComponent,
    double RepetitionPenalty,
    string Explanation);
