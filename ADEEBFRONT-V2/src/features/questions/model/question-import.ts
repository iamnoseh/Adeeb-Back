import type {
  QuestionImportConfirmRequest,
  QuestionImportIssue,
  QuestionImportPreviewResponse,
} from '@/features/questions/model/question.types'
import { QuestionTypeValue } from '@/features/questions/model/question.types'

export const questionImportLimits = {
  maxFileSizeBytes: 5 * 1024 * 1024,
  maxOptionsPerQuestion: 8,
  maxQuestionTextLength: 4000,
  maxOptionTextLength: 1000,
}

export type EditableImportedOption = {
  key: string
  label: string
  text: string
  isCorrect: boolean
}

export type EditableImportedQuestion = {
  key: string
  questionType: QuestionTypeValue
  questionTypeName: string
  questionText: string
  expectedAnswer: string
  options: EditableImportedOption[]
  serverErrors: QuestionImportIssue[]
  serverWarnings: QuestionImportIssue[]
  removed: boolean
}

export type ImportValidationResult = {
  isValid: boolean
  errors: QuestionImportIssue[]
  warnings: QuestionImportIssue[]
}

export function toEditableImportQuestions(preview: QuestionImportPreviewResponse): EditableImportedQuestion[] {
  return preview.questions.map((question) => {
    const questionType = toImportQuestionType(question.questionType)
    return {
      key: question.clientKey,
      questionType,
      questionTypeName: question.questionTypeName ?? (questionType === QuestionTypeValue.ClosedAnswer ? 'ClosedAnswer' : 'SingleChoice'),
      questionText: question.questionText,
      expectedAnswer: question.expectedAnswer ?? '',
      options: question.options.map((option, index) => ({
        key: `${question.clientKey}-${option.label || index}`,
        label: option.label || String.fromCharCode(65 + index),
        text: option.text,
        isCorrect: option.isCorrect,
      })),
      serverErrors: question.errors,
      serverWarnings: question.warnings,
      removed: false,
    }
  })
}

export function validateImportedQuestion(question: EditableImportedQuestion): ImportValidationResult {
  const errors: QuestionImportIssue[] = []
  const warnings: QuestionImportIssue[] = question.serverWarnings

  if (!question.questionText.trim()) {
    errors.push({ code: 'frontend.question_text_required', message: 'Question text is required.' })
  }

  if (question.questionText.length > questionImportLimits.maxQuestionTextLength) {
    errors.push({ code: 'frontend.question_text_too_long', message: `Question text cannot exceed ${questionImportLimits.maxQuestionTextLength} characters.` })
  }

  if (question.questionType === QuestionTypeValue.ClosedAnswer) {
    if (!question.expectedAnswer.trim()) {
      errors.push({ code: 'frontend.expected_answer_required', message: 'Expected answer is required.' })
    }

    if (question.expectedAnswer.length > questionImportLimits.maxOptionTextLength) {
      errors.push({ code: 'frontend.expected_answer_too_long', message: `Expected answer cannot exceed ${questionImportLimits.maxOptionTextLength} characters.` })
    }
  } else {
    if (question.options.length < 2) {
      errors.push({ code: 'frontend.too_few_options', message: 'At least two options are required.' })
    }

    if (question.options.length > questionImportLimits.maxOptionsPerQuestion) {
      errors.push({ code: 'frontend.too_many_options', message: `No more than ${questionImportLimits.maxOptionsPerQuestion} options are allowed.` })
    }

    if (question.options.filter((option) => option.isCorrect).length !== 1) {
      errors.push({ code: 'frontend.one_correct_required', message: 'Exactly one correct answer is required.' })
    }

    for (const option of question.options) {
      if (!option.text.trim()) {
        errors.push({ code: 'frontend.option_text_required', message: `Option ${option.label} text is required.` })
      }

      if (option.text.length > questionImportLimits.maxOptionTextLength) {
        errors.push({ code: 'frontend.option_text_too_long', message: `Option ${option.label} cannot exceed ${questionImportLimits.maxOptionTextLength} characters.` })
      }
    }
  }

  return { isValid: errors.length === 0, errors, warnings }
}

export function buildConfirmImportRequest(
  subjectId: string,
  topicId: string | null,
  difficulty: number,
  language: number,
  questions: EditableImportedQuestion[],
): QuestionImportConfirmRequest {
  return {
    subjectId,
    topicId,
    difficulty,
    language,
    questions: questions
      .filter((question) => !question.removed)
      .map((question) => question.questionType === QuestionTypeValue.ClosedAnswer
        ? {
            questionType: QuestionTypeValue.ClosedAnswer,
            questionText: question.questionText.trim(),
            expectedAnswer: question.expectedAnswer.trim(),
            options: [],
          }
        : {
            questionType: QuestionTypeValue.SingleChoice,
            questionText: question.questionText.trim(),
            expectedAnswer: null,
            options: question.options.map((option) => ({
              text: option.text.trim(),
              isCorrect: option.isCorrect,
            })),
          }),
  }
}

export function validateImportFile(file: File | null): QuestionImportIssue | null {
  if (!file) {
    return { code: 'frontend.file_required', message: 'File is required.' }
  }

  const extension = file.name.toLowerCase().slice(file.name.lastIndexOf('.'))
  if (extension !== '.docx' && extension !== '.pdf') {
    return { code: 'frontend.unsupported_file', message: 'Only DOCX and PDF files are supported.' }
  }

  if (file.size > questionImportLimits.maxFileSizeBytes) {
    return { code: 'frontend.file_too_large', message: 'File size cannot exceed 5 MB.' }
  }

  return null
}

function toImportQuestionType(value: number | undefined): QuestionTypeValue {
  if (value === QuestionTypeValue.ClosedAnswer) return QuestionTypeValue.ClosedAnswer
  return QuestionTypeValue.SingleChoice
}
