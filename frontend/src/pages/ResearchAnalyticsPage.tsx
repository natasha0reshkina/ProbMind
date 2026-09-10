import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Activity, Network } from 'lucide-react'
import { api } from '../api/client'
import { BarChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { statusLabel } from '../components/StatusBadge'
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
  const strongest = [...edges].sort((a, b) => b.lift - a.lift || b.together - a.together).slice(0, 8)
  const meanLift = edges.length ? edges.reduce((sum, edge) => sum + edge.lift, 0) / edges.length : null
  const maxLift = strongest[0]?.lift ?? null

  const calibrationBars = (calibration.data?.buckets ?? []).map((bucket) => ({
    range: `${percent(bucket.from, 0)}–${percent(bucket.to, 0)}`,
    expected: (bucket.from + bucket.to) / 2,
    observed: bucket.observedRate,
  }))

  const clusterBars = (clusters.data ?? []).map((cluster) => ({
    cluster: statusLabel(cluster.label),
    learners: cluster.learners,
  }))

  const columns: DataColumn<CooccurrenceEdge>[] = useMemo(() => [
    { key: 'a', title: 'Первый тип ошибки', width: '30%', render: (row) => <span className="table-wrap-text"><strong>{row.misconceptionATitle}</strong></span>, sortValue: (row) => row.misconceptionATitle },
    { key: 'b', title: 'Второй тип ошибки', width: '30%', render: (row) => <span className="table-wrap-text"><strong>{row.misconceptionBTitle}</strong></span>, sortValue: (row) => row.misconceptionBTitle },
    { key: 'together', title: 'Студентов с обеими', render: (row) => row.together, sortValue: (row) => row.together, align: 'center' as const },
    { key: 'overlap', title: 'Доля общих случаев', render: (row) => percent(row.jaccard, 0), sortValue: (row) => row.jaccard, align: 'right' as const },
    { key: 'association', title: 'Чаще ожидаемого', render: (row) => `${number(row.lift, 1)} раза`, sortValue: (row) => row.lift, align: 'right' as const },
  ], [])

  return (
    <div>
      <PageHeader
        eyebrow="Исследовательская статистика"
        title="Связи между типичными ошибками"
        description="Раздел показывает только связи, для которых уже накоплено достаточно наблюдений. Внутренние коды системы в интерфейсе не используются."
      />

      <KpiStrip items={[
        { label: 'Пар с достаточными данными', value: edges.length },
        { label: 'Средняя сила связи', value: meanLift == null ? '-' : `${number(meanLift, 1)} раза` },
        { label: 'Максимальная сила связи', value: maxLift == null ? '-' : `${number(maxLift, 1)} раза` },
        { label: 'Групп профилей', value: clusters.data?.length ?? 0 },
        { label: 'Ошибка калибровки', value: calibration.data ? number(calibration.data.brierScore, 3) : '-' },
      ]} />

      <section className="panel">
        <div className="panel-title">
          <div>
            <h2>Совместная встречаемость типичных затруднений</h2>
            <p className="muted chart-subtitle">Пара отображается только тогда, когда оба типа ошибки встречаются минимум у двух студентов. Это убирает ложные сильные связи, возникающие из одного наблюдения.</p>
          </div>
          <Network size={20} />
        </div>
        <DataTable
          rows={edges}
          columns={columns}
          rowKey={(row) => `${row.misconceptionAId}-${row.misconceptionBId}`}
          searchText={(row) => `${row.misconceptionATitle} ${row.misconceptionBTitle}`}
          searchPlaceholder="Название типичного затруднения…"
          pageSize={12}
          emptyText="Пока недостаточно данных для надёжного сравнения типичных затруднений между студентами"
        />
      </section>

      {strongest.length > 0 && (
        <section className="panel">
          <div className="panel-title"><div><h2>Наиболее заметные связи</h2><p className="muted chart-subtitle">Чем выше показатель, тем чаще два типа ошибки встречаются у одних и тех же студентов относительно ожидаемого уровня.</p></div><Network size={20} /></div>
          <div className="association-grid">
            {strongest.map((edge) => (
              <article className="association-card" key={`${edge.misconceptionAId}-${edge.misconceptionBId}`}>
                <div className="association-card__titles"><strong>{edge.misconceptionATitle}</strong><span>и</span><strong>{edge.misconceptionBTitle}</strong></div>
                <div className="association-card__metrics">
                  <div><span>Студентов с обеими</span><strong>{edge.together}</strong></div>
                  <div><span>Доля общих случаев</span><strong>{percent(edge.jaccard, 0)}</strong></div>
                  <div><span>Чаще ожидаемого</span><strong>{number(edge.lift, 1)} раза</strong></div>
                </div>
              </article>
            ))}
          </div>
        </section>
      )}

      <div className="two-column">
        <BarChartPanel
          title="Расчётная уверенность и фактическое подтверждение"
          subtitle="Для каждого диапазона сравнивается ожидаемый уровень уверенности и доля последующих подтверждений."
          data={calibrationBars}
          xKey="range"
          series={[
            { key: 'expected', label: 'Расчётная уверенность', colorIndex: 0 },
            { key: 'observed', label: 'Подтверждено', colorIndex: 1 },
          ]}
          percent
          height={330}
        />
        <BarChartPanel
          title="Группы студентов по профилю результатов"
          subtitle="Группировка строится по совокупности показателей освоения и устойчивых ошибок."
          data={clusterBars}
          xKey="cluster"
          series={[{ key: 'learners', label: 'Студенты' }]}
          height={330}
        />
      </div>

      <section className="panel">
        <div className="panel-title"><div><h2>Проверка диагностической уверенности</h2><p className="muted chart-subtitle">Для каждого диапазона показано, как часто предполагаемое заблуждение подтверждалось последующим заданием.</p></div><Activity size={20} /></div>
        <div className="calibration-list">
          {(calibration.data?.buckets ?? []).map((bucket) => (
            <div className="calibration-row" key={`${bucket.from}-${bucket.to}`}>
              <div><strong>{percent(bucket.from)}–{percent(bucket.to)}</strong><span>{bucket.predictions} наблюдений</span></div>
              <div className="progress-track"><div className="progress-value" style={{ width: `${bucket.observedRate * 100}%` }} /></div>
              <div><strong>{percent(bucket.observedRate)}</strong><span>{bucket.confirmed} подтверждено</span></div>
            </div>
          ))}
        </div>
      </section>
    </div>
  )
}
