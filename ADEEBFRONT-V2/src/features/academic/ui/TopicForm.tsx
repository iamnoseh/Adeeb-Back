import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Save } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { subjectKeys, subjectsApi } from '@/features/academic/api/subjects.api'
import { topicKeys, topicsApi } from '@/features/academic/api/topics.api'
import { createTopicFormSchema } from '@/features/academic/model/academic.schema'
import type { TopicFormValues } from '@/features/academic/model/academic.types'
import { ApiError } from '@/shared/api/problem-details'
import { localizedName } from '@/shared/i18n/localized-content'
import { Button } from '@/shared/ui/Button'
import { FormField } from '@/shared/ui/FormField'
import { Input, Textarea } from '@/shared/ui/Input'
import { SelectField } from '@/shared/ui/SelectField'

type TopicFormProps = {
  topicId?: string | undefined
}

export function TopicForm({ topicId }: TopicFormProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { i18n, t } = useTranslation()
  const [formError, setFormError] = useState<string | null>(null)

  const subjectsQuery = useQuery({
    queryKey: subjectKeys.list({ pageSize: 100 }),
    queryFn: () => subjectsApi.list({ pageSize: 100 }),
  })
  const topicsQuery = useQuery({
    queryKey: topicId ? topicKeys.detail(topicId) : ['topics', 'new'],
    queryFn: () => topicsApi.detail(topicId ?? ''),
    enabled: Boolean(topicId),
  })

  const form = useForm<TopicFormValues, unknown, TopicFormValues>({
    resolver: zodResolver(createTopicFormSchema(t)) as unknown as Resolver<TopicFormValues>,
    defaultValues: {
      subjectId: '',
      displayOrder: 0,
      status: 1,
      nameTg: '',
      nameRu: '',
      nameEn: '',
      descriptionTg: '',
      descriptionRu: '',
      descriptionEn: '',
    },
  })

  useEffect(() => {
    const existingTopic = topicsQuery.data
    if (!existingTopic) return

    const byLanguage = new Map(existingTopic.translations.map((item) => [item.language, item]))
    form.reset({
      subjectId: existingTopic.subjectId,
      displayOrder: existingTopic.displayOrder,
      status: existingTopic.status,
      nameTg: byLanguage.get(0)?.name ?? '',
      nameRu: byLanguage.get(1)?.name ?? '',
      nameEn: byLanguage.get(2)?.name ?? '',
      descriptionTg: byLanguage.get(0)?.description ?? '',
      descriptionRu: byLanguage.get(1)?.description ?? '',
      descriptionEn: byLanguage.get(2)?.description ?? '',
    })
  }, [form, topicsQuery.data])

  const mutation = useMutation({
    mutationFn: (values: TopicFormValues) => (topicId ? topicsApi.update(topicId, values) : topicsApi.create(values)),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['topics'] })
      navigate('/admin/topics')
    },
    onError: (error: unknown) => {
      if (error instanceof ApiError) {
        setFormError(error.problem?.title ?? error.message)
        return
      }
      setFormError(t('saveFailed'))
    },
  })
  const subjectOptions = [
    ...(subjectsQuery.data?.items.map((subject) => ({
      value: subject.id,
      label: localizedName(subject.translations, i18n.language, subject.name),
    })) ?? []),
  ]

  return (
    <form className="app-surface mx-auto grid w-full max-w-5xl gap-6 rounded-[2rem] p-5 md:p-7" onSubmit={(event) => void form.handleSubmit((values: TopicFormValues) => mutation.mutate(values))(event)}>
      {formError ? <div className="rounded-2xl border border-red-100 bg-red-50 px-4 py-3 text-sm font-semibold text-[var(--danger)]">{formError}</div> : null}

      <section className="rounded-[1.5rem] bg-[var(--surface-soft)] p-5 ring-1 ring-[var(--border)]">
        <h2 className="text-base font-bold">{topicId ? t('editTopic') : t('newTopic')}</h2>
        <p className="mt-1 text-sm text-[var(--muted)]">{t('topicFormIntro')}</p>
      </section>

      <div className="grid gap-4">
        <FormField label={t('parentSubject')} error={form.formState.errors.subjectId?.message}>
          <SelectField
            value={form.watch('subjectId')}
            options={subjectOptions}
            placeholder={t('chooseSubject')}
            onValueChange={(value) => form.setValue('subjectId', value, { shouldDirty: true, shouldValidate: true })}
          />
        </FormField>
      </div>

      <section className="grid gap-4 md:grid-cols-2">
        <FormField label={t('topicNameTg')} error={form.formState.errors.nameTg?.message}>
          <Input {...form.register('nameTg')} />
        </FormField>
        <FormField label={t('topicNameRu')} error={form.formState.errors.nameRu?.message}>
          <Input {...form.register('nameRu')} />
        </FormField>
        <FormField label={t('descriptionTg')}>
          <Textarea {...form.register('descriptionTg')} />
        </FormField>
        <FormField label={t('descriptionRu')}>
          <Textarea {...form.register('descriptionRu')} />
        </FormField>
        <FormField label={t('topicNameEn')}>
          <Input {...form.register('nameEn')} />
        </FormField>
        <FormField label={t('descriptionEn')}>
          <Textarea {...form.register('descriptionEn')} />
        </FormField>
      </section>

      <input type="hidden" {...form.register('status', { valueAsNumber: true })} />
      <input type="hidden" {...form.register('displayOrder', { valueAsNumber: true })} />

      <div className="flex justify-end gap-2 border-t border-[var(--border)] pt-5">
        <Button type="button" variant="secondary" onClick={() => navigate('/admin/topics')}>
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
