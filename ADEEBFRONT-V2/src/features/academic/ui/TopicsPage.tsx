import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PenLine, Plus, Power } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { subjectKeys, subjectsApi } from '@/features/academic/api/subjects.api'
import { topicKeys, topicsApi } from '@/features/academic/api/topics.api'
import { StatusBadge } from '@/features/academic/ui/StatusBadge'
import { TranslationBadges } from '@/features/academic/ui/TranslationBadges'
import { localizedName } from '@/shared/i18n/localized-content'
import { AdminListToolbar } from '@/shared/ui/AdminListToolbar'
import { useColumnVisibility, type AdminListColumn } from '@/shared/ui/useColumnVisibility'
import { ListPagination } from '@/shared/ui/ListPagination'
import { SelectField } from '@/shared/ui/SelectField'
import { EmptyState, ErrorState } from '@/shared/ui/StateBlock'
import { Table, TableShell } from '@/shared/ui/Table'
import { TableActionButton } from '@/shared/ui/TableActionButton'

export function TopicsPage() {
  const { i18n, t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const queryClient = useQueryClient()
  const page = Math.max(1, Number(params.get('page')) || 1)
  const search = params.get('search') ?? ''
  const subjectId = params.get('subjectId') ?? ''
  const topicQueryParams = { page, pageSize: 10, ...(search ? { search } : {}), ...(subjectId ? { subjectId } : {}) }
  const columns: AdminListColumn[] = [
    { id: 'topic', label: t('navTopics'), locked: true },
    { id: 'status', label: t('status') },
    { id: 'translations', label: t('translations') },
    { id: 'order', label: t('order') },
    { id: 'actions', label: t('actions'), locked: true },
  ]
  const columnVisibility = useColumnVisibility('adeeb.columns.topics', columns)

  const subjectsQuery = useQuery({ queryKey: subjectKeys.list({ pageSize: 50 }), queryFn: () => subjectsApi.list({ pageSize: 50 }) })
  const topicsQuery = useQuery({ queryKey: topicKeys.list(topicQueryParams), queryFn: () => topicsApi.list(topicQueryParams) })
  const removeMutation = useMutation({ mutationFn: topicsApi.remove, onSuccess: () => queryClient.invalidateQueries({ queryKey: ['topics'] }) })

  function setParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.set('page', '1')
    setParams(next)
  }

  if (topicsQuery.isError) return <ErrorState title={t('topicsLoadFailed')} />
  const topics = topicsQuery.data?.items ?? []
  const subjectOptions = [{ value: '', label: t('allSubjects') }, ...(subjectsQuery.data?.items.map((subject) => ({ value: subject.id, label: localizedName(subject.translations, i18n.language, subject.name) })) ?? [])]

  return (
    <div className="grid gap-4">
      <AdminListToolbar
        searchValue={search}
        onSearchChange={(value) => setParam('search', value)}
        searchPlaceholder={t('search')}
        filterCount={subjectId ? 1 : 0}
        onClearFilters={() => setParam('subjectId', '')}
        filters={<SelectField searchable searchPlaceholder={t('searchInList')} value={subjectId} options={subjectOptions} onValueChange={(value) => setParam('subjectId', value)} />}
        columns={columns}
        columnVisibility={columnVisibility}
      />
      {topicsQuery.isLoading ? <div className="text-sm text-[var(--muted)]">{t('topicsLoading')}</div> : null}
      {!topicsQuery.isLoading && topics.length === 0 ? <EmptyState title={t('noTopics')} description={t('createTopicForSubject')} /> : null}
      {topics.length > 0 ? (
        <TableShell><Table>
          <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]"><tr>
            <th className="px-4 py-3">{t('navTopics')}</th>
            {columnVisibility.isVisible('status') ? <th className="px-4 py-3">{t('status')}</th> : null}
            {columnVisibility.isVisible('translations') ? <th className="px-4 py-3">{t('translations')}</th> : null}
            {columnVisibility.isVisible('order') ? <th className="px-4 py-3">{t('order')}</th> : null}
            <th className="px-4 py-3 text-right">{t('actions')}</th>
          </tr></thead>
          <tbody>{topics.map((topic) => (
            <tr key={topic.id} className="border-t border-[var(--border)]">
              <td className="px-4 py-3"><strong>{localizedName(topic.translations, i18n.language, topic.name)}</strong><p className="mt-0.5 font-mono text-xs text-[var(--muted)]">{topic.code}</p></td>
              {columnVisibility.isVisible('status') ? <td className="px-4 py-3"><StatusBadge status={topic.status} /></td> : null}
              {columnVisibility.isVisible('translations') ? <td className="px-4 py-3"><TranslationBadges translations={topic.translations} /></td> : null}
              {columnVisibility.isVisible('order') ? <td className="px-4 py-3">{topic.displayOrder}</td> : null}
              <td className="px-4 py-3"><div className="flex justify-end gap-2">
                <TableActionButton to={`/admin/topics/${topic.id}/edit`} label={t('edit')} icon={<PenLine className="h-5 w-5" />} />
                <TableActionButton label={t('delete')} icon={<Power className="h-5 w-5" />} disabled={topic.status === 2} onClick={() => { if (window.confirm(t('confirmDelete'))) removeMutation.mutate(topic.id) }} />
              </div></td>
            </tr>
          ))}</tbody>
        </Table></TableShell>
      ) : null}
      {topicsQuery.data ? <ListPagination page={topicsQuery.data.page} pageSize={topicsQuery.data.pageSize} total={topicsQuery.data.totalCount} onPage={(value) => setParam('page', String(value))} /> : null}
    </div>
  )
}

export function TopicsPageActions() {
  const { t } = useTranslation()
  return <Link to="/admin/topics/new" className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md bg-[var(--primary)] px-4 py-2 text-sm font-semibold text-white no-underline hover:bg-[var(--primary-strong)]"><Plus className="h-4 w-4" />{t('newTopic')}</Link>
}
