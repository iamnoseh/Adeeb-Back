import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { PenLine, Plus, Power } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { subjectKeys, subjectsApi } from '@/features/academic/api/subjects.api'
import { StatusBadge } from '@/features/academic/ui/StatusBadge'
import { TranslationBadges } from '@/features/academic/ui/TranslationBadges'
import { appConfig } from '@/shared/config/env'
import { localizedName } from '@/shared/i18n/localized-content'
import { AdminListToolbar } from '@/shared/ui/AdminListToolbar'
import { useColumnVisibility, type AdminListColumn } from '@/shared/ui/useColumnVisibility'
import { ListPagination } from '@/shared/ui/ListPagination'
import { EmptyState, ErrorState } from '@/shared/ui/StateBlock'
import { Table, TableShell } from '@/shared/ui/Table'
import { TableActionButton } from '@/shared/ui/TableActionButton'

export function SubjectsPage() {
  const { i18n, t } = useTranslation()
  const [params, setParams] = useSearchParams()
  const queryClient = useQueryClient()
  const page = Math.max(1, Number(params.get('page')) || 1)
  const search = params.get('search') ?? ''
  const listQuery = { page, pageSize: 10, sort: 'displayOrder', ...(search ? { search } : {}) }
  const columns: AdminListColumn[] = [
    { id: 'subject', label: t('navSubjects'), locked: true },
    { id: 'status', label: t('status') },
    { id: 'translations', label: t('translations') },
    { id: 'order', label: t('order') },
    { id: 'actions', label: t('actions'), locked: true },
  ]
  const columnVisibility = useColumnVisibility('adeeb.columns.subjects', columns)
  const query = useQuery({ queryKey: subjectKeys.list(listQuery), queryFn: () => subjectsApi.list(listQuery) })
  const removeMutation = useMutation({ mutationFn: subjectsApi.remove, onSuccess: () => queryClient.invalidateQueries({ queryKey: ['subjects'] }) })

  function setParam(key: string, value: string) {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    if (key !== 'page') next.set('page', '1')
    setParams(next)
  }

  if (query.isError) return <ErrorState title={t('subjectsLoadFailed')} />
  const subjects = query.data?.items ?? []

  return (
    <div className="grid gap-4">
      <AdminListToolbar searchValue={search} onSearchChange={(value) => setParam('search', value)} searchPlaceholder={t('search')} columns={columns} columnVisibility={columnVisibility} />
      {query.isLoading ? <div className="text-sm text-[var(--muted)]">{t('subjectsLoading')}</div> : null}
      {!query.isLoading && subjects.length === 0 ? <EmptyState title={t('noSubjects')} description={t('createFirstSubject')} /> : null}
      {subjects.length > 0 ? (
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]"><tr>
              <th className="px-4 py-3">{t('navSubjects')}</th>
              {columnVisibility.isVisible('status') ? <th className="px-4 py-3">{t('status')}</th> : null}
              {columnVisibility.isVisible('translations') ? <th className="px-4 py-3">{t('translations')}</th> : null}
              {columnVisibility.isVisible('order') ? <th className="px-4 py-3">{t('order')}</th> : null}
              <th className="px-4 py-3 text-right">{t('actions')}</th>
            </tr></thead>
            <tbody>{subjects.map((subject) => (
              <tr key={subject.id} className="border-t border-[var(--border)]">
                <td className="px-4 py-3"><div className="flex items-center gap-3">
                  {subject.iconUrl ? <img className="h-10 w-10 rounded-md object-cover" src={`${appConfig.apiBaseUrl}${subject.iconUrl}`} alt="" /> : <span className="grid h-10 w-10 place-items-center rounded-md bg-[var(--surface-muted)] font-bold">A</span>}
                  <div><strong>{localizedName(subject.translations, i18n.language, subject.name)}</strong><p className="mt-0.5 font-mono text-xs text-[var(--muted)]">{subject.code}</p></div>
                </div></td>
                {columnVisibility.isVisible('status') ? <td className="px-4 py-3"><StatusBadge status={subject.status} /></td> : null}
                {columnVisibility.isVisible('translations') ? <td className="px-4 py-3"><TranslationBadges translations={subject.translations} /></td> : null}
                {columnVisibility.isVisible('order') ? <td className="px-4 py-3">{subject.displayOrder}</td> : null}
                <td className="px-4 py-3"><div className="flex justify-end gap-2">
                  <TableActionButton to={`/admin/subjects/${subject.id}/edit`} label={t('edit')} icon={<PenLine className="h-5 w-5" />} />
                  <TableActionButton label={t('delete')} icon={<Power className="h-5 w-5" />} disabled={subject.status === 2} onClick={() => { if (window.confirm(t('confirmDelete'))) removeMutation.mutate(subject.id) }} />
                </div></td>
              </tr>
            ))}</tbody>
          </Table>
        </TableShell>
      ) : null}
      {query.data ? <ListPagination page={query.data.page} pageSize={query.data.pageSize} total={query.data.totalCount} onPage={(value) => setParam('page', String(value))} /> : null}
    </div>
  )
}

export function SubjectsPageActions() {
  const { t } = useTranslation()
  return <Link to="/admin/subjects/new" className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md bg-[var(--primary)] px-4 py-2 text-sm font-semibold text-white no-underline hover:bg-[var(--primary-strong)]"><Plus className="h-4 w-4" />{t('newSubject')}</Link>
}
