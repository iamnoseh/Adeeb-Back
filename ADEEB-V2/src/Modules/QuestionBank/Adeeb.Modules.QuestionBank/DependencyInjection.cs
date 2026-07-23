using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Application.Assessment;
using Adeeb.Modules.QuestionBank.Application.Import;
using Adeeb.Modules.QuestionBank.Infrastructure.DocumentExtraction;
using Adeeb.Modules.QuestionBank.Infrastructure.Files;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Adeeb.Application.Abstractions.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Modules.QuestionBank;

public static class DependencyInjection
{
    public static IServiceCollection AddQuestionBankModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("QuestionBank")
            ?? configuration.GetConnectionString("Default")
            ?? configuration.GetConnectionString("Identity");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("QuestionBank database connection string is required.");
        }

        services.AddDbContext<QuestionBankDbContext>(options => options.UseNpgsql(connectionString));
        services.AddOptions<QuestionImportOptions>()
            .Bind(configuration.GetSection(QuestionImportOptions.SectionName))
            .Validate(options => options.MaxFileSizeBytes > 0, "Question import max file size must be positive.")
            .Validate(options => options.MaxQuestionsPerImport > 0, "Question import max questions must be positive.")
            .ValidateOnStart();
        services.AddScoped<QuestionBankService>();
        services.AddOptions<StudentTestingOptions>()
            .Bind(configuration.GetSection(StudentTestingOptions.SectionName))
            .Validate(x => x.RedListMinimumQuestions >= 1 && x.RedListDefaultQuestions >= x.RedListMinimumQuestions,
                "StudentTesting Red List question counts are invalid.")
            .Validate(x => x.MmtPracticeDefaultQuestions is >= 1 and <= 200 && x.MonthlyExamQuestionCount is >= 1 and <= 200,
                "StudentTesting exam question counts must be between 1 and 200.")
            .Validate(x => x.MinutesPerSubjectQuestion > 0 && x.MmtDurationMinutes > 0 && x.MonthlyExamWindowHours is >= 1 and <= 72,
                "StudentTesting timing values are invalid.")
            .Validate(x => x.ExtendedTimeSubjectCodes is { Length: > 0 }
                && x.ExtendedMinutesPerSubjectQuestion >= x.MinutesPerSubjectQuestion
                && x.ExtendedTimeSubjectCodes.All(code => !string.IsNullOrWhiteSpace(code)),
                "StudentTesting extended subject timing values are invalid.")
            .Validate(x => x.ExpiredAttemptSweepIntervalSeconds is >= 10 and <= 3600
                && x.ExpiredAttemptSweepBatchSize is >= 1 and <= 1000,
                "StudentTesting expired-attempt sweep values are invalid.")
            .ValidateOnStart();
        services.AddOptions<TestXpRewardOptions>()
            .Bind(configuration.GetSection(TestXpRewardOptions.SectionName))
            .Validate(x => x.EasyCorrectXpUnits is >= 0 and <= TestXpRewardOptions.MaximumConfiguredUnits
                && x.MediumCorrectXpUnits is >= 0 and <= TestXpRewardOptions.MaximumConfiguredUnits
                && x.HardCorrectXpUnits is >= 0 and <= TestXpRewardOptions.MaximumConfiguredUnits
                && x.CompletionBonusXpUnits is >= 0 and <= TestXpRewardOptions.MaximumConfiguredUnits
                && x.MmtPracticeCompletionBonusXpUnits is >= 0 and <= TestXpRewardOptions.MaximumConfiguredUnits
                && x.MonthlyExamCompletionBonusXpUnits is >= 0 and <= TestXpRewardOptions.MaximumConfiguredUnits
                && x.RedListPracticeRewardPercent is >= 1 and < 100
                && x.RedListMasteryBonusXpUnits == TestXpRewardOptions.UnitsPerXp,
                "Test XP reward units must be non-negative and within the supported limit.")
            .ValidateOnStart();
        services.AddSingleton<ITestXpPolicy, TestXpPolicy>();
        services.AddScoped<IStudentXpService, StudentXpService>();
        services.AddScoped<IStudentXpReadService, StudentXpReadService>();
        services.AddHostedService<XpGrantOutboxDispatcher>();
        services.AddSingleton<ITestingRandomizer, TestingRandomizer>();
        services.AddScoped<IQuestionPickerService, QuestionPickerService>();
        services.AddScoped<RedListService>();
        services.AddScoped<IMonthlyExamAvailabilityService, MonthlyExamAvailabilityService>();
        services.AddScoped<ISubjectTestTimingPolicy, SubjectTestTimingPolicy>();
        services.AddScoped<StudentTestingService>();
        services.AddHostedService<ExpiredTestAttemptFinalizer>();
        services.AddSingleton<IQuestionAnswerEvaluator, SingleChoiceAnswerEvaluator>();
        services.AddSingleton<IQuestionAnswerEvaluator, ClosedAnswerEvaluator>();
        services.AddSingleton<IQuestionAnswerEvaluator, MatchingAnswerEvaluator>();
        services.AddScoped<IAnswerEvaluationService, AnswerEvaluationService>();
        services.AddHostedService<AnswerEvaluatorStartupValidator>();
        services.AddScoped<IAssessmentPresentationRandomizer, AssessmentPresentationRandomizer>();
        services.AddScoped<IQuestionImportService, QuestionImportService>();
        services.AddSingleton<IQuestionImportTextNormalizer, QuestionImportTextNormalizer>();
        services.AddSingleton<IQuestionDocumentParser, QuestionDocumentParser>();
        services.AddSingleton<IDocumentTextExtractor, DocxQuestionTextExtractor>();
        services.AddSingleton<IDocumentTextExtractor, PdfQuestionTextExtractor>();
        services.AddScoped<QuestionImageStorage>();
        return services;
    }
}
