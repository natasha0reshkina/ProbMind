import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Archive, CirclePlus, Eye, Filter, History, Send, SquarePen } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge, statusLabel } from '../components/StatusBadge'
import type { ContentStatus, QuestionDetail, QuestionSummary, Topic } from '../types/api'
import { dateTime } from '../utils/format'

interface QuestionRow extends QuestionSummary { topicName: string }

const statusLabels: Record<ContentStatus, string> = {
  Published: 'Опубликовано',
  Draft: 'Черновик',
  Archived: 'Архив',
}

export function ContentManagerPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [topicId, setTopicId] = useState('')
  const [status, setStatus] = useState('')

  const topics = useQuery({
    queryKey: ['content-topics'],
    queryFn: async () => (await api.get<Topic[]>('/content/topics')).data,
  })
  const questions = useQuery({
    queryKey: ['teacher-questions', topicId, status],
    queryFn: async () => (await api.get<QuestionSummary[]>('/content/questions', { params: { ...(topicId ? { topicId } : {}), ...(status ? { status } : {}) } })).data,
  })
  const topicById = useMemo(() => new Map((topics.data ?? []).map((item) => [item.id, item])), [topics.data])
  const rows = useMemo<QuestionRow[]>(() => (questions.data ?? []).map((q) => ({ ...q, topicName: topicById.get(q.topicId)?.nameRu ?? '-' })), [questions.data, topicById])

  const mutateStatus = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: 'publish' | 'archive' }) => (await api.post<QuestionDetail>(`/content/questions/${id}/${action}`)).data,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['teacher-questions'] }),
  })

  const counts = {
    published: rows.filter((row) => row.status === 'Published').length,
    draft: rows.filter((row) => row.status === 'Draft').length,
    archived: rows.filter((row) => row.status === 'Archived').length,
    transfer: rows.filter((row) => row.kind === 'Transfer').length,
  }

  const columns: DataColumn<QuestionRow>[] = [
    { key: 'code', title: 'Код', render: (row) => <div><code>{row.code}</code><span className="table-secondary">ID {row.id.slice(0, 8)}</span></div>, sortValue: (row) => row.code },
    { key: 'topic', title: 'Тема', render: (row) => row.topicName, sortValue: (row) => row.topicName },
    { key: 'kind', title: 'Тип', render: (row) => <span className="soft-label">{statusLabel(row.kind)}</span>, sortValue: (row) => row.kind },
    { key: 'version', title: 'Версия', render: (row) => <strong>v{row.currentVersionNumber}</strong>, sortValue: (row) => row.currentVersionNumber, align: 'center' },
    { key: 'status', title: 'Статус', render: (row) => <StatusBadge value={row.status} />, sortValue: (row) => row.status },
    { key: 'published', title: 'Публикация', render: (row) => dateTime(row.publishedAt), sortValue: (row) => row.publishedAt ? new Date(row.publishedAt).getTime() : 0 },
    {
      key: 'actions', title: '', align: 'right', render: (row) => (
        <div className="row-actions" onClick={(event) => event.stopPropagation()}>
          <button className="icon-button" title="Открыть редактор" onClick={() => navigate(`/teacher/content/${row.id}`)}><SquarePen size={16} /></button>
          {row.status === 'Draft' && <button className="icon-button" title="Опубликовать" onClick={() => mutateStatus.mutate({ id: row.id, action: 'publish' })}><Send size={16} /></button>}
          {row.status !== 'Archived' && <button className="icon-button" title="Архивировать" onClick={() => mutateStatus.mutate({ id: row.id, action: 'archive' })}><Archive size={16} /></button>}
        </div>
      ),
    },
  ]

  return (
    <div>
      <PageHeader
        eyebrow="Управление банком заданий"
        title="Банк диагностических и коррекционных заданий"
        description="У каждого задания сохраняется история версий. Старые диагностические ответы остаются связаны с той формулировкой, которую видел студент, а редактирование создаёт новую версию."
        actions={<button className="primary-button" onClick={() => navigate('/teacher/content/new')}><CirclePlus size={17} /> Новое задание</button>}
      />

      <KpiStrip items={[
        { label: 'В текущей выборке', value: rows.length },
        { label: 'Опубликовано', value: counts.published, tone: 'positive' },
        { label: 'Черновиков', value: counts.draft, tone: counts.draft ? 'warning' : 'default' },
        { label: 'В архиве', value: counts.archived },
        { label: 'Заданий на перенос', value: counts.transfer },
      ]} />

      <section className="panel filter-panel">
        <div className="filter-panel__title"><Filter size={18} /><strong>Фильтры банка</strong></div>
        <div className="filters">
          <label>Тема
            <select value={topicId} onChange={(event) => setTopicId(event.target.value)}>
              <option value="">Все темы</option>
              {(topics.data ?? []).map((topic) => <option value={topic.id} key={topic.id}>{topic.nameRu}</option>)}
            </select>
          </label>
          <label>Статус
            <select value={status} onChange={(event) => setStatus(event.target.value)}>
              <option value="">Все статусы</option>
              {(['Published', 'Draft', 'Archived'] as ContentStatus[]).map((item) => <option key={item} value={item}>{statusLabels[item]}</option>)}
            </select>
          </label>
          <button className="ghost-button" onClick={() => { setTopicId(''); setStatus('') }}>Сбросить</button>
        </div>
      </section>

      <section className="panel">
        <div className="panel-title">
          <div><h2>Банк заданий</h2><p className="muted chart-subtitle">По строке открывается редактор версии задания, вариантов ответа и связей с типичными заблуждениями.</p></div>
          <div className="inline-badges"><span><Eye size={14} /> {counts.published} опубликовано</span><span><History size={14} /> с версиями</span></div>
        </div>
        <DataTable rows={rows} columns={columns} rowKey={(row) => row.id} searchText={(row) => `${row.code} ${row.topicName} ${row.kind} ${row.status}`} pageSize={16} onRowClick={(row) => navigate(`/teacher/content/${row.id}`)} />
      </section>
    </div>
  )
}
