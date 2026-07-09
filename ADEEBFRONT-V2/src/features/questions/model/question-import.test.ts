import { describe, expect, it } from 'vitest'
import {
  buildConfirmImportRequest,
  type EditableImportedQuestion,
  toEditableImportQuestions,
  validateImportedQuestion,
  validateImportFile,
} from '@/features/questions/model/question-import'
import { QuestionTypeValue } from '@/features/questions/model/question.types'

describe('question import model', () => {
  it('maps preview questions to editable local state', () => {
    const editable = toEditableImportQuestions({
      fileName: 'questions.docx',
      summary: { totalDetected: 1, valid: 1, invalid: 0, warnings: 0 },
      questions: [
        {
          clientKey: 'q-1',
          questionType: QuestionTypeValue.SingleChoice,
          questionTypeName: 'SingleChoice',
          questionText: 'Question?',
          expectedAnswer: null,
          isValid: true,
          errors: [],
          warnings: [],
          options: [
            { label: 'A', text: 'Correct', isCorrect: true },
            { label: 'B', text: 'Wrong', isCorrect: false },
          ],
        },
      ],
    })

    expect(editable).toHaveLength(1)
    expect(editable[0]!.key).toBe('q-1')
    expect(editable[0]!.options[0]!.isCorrect).toBe(true)
  })

  it('maps closed answer preview and preserves expected answer text', () => {
    const editable = toEditableImportQuestions({
      fileName: 'questions.docx',
      summary: { totalDetected: 1, valid: 1, invalid: 0, warnings: 0 },
      questions: [
        {
          clientKey: 'q-1',
          questionType: QuestionTypeValue.ClosedAnswer,
          questionTypeName: 'ClosedAnswer',
          questionText: '2 + 5 = ?',
          expectedAnswer: '7',
          isValid: true,
          errors: [],
          warnings: [],
          options: [],
        },
      ],
    })

    expect(editable[0]!.questionType).toBe(QuestionTypeValue.ClosedAnswer)
    expect(editable[0]!.expectedAnswer).toBe('7')
    expect(editable[0]!.options).toEqual([])
  })

  it('validates exactly one correct answer', () => {
    const question = editableQuestion({
      options: [
        { key: 'a', label: 'A', text: 'One', isCorrect: true },
        { key: 'b', label: 'B', text: 'Two', isCorrect: true },
      ],
    })

    const result = validateImportedQuestion(question)

    expect(result.isValid).toBe(false)
    expect(result.errors.some((error) => error.code === 'frontend.one_correct_required')).toBe(true)
  })

  it('builds confirm request without parser metadata', () => {
    const request = buildConfirmImportRequest('subject', null, 2, [
      editableQuestion({
        serverErrors: [{ code: 'server.old', message: 'old' }],
        serverWarnings: [{ code: 'server.warning', message: 'warning' }],
      }),
    ])

    expect(request).toEqual({
      subjectId: 'subject',
      topicId: null,
      difficulty: 2,
      questions: [
        {
          questionType: QuestionTypeValue.SingleChoice,
          questionText: 'Question?',
          expectedAnswer: null,
          options: [
            { text: 'Correct', isCorrect: true },
            { text: 'Wrong', isCorrect: false },
          ],
        },
      ],
    })
  })

  it('builds closed answer confirm request without fake options', () => {
    const request = buildConfirmImportRequest('subject', null, 1, [
      editableQuestion({
        questionType: QuestionTypeValue.ClosedAnswer,
        questionTypeName: 'ClosedAnswer',
        expectedAnswer: '-7',
        options: [],
      }),
    ])

    expect(request.questions).toEqual([
      {
        questionType: QuestionTypeValue.ClosedAnswer,
        questionText: 'Question?',
        expectedAnswer: '-7',
        options: [],
      },
    ])
  })

  it('builds mixed confirm request with single choice and closed answer', () => {
    const request = buildConfirmImportRequest('subject', 'topic', 2, [
      editableQuestion(),
      editableQuestion({
        key: 'q-2',
        questionType: QuestionTypeValue.ClosedAnswer,
        questionTypeName: 'ClosedAnswer',
        questionText: 'Half?',
        expectedAnswer: '1/2',
        options: [],
      }),
    ])

    expect(request.questions).toHaveLength(2)
    expect(request.questions[0]!.questionType).toBe(QuestionTypeValue.SingleChoice)
    expect(request.questions[1]).toMatchObject({
      questionType: QuestionTypeValue.ClosedAnswer,
      expectedAnswer: '1/2',
      options: [],
    })
  })

  it('validates closed answer expected answer locally', () => {
    const invalid = validateImportedQuestion(editableQuestion({
      questionType: QuestionTypeValue.ClosedAnswer,
      questionTypeName: 'ClosedAnswer',
      expectedAnswer: '   ',
      options: [],
    }))
    const valid = validateImportedQuestion(editableQuestion({
      questionType: QuestionTypeValue.ClosedAnswer,
      questionTypeName: 'ClosedAnswer',
      expectedAnswer: 'Душанбе',
      options: [],
    }))

    expect(invalid.isValid).toBe(false)
    expect(invalid.errors.some((error) => error.code === 'frontend.expected_answer_required')).toBe(true)
    expect(valid.isValid).toBe(true)
  })

  it('does not include removed questions in confirm request', () => {
    const request = buildConfirmImportRequest('subject', 'topic', 1, [
      editableQuestion({ removed: true }),
      editableQuestion({ key: 'q-2', questionText: 'Second?' }),
    ])

    expect(request.questions).toHaveLength(1)
    expect(request.questions[0]!.questionText).toBe('Second?')
  })

  it('rejects unsupported and too-large files', () => {
    expect(validateImportFile(new File(['x'], 'questions.txt', { type: 'text/plain' }))?.code).toBe('frontend.unsupported_file')
    expect(validateImportFile(new File([new Uint8Array(6 * 1024 * 1024)], 'questions.pdf', { type: 'application/pdf' }))?.code).toBe('frontend.file_too_large')
  })
})

function editableQuestion(overrides: Partial<EditableImportedQuestion> = {}): EditableImportedQuestion {
  return {
    key: 'q-1',
    questionType: QuestionTypeValue.SingleChoice,
    questionTypeName: 'SingleChoice',
    questionText: 'Question?',
    expectedAnswer: '',
    options: [
      { key: 'a', label: 'A', text: 'Correct', isCorrect: true },
      { key: 'b', label: 'B', text: 'Wrong', isCorrect: false },
    ],
    serverErrors: [],
    serverWarnings: [],
    removed: false,
    ...overrides,
  }
}
