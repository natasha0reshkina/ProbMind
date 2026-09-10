import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Award } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { BarChartPanel, DonutChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge, statusLabel } from '../components/StatusBadge'
import type { CohortBenchmark, StudentRisk } from '../types/api'
import { number, percent } from '../utils/format'

interface BenchmarkRow extends CohortBenchmark { risk?: StudentRisk }

export function CohortBenchmarksPage() {
  const navigate = useNavigate()
  const benchmarks = useQuery({ queryKey: ['teacher-benchmarks'], queryFn: async () => (await api.get<CohortBenchmark[]>('/advanced-analytics/cohort/benchmarks')).data })
  const risks = useQuery({ queryKey: ['teacher-risk'], queryFn: async () => (await api.get<StudentRisk[]>('/advanced-analytics/cohort/risk')).data })
  const rows = useMemo<BenchmarkRow[]>(() => {
    const riskMap = new Map((risks.data ?? []).map((item) => [item.userId, item]))
    return (benchmarks.data ?? []).map((benchmark) => ({ ...benchmark, risk: riskMap.get(benchmark.userId) }))
  }, [benchmarks.data, risks.data])

  const meanMastery = rows.length ? rows.reduce((sum, row) => sum + row.mastery, 0) / rows.length : 0
  const median = [...rows].sort((a, b) => a.mastery - b.mastery)[Math.floor(rows.length / 2)]?.mastery ?? 0
  const lowerQuartile = rows.filter((row) => row.percentile <= .25).length
  const highRisk = rows.filter((row) => (row.risk?.risk ?? 0) >= .6).length

  const bandMap = rows.reduce<Map<string, number>>((map, row) => {
    map.set(row.band, (map.get(row.band) ?? 0) + 1)
    return map
  }, new Map<string, number>())
  const bandCounts: Array<{ name: string; value: number }> = []
  bandMap.forEach((value, name) => bandCounts.push({ name: statusLabel(name), value }))
  const ranked = [...rows].sort((a, b) => b.mastery - a.mastery).map((row) => ({ name: row.displayName.split(' ')[0], mastery: row.mastery }))

  const columns: DataColumn<BenchmarkRow>[] = [
    { key: 'student', title: 'Студент', render: (row) => <strong>{row.displayName}</strong>, sortValue: (row) => row.displayName },
    { key: 'mastery', title: 'Уровень', render: (row) => percent(row.mastery), sortValue: (row) => row.mastery, align: 'right' },
    { key: 'percentile', title: 'Перцентиль', render: (row) => <strong>{percent(row.percentile)}</strong>, sortValue: (row) => row.percentile, align: 'right' },
    { key: 'z', title: 'Отклонение от среднего', render: (row) => number(row.zScore, 2), sortValue: (row) => row.zScore, align: 'right' },
    { key: 'band', title: 'Диапазон', render: (row) => <StatusBadge value={row.band} />, sortValue: (row) => row.band },
    { key: 'mis', title: 'Активных заблуждений', render: (row) => row.activeMisconceptions, sortValue: (row) => row.activeMisconceptions, align: 'right' },
    { key: 'risk', title: 'Риск', render: (row) => row.risk ? <StatusBadge value={row.risk.level} /> : '-', sortValue: (row) => row.risk?.risk ?? 0 },
  ]

  return (
    <div>
      <PageHeader eyebrow="Сравнение результатов" title="Сравнение внутри когорты" description="Сравнение результатов студентов внутри текущей группы по уровню освоения и накопленным учебным показателям." />
      <KpiStrip items={[
        { label: 'Студентов', value: rows.length },
        { label: 'Среднее освоение', value: percent(meanMastery) },
        { label: 'Медианный уровень', value: percent(median) },
        { label: 'Нижний квартиль', value: lowerQuartile, tone: lowerQuartile ? 'warning' : 'default' },
        { label: 'Высокий риск', value: highRisk, tone: highRisk ? 'danger' : 'positive' },
      ]} />

      <div className="two-column">
        <BarChartPanel title="Распределение уровня освоения" subtitle="Сортировка показывает относительное положение студентов внутри группы." data={ranked} xKey="name" series={[{ key: 'mastery', label: 'Уровень' }]} percent height={350} />
        <DonutChartPanel title="Диапазоны результатов" subtitle="Распределение студентов по нормированным диапазонам." data={bandCounts} height={350} />
      </div>

      <section className="panel">
        <div className="panel-title"><div><h2>Сравнение студентов</h2><p className="muted chart-subtitle">Строка открывает подробный профиль студента с результатами по темам и состояниями выявленных заблуждений.</p></div><Award size={20} /></div>
        <DataTable rows={rows} columns={columns} rowKey={(row) => row.userId} searchText={(row) => `${row.displayName} ${row.band} ${row.risk?.level ?? ''}`} pageSize={16} onRowClick={(row) => navigate(`/teacher/students/${row.userId}`)} />
      </section>

    </div>
  )
}
