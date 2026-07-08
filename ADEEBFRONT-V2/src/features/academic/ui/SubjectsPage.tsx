import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Edit, Plus, Trash2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { subjectKeys, subjectsApi } from '@/features/academic/api/subjects.api'
import { StatusBadge } from '@/features/academic/ui/StatusBadge'
import { TranslationBadges } from '@/features/academic/ui/TranslationBadges'
import { appConfig } from '@/shared/config/env'
import { localizedName } from '@/shared/i18n/localized-content'
import { Button } from '@/shared/ui/Button'
import { EmptyState, ErrorState } from '@/shared/ui/StateBlock'
import { Table, TableShell } from '@/shared/ui/Table'

export function SubjectsPage() {
  const { i18n, t } = useTranslation()
  const queryClient = useQueryClient()
  const query = useQuery({
    queryKey: subjectKeys.list({ pageSize: 100 }),
    queryFn: () => subjectsApi.list({ pageSize: 100, sort: 'displayOrder' }),
  })

  const removeMutation = useMutation({
    mutationFn: subjectsApi.remove,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['subjects'] }),
  })

  if (query.isLoading) return <div className="text-sm text-[var(--muted)]">{t('subjectsLoading')}</div>
  if (query.isError) return <ErrorState title={t('subjectsLoadFailed')} />

  const subjects = query.data?.items ?? []
  if (subjects.length === 0) {
    return <EmptyState title={t('noSubjects')} description={t('createFirstSubject')} />
  }

  return (
    <TableShell>
      <Table>
        <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
          <tr>
            <th className="px-4 py-3">{t('navSubjects')}</th>
            <th className="px-4 py-3">{t('status')}</th>
            <th className="px-4 py-3">{t('translations')}</th>
            <th className="px-4 py-3">{t('order')}</th>
            <th className="px-4 py-3 text-right">{t('actions')}</th>
          </tr>
        </thead>
        <tbody>
          {subjects.map((subject) => (
            <tr key={subject.id} className="border-t border-[var(--border)]">
              <td className="px-4 py-3">
                <div className="flex items-center gap-3">
                  {subject.iconUrl ? (
                    <img className="h-10 w-10 rounded-md object-cover" src={`${appConfig.apiBaseUrl}${subject.iconUrl}`} alt="" />
                  ) : (
                    <span className="grid h-10 w-10 place-items-center rounded-md bg-[var(--surface-muted)] font-bold">A</span>
                  )}
                  <div>
                    <strong>{localizedName(subject.translations, i18n.language, subject.name)}</strong>
                    <p className="mt-0.5 font-mono text-xs text-[var(--muted)]">{subject.code}</p>
                  </div>
                </div>
              </td>
              <td className="px-4 py-3"><StatusBadge status={subject.status} /></td>
              <td className="px-4 py-3"><TranslationBadges translations={subject.translations} /></td>
              <td className="px-4 py-3">{subject.displayOrder}</td>
              <td className="px-4 py-3">
                <div className="flex justify-end gap-2">
                  <Link
                    className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-sm font-semibold text-[var(--text)] no-underline hover:bg-[var(--surface-muted)]"
                    to={`/admin/subjects/${subject.id}/edit`}
                  >
                    <Edit className="h-4 w-4" /> {t('edit')}
                  </Link>
                  <Button
                    variant="danger"
                    className="px-3"
                    disabled={subject.status === 2}
                    onClick={() => {
                      if (window.confirm(t('confirmDelete'))) {
                        removeMutation.mutate(subject.id)
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
  )
}

export function SubjectsPageActions() {
  const { t } = useTranslation()
  return (
    <Link to="/admin/subjects/new" className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md bg-[var(--primary)] px-4 py-2 text-sm font-semibold text-white no-underline hover:bg-[var(--primary-strong)]">
      <Plus className="h-4 w-4" />
      {t('newSubject')}
    </Link>
  )
}
