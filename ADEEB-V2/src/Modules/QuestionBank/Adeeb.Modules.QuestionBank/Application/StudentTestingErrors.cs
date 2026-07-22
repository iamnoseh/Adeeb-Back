using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.QuestionBank.Application;

public static class StudentTestingErrors
{
    public static readonly Error NotEnoughRedListQuestions = Error.Validation("redlist.not_enough_questions", "Testing.RedListNotEnough");
    public static readonly Error NotEnoughQuestions = Error.Validation("test.not_enough_questions", "Testing.NotEnoughQuestions");
    public static readonly Error AttemptNotFound = Error.NotFound("test.attempt_not_found", "Testing.AttemptNotFound");
    public static readonly Error AttemptAlreadySubmitted = Error.Conflict("test.attempt_already_submitted", "Testing.AttemptAlreadySubmitted");
    public static readonly Error AttemptExpired = Error.Conflict("test.attempt_expired", "Testing.AttemptExpired");
    public static readonly Error RewardCalculationFailed = Error.Conflict("test.attempt_reward_calculation_failed", "Testing.AttemptRewardCalculationFailed");
    public static readonly Error RewardConflict = Error.Conflict("test.attempt_reward_conflict", "Testing.AttemptRewardConflict");
    public static readonly Error InvalidMode = Error.Validation("test.invalid_mode", "Testing.InvalidMode");
    public static readonly Error ImmediateCheckNotAllowed = Error.Validation("test.immediate_check_not_allowed", "Testing.ImmediateCheckNotAllowed");
    public static readonly Error QuestionNotInAttempt = Error.NotFound("test.question_not_in_attempt", "Testing.QuestionNotInAttempt");
    public static readonly Error AnswerRequired = Error.Validation("test.answer_required", "Testing.AnswerRequired");
    public static readonly Error InvalidQuestionCount = Error.Validation("test.invalid_question_count", "Testing.InvalidQuestionCount");
    public static readonly Error ProfileRequired = Error.Validation("mmt.profile_required", "Testing.MmtProfileRequired");
    public static readonly Error ChoicesRequired = Error.Validation("mmt.choices_required", "Testing.MmtChoicesRequired");
    public static readonly Error MmtExamNotConfigured = Error.Conflict("mmt.exam_not_configured", "Testing.MmtExamNotConfigured");
    public static readonly Error MonthlyExamClosed = Error.Validation("monthly_exam.closed", "Testing.MonthlyExamClosed");
    public static readonly Error MonthlyExamAlreadyStarted = Error.Conflict("monthly_exam.already_started", "Testing.MonthlyExamAlreadyStarted");
    public static readonly Error UserRequired = Error.Unauthorized("test.user_required", "Common.Unauthorized");
    public static readonly Error RedListItemNotFound = Error.NotFound("redlist.item_not_found", "Testing.RedListItemNotFound");
}
