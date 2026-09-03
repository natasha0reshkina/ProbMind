import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Download, Filter, Gauge } from 'lucide-react'
import { api } from '../api/client'
import { BarChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import type { QuestionAnalytics, Topic } from '../types/api'
import { duration, number, percent, questionLabel } from '../utils/format'

async function exportQuestions(topicId: string) {
  const response = await api.get(`/exports/questions${topicId ? `?topicId=${topicId}` : ''}`, { responseType: 'blob' })
  const url = URL.createObjectURL(response.data)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = topicId ? `statistika-zadaniy-${topicId}.xlsx` : 'statistika-zadaniy.xlsx'
  anchor.click()
  URL.revokeObjectURL(url)
}

export function QuestionAnalyticsPage() {
  const [topicId, setTopicId] = useState('')
  const topics = useQuery({ queryKey: ['content-topics'], queryFn: async () => (await api.get<Topic[]>('/content/topics')).data })
  const analytics = useQuery({
    queryKey: ['question-analytics', topicId],
    queryFn: async () => (await api.get<QuestionAnalytics[]>('/teacher/questions/analytics', { params: topicId ? { topicId } : {} })).data,
  })

  const rows = analytics.data ?? []
  const totalResponses = rows.reduce((sum, row) => sum + row.responses, 0)
  const weightedCorrect = rows.reduce((sum, row) => sum + row.correctRate * row.responses, 0) / Math.max(1, totalResponses)
  const meanDiscrimination = rows.length ? rows.reduce((sum, row) => sum + row.discrimination, 0) / rows.length : 0
  const weak = rows.filter((row) => row.discrimination < .15 || row.qualityBand === 'review').length
  const slow = [...rows].sort((a, b) => b.medianResponseSeconds - a.medianResponseSeconds).slice(0, 10).map((row) => ({ code: questionLabel(row.code), seconds: row.medianResponseSeconds }))
  const hardest = [...rows].filter((row) => row.responses > 0).sort((a, b) => a.correctRate - b.correctRate).slice(0, 12).map((row) => ({ code: questionLabel(row.code), correctRate: row.correctRate }))

  const columns: DataColumn<QuestionAnalytics>[] = useMemo(() => [
    { key: 'code', title: 'Задание', render: (row) => <strong>{questionLabel(row.code)}</strong>, sortValue: (row) => row.code },
    { key: 'responses', title: 'Ответов', render: (row) => row.responses.toLocaleString('ru-RU'), sortValue: (row) => row.responses, align: 'right' as const },
    { key: 'correct', title: 'Доля правильных', render: (row) => percent(row.correctRate, 1), sortValue: (row) => row.correctRate, align: 'right' as const },
    { key: 'difficulty', title: 'Сложность', render: (row) => number(row.difficulty, 2), sortValue: (row) => row.difficulty, align: 'right' as const },
    { key: 'discrimination', title: 'Различающая способность', render: (row) => <strong>{number(row.discrimination, 2)}</strong>, sortValue: (row) => row.discrimination, align: 'right' as const },
    { key: 'entropy', title: 'Распределение вариантов', render: (row) => number(row.distractorEntropy, 2), sortValue: (row) => row.distractorEntropy, align: 'right' as const },
    { key: 'time', title: 'Медианное время', render: (row) => duration(row.medianResponseSeconds), sortValue: (row) => row.medianResponseSeconds },
    { key: 'quality', title: 'Качество', render: (row) => <StatusBadge value={row.qualityBand} />, sortValue: (row) => row.qualityBand },
  ], [])

  return (
    <div>
      <PageHeader
        eyebrow="Аналитика заданий"
        title="Поведение диагностических заданий"
        description="Аналитика банка заданий: фактическая сложность, доля правильных ответов, различающая способность, распределение вариантов и время ответа. Экран помогает находить задания, которые требуют пересмотра."
        actions={<button className="secondary-button" onClick={() => void exportQuestions(topicId)}><Download size={16} /> Скачать таблицу</button>}
      />

      <section className="panel filter-panel">
        <div className="filter-panel__title"><Filter size={18} /><strong>Тема</strong></div>
        <label>Тема
          <select value={topicId} onChange={(event) => setTopicId(event.target.value)}>
            <option value="">Все темы</option>
            {(topics.data ?? []).map((topic) => <option key={topic.id} value={topic.id}>{topic.nameRu}</option>)}
          </select>
        </label>
      </section>

      <KpiStrip items={[
        { label: 'Заданий', value: rows.length },
        { label: 'Ответов', value: totalResponses.toLocaleString('ru-RU') },
        { label: 'Доля правильных ответов', value: totalResponses ? percent(weightedCorrect) : '—', tone: totalResponses && weightedCorrect >= .65 ? 'positive' : 'default' },
        { label: 'Средняя различающая способность', value: rows.length ? number(meanDiscrimination, 2) : '—', tone: rows.length && meanDiscrimination >= .2 ? 'positive' : 'default' },
        { label: 'Заданий для проверки', value: weak, tone: weak ? 'warning' : 'positive' },
      ]} />

      <div className="two-column">
        <BarChartPanel
          title="Задания с наименьшей долей правильных ответов"
          subtitle="Сначала показаны задания, в которых студенты чаще всего ошибаются."
          data={hardest}
          xKey="code"
          series={[{ key: 'correctRate', label: 'Правильные ответы' }]}
          percent
          horizontal
          height={340}
        />
        <BarChartPanel title="Самые долгие вопросы" subtitle="Медианное время ответа помогает находить перегруженные формулировки." data={slow} xKey="code" series={[{ key: 'seconds', label: 'Секунды' }]} height={340} horizontal />
      </div>

      <section className="panel">
        <div className="panel-title"><div><h2>Показатели заданий</h2><p className="muted chart-subtitle">Показатели помогают определить, какие задания стоит проверить и переработать в первую очередь.</p></div><Gauge size={20} /></div>
        <DataTable rows={rows} columns={columns} rowKey={(row) => row.questionId} searchText={(row) => `${questionLabel(row.code)} ${row.qualityBand}`} pageSize={16} />
      </section>

    </div>
  )
}
