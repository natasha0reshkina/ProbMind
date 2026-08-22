import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Activity, FlaskConical, Network, Sigma } from 'lucide-react'
import { api } from '../api/client'
import { BarChartPanel, ScatterChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import type { CalibrationReport, CooccurrenceEdge, MisconceptionCluster } from '../types/api'
import { number, percent } from '../utils/format'

export function ResearchAnalyticsPage() {
  const cooccurrence = useQuery({
    queryKey: ['research-cooccurrence'],
    queryFn: async () => (await api.get<CooccurrenceEdge[]>('/research/misconceptions/cooccurrence')).data,
  })
  const calibration = useQuery({
    queryKey: ['research-calibration'],
    queryFn: async () => (await api.get<CalibrationReport>('/research/diagnostics/calibration')).data,
  })
  const clusters = useQuery({
    queryKey: ['research-clusters'],
    queryFn: async () => (await api.get<MisconceptionCluster[]>('/advanced-analytics/cohort/clusters?clusters=4')).data,
  })

  const edges = cooccurrence.data ?? []
  const strongest = [...edges].sort((a, b) => b.lift - a.lift).slice(0, 10)
  const maxLift = strongest[0]?.lift ?? 0
  const meanLift = edges.length ? edges.reduce((sum, edge) => sum + edge.lift, 0) / edges.length : 0
  const highAssociations = edges.filter((edge) => edge.lift >= 1.5).length

  const calibrationPoints = (calibration.data?.buckets ?? []).map((bucket) => ({
    predicted: Math.round(((bucket.from + bucket.to) / 2) * 100),
    observed: Math.round(bucket.observedRate * 100),
    наблюдений: bucket.наблюдений,
  }))

  const clusterBars = (clusters.data ?? []).map((cluster) => ({
    cluster: `C${cluster.cluster}: ${cluster.label}`,
    learners: cluster.learners,
  }))

  const columns: DataColumn<CooccurrenceEdge>[] = useMemo(() => [
    { key: 'a', title: 'Заблуждение A', render: (row) => <code>{row.misconceptionACode}</code>, sortValue: (row) => row.misconceptionACode },
    { key: 'b', title: 'Заблуждение B', render: (row) => <code>{row.misconceptionBCode}</code>, sortValue: (row) => row.misconceptionBCode },
    { key: 'together', title: 'Вместе', render: (row) => row.together, sortValue: (row) => row.together, align: 'right' as const },
    { key: 'jaccard', title: 'Jaccard', render: (row) => number(row.jaccard, 3), sortValue: (row) => row.jaccard, align: 'right' as const },
    { key: 'lift', title: 'Lift', render: (row) => <strong>{number(row.lift, 2)}</strong>, sortValue: (row) => row.lift, align: 'right' as const },
  ], [])

  return (
    <div>
      <PageHeader
        eyebrow="Исследовательская аналитика"
        title="Исследовательская аналитика"
        description="Инструменты для анализа совместной встречаемости заблуждений, калибровки диагностической уверенности и структуры профилей группы. Расчёты выполняются по накопленным данным студентов."
      />

      <KpiStrip items={[
        { label: 'Пар заблуждений', value: edges.length },
        { label: 'Средний lift', value: number(meanLift, 2), hint: 'ассоциация относительно независимости' },
        { label: 'Максимальный lift', value: number(maxLift, 2), tone: maxLift > 2 ? 'warning' : 'default' },
        { label: 'Сильных ассоциаций', value: highAssociations, hint: 'lift ≥ 1.5' },
        { label: 'Brier score', value: number(calibration.data?.brierScore, 3), tone: (calibration.data?.brierScore ?? 1) < .2 ? 'positive' : 'warning' },
      ]} />

      <div className="two-column">
        <ScatterChartPanel
          title="Калибровка диагностической уверенности"
          subtitle="При хорошей калибровке прогнозируемая уверенность близка к фактической доле подтверждений."
          data={calibrationPoints}
          xKey="predicted"
          yKey="observed"
          xLabel="Прогноз, %"
          yLabel="Подтверждение, %"
          height={330}
        />
        <BarChartPanel
          title="Когнитивные кластеры"
          subtitle="Группировка студентов по профилям диагностической уверенности."
          data={clusterBars}
          xKey="cluster"
          series={[{ key: 'learners', label: 'Студенты' }]}
          height={330}
        />
      </div>

      <section className="panel">
        <div className="panel-title">
          <div><h2>Совместная встречаемость заблуждений</h2><p className="muted chart-subtitle">Lift больше 1 означает, что два типа ошибок встречаются вместе чаще, чем ожидалось при независимости.</p></div>
          <Network size={20} />
        </div>
        <DataTable
          rows={edges}
          columns={columns}
          rowKey={(row) => `${row.misconceptionAId}-${row.misconceptionBId}`}
          searchText={(row) => `${row.misconceptionACode} ${row.misconceptionBCode}`}
          searchPlaceholder="Код заблуждения…"
          pageSize={15}
        />
      </section>

      <section className="panel">
        <div className="panel-title"><div><h2>Наиболее заметные связи</h2><p className="muted chart-subtitle">Пары ошибок, которые чаще других встречаются у одних и тех же студентов.</p></div><Sigma size={20} /></div>
        <div className="association-grid">
          {strongest.map((edge) => (
            <article className="association-card" key={`${edge.misconceptionAId}-${edge.misconceptionBId}`}>
              <div className="association-card__codes"><code>{edge.misconceptionACode}</code><span>↔</span><code>{edge.misconceptionBCode}</code></div>
              <div className="association-card__metrics">
                <div><span>Lift</span><strong>{number(edge.lift, 2)}</strong></div>
                <div><span>Jaccard</span><strong>{number(edge.jaccard, 2)}</strong></div>
                <div><span>Совместно</span><strong>{edge.together}</strong></div>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="panel">
        <div className="panel-title"><div><h2>Калибровка уверенности</h2><p className="muted chart-subtitle">Сравнение рассчитанной уверенности с фактической долей подтверждений.</p></div><Activity size={20} /></div>
        <div className="calibration-list">
          {(calibration.data?.buckets ?? []).map((bucket) => (
            <div className="calibration-row" key={`${bucket.from}-${bucket.to}`}>
              <div><strong>{percent(bucket.from)}–{percent(bucket.to)}</strong><span>{bucket.наблюдений} наблюдений</span></div>
              <div className="progress-track"><div className="progress-value" style={{ width: `${bucket.observedRate * 100}%` }} /></div>
              <div><strong>{percent(bucket.observedRate)}</strong><span>{bucket.подтверждено} подтверждено</span></div>
            </div>
          ))}
        </div>
      </section>

      <section className="insight-grid">
        <article className="insight-card"><FlaskConical size={20} /><div><strong>Набор показателей</strong><p>Совместная встречаемость ошибок, калибровка и группировка профилей помогают оценивать не только результат обучения, но и качество диагностики.</p></div></article>
        <article className="insight-card"><Network size={20} /><div><strong>Связи между заблуждениями</strong><p>Коэффициент lift показывает, какие типы ошибок встречаются вместе чаще, чем ожидалось при независимости.</p></div></article>
      </section>
    </div>
  )
}
