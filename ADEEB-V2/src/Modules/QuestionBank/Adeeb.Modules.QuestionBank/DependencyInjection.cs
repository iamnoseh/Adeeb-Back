using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Application.Assessment;
using Adeeb.Modules.QuestionBank.Application.Import;
using Adeeb.Modules.QuestionBank.Infrastructure.DocumentExtraction;
using Adeeb.Modules.QuestionBank.Infrastructure.Files;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
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
