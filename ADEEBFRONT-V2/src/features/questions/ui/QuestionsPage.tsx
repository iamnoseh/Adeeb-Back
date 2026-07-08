import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Edit, Plus, Trash2 } from 'lucide-react'
import { Link, useSearchParams } from 'react-router-dom'
import { subjectKeys, subjectsApi } from '@/features/academic/api/subjects.api'
import { StatusBadge } from '@/features/academic/ui/StatusBadge'
import { difficultyLabel, questionTypeLabel } from '@/features/questions/lib/question-labels'
import { questionKeys, questionsApi } from '@/features/questions/api/questions.api'
import type { QuestionListQuery } from '@/features/questions/model/question.types'
import { Button } from '@/shared/ui/Button'
import { Input, Select } from '@/shared/ui/Input'
import { EmptyState, ErrorState } from '@/shared/ui/StateBlock'
import { Table, TableShell } from '@/shared/ui/Table'

export function QuestionsPage() {
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

  if (questionsQuery.isError) return <ErrorState title="Саволҳо бор нашуданд" />

  const questions = questionsQuery.data?.items ?? []

  return (
    <div className="grid gap-4">
      <div className="app-surface grid gap-3 rounded-lg p-4 md:grid-cols-4">
        <Input placeholder="Ҷустуҷӯ..." value={params.get('search') ?? ''} onChange={(event) => updateParam('search', event.target.value)} />
        <Select value={params.get('subjectId') ?? ''} onChange={(event) => updateParam('subjectId', event.target.value)}>
          <option value="">Ҳамаи фанҳо</option>
          {subjectsQuery.data?.items.map((subject) => (
            <option key={subject.id} value={subject.id}>{subject.name}</option>
          ))}
        </Select>
        <Select value={params.get('type') ?? ''} onChange={(event) => updateParam('type', event.target.value)}>
          <option value="">Ҳамаи типҳо</option>
          <option value="1">Single Choice</option>
          <option value="2">Matching</option>
          <option value="3">Closed Answer</option>
        </Select>
        <Select value={params.get('difficulty') ?? ''} onChange={(event) => updateParam('difficulty', event.target.value)}>
          <option value="">Ҳамаи сатҳҳо</option>
          <option value="1">Easy</option>
          <option value="2">Medium</option>
          <option value="3">Hard</option>
        </Select>
      </div>

      {questionsQuery.isLoading ? <div className="text-sm text-[var(--muted)]">Саволҳо бор мешаванд...</div> : null}
      {!questionsQuery.isLoading && questions.length === 0 ? <EmptyState title="Савол ёфт нашуд" description="Филтрҳоро тағйир диҳед ё саволи нав созед." /> : null}

      {questions.length > 0 ? (
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
              <tr>
                <th className="px-4 py-3">Савол</th>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Difficulty</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {questions.map((question) => (
                <tr key={question.id} className="border-t border-[var(--border)]">
                  <td className="max-w-xl px-4 py-3">
                    <strong className="line-clamp-2">{question.content}</strong>
                  </td>
                  <td className="px-4 py-3">{questionTypeLabel(question.type)}</td>
                  <td className="px-4 py-3">{difficultyLabel(question.difficulty)}</td>
                  <td className="px-4 py-3"><StatusBadge status={question.status} /></td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-2">
                      <Link className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-sm font-semibold text-[var(--text)] no-underline hover:bg-[var(--surface-muted)]" to={`/admin/questions/${question.id}/edit`}>
                        <Edit className="h-4 w-4" /> Edit
                      </Link>
                      <Button variant="danger" className="px-3" onClick={() => void removeMutation.mutate(question.id)}>
                        <Trash2 className="h-4 w-4" /> Delete
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </TableShell>
      ) : null}
    </div>
  )
}

export function QuestionsPageActions() {
  return (
    <Link to="/admin/questions/new" className="inline-flex min-h-10 items-center justify-center gap-2 rounded-md bg-[var(--primary)] px-4 py-2 text-sm font-semibold text-white no-underline hover:bg-[var(--primary-strong)]">
      <Plus className="h-4 w-4" />
      Саволи нав
    </Link>
  )
}

function toQuestionListQuery(params: URLSearchParams): QuestionListQuery {
  const query: QuestionListQuery = {
    page: toNumber(params.get('page')) ?? 1,
    pageSize: 20,
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
