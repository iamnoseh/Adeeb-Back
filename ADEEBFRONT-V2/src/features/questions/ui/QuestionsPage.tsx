import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, FileUp, ImageIcon, PenLine, Plus, Power } from 'lucide-react'
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
import { AdminListToolbar } from '@/shared/ui/AdminListToolbar'
import { useColumnVisibility, type AdminListColumn } from '@/shared/ui/useColumnVisibility'
import { SelectField } from '@/shared/ui/SelectField'
import { EmptyState, ErrorState } from '@/shared/ui/StateBlock'
import { Table, TableShell } from '@/shared/ui/Table'
import { TableActionButton } from '@/shared/ui/TableActionButton'

export function QuestionsPage() {
  const { i18n, t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const queryClient = useQueryClient()
  const listQuery = toQuestionListQuery(params)
  const columns: AdminListColumn[] = [
    { id: 'question', label: t('question'), locked: true },
    { id: 'subject', label: t('parentSubject') },
    { id: 'type', label: t('type') },
    { id: 'difficulty', label: t('difficulty') },
    { id: 'status', label: t('status') },
    { id: 'actions', label: t('actions'), locked: true },
  ]
  const columnVisibility = useColumnVisibility('adeeb.columns.questions', columns)

  const subjectsQuery = useQuery({
    queryKey: subjectKeys.list({ pageSize: 50 }),
    queryFn: () => subjectsApi.list({ pageSize: 50 }),
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
  const pageSize = questionsQuery.data?.pageSize ?? listQuery.pageSize ?? 10
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
  const filterCount = ['subjectId', 'type', 'difficulty'].filter((key) => params.has(key)).length

  return (
    <div className="grid gap-4">
      <AdminListToolbar
        searchValue={params.get('search') ?? ''}
        onSearchChange={(value) => updateParam('search', value)}
        searchPlaceholder={t('search')}
        filterCount={filterCount}
        onClearFilters={() => {
          const next = new URLSearchParams(params)
          ;['subjectId', 'type', 'difficulty'].forEach((key) => next.delete(key))
          next.set('page', '1')
          setParams(next)
        }}
        filters={
          <>
            <SelectField searchable searchPlaceholder={t('searchInList')} value={params.get('subjectId') ?? ''} options={subjectOptions} onValueChange={(value) => updateParam('subjectId', value)} />
            <SelectField value={params.get('type') ?? ''} options={typeOptions} onValueChange={(value) => updateParam('type', value)} />
            <SelectField value={params.get('difficulty') ?? ''} options={difficultyOptions} onValueChange={(value) => updateParam('difficulty', value)} />
          </>
        }
        columns={columns}
        columnVisibility={columnVisibility}
      />

      {questionsQuery.isLoading ? <div className="text-sm text-[var(--muted)]">{t('questionsLoading')}</div> : null}
      {!questionsQuery.isLoading && questions.length === 0 ? <EmptyState title={t('noQuestions')} description={t('adjustFiltersOrCreateQuestion')} /> : null}

      {questions.length > 0 ? (
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
              <tr>
                <th className="px-4 py-3">{t('question')}</th>
                {columnVisibility.isVisible('subject') ? <th className="px-4 py-3">{t('parentSubject')}</th> : null}
                {columnVisibility.isVisible('type') ? <th className="px-4 py-3">{t('type')}</th> : null}
                {columnVisibility.isVisible('difficulty') ? <th className="px-4 py-3">{t('difficulty')}</th> : null}
                {columnVisibility.isVisible('status') ? <th className="px-4 py-3">{t('status')}</th> : null}
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
                  {columnVisibility.isVisible('subject') ? <td className="px-4 py-3">
                    {(() => {
                      const subject = subjectsById.get(question.subjectId)
                      return subject ? localizedName(subject.translations, i18n.language, subject.name) : question.subjectId
                    })()}
                  </td> : null}
                  {columnVisibility.isVisible('type') ? <td className="px-4 py-3">{questionTypeLabel(question.type, t)}</td> : null}
                  {columnVisibility.isVisible('difficulty') ? <td className="px-4 py-3">{difficultyLabel(question.difficulty, t)}</td> : null}
                  {columnVisibility.isVisible('status') ? <td className="px-4 py-3"><StatusBadge status={question.status} /></td> : null}
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-2">
                      <TableActionButton to={`/admin/questions/${question.id}/edit`} label={t('edit')} icon={<PenLine className="h-5 w-5" />} />
                      <TableActionButton
                        label={t('delete')}
                        icon={<Power className="h-5 w-5" />}
                        disabled={question.status === 2}
                        onClick={() => {
                          if (window.confirm(t('confirmDelete'))) {
                            removeMutation.mutate(question.id)
                          }
                        }}
                      />
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
    <>
      <Link to="/admin/questions/import" className="inline-flex min-h-11 items-center justify-center gap-2 rounded-2xl border border-[var(--border)] bg-white px-4 py-2.5 text-sm font-bold text-[var(--text)] no-underline shadow-sm hover:bg-[var(--surface-soft)]">
        <FileUp className="h-4 w-4" />
        Import
      </Link>
      <Link to="/admin/questions/new" className="inline-flex min-h-11 items-center justify-center gap-2 rounded-2xl bg-[linear-gradient(180deg,var(--primary),var(--primary-strong))] px-4 py-2.5 text-sm font-bold text-white no-underline shadow-[0_12px_24px_rgb(47_125_115/0.24)] hover:brightness-105">
        <Plus className="h-4 w-4" />
        {t('newQuestion')}
      </Link>
    </>
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
