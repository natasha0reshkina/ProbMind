import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { BarChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import type { QuestionAnalytics, Topic } from '../types/api'
import { duration, number, percent } from '../utils/format'

async function exportQuestions(topicId: string) {
  const response = await api.get(`/exports/questions${topicId ? `?topicId=${topicId}` : ''}`, { responseType: 'blob' })
  const url = URL.createObjectURL(response.data)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = topicId ? `statistika-zadaniy-${topicId}.xlsx` : 'statistika-zadaniy.xlsx'
  anchor.click()
  URL.revokeObjectURL(url)
}

function shortPrompt(row: QuestionAnalytics) {
  const cleaned = row.prompt.replace(/\s+/g, ' ').trim()
  const short = cleaned.length > 46 ? `${cleaned.slice(0, 43)}…` : cleaned
  return `${row.topicName} · ${short}`
}

function buildTopicErrorRates(rows: QuestionAnalytics[]) {
  const grouped = new Map<string, { responses: number; correct: number }>()

  for (const row of rows) {
    if (row.responses <= 0) continue
    const current = grouped.get(row.topicName) ?? { responses: 0, correct: 0 }
    current.responses += row.responses
    current.correct += Math.min(1, Math.max(0, row.correctRate)) * row.responses
    grouped.set(row.topicName, current)
  }

  return [...grouped.entries()]
    .map(([topic, values]) => ({
      topic,
      errorRate: values.responses ? 1 - values.correct / values.responses : 0,
      responses: values.responses,
    }))
    .sort((a, b) => b.errorRate - a.errorRate)
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
  const weightedCorrect = rows.reduce((sum, row) => sum + Math.min(1, Math.max(0, row.correctRate)) * row.responses, 0) / Math.max(1, totalResponses)
  const withResponses = rows.filter((row) => row.responses > 0)
  const meanDiscrimination = withResponses.length ? withResponses.reduce((sum, row) => sum + row.discrimination, 0) / withResponses.length : 0
  const weak = withResponses.filter((row) => row.discrimination < .15 || row.qualityBand === 'review').length
  const slow = [...withResponses]
    .filter((row) => row.medianResponseSeconds > 0)
    .sort((a, b) => b.medianResponseSeconds - a.medianResponseSeconds)
    .slice(0, 8)
    .map((row) => ({ label: shortPrompt(row), seconds: row.medianResponseSeconds }))
  const topicErrors = buildTopicErrorRates(withResponses)

  const columns: DataColumn<QuestionAnalytics>[] = useMemo(() => [
    {
      key: 'question',
      title: 'Задание',
      width: '42%',
      render: (row) => <div><strong>{row.topicName}</strong><span className="table-secondary question-prompt-cell">{row.prompt}</span></div>,
      sortValue: (row) => row.prompt,
    },
    { key: 'responses', title: 'Ответов', render: (row) => row.responses.toLocaleString('ru-RU'), sortValue: (row) => row.responses, align: 'right' as const },
    { key: 'correct', title: 'Правильных', render: (row) => row.responses ? percent(Math.min(1, Math.max(0, row.correctRate)), 0) : '-', sortValue: (row) => row.correctRate, align: 'right' as const },
    { key: 'difficulty', title: 'Факт. сложность', render: (row) => row.responses ? number(row.difficulty, 2) : '-', sortValue: (row) => row.difficulty, align: 'right' as const },
    { key: 'discrimination', title: 'Различение', render: (row) => row.responses ? number(row.discrimination, 2) : '-', sortValue: (row) => row.discrimination, align: 'right' as const },
    { key: 'time', title: 'Время ответа', render: (row) => row.responses ? duration(row.medianResponseSeconds) : '-', sortValue: (row) => row.medianResponseSeconds },
    { key: 'quality', title: 'Состояние', render: (row) => <StatusBadge value={row.qualityBand} />, sortValue: (row) => row.qualityBand },
  ], [])

  return (
    <div>
      <PageHeader
        eyebrow="Преподаватель"
        title="Статистика заданий"
        description="Короткая сводка по тому, где студенты ошибаются и какие задания занимают больше времени."
        actions={<button className="secondary-button" onClick={() => void exportQuestions(topicId)}>Скачать таблицу</button>}
      />

      <section className="plain-section analytics-filter-row">
        <label>Показать тему
          <select value={topicId} onChange={(event) => setTopicId(event.target.value)}>
            <option value="">Все темы</option>
            {(topics.data ?? []).map((topic) => <option key={topic.id} value={topic.id}>{topic.nameRu}</option>)}
          </select>
        </label>
      </section>

      <KpiStrip items={[
        { label: 'Заданий с ответами', value: withResponses.length },
        { label: 'Всего ответов', value: totalResponses.toLocaleString('ru-RU') },
        { label: 'Правильных ответов', value: totalResponses ? percent(weightedCorrect) : '-' },
        { label: 'Среднее различение', value: withResponses.length ? number(meanDiscrimination, 2) : '-' },
        { label: 'Стоит проверить', value: weak, tone: weak ? 'warning' : 'positive' },
      ]} />

      <div className="two-column question-analytics-charts">
        <BarChartPanel
          title="Ошибки по темам"
          subtitle="Доля неправильных ответов по каждой теме. Чем длиннее полоса, тем чаще студенты ошибаются."
          data={topicErrors}
          xKey="topic"
          series={[{ key: 'errorRate', label: 'Ошибки' }]}
          percent
          horizontal
          height={320}
        />
        <BarChartPanel
          title="Где отвечают дольше"
          subtitle="Медианное время ответа."
          data={slow}
          xKey="label"
          series={[{ key: 'seconds', label: 'Секунды' }]}
          height={320}
          horizontal
        />
      </div>

      <section className="panel question-metrics-panel">
        <div className="panel-title"><div><h2>Все задания</h2><p className="muted chart-subtitle">Можно искать по теме или тексту условия и сортировать по любому показателю.</p></div></div>
        <DataTable
          rows={rows}
          columns={columns}
          rowKey={(row) => row.questionId}
          searchText={(row) => `${row.topicName} ${row.prompt} ${row.code} ${row.qualityBand}`}
          searchPlaceholder="Тема или текст задания…"
          pageSize={14}
        />
      </section>
    </div>
  )
}
