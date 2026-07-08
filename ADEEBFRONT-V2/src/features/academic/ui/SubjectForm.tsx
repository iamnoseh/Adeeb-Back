import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Save } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useForm, type Resolver } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'
import { subjectKeys, subjectsApi } from '@/features/academic/api/subjects.api'
import { createSubjectFormSchema } from '@/features/academic/model/academic.schema'
import type { SubjectFormValues } from '@/features/academic/model/academic.types'
import { ApiError } from '@/shared/api/problem-details'
import { Button } from '@/shared/ui/Button'
import { FormField } from '@/shared/ui/FormField'
import { Input, Textarea } from '@/shared/ui/Input'

type SubjectFormProps = {
  subjectId?: string | undefined
}

export function SubjectForm({ subjectId }: SubjectFormProps) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { t } = useTranslation()
  const [formError, setFormError] = useState<string | null>(null)
  const isEdit = Boolean(subjectId)

  const subjectQuery = useQuery({
    queryKey: subjectId ? subjectKeys.detail(subjectId) : ['subjects', 'new'],
    queryFn: () => subjectsApi.detail(subjectId ?? ''),
    enabled: isEdit,
  })

  const form = useForm<SubjectFormValues, unknown, SubjectFormValues>({
    resolver: zodResolver(createSubjectFormSchema(t)) as unknown as Resolver<SubjectFormValues>,
    defaultValues: {
      nameTg: '',
      nameRu: '',
      nameEn: '',
      descriptionTg: '',
      descriptionRu: '',
      descriptionEn: '',
      status: 1,
      displayOrder: 0,
    },
  })

  useEffect(() => {
    if (subjectQuery.data) {
      const byLanguage = new Map(subjectQuery.data.translations.map((item) => [item.language, item]))
      form.reset({
        nameTg: byLanguage.get(1)?.name ?? '',
        nameRu: byLanguage.get(2)?.name ?? '',
        nameEn: byLanguage.get(3)?.name ?? '',
        descriptionTg: byLanguage.get(1)?.description ?? '',
        descriptionRu: byLanguage.get(2)?.description ?? '',
        descriptionEn: byLanguage.get(3)?.description ?? '',
        status: subjectQuery.data.status,
        displayOrder: subjectQuery.data.displayOrder,
      })
    }
  }, [form, subjectQuery.data])

  const mutation = useMutation({
    mutationFn: (values: SubjectFormValues) => (subjectId ? subjectsApi.update(subjectId, values) : subjectsApi.create(values)),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['subjects'] })
      navigate('/admin/subjects')
    },
    onError: (error: unknown) => {
      if (error instanceof ApiError) {
        setFormError(error.problem?.title ?? error.message)
        return
      }
      setFormError(t('saveFailed'))
    },
  })

  return (
    <form className="app-surface grid max-w-4xl gap-6 rounded-xl p-5 shadow-sm" onSubmit={(event) => void form.handleSubmit((values: SubjectFormValues) => mutation.mutate(values))(event)}>
      {formError ? <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm font-semibold text-[var(--danger)]">{formError}</div> : null}

      <section className="rounded-lg bg-[var(--surface-muted)] p-4">
        <h2 className="text-base font-bold">{isEdit ? t('editSubject') : t('newSubject')}</h2>
        <p className="mt-1 text-sm text-[var(--muted)]">{t('subjectFormIntro')}</p>
      </section>

      <div className="grid gap-4 md:grid-cols-2">
        <FormField label={t('subjectNameTg')} error={form.formState.errors.nameTg?.message}>
          <Input {...form.register('nameTg')} placeholder="Математика" />
        </FormField>
        <FormField label={t('subjectNameRu')} error={form.formState.errors.nameRu?.message}>
          <Input {...form.register('nameRu')} placeholder="Математика" />
        </FormField>
        <FormField label={t('descriptionTg')}>
          <Textarea rows={3} {...form.register('descriptionTg')} />
        </FormField>
        <FormField label={t('descriptionRu')}>
          <Textarea rows={3} {...form.register('descriptionRu')} />
        </FormField>
        <FormField label={t('subjectNameEn')}>
          <Input {...form.register('nameEn')} />
        </FormField>
        <FormField label={t('descriptionEn')}>
          <Textarea rows={3} {...form.register('descriptionEn')} />
        </FormField>
      </div>

      <input type="hidden" {...form.register('status', { valueAsNumber: true })} />
      <input type="hidden" {...form.register('displayOrder', { valueAsNumber: true })} />

      <FormField label={isEdit ? t('subjectIconNew') : t('subjectIcon')} error={form.formState.errors.icon?.message?.toString()}>
        <Input type="file" accept="image/png,image/jpeg,image/jpg,image/svg+xml" {...form.register('icon')} />
      </FormField>

      <div className="flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={() => navigate('/admin/subjects')}>
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
