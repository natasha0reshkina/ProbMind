import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ArrowDownRight, ArrowUpRight, Target } from 'lucide-react'
import { api } from '../api/client'
import { BarChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import type { InterventionEffectiveness } from '../types/api'
import { number, percent } from '../utils/format'

export function InterventionsPage() {
  const interventions = useQuery({
    queryKey: ['teacher-interventions'],
    queryFn: async () => (await api.get<InterventionEffectiveness[]>('/teacher/interventions/effectiveness')).data,
  })

  const rows = interventions.data ?? []
  const weightedEffect = rows.reduce((sum, row) => sum + row.compositeEffectiveness * row.learners, 0) / Math.max(1, rows.reduce((sum, row) => sum + row.learners, 0))
  const meanTransfer = rows.length ? rows.reduce((sum, row) => sum + row.transferPassRate, 0) / rows.length : 0
  const meanGain = rows.length ? rows.reduce((sum, row) => sum + row.meanMasteryGain, 0) / rows.length : 0
  const meanReduction = rows.length ? rows.reduce((sum, row) => sum + row.meanConfidenceReduction, 0) / rows.length : 0
  const top = [...rows].sort((a, b) => b.compositeEffectiveness - a.compositeEffectiveness)[0]

  const effectivenessBars = [...rows].sort((a, b) => b.compositeEffectiveness - a.compositeEffectiveness).map((row) => ({
    name: row.title, effectiveness: row.compositeEffectiveness,
  }))
  const changeBars = [...rows].sort((a, b) => b.compositeEffectiveness - a.compositeEffectiveness).map((row) => ({
    name: row.title,
    confidenceReduction: row.meanConfidenceReduction,
    masteryGain: row.meanMasteryGain,
  }))

  const columns: DataColumn<InterventionEffectiveness>[] = useMemo(() => [
    { key: 'misconception', title: 'Типичная ошибка', render: (row) => <strong>{row.title}</strong>, sortValue: (row) => row.title },
    { key: 'learners', title: 'Студентов', render: (row) => row.learners, sortValue: (row) => row.learners, align: 'right' as const },
    { key: 'reduction', title: 'Снижение выраженности', render: (row) => <span className="metric-positive"><ArrowDownRight size={14} /> {percent(row.meanConfidenceReduction, 1)}</span>, sortValue: (row) => row.meanConfidenceReduction, align: 'right' as const },
    { key: 'gain', title: 'Рост освоения', render: (row) => <span className="metric-positive"><ArrowUpRight size={14} /> {percent(row.meanMasteryGain, 1)}</span>, sortValue: (row) => row.meanMasteryGain, align: 'right' as const },
    { key: 'transfer', title: 'Перенос знания', render: (row) => percent(row.transferPassRate), sortValue: (row) => row.transferPassRate, align: 'right' as const },
    { key: 'exercises', title: 'Среднее число заданий', render: (row) => number(row.meanExercises, 1), sortValue: (row) => row.meanExercises, align: 'right' as const },
    { key: 'effect', title: 'Итоговый результат', render: (row) => <strong>{percent(row.compositeEffectiveness, 1)}</strong>, sortValue: (row) => row.compositeEffectiveness, align: 'right' as const },
  ], [])

  return (
    <div>
      <PageHeader
        eyebrow="Повторная работа"
        title="Результаты повторной работы"
        description="Раздел показывает, как меняются результаты после дополнительных заданий: уменьшается ли выраженность типичной ошибки и улучшается ли освоение темы."
      />

      <KpiStrip items={[
        { label: 'Взвешенная эффективность', value: rows.length ? percent(weightedEffect) : '—', tone: rows.length && weightedEffect >= .6 ? 'positive' : 'default' },
        { label: 'Успешность переноса', value: rows.length ? percent(meanTransfer) : '—', tone: rows.length && meanTransfer >= .65 ? 'positive' : 'default' },
        { label: 'Средний прирост освоения', value: rows.length ? percent(meanGain) : '—', tone: rows.length && meanGain > 0 ? 'positive' : 'default' },
        { label: 'Снижение выраженности ошибки', value: rows.length ? percent(meanReduction) : '—', tone: rows.length && meanReduction > 0 ? 'positive' : 'default' },
        { label: 'Лучшая коррекция', value: top?.title ?? '—', hint: top ? percent(top.compositeEffectiveness) : undefined },
      ]} />

      <div className="two-column">
        <BarChartPanel title="Сводная эффективность" subtitle="Сводная эффективность по каждому типу заблуждения." data={effectivenessBars} xKey="name" series={[{ key: 'effectiveness', label: 'Эффективность' }]} percent horizontal height={360} />
        <BarChartPanel
          title="Изменение после повторной работы"
          subtitle="Сопоставление снижения выраженности ошибки и прироста освоения по каждому типу затруднения."
          data={changeBars}
          xKey="name"
          series={[
            { key: 'confidenceReduction', label: 'Снижение выраженности', colorIndex: 0 },
            { key: 'masteryGain', label: 'Рост освоения', colorIndex: 2 },
          ]}
          percent
          horizontal
          height={360}
        />
      </div>

      <section className="panel">
        <div className="panel-title"><div><h2>Результаты коррекционных модулей</h2><p className="muted chart-subtitle">Сортируйте по успешности переноса, снижению уверенности или итоговой оценке эффективности.</p></div><Target size={20} /></div>
        <DataTable rows={rows} columns={columns} rowKey={(row) => row.misconceptionId} searchText={(row) => row.title} pageSize={14} />
      </section>

    </div>
  )
}
