import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Edit, ImageIcon, Plus, Trash2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { subjectKeys, subjectsApi } from '@/features/academic/api/subjects.api'
import { StatusBadge } from '@/features/academic/ui/StatusBadge'
import { difficultyLabel, questionTypeLabel } from '@/features/questions/lib/question-labels'
import { questionKeys, questionsApi } from '@/features/questions/api/questions.api'
import type { QuestionListQuery } from '@/features/questions/model/question.types'
import { appConfig } from '@/shared/config/env'
import { localizedName } from '@/shared/i18n/localized-content'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'
import { SelectField } from '@/shared/ui/SelectField'
import { EmptyState, ErrorState } from '@/shared/ui/StateBlock'
import { Table, TableShell } from '@/shared/ui/Table'

export function QuestionsPage() {
  const { i18n, t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const queryClient = useQueryClient()
  const listQuery = toQuestionListQuery(params)

  const subjectsQuery = useQuery({
    queryKey: subjectKeys.list({ pageSize: 100 }),
    queryFn: () => subjectsApi.list({ pageSize: 100 }),
  })
  const questionsQuery = useQuery({
    queryKey: questionKeys.list(listQuery),
    queryFn: () => questionsApi.list(listQuery),
  })
  const removeMutation = useMutation({
    mutationFn: questionsApi.remove,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['questions'] }),
  })

  function updateParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    next.set('page', '1')
    setParams(next)
  }

  function setPage(page: number) {
    const next = new URLSearchParams(params)
    next.set('page', String(page))
    setParams(next)
  }

  if (questionsQuery.isError) return <ErrorState title={t('questionsLoadFailed')} />

  const questions = questionsQuery.data?.items ?? []
  const page = questionsQuery.data?.page ?? listQuery.page ?? 1
  const pageSize = questionsQuery.data?.pageSize ?? listQuery.pageSize ?? 20
  const totalCount = questionsQuery.data?.totalCount ?? 0
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const subjectsById = new Map((subjectsQuery.data?.items ?? []).map((subject) => [subject.id, subject]))
  const subjectOptions = [
    { value: '', label: t('allSubjects') },
    ...(subjectsQuery.data?.items.map((subject) => ({
      value: subject.id,
      label: localizedName(subject.translations, i18n.language, subject.name),
    })) ?? []),
  ]
  const typeOptions = [
    { value: '', label: t('allTypes') },
    { value: '1', label: t('typeSingleChoice') },
    { value: '2', label: t('typeMatching') },
    { value: '3', label: t('typeClosedAnswer') },
  ]
  const difficultyOptions = [
    { value: '', label: t('allDifficulties') },
    { value: '1', label: t('difficultyEasy') },
    { value: '2', label: t('difficultyMedium') },
    { value: '3', label: t('difficultyHard') },
  ]

  return (
    <div className="grid gap-4">
      <div className="app-surface grid gap-3 rounded-lg p-4 md:grid-cols-4">
        <Input placeholder={t('search')} value={params.get('search') ?? ''} onChange={(event) => updateParam('search', event.target.value)} />
        <SelectField value={params.get('subjectId') ?? ''} options={subjectOptions} onValueChange={(value) => updateParam('subjectId', value)} />
        <SelectField value={params.get('type') ?? ''} options={typeOptions} onValueChange={(value) => updateParam('type', value)} />
        <SelectField value={params.get('difficulty') ?? ''} options={difficultyOptions} onValueChange={(value) => updateParam('difficulty', value)} />
      </div>

      {questionsQuery.isLoading ? <div className="text-sm text-[var(--muted)]">{t('questionsLoading')}</div> : null}
      {!questionsQuery.isLoading && questions.length === 0 ? <EmptyState title={t('noQuestions')} description={t('adjustFiltersOrCreateQuestion')} /> : null}

      {questions.length > 0 ? (
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
              <tr>
                <th className="px-4 py-3">{t('question')}</th>
                <th className="px-4 py-3">{t('parentSubject')}</th>
                <th className="px-4 py-3">{t('type')}</th>
                <th className="px-4 py-3">{t('difficulty')}</th>
                <th className="px-4 py-3">{t('status')}</th>
                <th className="px-4 py-3 text-right">{t('actions')}</th>
              </tr>
            </thead>
            <tbody>
              {questions.map((question) => (
                <tr key={question.id} className="border-t border-[var(--border)]">
                  <td className="max-w-xl px-4 py-3">
                    <div className="flex items-center gap-3">
                      {question.imageUrl ? (
                        <img className="h-14 w-16 rounded-2xl object-cover ring-1 ring-[var(--border)]" src={toAssetUrl(question.imageUrl)} alt="" />
                      ) : (
                        <span className="grid h-14 w-16 place-items-center rounded-2xl bg-[var(--surface-muted)] text-[var(--muted)]">
                          <ImageIcon className="h-5 w-5" aria-hidden />
                        </span>
                      )}
                      <strong className="line-clamp-2">{question.content}</strong>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    {(() => {
                      const subject = subjectsById.get(question.subjectId)
                      return subject ? localizedName(subject.translations, i18n.language, subject.name) : question.subjectId
                    })()}
                  </td>
                  <td className="px-4 py-3">{questionTypeLabel(question.type, t)}</td>
                  <td className="px-4 py-3">{difficultyLabel(question.difficulty, t)}</td>
                  <td className="px-4 py-3"><StatusBadge status={question.status} /></td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-2">
                      <Link className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-sm font-semibold text-[var(--text)] no-underline hover:bg-[var(--surface-muted)]" to={`/admin/questions/${question.id}/edit`}>
                        <Edit className="h-4 w-4" /> {t('edit')}
                      </Link>
                      <Button
                        variant="danger"
                        className="px-3"
                        disabled={question.status === 2}
                        onClick={() => {
                          if (window.confirm(t('confirmDelete'))) {
                            removeMutation.mutate(question.id)
                          }
                        }}
                      >
                        <Trash2 className="h-4 w-4" /> {t('delete')}
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </TableShell>
      ) : null}

      {questionsQuery.data && totalCount > pageSize ? (
        <div className="flex flex-col gap-3 rounded-[1.5rem] bg-white/80 p-3 shadow-sm ring-1 ring-[var(--border)] sm:flex-row sm:items-center sm:justify-between">
          <p className="px-2 text-sm font-semibold text-[var(--muted)]">
            {page} / {totalPages} · {totalCount}
          </p>
          <div className="flex gap-2">
            <Button type="button" variant="secondary" disabled={page <= 1} onClick={() => setPage(page - 1)}>
              <ChevronLeft className="h-4 w-4" aria-hidden />
            </Button>
            <Button type="button" variant="secondary" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>
              <ChevronRight className="h-4 w-4" aria-hidden />
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  )
}

export function QuestionsPageActions() {
  const { t } = useTranslation()
  return (
    <Link to="/admin/questions/new" className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md bg-[var(--primary)] px-4 py-2 text-sm font-semibold text-white no-underline hover:bg-[var(--primary-strong)]">
      <Plus className="h-4 w-4" />
      {t('newQuestion')}
    </Link>
  )
}

function toQuestionListQuery(params: URLSearchParams): QuestionListQuery {
  const query: QuestionListQuery = {
    page: toNumber(params.get('page')) ?? 1,
    pageSize: toNumber(params.get('pageSize')) ?? 10,
  }
  const subjectId = params.get('subjectId')
  const topicId = params.get('topicId')
  const search = params.get('search')
  const type = toNumber(params.get('type'))
  const difficulty = toNumber(params.get('difficulty'))
  const status = toNumber(params.get('status'))

  if (subjectId) query.subjectId = subjectId
  if (topicId) query.topicId = topicId
  if (search) query.search = search
  if (type !== undefined) query.type = type
  if (difficulty !== undefined) query.difficulty = difficulty
  if (status !== undefined) query.status = status

  return query
}

function toNumber(value: string | null) {
  return value ? Number(value) : undefined
}

function toAssetUrl(url: string) {
  if (/^https?:\/\//i.test(url)) return url
  return `${appConfig.apiBaseUrl}${url}`
}
