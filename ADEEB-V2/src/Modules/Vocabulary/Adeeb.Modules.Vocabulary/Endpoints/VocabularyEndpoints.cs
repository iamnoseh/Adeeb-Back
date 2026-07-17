using Adeeb.Application.Abstractions.Authorization;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Vocabulary.Application;
using Adeeb.Modules.Vocabulary.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Adeeb.Modules.Vocabulary.Endpoints;

public static class VocabularyEndpoints
{
    public static IEndpointRouteBuilder MapVocabularyEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v2/admin/vocabulary").WithTags("Vocabulary Admin");
        var view = admin.MapGroup("").RequireAuthorization(Permissions.Vocabulary.View);
        view.MapGet("/languages", async ([AsParameters] VocabularyListQuery q, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetLanguagesAsync(q, ct)).ToHttpResult(c, l));
        view.MapGet("/topics", async ([AsParameters] VocabularyListQuery q, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetTopicsAsync(q, ct)).ToHttpResult(c, l));
        view.MapGet("/words", async ([AsParameters] VocabularyListQuery q, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetWordsAsync(q, ct)).ToHttpResult(c, l));
        view.MapGet("/words/{id:guid}", async (Guid id, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetWordAsync(id, ct)).ToHttpResult(c, l));
        view.MapGet("/questions", async ([AsParameters] VocabularyListQuery q, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetQuestionsAsync(q, ct)).ToHttpResult(c, l));
        view.MapGet("/daily-words", async ([AsParameters] VocabularyListQuery q, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetDailyWordsAsync(q, ct)).ToHttpResult(c, l));

        var manage = admin.MapGroup("").RequireAuthorization(Permissions.Vocabulary.Manage);
        manage.MapPost("/languages", async (LanguageUpsertRequest r, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateLanguageAsync(r, ct)).ToHttpResult(c, l));
        manage.MapPut("/languages/{id:guid}", async (Guid id, LanguageUpsertRequest r, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateLanguageAsync(id, r, ct)).ToHttpResult(c, l));
        manage.MapPost("/topics", async (TopicUpsertRequest r, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateTopicAsync(r, ct)).ToHttpResult(c, l));
        manage.MapPut("/topics/{id:guid}", async (Guid id, TopicUpsertRequest r, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateTopicAsync(id, r, ct)).ToHttpResult(c, l));
        manage.MapPost("/words", async (WordUpsertRequest r, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateWordAsync(r, ct)).ToHttpResult(c, l));
        manage.MapPut("/words/{id:guid}", async (Guid id, WordUpsertRequest r, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateWordAsync(id, r, ct)).ToHttpResult(c, l));
        manage.MapPost("/words/{id:guid}/archive", async (Guid id, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.ArchiveWordAsync(id, ct)).ToHttpResult(c, l));
        manage.MapPost("/questions", async (QuestionUpsertRequest r, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateQuestionAsync(r, ct)).ToHttpResult(c, l));
        manage.MapPut("/questions/{id:guid}", async (Guid id, QuestionUpsertRequest r, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateQuestionAsync(id, r, ct)).ToHttpResult(c, l));
        manage.MapPost("/words/{id:guid}/question-drafts/generate", async (Guid id, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GenerateDraftsAsync(id, ct)).ToHttpResult(c, l));
        manage.MapPost("/questions/{id:guid}/publish", async (Guid id, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.PublishQuestionAsync(id, c.User, ct)).ToHttpResult(c, l));
        manage.MapPost("/questions/{id:guid}/archive", async (Guid id, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.ArchiveQuestionAsync(id, ct)).ToHttpResult(c, l));
        manage.MapPost("/daily-words", async (DailyWordUpsertRequest r, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpsertDailyWordAsync(r, ct)).ToHttpResult(c, l));
        manage.MapPut("/daily-words", async (DailyWordUpsertRequest r, VocabularyAdminService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpsertDailyWordAsync(r, ct)).ToHttpResult(c, l));

        var student = app.MapGroup("/api/v2/students/me/vocabulary").WithTags("Student Vocabulary").RequireAuthorization();
        student.MapGet("/languages", async (VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetLanguagesAsync(c.User, ct)).ToHttpResult(c, l));
        student.MapGet("/course", async (VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetCourseAsync(c.User, ct)).ToHttpResult(c, l));
        student.MapPut("/course", async (StudentVocabularyCourseRequest r, VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SetCourseAsync(c.User, r, ct)).ToHttpResult(c, l));
        student.MapGet("/today", async (VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetTodayAsync(c.User, ct)).ToHttpResult(c, l));
        student.MapGet("/dashboard", async (VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetDashboardAsync(c.User, ct)).ToHttpResult(c, l));
        student.MapPost("/sessions", async (StartVocabularySessionRequest r, VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.StartSessionAsync(c.User, r, ct)).ToHttpResult(c, l));
        student.MapGet("/sessions/{id:guid}", async (Guid id, VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetSessionAsync(c.User, id, ct)).ToHttpResult(c, l));
        student.MapPost("/sessions/{id:guid}/answers", async (Guid id, SubmitVocabularyAnswerRequest r, VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SubmitAnswerAsync(c.User, id, r, ct)).ToHttpResult(c, l));
        student.MapPost("/sessions/{id:guid}/complete", async (Guid id, VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CompleteSessionAsync(c.User, id, ct)).ToHttpResult(c, l));
        student.MapGet("/history", async (int? page, int? pageSize, VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetHistoryAsync(c.User, page ?? 1, pageSize ?? 10, ct)).ToHttpResult(c, l));
        student.MapGet("/mistakes", async (int? page, int? pageSize, VocabularyStudentService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetMistakesAsync(c.User, page ?? 1, pageSize ?? 10, ct)).ToHttpResult(c, l));
        return app;
    }
}
