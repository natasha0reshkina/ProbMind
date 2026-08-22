import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Activity, Filter, Gauge, Target } from 'lucide-react'
import { api } from '../api/client'
import { BarChartPanel, ScatterChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import type { PsychometricItem, Topic } from '../types/api'
import { duration, number, percent } from '../utils/format'

export function PsychometricsPage() {
  const [topicId, setTopicId] = useState('')
  const topics = useQuery({
    queryKey: ['content-topics'],
    queryFn: async () => (await api.get<Topic[]>('/content/topics')).data,
  })
  const items = useQuery({
    queryKey: ['psychometrics-items', topicId],
    queryFn: async () => (await api.get<PsychometricItem[]>('/advanced-analytics/psychometrics/items', { params: topicId ? { topicId } : {} })).data,
  })

  const rows = items.data ?? []
  const meanDiscrimination = rows.length ? rows.reduce((sum, row) => sum + row.discrimination, 0) / rows.length : 0
  const meanInformation = rows.length ? rows.reduce((sum, row) => sum + row.informationAtAverageAbility, 0) / rows.length : 0
  const problematic = rows.filter((row) => row.qualityBand === 'review' || row.discrimination < .15).length
  const highInfo = rows.filter((row) => row.informationAtAverageAbility >= .5).length

  const scatter = rows.map((row) => ({
    code: row.code,
    difficulty: row.irtDifficulty,
    discrimination: row.discrimination,
  }))
  const information = [...rows].sort((a, b) => b.informationAtAverageAbility - a.informationAtAverageAbility).slice(0, 14).map((row) => ({
    code: row.code,
    information: row.informationAtAverageAbility,
  }))

  const columns: DataColumn<PsychometricItem>[] = useMemo(() => [
    { key: 'code', title: 'Задание', render: (row) => <div><strong>{row.code}</strong><span className="table-secondary">{row.topicCode}</span></div>, sortValue: (row) => row.code },
    { key: 'responses', title: 'N', render: (row) => row.responses, sortValue: (row) => row.responses, align: 'right' as const },
    { key: 'correct', title: 'Доля правильных', render: (row) => percent(row.correctRate), sortValue: (row) => row.correctRate, align: 'right' as const },
    { key: 'difficulty', title: 'Сложность IRT (b)', render: (row) => number(row.irtDifficulty, 2), sortValue: (row) => row.irtDifficulty, align: 'right' as const },
    { key: 'disc', title: 'Дискриминативность', render: (row) => <strong>{number(row.discrimination, 2)}</strong>, sortValue: (row) => row.discrimination, align: 'right' as const },
    { key: 'info', title: 'Информация I(θ≈0)', render: (row) => number(row.informationAtAverageAbility, 2), sortValue: (row) => row.informationAtAverageAbility, align: 'right' as const },
    { key: 'time', title: 'Медианное время', render: (row) => duration(row.medianResponseSeconds), sortValue: (row) => row.medianResponseSeconds },
    { key: 'quality', title: 'Качество', render: (row) => <StatusBadge value={row.qualityBand} />, sortValue: (row) => row.qualityBand },
  ], [])

  return (
    <div>
      <PageHeader
        eyebrow="Психометрика"
        title="Качество диагностических заданий"
        description="Для каждого задания рассчитываются эмпирическая сложность, дискриминативность, параметры IRT, информативность и время ответа."
      />

      <section className="panel filter-panel">
        <div className="filter-panel__title"><Filter size={18} /><strong>Срез данных</strong></div>
        <label>Тема
          <select value={topicId} onChange={(event) => setTopicId(event.target.value)}>
            <option value="">Все темы</option>
            {(topics.data ?? []).map((topic) => <option key={topic.id} value={topic.id}>{topic.nameRu}</option>)}
          </select>
        </label>
      </section>

      <KpiStrip items={[
        { label: 'Заданий', value: rows.length },
        { label: 'Средняя дискриминативность', value: number(meanDiscrimination, 2), tone: meanDiscrimination >= .25 ? 'positive' : 'warning' },
        { label: 'Средняя информативность', value: number(meanInformation, 2) },
        { label: 'Заданий для проверки', value: problematic, tone: problematic ? 'warning' : 'positive' },
        { label: 'Высокоинформативных заданий', value: highInfo, hint: 'I(θ≈0) ≥ 0.5' },
      ]} />

      <div className="two-column">
        <ScatterChartPanel
          title="Сложность IRT × дискриминативность"
          subtitle="Хорошее задание различает уровень подготовки студентов и имеет подходящую сложность."
          data={scatter}
          xKey="difficulty"
          yKey="discrimination"
          xLabel="Сложность IRT (b)"
          yLabel="Дискриминативность"
          height={340}
        />
        <BarChartPanel
          title="Информативность около среднего уровня"
          subtitle="Задания, которые лучше различают студентов около среднего уровня θ."
          data={information}
          xKey="code"
          series={[{ key: 'information', label: 'Информативность' }]}
          height={340}
        />
      </div>

      <section className="panel">
        <div className="panel-title"><div><h2>Анализ заданий</h2><p className="muted chart-subtitle">Таблица поддерживает полнотекстовый поиск и сортировку по каждой психометрической метрике.</p></div><Target size={20} /></div>
        <DataTable rows={rows} columns={columns} rowKey={(row) => row.questionId} searchText={(row) => `${row.code} ${row.topicCode} ${row.qualityBand}`} pageSize={16} />
      </section>

      <section className="insight-grid">
        <article className="insight-card"><Gauge size={20} /><div><strong>Дискриминативность</strong><p>Низкое значение означает, что вопрос плохо различает более и менее подготовленных студентов и может требовать переработки.</p></div></article>
        <article className="insight-card"><Activity size={20} /><div><strong>Информационность задания</strong><p>Информационная функция помогает выбирать задания, которые лучше уточняют оценку уровня знаний студента в нужном диапазоне.</p></div></article>
      </section>
    </div>
  )
}
