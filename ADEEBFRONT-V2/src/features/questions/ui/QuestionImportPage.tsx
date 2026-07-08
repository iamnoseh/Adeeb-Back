import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, CheckCircle2, FileUp, Plus, Trash2, XCircle } from 'lucide-react'
import { type DragEvent, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { subjectKeys, subjectsApi } from '@/features/academic/api/subjects.api'
import { topicKeys, topicsApi } from '@/features/academic/api/topics.api'
import { questionsApi, questionKeys } from '@/features/questions/api/questions.api'
import {
  buildConfirmImportRequest,
  type EditableImportedQuestion,
  questionImportLimits,
  toEditableImportQuestions,
  validateImportedQuestion,
  validateImportFile,
} from '@/features/questions/model/question-import'
import { ApiError } from '@/shared/api/problem-details'
import { localizedName } from '@/shared/i18n/localized-content'
import { Badge } from '@/shared/ui/Badge'
import { Button } from '@/shared/ui/Button'
import { FormField } from '@/shared/ui/FormField'
import { Input, Textarea } from '@/shared/ui/Input'
import { SelectField } from '@/shared/ui/SelectField'

type PreviewFilter = 'all' | 'valid' | 'warnings' | 'invalid'

const text = {
  tg: {
    title: 'Ворид кардани саволҳо аз файл',
    description: 'DOCX ва PDF-и матнӣ дастгирӣ мешаванд. PDF-и сканшуда ҳоло дастгирӣ намешавад.',
    configure: 'Танзимот',
    preview: 'Пешнамоиш',
    subject: 'Фан',
    topic: 'Мавзуъ',
    noTopic: 'Бе мавзуъ',
    difficulty: 'Сатҳ',
    file: 'Файл',
    drop: 'Файлро ин ҷо гузоред ё интихоб кунед',
    supported: 'Танҳо .docx ва .pdf то 5 MB.',
    analyze: 'Таҳлил кардан',
    analyzing: 'Таҳлил...',
    confirm: 'Импорт кардан',
    confirming: 'Импорт...',
    removeFile: 'Гирифтан',
    valid: 'Дуруст',
    warnings: 'Огоҳӣ',
    invalid: 'Хато',
    all: 'Ҳама',
    remove: 'Нест кардан',
    addOption: 'Вариант илова кардан',
    questionText: 'Матни савол',
    option: 'Вариант',
    correct: 'Дуруст',
    back: 'Бозгашт',
    imported: 'савол ворид шуд',
    chooseSubject: 'Фанро интихоб кунед',
    chooseDifficulty: 'Сатҳро интихоб кунед',
    success: 'Импорт бомуваффақият анҷом шуд.',
  },
  ru: {
    title: 'Импорт вопросов из файла',
    description: 'Поддерживаются DOCX и текстовые PDF. Сканированные PDF пока не поддерживаются.',
    configure: 'Настройки',
    preview: 'Предпросмотр',
    subject: 'Предмет',
    topic: 'Тема',
    noTopic: 'Без темы',
    difficulty: 'Уровень',
    file: 'Файл',
    drop: 'Перетащите файл сюда или выберите его',
    supported: 'Только .docx и .pdf до 5 MB.',
    analyze: 'Анализировать файл',
    analyzing: 'Анализ...',
    confirm: 'Импортировать',
    confirming: 'Импорт...',
    removeFile: 'Убрать',
    valid: 'Валидные',
    warnings: 'Предупреждения',
    invalid: 'Ошибки',
    all: 'Все',
    remove: 'Удалить',
    addOption: 'Добавить вариант',
    questionText: 'Текст вопроса',
    option: 'Вариант',
    correct: 'Правильный',
    back: 'Назад',
    imported: 'вопросов импортировано',
    chooseSubject: 'Выберите предмет',
    chooseDifficulty: 'Выберите уровень',
    success: 'Импорт успешно завершен.',
  },
}

export function QuestionImportPage() {
  const { i18n, t } = useTranslation()
  const labels = i18n.language === 'ru-RU' ? text.ru : text.tg
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [subjectId, setSubjectId] = useState('')
  const [topicId, setTopicId] = useState('')
  const [difficulty, setDifficulty] = useState('1')
  const [file, setFile] = useState<File | null>(null)
  const [fileError, setFileError] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [questions, setQuestions] = useState<EditableImportedQuestion[]>([])
  const [filter, setFilter] = useState<PreviewFilter>('all')
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const subjectsQuery = useQuery({
    queryKey: subjectKeys.list({ pageSize: 100 }),
    queryFn: () => subjectsApi.list({ pageSize: 100 }),
  })
  const topicsQuery = useQuery({
    queryKey: subjectId ? topicKeys.bySubject(subjectId) : ['topics', 'empty'],
    queryFn: () => topicsApi.publicBySubject(subjectId),
    enabled: subjectId.length > 0,
  })

  const parseMutation = useMutation({
    mutationFn: questionsApi.parseImport,
    onSuccess: (preview) => {
      setQuestions(toEditableImportQuestions(preview))
      setFilter('all')
      setFormError(null)
      setSuccessMessage(null)
    },
    onError: (error: unknown) => setFormError(toImportErrorMessage(error, labels.description)),
  })

  const confirmMutation = useMutation({
    mutationFn: questionsApi.confirmImport,
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: ['questions'] })
      await queryClient.invalidateQueries({ queryKey: questionKeys.list({}) })
      setSuccessMessage(`${result.importedCount} ${labels.imported}`)
    },
    onError: (error: unknown) => setFormError(toImportErrorMessage(error, labels.description)),
  })

  const subjectOptions = subjectsQuery.data?.items.map((subject) => ({
    value: subject.id,
    label: localizedName(subject.translations, i18n.language, subject.name),
  })) ?? []
  const topicOptions = [
    { value: '', label: labels.noTopic },
    ...(topicsQuery.data?.items.map((topic) => ({
      value: topic.id,
      label: localizedName(topic.translations, i18n.language, topic.name),
    })) ?? []),
  ]
  const difficultyOptions = [
    { value: '1', label: t('difficultyEasy') },
    { value: '2', label: t('difficultyMedium') },
    { value: '3', label: t('difficultyHard') },
  ]

  const activeQuestions = questions.filter((question) => !question.removed)
  const validationByKey = new Map(activeQuestions.map((question) => [question.key, validateImportedQuestion(question)]))
  const summary = {
    total: activeQuestions.length,
    valid: activeQuestions.filter((question) => validationByKey.get(question.key)?.isValid).length,
    warnings: activeQuestions.filter((question) => (validationByKey.get(question.key)?.warnings.length ?? 0) > 0).length,
    invalid: activeQuestions.filter((question) => !(validationByKey.get(question.key)?.isValid)).length,
  }
  const visibleQuestions = activeQuestions.filter((question) => {
    const validation = validationByKey.get(question.key)
    if (filter === 'valid') return validation?.isValid
    if (filter === 'warnings') return (validation?.warnings.length ?? 0) > 0
    if (filter === 'invalid') return !validation?.isValid
    return true
  })
  const canParse = subjectId.length > 0 && difficulty.length > 0 && file !== null && fileError === null && !parseMutation.isPending
  const canConfirm = activeQuestions.length > 0 && summary.invalid === 0 && !confirmMutation.isPending

  function chooseFile(nextFile: File | null) {
    const validation = validateImportFile(nextFile)
    setFile(nextFile)
    setFileError(validation?.message ?? null)
  }

  function onDrop(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault()
    chooseFile(event.dataTransfer.files.item(0))
  }

  function parse() {
    if (!file || !canParse) return
    parseMutation.mutate({ subjectId, topicId: topicId || null, difficulty: Number(difficulty), file })
  }

  function confirm() {
    if (!canConfirm) return
    const subject = subjectOptions.find((item) => item.value === subjectId)?.label ?? ''
    const proceed = window.confirm(`${labels.confirm}: ${summary.total}\n${labels.subject}: ${subject}`)
    if (!proceed) return
    confirmMutation.mutate(buildConfirmImportRequest(subjectId, topicId || null, Number(difficulty), activeQuestions))
  }

  function updateQuestion(key: string, update: (question: EditableImportedQuestion) => EditableImportedQuestion) {
    setQuestions((current) => current.map((question) => (question.key === key ? update(question) : question)))
  }

  const selectedFileSize = file ? `${(file.size / 1024 / 1024).toFixed(2)} MB` : ''

  return (
    <div className="grid gap-5">
      <section className="app-surface mx-auto grid w-full max-w-6xl gap-5 rounded-[2rem] p-5 md:p-7">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-xl font-black">{labels.configure}</h2>
            <p className="mt-1 text-sm text-[var(--muted)]">{labels.description}</p>
          </div>
          <Button type="button" variant="secondary" onClick={() => navigate('/admin/questions')}>
            {labels.back}
          </Button>
        </div>

        {formError ? <IssuePanel tone="danger" message={formError} /> : null}
        {successMessage ? <IssuePanel tone="success" message={`${successMessage}. ${labels.success}`} /> : null}

        <div className="grid gap-4 md:grid-cols-3">
          <FormField label={labels.subject}>
            <SelectField
              value={subjectId}
              options={subjectOptions}
              placeholder={labels.chooseSubject}
              onValueChange={(value) => {
                setSubjectId(value)
                setTopicId('')
              }}
            />
          </FormField>
          <FormField label={labels.topic}>
            <SelectField value={topicId} options={topicOptions} disabled={!subjectId} onValueChange={setTopicId} />
          </FormField>
          <FormField label={labels.difficulty}>
            <SelectField value={difficulty} options={difficultyOptions} placeholder={labels.chooseDifficulty} onValueChange={setDifficulty} />
          </FormField>
        </div>

        <div className="grid gap-3">
          <span className="px-1 text-sm font-semibold">{labels.file}</span>
          <label
            className="grid cursor-pointer place-items-center rounded-[1.5rem] border-2 border-dashed border-[var(--border)] bg-[var(--surface-soft)] p-8 text-center transition hover:border-[var(--primary)]"
            onDragOver={(event) => event.preventDefault()}
            onDrop={onDrop}
          >
            <FileUp className="mb-3 h-8 w-8 text-[var(--primary)]" aria-hidden />
            <span className="font-bold">{file ? file.name : labels.drop}</span>
            <span className="mt-1 text-sm text-[var(--muted)]">{file ? selectedFileSize : labels.supported}</span>
            <input
              className="sr-only"
              type="file"
              accept=".docx,.pdf,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
              onChange={(event) => chooseFile(event.target.files?.item(0) ?? null)}
            />
          </label>
          {fileError ? <p className="text-sm font-semibold text-[var(--danger)]">{fileError}</p> : null}
          {file ? (
            <Button type="button" variant="ghost" className="justify-self-start" onClick={() => chooseFile(null)}>
              {labels.removeFile}
            </Button>
          ) : null}
        </div>

        <div className="flex justify-end">
          <Button type="button" disabled={!canParse} onClick={parse}>
            <FileUp className="h-4 w-4" aria-hidden />
            {parseMutation.isPending ? labels.analyzing : labels.analyze}
          </Button>
        </div>
      </section>

      {questions.length > 0 ? (
        <section className="mx-auto grid w-full max-w-6xl gap-4">
          <ImportSummary summary={summary} filter={filter} setFilter={setFilter} labels={labels} />
          {visibleQuestions.map((question, index) => (
            <ImportedQuestionCard
              key={question.key}
              index={index + 1}
              question={question}
              labels={labels}
              validation={validationByKey.get(question.key) ?? validateImportedQuestion(question)}
              updateQuestion={updateQuestion}
            />
          ))}
          <div className="sticky bottom-4 flex justify-end rounded-[1.5rem] bg-white/90 p-3 shadow-[var(--shadow)] ring-1 ring-[var(--border)] backdrop-blur">
            <Button type="button" disabled={!canConfirm} onClick={confirm}>
              <CheckCircle2 className="h-4 w-4" aria-hidden />
              {confirmMutation.isPending ? labels.confirming : `${labels.confirm} (${summary.total})`}
            </Button>
          </div>
        </section>
      ) : null}
    </div>
  )
}

function ImportSummary({
  summary,
  filter,
  setFilter,
  labels,
}: {
  summary: { total: number; valid: number; warnings: number; invalid: number }
  filter: PreviewFilter
  setFilter: (filter: PreviewFilter) => void
  labels: typeof text.tg
}) {
  const filters: { key: PreviewFilter; label: string; count: number }[] = [
    { key: 'all', label: labels.all, count: summary.total },
    { key: 'valid', label: labels.valid, count: summary.valid },
    { key: 'warnings', label: labels.warnings, count: summary.warnings },
    { key: 'invalid', label: labels.invalid, count: summary.invalid },
  ]

  return (
    <div className="app-surface grid gap-4 rounded-[2rem] p-5">
      <div>
        <h2 className="text-xl font-black">{labels.preview}</h2>
        <p className="text-sm text-[var(--muted)]">{summary.total} questions detected</p>
      </div>
      <div className="flex flex-wrap gap-2">
        {filters.map((item) => (
          <button
            key={item.key}
            type="button"
            className={`rounded-2xl px-4 py-2 text-sm font-black transition ${filter === item.key ? 'bg-[var(--primary)] text-white' : 'bg-[var(--surface-muted)] text-[var(--text)]'}`}
            onClick={() => setFilter(item.key)}
          >
            {item.label}: {item.count}
          </button>
        ))}
      </div>
    </div>
  )
}

function ImportedQuestionCard({
  index,
  question,
  labels,
  validation,
  updateQuestion,
}: {
  index: number
  question: EditableImportedQuestion
  labels: typeof text.tg
  validation: ReturnType<typeof validateImportedQuestion>
  updateQuestion: (key: string, update: (question: EditableImportedQuestion) => EditableImportedQuestion) => void
}) {
  const statusTone = validation.isValid ? (validation.warnings.length > 0 ? 'warning' : 'success') : 'danger'
  const statusLabel = validation.isValid ? (validation.warnings.length > 0 ? labels.warnings : labels.valid) : labels.invalid

  return (
    <article className="app-surface grid gap-4 rounded-[2rem] p-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <span className="grid h-10 w-10 place-items-center rounded-2xl bg-[var(--surface-muted)] font-black">{String(index).padStart(2, '0')}</span>
          <Badge tone={statusTone}>{statusLabel}</Badge>
        </div>
        <Button
          type="button"
          variant="danger"
          onClick={() => updateQuestion(question.key, (current) => ({ ...current, removed: true }))}
        >
          <Trash2 className="h-4 w-4" aria-hidden />
          {labels.remove}
        </Button>
      </div>

      <FormField label={labels.questionText}>
        <Textarea
          value={question.questionText}
          onChange={(event) => updateQuestion(question.key, (current) => ({ ...current, questionText: event.target.value }))}
        />
      </FormField>

      <div className="grid gap-3">
        {question.options.map((option) => (
          <div key={option.key} className="grid gap-3 rounded-2xl border border-[var(--border)] bg-[var(--surface-soft)] p-3 md:grid-cols-[110px_1fr_auto]">
            <label className="inline-flex items-center gap-2 rounded-xl bg-white px-3 py-2 text-sm font-black shadow-sm">
              <input
                type="radio"
                name={`correct-${question.key}`}
                checked={option.isCorrect}
                onChange={() => updateQuestion(question.key, (current) => ({
                  ...current,
                  options: current.options.map((item) => ({ ...item, isCorrect: item.key === option.key })),
                }))}
              />
              {labels.correct}
            </label>
            <Input
              value={option.text}
              onChange={(event) => updateQuestion(question.key, (current) => ({
                ...current,
                options: current.options.map((item) => (item.key === option.key ? { ...item, text: event.target.value } : item)),
              }))}
            />
            <Button
              type="button"
              variant="ghost"
              disabled={question.options.length <= 2}
              onClick={() => updateQuestion(question.key, (current) => ({
                ...current,
                options: current.options.filter((item) => item.key !== option.key),
              }))}
            >
              <Trash2 className="h-4 w-4" aria-hidden />
            </Button>
          </div>
        ))}
        <Button
          type="button"
          variant="secondary"
          disabled={question.options.length >= questionImportLimits.maxOptionsPerQuestion}
          onClick={() => updateQuestion(question.key, (current) => ({
            ...current,
            options: [
              ...current.options,
              {
                key: `${current.key}-new-${Date.now()}`,
                label: String.fromCharCode(65 + current.options.length),
                text: '',
                isCorrect: false,
              },
            ],
          }))}
        >
          <Plus className="h-4 w-4" aria-hidden />
          {labels.addOption}
        </Button>
      </div>

      {validation.errors.map((issue) => <IssuePanel key={issue.code + issue.message} tone="danger" message={issue.message} />)}
      {validation.warnings.map((issue) => <IssuePanel key={issue.code + issue.message} tone="warning" message={issue.message} />)}
    </article>
  )
}

function IssuePanel({ tone, message }: { tone: 'danger' | 'warning' | 'success'; message: string }) {
  const icon = tone === 'success' ? <CheckCircle2 className="h-4 w-4" /> : tone === 'warning' ? <AlertTriangle className="h-4 w-4" /> : <XCircle className="h-4 w-4" />
  const className = tone === 'success'
    ? 'border-emerald-100 bg-emerald-50 text-[var(--success)]'
    : tone === 'warning'
      ? 'border-amber-100 bg-amber-50 text-[var(--warning)]'
      : 'border-red-100 bg-red-50 text-[var(--danger)]'

  return (
    <div className={`flex items-start gap-2 rounded-2xl border px-4 py-3 text-sm font-semibold ${className}`}>
      {icon}
      <span>{message}</span>
    </div>
  )
}

function toImportErrorMessage(error: unknown, fallback: string) {
  if (!(error instanceof ApiError)) return fallback
  const code = error.problem?.code
  if (code === 'question_import.no_extractable_text') {
    return 'Text could not be extracted from this PDF. The file may be scanned or image-based.'
  }

  const validationError = error.problem?.errors ? Object.values(error.problem.errors).flat()[0] : undefined
  return validationError?.message ?? error.problem?.title ?? error.message
}
