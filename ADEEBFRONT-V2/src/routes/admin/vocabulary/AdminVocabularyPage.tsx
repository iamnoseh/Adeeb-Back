import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, BookOpenText, CalendarDays, FileQuestion, Languages, Plus, Search, Send, Wand2, WholeWord, X } from 'lucide-react'
import type { ElementType, ReactNode } from 'react'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { adminVocabularyApi, vocabularyKeys } from '@/features/vocabulary/api/vocabulary.api'
import { VocabularyContentStatus, type DailyWordDto, type DailyWordUpsertRequest, type LanguageUpsertRequest, type LearningLanguageDto, type TopicUpsertRequest, type VocabularyListQuery, type VocabularyQuestionDto, type VocabularyTopicDto, type VocabularyWordDto, type WordUpsertRequest } from '@/features/vocabulary/model/vocabulary.types'
import { vocabularyLevelLabel, vocabularyQuestionTypeLabel } from '@/features/vocabulary/lib/vocabulary'
import { cn } from '@/shared/lib/cn'
import { OverflowMarquee } from '@/shared/ui/OverflowMarquee'
import { ListPagination } from '@/shared/ui/ListPagination'
import { Button } from '@/shared/ui/Button'
import { FormField } from '@/shared/ui/FormField'
import { Input, Textarea } from '@/shared/ui/Input'
import { SelectField } from '@/shared/ui/SelectField'

type Resource = 'languages' | 'topics' | 'words' | 'questions' | 'dailyWords'
type AdminVocabularyItem = LearningLanguageDto | VocabularyTopicDto | VocabularyWordDto | VocabularyQuestionDto | DailyWordDto

export function AdminVocabularyPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [resource, setResource] = useState<Resource>('words')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [formOpen, setFormOpen] = useState(false)
  const query = useMemo<VocabularyListQuery>(() => ({ search, page, pageSize: 10 }), [search, page])
  const dataQuery = useQuery({
    queryKey: vocabularyKeys.admin.list(resource, query),
    queryFn: () => {
      if (resource === 'languages') return adminVocabularyApi.languages(query) as Promise<{ items: AdminVocabularyItem[]; page: number; pageSize: number; totalCount: number }>
      if (resource === 'topics') return adminVocabularyApi.topics(query) as Promise<{ items: AdminVocabularyItem[]; page: number; pageSize: number; totalCount: number }>
      if (resource === 'questions') return adminVocabularyApi.questions(query) as Promise<{ items: AdminVocabularyItem[]; page: number; pageSize: number; totalCount: number }>
      if (resource === 'dailyWords') return adminVocabularyApi.dailyWords(query) as Promise<{ items: AdminVocabularyItem[]; page: number; pageSize: number; totalCount: number }>
      return adminVocabularyApi.words(query) as Promise<{ items: AdminVocabularyItem[]; page: number; pageSize: number; totalCount: number }>
    },
  })
  const languageQuery = useQuery({ queryKey: vocabularyKeys.admin.list('languages', { page: 1, pageSize: 50 }), queryFn: () => adminVocabularyApi.languages({ page: 1, pageSize: 50 }) })
  const topicQuery = useQuery({ queryKey: vocabularyKeys.admin.list('topics', { page: 1, pageSize: 50 }), queryFn: () => adminVocabularyApi.topics({ page: 1, pageSize: 50 }) })
  const wordQuery = useQuery({ queryKey: vocabularyKeys.admin.list('words', { page: 1, pageSize: 50 }), queryFn: () => adminVocabularyApi.words({ page: 1, pageSize: 50 }) })
  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: vocabularyKeys.admin.all })
  }
  const generateMutation = useMutation({ mutationFn: adminVocabularyApi.generateQuestionDrafts, onSuccess: invalidate })
  const publishMutation = useMutation({ mutationFn: adminVocabularyApi.publishQuestion, onSuccess: invalidate })
  const archiveQuestionMutation = useMutation({ mutationFn: adminVocabularyApi.archiveQuestion, onSuccess: invalidate })
  const archiveWordMutation = useMutation({ mutationFn: adminVocabularyApi.archiveWord, onSuccess: invalidate })
  const tabs: { id: Resource; label: string; icon: ElementType }[] = [
    { id: 'languages', label: t('vocabulary.admin.languages'), icon: Languages },
    { id: 'topics', label: t('vocabulary.admin.topics'), icon: BookOpenText },
    { id: 'words', label: t('vocabulary.admin.words'), icon: WholeWord },
    { id: 'questions', label: t('vocabulary.admin.questions'), icon: FileQuestion },
    { id: 'dailyWords', label: t('vocabulary.admin.dailyWords'), icon: CalendarDays },
  ]

  return (
    <div className="grid gap-6">
      <header className="rounded-[1.5rem] border border-white/70 bg-white/78 p-6 shadow-[0_18px_45px_rgb(24_49_45/0.08)]">
        <h1 className="text-3xl font-black tracking-normal">{t('vocabulary.admin.title')}</h1>
        <p className="mt-2 text-sm text-[var(--muted)]">{t('vocabulary.admin.description')}</p>
      </header>
      <section className="app-surface rounded-[1.5rem] p-4">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div className="custom-scrollbar flex gap-2 overflow-x-auto">
            {tabs.map((tab) => <button key={tab.id} type="button" onClick={() => { setResource(tab.id); setPage(1); setFormOpen(false) }} className={cn('inline-flex min-h-11 shrink-0 items-center gap-2 rounded-lg px-3 text-sm font-black transition', resource === tab.id ? 'bg-[var(--primary-soft)] text-[var(--primary-strong)]' : 'text-[var(--muted)] hover:bg-[var(--surface-soft)] hover:text-[var(--text)]')}><tab.icon className="h-4 w-4" />{tab.label}</button>)}
          </div>
          <div className="flex min-w-0 flex-col gap-2 sm:flex-row">
            <label className="relative block min-w-0 lg:w-80">
              <Search className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--muted)]" />
              <input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1) }} placeholder={t('vocabulary.admin.search')} className="min-h-12 w-full rounded-lg border border-[var(--border)] bg-white pl-10 pr-4 text-sm font-semibold outline-none focus:border-[var(--primary)]" />
            </label>
            {resource !== 'questions' ? <Button type="button" onClick={() => setFormOpen((current) => !current)} className="min-h-12 rounded-lg"><Plus className="h-4 w-4" />{t('vocabulary.admin.add')}</Button> : null}
          </div>
        </div>
        {formOpen ? <VocabularyCreateForm resource={resource} languages={languageQuery.data?.items ?? []} topics={topicQuery.data?.items ?? []} words={wordQuery.data?.items ?? []} onClose={() => setFormOpen(false)} /> : null}
        <div className="mt-4 overflow-hidden rounded-lg border border-[var(--border)]">
          <div className="grid grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)_120px_150px] bg-[var(--surface-muted)] px-4 py-3 text-xs font-black uppercase text-[var(--muted)]">
            <span>{t('vocabulary.admin.words')}</span><span>{t('vocabulary.language')}</span><span>{t('vocabulary.admin.status')}</span><span className="text-right">{t('actions')}</span>
          </div>
          {dataQuery.data?.items.length ? dataQuery.data.items.map((item) => <ResourceRow key={rowKey(item)} item={item} resource={resource} onGenerate={generateMutation.mutate} onPublish={publishMutation.mutate} onArchiveQuestion={archiveQuestionMutation.mutate} onArchiveWord={archiveWordMutation.mutate} pending={generateMutation.isPending || publishMutation.isPending || archiveQuestionMutation.isPending || archiveWordMutation.isPending} />) : <div className="p-8 text-center text-sm font-bold text-[var(--muted)]">{dataQuery.isLoading ? t('vocabulary.loading') : t('vocabulary.admin.empty')}</div>}
        </div>
        {dataQuery.data ? <ListPagination page={dataQuery.data.page} pageSize={dataQuery.data.pageSize} total={dataQuery.data.totalCount} onPage={setPage} /> : null}
      </section>
    </div>
  )
}

function ResourceRow({ item, resource, onGenerate, onPublish, onArchiveQuestion, onArchiveWord, pending }: { item: AdminVocabularyItem; resource: Resource; onGenerate: (id: string) => void; onPublish: (id: string) => void; onArchiveQuestion: (id: string) => void; onArchiveWord: (id: string) => void; pending: boolean }) {
  const { t } = useTranslation()
  let title = ''
  let detail = ''
  let status: number = VocabularyContentStatus.Published
  if (resource === 'languages') {
    const value = item as LearningLanguageDto
    title = value.name
    detail = value.code
    status = value.isActive ? VocabularyContentStatus.Published : VocabularyContentStatus.Archived
  } else if (resource === 'topics') {
    const value = item as VocabularyTopicDto
    title = value.name
    detail = vocabularyLevelLabel(value.level)
    status = value.status
  } else if (resource === 'questions') {
    const value = item as VocabularyQuestionDto
    title = value.prompt
    detail = vocabularyQuestionTypeLabel(value.type, t)
    status = value.status
  } else if (resource === 'dailyWords') {
    const value = item as DailyWordDto
    title = value.word.targetText
    detail = value.localDate
    status = VocabularyContentStatus.Published
  } else {
    const value = item as VocabularyWordDto
    title = value.targetText
    detail = value.translation
    status = value.status
  }
  const id = 'id' in item ? item.id : undefined
  return <div className="grid min-h-14 grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)_120px_150px] items-center gap-3 border-t border-[var(--border)] px-4 py-3 text-sm"><OverflowMarquee text={title} className="font-black" /><OverflowMarquee text={detail} className="text-[var(--muted)]" /><Status status={status} /><div className="flex justify-end gap-1">{resource === 'words' && id ? <IconAction label={t('vocabulary.admin.generate')} disabled={pending} onClick={() => onGenerate(id)} icon={<Wand2 />} /> : null}{resource === 'words' && id ? <IconAction label={t('vocabulary.admin.archive')} disabled={pending || status === VocabularyContentStatus.Archived} onClick={() => onArchiveWord(id)} icon={<Archive />} /> : null}{resource === 'questions' && id ? <IconAction label={t('vocabulary.admin.publish')} disabled={pending || status === VocabularyContentStatus.Published} onClick={() => onPublish(id)} icon={<Send />} /> : null}{resource === 'questions' && id ? <IconAction label={t('vocabulary.admin.archive')} disabled={pending || status === VocabularyContentStatus.Archived} onClick={() => onArchiveQuestion(id)} icon={<Archive />} /> : null}</div></div>
}

function Status({ status }: { status: number }) {
  const { t } = useTranslation()
  const label = status === VocabularyContentStatus.Published ? t('vocabulary.admin.published') : status === VocabularyContentStatus.Archived ? t('vocabulary.admin.archived') : t('vocabulary.admin.draft')
  const color = status === VocabularyContentStatus.Published ? 'bg-emerald-50 text-emerald-700' : status === VocabularyContentStatus.Archived ? 'bg-slate-100 text-slate-600' : 'bg-amber-50 text-amber-700'
  return <span className={cn('inline-flex w-fit rounded-md px-2 py-1 text-xs font-black', color)}>{label}</span>
}

function rowKey(item: { id?: string; languageId?: string; localDate?: string }) {
  return item.id ?? `${item.languageId}-${item.localDate}`
}

function VocabularyCreateForm({ resource, languages, topics, words, onClose }: { resource: Resource; languages: LearningLanguageDto[]; topics: VocabularyTopicDto[]; words: VocabularyWordDto[]; onClose: () => void }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [language, setLanguage] = useState<LanguageUpsertRequest>({ code: '', nameTg: '', nameRu: '', displayOrder: languages.length + 1, isActive: true })
  const [topic, setTopic] = useState<TopicUpsertRequest>({ languageId: languages[0]?.id ?? '', level: 0, nameTg: '', nameRu: '', descriptionTg: '', descriptionRu: '', status: VocabularyContentStatus.Published })
  const [word, setWord] = useState<WordUpsertRequest>({ languageId: languages[0]?.id ?? '', topicId: topics[0]?.id ?? '', level: 0, targetText: '', translationTg: '', translationRu: '', explanationTg: '', explanationRu: '', exampleTarget: '', exampleTg: '', exampleRu: '', status: VocabularyContentStatus.Draft, relations: [] })
  const [daily, setDaily] = useState<DailyWordUpsertRequest>({ languageId: languages[0]?.id ?? '', localDate: new Date().toISOString().slice(0, 10), wordId: words[0]?.id ?? '' })
  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: vocabularyKeys.admin.all })
    onClose()
  }
  const languageMutation = useMutation({ mutationFn: adminVocabularyApi.createLanguage, onSuccess: invalidate })
  const topicMutation = useMutation({ mutationFn: adminVocabularyApi.createTopic, onSuccess: invalidate })
  const wordMutation = useMutation({ mutationFn: adminVocabularyApi.createWord, onSuccess: invalidate })
  const dailyMutation = useMutation({ mutationFn: adminVocabularyApi.upsertDailyWord, onSuccess: invalidate })
  const languageOptions = languages.map((item) => ({ value: item.id, label: item.name }))
  const topicOptions = topics.filter((item) => !word.languageId || item.languageId === word.languageId).map((item) => ({ value: item.id, label: `${vocabularyLevelLabel(item.level)} · ${item.name}` }))
  const wordOptions = words.filter((item) => !daily.languageId || item.languageId === daily.languageId).map((item) => ({ value: item.id, label: `${item.targetText} · ${item.translation}` }))
  const levelOptions = [0, 1, 2, 3, 4, 5].map((item) => ({ value: String(item), label: vocabularyLevelLabel(item) }))
  const statusOptions = [
    { value: String(VocabularyContentStatus.Draft), label: t('vocabulary.admin.draft') },
    { value: String(VocabularyContentStatus.Published), label: t('vocabulary.admin.published') },
    { value: String(VocabularyContentStatus.Archived), label: t('vocabulary.admin.archived') },
  ]
  const pending = languageMutation.isPending || topicMutation.isPending || wordMutation.isPending || dailyMutation.isPending
  const error = languageMutation.isError || topicMutation.isError || wordMutation.isError || dailyMutation.isError

  function submit() {
    if (resource === 'languages') languageMutation.mutate(language)
    if (resource === 'topics') topicMutation.mutate(topic)
    if (resource === 'words') wordMutation.mutate(word)
    if (resource === 'dailyWords') dailyMutation.mutate(daily)
  }

  return (
    <div className="mt-4 rounded-lg border border-[var(--border)] bg-[var(--surface-soft)] p-4">
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="text-base font-black">{t('vocabulary.admin.add')}</h2>
        <button type="button" className="grid h-9 w-9 place-items-center rounded-lg text-[var(--muted)] hover:bg-white hover:text-[var(--text)]" onClick={onClose} aria-label={t('cancel')}><X className="h-4 w-4" /></button>
      </div>
      {resource === 'languages' ? (
        <div className="grid gap-3 md:grid-cols-2">
          <FormField label={t('vocabulary.admin.code')}><Input value={language.code} onChange={(event) => setLanguage((current) => ({ ...current, code: event.target.value }))} /></FormField>
          <FormField label={t('order')}><Input type="number" value={language.displayOrder} onChange={(event) => setLanguage((current) => ({ ...current, displayOrder: Number(event.target.value) }))} /></FormField>
          <FormField label={t('vocabulary.admin.nameTg')}><Input value={language.nameTg} onChange={(event) => setLanguage((current) => ({ ...current, nameTg: event.target.value }))} /></FormField>
          <FormField label={t('vocabulary.admin.nameRu')}><Input value={language.nameRu} onChange={(event) => setLanguage((current) => ({ ...current, nameRu: event.target.value }))} /></FormField>
        </div>
      ) : null}
      {resource === 'topics' ? (
        <div className="grid gap-3 md:grid-cols-2">
          <FormField label={t('vocabulary.language')}><SelectField searchable value={topic.languageId} options={languageOptions} onValueChange={(value) => setTopic((current) => ({ ...current, languageId: value }))} /></FormField>
          <FormField label={t('vocabulary.level')}><SelectField value={String(topic.level)} options={levelOptions} onValueChange={(value) => setTopic((current) => ({ ...current, level: Number(value) }))} /></FormField>
          <FormField label={t('vocabulary.admin.nameTg')}><Input value={topic.nameTg} onChange={(event) => setTopic((current) => ({ ...current, nameTg: event.target.value }))} /></FormField>
          <FormField label={t('vocabulary.admin.nameRu')}><Input value={topic.nameRu} onChange={(event) => setTopic((current) => ({ ...current, nameRu: event.target.value }))} /></FormField>
          <FormField label={t('descriptionTg')}><Textarea value={topic.descriptionTg ?? ''} onChange={(event) => setTopic((current) => ({ ...current, descriptionTg: event.target.value }))} /></FormField>
          <FormField label={t('descriptionRu')}><Textarea value={topic.descriptionRu ?? ''} onChange={(event) => setTopic((current) => ({ ...current, descriptionRu: event.target.value }))} /></FormField>
          <FormField label={t('vocabulary.admin.status')}><SelectField value={String(topic.status)} options={statusOptions} onValueChange={(value) => setTopic((current) => ({ ...current, status: Number(value) }))} /></FormField>
        </div>
      ) : null}
      {resource === 'words' ? (
        <div className="grid gap-3 md:grid-cols-2">
          <FormField label={t('vocabulary.language')}><SelectField searchable value={word.languageId} options={languageOptions} onValueChange={(value) => setWord((current) => ({ ...current, languageId: value, topicId: '' }))} /></FormField>
          <FormField label={t('vocabulary.admin.topics')}><SelectField searchable value={word.topicId} options={topicOptions} onValueChange={(value) => setWord((current) => ({ ...current, topicId: value }))} /></FormField>
          <FormField label={t('vocabulary.level')}><SelectField value={String(word.level)} options={levelOptions} onValueChange={(value) => setWord((current) => ({ ...current, level: Number(value) }))} /></FormField>
          <FormField label={t('vocabulary.admin.status')}><SelectField value={String(word.status)} options={statusOptions} onValueChange={(value) => setWord((current) => ({ ...current, status: Number(value) }))} /></FormField>
          <FormField label={t('vocabulary.admin.targetText')}><Input value={word.targetText} onChange={(event) => setWord((current) => ({ ...current, targetText: event.target.value }))} /></FormField>
          <FormField label={t('vocabulary.admin.translationTg')}><Input value={word.translationTg} onChange={(event) => setWord((current) => ({ ...current, translationTg: event.target.value }))} /></FormField>
          <FormField label={t('vocabulary.admin.translationRu')}><Input value={word.translationRu} onChange={(event) => setWord((current) => ({ ...current, translationRu: event.target.value }))} /></FormField>
          <FormField label={t('vocabulary.admin.exampleTarget')}><Textarea value={word.exampleTarget} onChange={(event) => setWord((current) => ({ ...current, exampleTarget: event.target.value }))} /></FormField>
          <FormField label={t('vocabulary.admin.exampleTg')}><Textarea value={word.exampleTg} onChange={(event) => setWord((current) => ({ ...current, exampleTg: event.target.value }))} /></FormField>
          <FormField label={t('vocabulary.admin.exampleRu')}><Textarea value={word.exampleRu} onChange={(event) => setWord((current) => ({ ...current, exampleRu: event.target.value }))} /></FormField>
          <FormField label={t('vocabulary.admin.explanationTg')}><Textarea value={word.explanationTg ?? ''} onChange={(event) => setWord((current) => ({ ...current, explanationTg: event.target.value }))} /></FormField>
          <FormField label={t('vocabulary.admin.explanationRu')}><Textarea value={word.explanationRu ?? ''} onChange={(event) => setWord((current) => ({ ...current, explanationRu: event.target.value }))} /></FormField>
        </div>
      ) : null}
      {resource === 'dailyWords' ? (
        <div className="grid gap-3 md:grid-cols-3">
          <FormField label={t('vocabulary.language')}><SelectField searchable value={daily.languageId} options={languageOptions} onValueChange={(value) => setDaily((current) => ({ ...current, languageId: value, wordId: '' }))} /></FormField>
          <FormField label={t('vocabulary.dailyWord')}><SelectField searchable value={daily.wordId} options={wordOptions} onValueChange={(value) => setDaily((current) => ({ ...current, wordId: value }))} /></FormField>
          <FormField label={t('date')}><Input type="date" value={daily.localDate} onChange={(event) => setDaily((current) => ({ ...current, localDate: event.target.value }))} /></FormField>
        </div>
      ) : null}
      {error ? <p className="mt-3 text-sm font-bold text-[var(--danger)]">{t('saveFailed')}</p> : null}
      <div className="mt-4 flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={onClose}>{t('cancel')}</Button>
        <Button type="button" disabled={pending} onClick={submit}>{pending ? t('saving') : t('save')}</Button>
      </div>
    </div>
  )
}

function IconAction({ label, icon, onClick, disabled }: { label: string; icon: ReactNode; onClick: () => void; disabled?: boolean }) {
  return <button type="button" title={label} aria-label={label} disabled={disabled} onClick={onClick} className="grid h-9 w-9 place-items-center rounded-lg border border-[var(--border)] bg-white text-[var(--muted)] shadow-sm transition hover:bg-[var(--surface-soft)] hover:text-[var(--primary-strong)] disabled:cursor-not-allowed disabled:opacity-45">{icon}</button>
}
