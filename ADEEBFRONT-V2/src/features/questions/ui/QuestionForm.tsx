import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Save } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver, type UseFormReturn } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { subjectKeys, subjectsApi } from '@/features/academic/api/subjects.api'
import { topicKeys, topicsApi } from '@/features/academic/api/topics.api'
import { questionKeys, questionsApi } from '@/features/questions/api/questions.api'
import { createQuestionFormSchema } from '@/features/questions/model/question.schema'
import type { QuestionFormValues } from '@/features/questions/model/question.types'
import { ApiError } from '@/shared/api/problem-details'
import { localizedName } from '@/shared/i18n/localized-content'
import { Button } from '@/shared/ui/Button'
import { FormField } from '@/shared/ui/FormField'
import { Input, Textarea } from '@/shared/ui/Input'
import { SelectField } from '@/shared/ui/SelectField'

type QuestionFormProps = {
  questionId?: string | undefined
}

const defaultAnswers = [
  { textTg: '', textRu: '', isCorrect: true },
  { textTg: '', textRu: '', isCorrect: false },
  { textTg: '', textRu: '', isCorrect: false },
  { textTg: '', textRu: '', isCorrect: false },
]

const defaultMatchingPairs = [
  { textTg: '', textRu: '', matchPairTg: '', matchPairRu: '' },
  { textTg: '', textRu: '', matchPairTg: '', matchPairRu: '' },
  { textTg: '', textRu: '', matchPairTg: '', matchPairRu: '' },
  { textTg: '', textRu: '', matchPairTg: '', matchPairRu: '' },
]

export function QuestionForm({ questionId }: QuestionFormProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { i18n, t } = useTranslation()
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<QuestionFormValues, unknown, QuestionFormValues>({
    resolver: zodResolver(createQuestionFormSchema(t)) as unknown as Resolver<QuestionFormValues>,
    defaultValues: {
      subjectId: '',
      topicId: '',
      contentTg: '',
      contentRu: '',
      explanationTg: '',
      explanationRu: '',
      type: 1,
      difficulty: 1,
      status: 1,
      answers: defaultAnswers,
      matchingPairs: defaultMatchingPairs,
      correctAnswerTg: '',
      correctAnswerRu: '',
    },
  })

  const selectedSubjectId = form.watch('subjectId')
  const selectedType = form.watch('type')

  const subjectsQuery = useQuery({
    queryKey: subjectKeys.publicList({ pageSize: 50 }),
    queryFn: () => subjectsApi.publicList({ pageSize: 50 }),
  })
  const topicsQuery = useQuery({
    queryKey: selectedSubjectId ? topicKeys.bySubject(selectedSubjectId) : ['topics', 'empty'],
    queryFn: () => topicsApi.publicBySubject(selectedSubjectId),
    enabled: selectedSubjectId.length > 0,
  })
  const questionQuery = useQuery({
    queryKey: questionId ? questionKeys.detail(questionId) : ['questions', 'new'],
    queryFn: () => questionsApi.detail(questionId ?? ''),
    enabled: Boolean(questionId),
  })

  useEffect(() => {
    const question = questionQuery.data
    if (!question) return

    const sortedOptions = [...question.answerOptions].sort((a, b) => a.displayOrder - b.displayOrder)
    const asAnswer = sortedOptions.map((option) => ({
      textTg: optionTranslation(option.translations, 0)?.text ?? '',
      textRu: optionTranslation(option.translations, 1)?.text ?? '',
      isCorrect: option.isCorrect,
    }))
    const asPairs = sortedOptions.map((option) => ({
      textTg: optionTranslation(option.translations, 0)?.text ?? '',
      textRu: optionTranslation(option.translations, 1)?.text ?? '',
      matchPairTg: optionTranslation(option.translations, 0)?.matchPairText ?? '',
      matchPairRu: optionTranslation(option.translations, 1)?.matchPairText ?? '',
    }))
    const tajik = question.translations.find((translation) => translation.language === 0)
    const russian = question.translations.find((translation) => translation.language === 1)
    const closedOption = sortedOptions[0]

    form.reset({
      subjectId: question.subjectId,
      topicId: question.topicId ?? '',
      contentTg: tajik?.content ?? '',
      contentRu: russian?.content ?? '',
      explanationTg: tajik?.explanation ?? '',
      explanationRu: russian?.explanation ?? '',
      type: question.type,
      difficulty: question.difficulty,
      status: question.status,
      answers: padAnswers(asAnswer),
      matchingPairs: padPairs(asPairs),
      correctAnswerTg: question.type === 3 ? optionTranslation(closedOption?.translations, 0)?.text ?? '' : '',
      correctAnswerRu: question.type === 3 ? optionTranslation(closedOption?.translations, 1)?.text ?? '' : '',
    })
  }, [form, questionQuery.data])

  const mutation = useMutation({
    mutationFn: (values: QuestionFormValues) => (questionId ? questionsApi.update(questionId, values) : questionsApi.create(values)),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['questions'] })
      navigate('/admin/questions')
    },
    onError: (error: unknown) => {
      if (error instanceof ApiError) {
        setFormError(error.problem?.title ?? error.message)
        return
      }
      setFormError(t('questionSaveFailed'))
    },
  })
  const subjectOptions = subjectsQuery.data?.items.map((subject) => ({
    value: subject.id,
    label: localizedName(subject.translations, i18n.language, subject.name),
  })) ?? []
  const topicOptions = topicsQuery.data?.items.map((topic) => ({
    value: topic.id,
    label: localizedName(topic.translations, i18n.language, topic.name),
  })) ?? []
  const typeOptions = [
    { value: '1', label: t('typeSingleChoice') },
    { value: '2', label: t('typeMatching') },
    { value: '3', label: t('typeClosedAnswer') },
  ]
  const difficultyOptions = [
    { value: '1', label: t('difficultyEasy') },
    { value: '2', label: t('difficultyMedium') },
    { value: '3', label: t('difficultyHard') },
  ]
  const statusOptions = [
    { value: '0', label: t('statusDraft') },
    { value: '1', label: t('statusActive') },
    { value: '2', label: t('statusArchived') },
  ]

  return (
    <form className="app-surface mx-auto grid w-full max-w-6xl gap-6 rounded-[2rem] p-5 md:p-7" onSubmit={(event) => void form.handleSubmit((values: QuestionFormValues) => mutation.mutate(values))(event)}>
      {formError ? <div className="rounded-2xl border border-red-100 bg-red-50 px-4 py-3 text-sm font-semibold text-[var(--danger)]">{formError}</div> : null}

      <section className="grid gap-4 rounded-[1.5rem] bg-[var(--surface-soft)] p-4 ring-1 ring-[var(--border)] md:grid-cols-3">
        <FormField label={t('parentSubject')} error={form.formState.errors.subjectId?.message}>
          <SelectField
            value={selectedSubjectId}
            options={subjectOptions}
            placeholder={t('chooseSubject')}
            onValueChange={(value) => {
              form.setValue('subjectId', value, { shouldDirty: true, shouldValidate: true })
              form.setValue('topicId', '', { shouldDirty: true, shouldValidate: true })
            }}
          />
        </FormField>
        <FormField label={t('topic')}>
          <SelectField
            value={form.watch('topicId')}
            options={[{ value: '', label: t('noTopic') }, ...topicOptions]}
            placeholder={selectedSubjectId ? t('noTopic') : t('selectSubjectFirst')}
            disabled={!selectedSubjectId}
            onValueChange={(value) => form.setValue('topicId', value, { shouldDirty: true, shouldValidate: true })}
          />
        </FormField>
        <FormField label={t('image')}>
          <Input type="file" accept="image/png,image/jpeg,image/jpg" {...form.register('image')} />
        </FormField>
      </section>

      <section className="grid gap-4 rounded-[1.5rem] bg-[var(--surface-soft)] p-4 ring-1 ring-[var(--border)] md:grid-cols-3">
        <FormField label={t('type')} error={form.formState.errors.type?.message}>
          <SelectField
            value={String(form.watch('type'))}
            options={typeOptions}
            onValueChange={(value) => form.setValue('type', Number(value), { shouldDirty: true, shouldValidate: true })}
          />
        </FormField>
        <FormField label={t('difficulty')} error={form.formState.errors.difficulty?.message}>
          <SelectField
            value={String(form.watch('difficulty'))}
            options={difficultyOptions}
            onValueChange={(value) => form.setValue('difficulty', Number(value), { shouldDirty: true, shouldValidate: true })}
          />
        </FormField>
        <FormField label={t('status')} error={form.formState.errors.status?.message}>
          <SelectField
            value={String(form.watch('status'))}
            options={statusOptions}
            onValueChange={(value) => form.setValue('status', Number(value), { shouldDirty: true, shouldValidate: true })}
          />
        </FormField>
      </section>

      <BilingualSection title={t('questionText')}>
        <FormField label={t('tajikLanguage')} error={form.formState.errors.contentTg?.message}>
          <Textarea rows={5} {...form.register('contentTg')} />
        </FormField>
        <FormField label={t('russianLanguage')} error={form.formState.errors.contentRu?.message}>
          <Textarea rows={5} {...form.register('contentRu')} />
        </FormField>
      </BilingualSection>

      {selectedType === 1 ? <SingleChoiceEditor form={form} /> : null}
      {selectedType === 2 ? <MatchingEditor form={form} /> : null}
      {selectedType === 3 ? (
        <BilingualSection title={t('correctAnswer')}>
          <FormField label={t('tajikLanguage')} error={form.formState.errors.correctAnswerTg?.message}>
            <Input {...form.register('correctAnswerTg')} />
          </FormField>
          <FormField label={t('russianLanguage')} error={form.formState.errors.correctAnswerRu?.message}>
            <Input {...form.register('correctAnswerRu')} />
          </FormField>
        </BilingualSection>
      ) : null}

      <BilingualSection title={t('explanation')}>
        <FormField label={t('tajikLanguage')}>
          <Textarea rows={4} {...form.register('explanationTg')} />
        </FormField>
        <FormField label={t('russianLanguage')}>
          <Textarea rows={4} {...form.register('explanationRu')} />
        </FormField>
      </BilingualSection>

      <div className="flex justify-end gap-2 border-t border-[var(--border)] pt-5">
        <Button type="button" variant="secondary" onClick={() => navigate('/admin/questions')}>
          {t('cancel')}
        </Button>
        <Button type="submit" disabled={mutation.isPending}>
          <Save className="h-4 w-4" aria-hidden />
          {mutation.isPending ? t('saving') : t('save')}
        </Button>
      </div>
    </form>
  )
}

type QuestionFormInnerProps = {
  form: UseFormReturn<QuestionFormValues, unknown, QuestionFormValues>
}

function SingleChoiceEditor({ form }: QuestionFormInnerProps) {
  const { t } = useTranslation()
  const answersError = form.formState.errors.answers?.message

  return (
    <section className="grid gap-3">
      <div>
        <h2 className="text-base font-bold">{t('typeSingleChoice')}</h2>
        <p className="text-sm text-[var(--muted)]">{t('singleChoiceHint')}</p>
      </div>
      {answersError ? <p className="text-sm font-semibold text-[var(--danger)]">{answersError}</p> : null}
      {[0, 1, 2, 3].map((index) => (
        <div key={index} className="grid gap-3 rounded-2xl border border-[var(--border)] bg-[var(--surface-soft)] p-3 md:grid-cols-[74px_minmax(0,1fr)_minmax(0,1fr)]">
          <label className="inline-flex items-center gap-2 rounded-xl bg-white px-3 py-2 text-sm font-black shadow-sm">
            <input
              type="radio"
              checked={form.watch(`answers.${index}.isCorrect`)}
              onChange={() => {
                for (const optionIndex of [0, 1, 2, 3]) {
                  form.setValue(`answers.${optionIndex}.isCorrect`, optionIndex === index, { shouldValidate: true })
                }
              }}
            />
            {String.fromCharCode(65 + index)}
          </label>
          <FormField label={t('tajikLanguage')}>
            <Input placeholder={`${t('option')} ${String.fromCharCode(65 + index)}`} {...form.register(`answers.${index}.textTg`)} />
          </FormField>
          <FormField label={t('russianLanguage')}>
            <Input placeholder={`${t('option')} ${String.fromCharCode(65 + index)}`} {...form.register(`answers.${index}.textRu`)} />
          </FormField>
        </div>
      ))}
    </section>
  )
}

function MatchingEditor({ form }: QuestionFormInnerProps) {
  const { t } = useTranslation()
  const matchingError = form.formState.errors.matchingPairs?.message

  return (
    <section className="grid gap-3">
      <div>
        <h2 className="text-base font-bold">{t('typeMatching')}</h2>
        <p className="text-sm text-[var(--muted)]">{t('matchingHint')}</p>
      </div>
      {matchingError ? <p className="text-sm font-semibold text-[var(--danger)]">{matchingError}</p> : null}
      {[0, 1, 2, 3].map((index) => (
        <div key={index} className="grid gap-3 rounded-2xl border border-[var(--border)] bg-[var(--surface-soft)] p-3 lg:grid-cols-2">
          <fieldset className="grid gap-2 rounded-xl border border-[var(--border)] bg-white p-3">
            <legend className="px-1 text-xs font-black text-[var(--muted)]">{t('tajikLanguage')}</legend>
            <Input placeholder={`${t('left')} ${index + 1}`} {...form.register(`matchingPairs.${index}.textTg`)} />
            <Input placeholder={`${t('right')} ${index + 1}`} {...form.register(`matchingPairs.${index}.matchPairTg`)} />
          </fieldset>
          <fieldset className="grid gap-2 rounded-xl border border-[var(--border)] bg-white p-3">
            <legend className="px-1 text-xs font-black text-[var(--muted)]">{t('russianLanguage')}</legend>
            <Input placeholder={`${t('left')} ${index + 1}`} {...form.register(`matchingPairs.${index}.textRu`)} />
            <Input placeholder={`${t('right')} ${index + 1}`} {...form.register(`matchingPairs.${index}.matchPairRu`)} />
          </fieldset>
        </div>
      ))}
    </section>
  )
}

function BilingualSection({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="grid gap-3">
      <h2 className="text-base font-bold">{title}</h2>
      <div className="grid gap-4 md:grid-cols-2">{children}</div>
    </section>
  )
}

function optionTranslation<T extends { language: number }>(translations: T[] | undefined, language: number) {
  return translations?.find((translation) => translation.language === language)
}

function padAnswers(values: { textTg: string; textRu: string; isCorrect: boolean }[]) {
  return [...values, ...defaultAnswers].slice(0, 4)
}

function padPairs(values: { textTg: string; textRu: string; matchPairTg: string; matchPairRu: string }[]) {
  return [...values, ...defaultMatchingPairs].slice(0, 4)
}
