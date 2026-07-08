import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Edit, Plus, Trash2 } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router-dom'
import { subjectKeys, subjectsApi } from '@/features/academic/api/subjects.api'
import { topicKeys, topicsApi } from '@/features/academic/api/topics.api'
import { StatusBadge } from '@/features/academic/ui/StatusBadge'
import { TranslationBadges } from '@/features/academic/ui/TranslationBadges'
import { localizedName } from '@/shared/i18n/localized-content'
import { Button } from '@/shared/ui/Button'
import { Select } from '@/shared/ui/Input'
import { EmptyState, ErrorState } from '@/shared/ui/StateBlock'
import { Table, TableShell } from '@/shared/ui/Table'

export function TopicsPage() {
  const { i18n, t } = useTranslation()
  const [searchParams, setSearchParams] = useSearchParams()
  const queryClient = useQueryClient()
  const subjectId = searchParams.get('subjectId') ?? undefined
  const topicQueryParams = subjectId ? { pageSize: 100, subjectId } : { pageSize: 100 }

  const subjectsQuery = useQuery({
    queryKey: subjectKeys.list({ pageSize: 100 }),
    queryFn: () => subjectsApi.list({ pageSize: 100 }),
  })
  const topicsQuery = useQuery({
    queryKey: topicKeys.list(topicQueryParams),
    queryFn: () => topicsApi.list(topicQueryParams),
  })
  const removeMutation = useMutation({
    mutationFn: topicsApi.remove,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['topics'] }),
  })

  if (topicsQuery.isLoading || subjectsQuery.isLoading) return <div className="text-sm text-[var(--muted)]">{t('topicsLoading')}</div>
  if (topicsQuery.isError) return <ErrorState title={t('topicsLoadFailed')} />

  const topics = topicsQuery.data?.items ?? []

  return (
    <div className="grid gap-4">
      <div className="app-surface rounded-lg p-4">
        <label className="grid max-w-md gap-1.5 text-sm font-semibold">
          <span>{t('filterBySubject')}</span>
          <Select value={subjectId ?? ''} onChange={(event) => setSearchParams(event.target.value ? { subjectId: event.target.value } : {})}>
            <option value="">{t('allSubjects')}</option>
            {subjectsQuery.data?.items.map((subject) => (
              <option key={subject.id} value={subject.id}>{localizedName(subject.translations, i18n.language, subject.name)}</option>
            ))}
          </Select>
        </label>
      </div>

      {topics.length === 0 ? (
        <EmptyState title={t('noTopics')} description={t('createTopicForSubject')} />
      ) : (
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
              <tr>
                <th className="px-4 py-3">{t('navTopics')}</th>
                <th className="px-4 py-3">{t('status')}</th>
                <th className="px-4 py-3">{t('translations')}</th>
                <th className="px-4 py-3">{t('order')}</th>
                <th className="px-4 py-3 text-right">{t('actions')}</th>
              </tr>
            </thead>
            <tbody>
              {topics.map((topic) => (
                <tr key={topic.id} className="border-t border-[var(--border)]">
                  <td className="px-4 py-3">
                    <strong>{localizedName(topic.translations, i18n.language, topic.name)}</strong>
                    <p className="mt-0.5 font-mono text-xs text-[var(--muted)]">{topic.code}</p>
                  </td>
                  <td className="px-4 py-3"><StatusBadge status={topic.status} /></td>
                  <td className="px-4 py-3"><TranslationBadges translations={topic.translations} /></td>
                  <td className="px-4 py-3">{topic.displayOrder}</td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-2">
                      <Link className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-sm font-semibold text-[var(--text)] no-underline hover:bg-[var(--surface-muted)]" to={`/admin/topics/${topic.id}/edit`}>
                        <Edit className="h-4 w-4" /> {t('edit')}
                      </Link>
                      <Button
                        variant="danger"
                        className="px-3"
                        disabled={topic.status === 2}
                        onClick={() => {
                          if (window.confirm(t('confirmDelete'))) {
                            removeMutation.mutate(topic.id)
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
      )}
    </div>
  )
}

export function TopicsPageActions() {
  const { t } = useTranslation()
  return (
    <Link to="/admin/topics/new" className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md bg-[var(--primary)] px-4 py-2 text-sm font-semibold text-white no-underline hover:bg-[var(--primary-strong)]">
      <Plus className="h-4 w-4" />
      {t('newTopic')}
    </Link>
  )
}
