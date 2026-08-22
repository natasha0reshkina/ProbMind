using Microsoft.Extensions.DependencyInjection;
using ProbMind.Application.Services;
using ProbMind.Application.Validation;
using ProbMind.Domain.Analytics;
using ProbMind.Domain.Diagnostics;
using ProbMind.Domain.Explainability;
using ProbMind.Domain.Learning;
using ProbMind.Domain.Psychometrics;

namespace ProbMind.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddProbMindApplication(this IServiceCollection services)
    {
        services.AddScoped<ConfidenceEngine>();
        services.AddScoped<ConfidenceCalibrationService>();
        services.AddScoped<MisconceptionStateMachine>();
        services.AddScoped<MasteryEngine>();
        services.AddScoped<AdaptiveQuestionSelector>();
        services.AddScoped<DiagnosticCoveragePlanner>();
        services.AddScoped<LearningPathBuilder>();
        services.AddScoped<CorrectionEngine>();

        services.AddScoped<InterventionEffectivenessAnalyzer>();
        services.AddScoped<ItemDifficultyEstimator>();
        services.AddScoped<DistractorAnalysisEngine>();
        services.AddScoped<DiagnosticReliabilityAnalyzer>();
        services.AddScoped<CohortSegmentationEngine>();
        services.AddScoped<MisconceptionCooccurrenceAnalyzer>();
        services.AddScoped<LearningPathStabilityAnalyzer>();
        services.AddScoped<CohortBenchmarkEngine>();
        services.AddScoped<StudentRiskScorer>();
        services.AddScoped<MisconceptionClusterer>();
        services.AddScoped<ContentDriftAnalyzer>();
        services.AddSingleton<ItemResponseTheoryModel>();
        services.AddSingleton<MasteryForecastEngine>();
        services.AddSingleton<EvidenceContributionAnalyzer>();
        services.AddSingleton<DiagnosticExplanationBuilder>();

        services.AddSingleton<IRequestValidator, RegisterRequestValidator>();
        services.AddSingleton<IRequestValidator, LoginRequestValidator>();
        services.AddSingleton<IRequestValidator, StartDiagnosticRequestValidator>();
        services.AddSingleton<IRequestValidator, SubmitDiagnosticAnswerRequestValidator>();
        services.AddSingleton<IRequestValidator, StartPracticeRequestValidator>();
        services.AddSingleton<IRequestValidator, SubmitPracticeAnswerRequestValidator>();
        services.AddSingleton<IRequestValidator, UpdateProfileRequestValidator>();
        services.AddSingleton<IRequestValidator, CreateQuestionRequestValidator>();
        services.AddSingleton<IRequestValidator, CreateQuestionVersionRequestValidator>();
        services.AddSingleton<IRequestValidator, SetUserRoleRequestValidator>();
        services.AddSingleton<IRequestValidator, SetUserActiveRequestValidator>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDiagnosticService, DiagnosticService>();
        services.AddScoped<ILearnerModelService, LearnerModelService>();
        services.AddScoped<ILearningPathService, LearningPathService>();
        services.AddScoped<IPracticeService, PracticeService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<ITeacherService, TeacherService>();
        services.AddScoped<IResearchAnalyticsService, ResearchAnalyticsService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IAdvancedAnalyticsService, AdvancedAnalyticsService>();

        return services;
    }
}
