import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ArrowDownRight, ArrowUpRight, Repeat2, Target, Trophy } from 'lucide-react'
import { api } from '../api/client'
import { BarChartPanel, ScatterChartPanel } from '../components/ChartPanel'
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
    name: row.code.replaceAll('_', ' '), effectiveness: row.compositeEffectiveness,
  }))
  const scatter = rows.map((row) => ({
    name: row.code,
    confidenceReduction: Math.round(row.meanConfidenceReduction * 100),
    masteryGain: Math.round(row.meanMasteryGain * 100),
  }))

  const columns: DataColumn<InterventionEffectiveness>[] = useMemo(() => [
    { key: 'misconception', title: 'Заблуждение', render: (row) => <div><strong>{row.title}</strong><span className="table-secondary"><code>{row.code}</code></span></div>, sortValue: (row) => row.title },
    { key: 'learners', title: 'Студентов', render: (row) => row.learners, sortValue: (row) => row.learners, align: 'right' as const },
    { key: 'reduction', title: 'Снижение выраженности', render: (row) => <span className="metric-positive"><ArrowDownRight size={14} /> {percent(row.meanConfidenceReduction, 1)}</span>, sortValue: (row) => row.meanConfidenceReduction, align: 'right' as const },
    { key: 'gain', title: 'Рост освоения', render: (row) => <span className="metric-positive"><ArrowUpRight size={14} /> {percent(row.meanMasteryGain, 1)}</span>, sortValue: (row) => row.meanMasteryGain, align: 'right' as const },
    { key: 'transfer', title: 'Перенос знания', render: (row) => percent(row.transferPassRate), sortValue: (row) => row.transferPassRate, align: 'right' as const },
    { key: 'exercises', title: 'Mean exercises', render: (row) => number(row.meanExercises, 1), sortValue: (row) => row.meanExercises, align: 'right' as const },
    { key: 'effect', title: 'Composite', render: (row) => <strong>{percent(row.compositeEffectiveness, 1)}</strong>, sortValue: (row) => row.compositeEffectiveness, align: 'right' as const },
  ], [])

  return (
    <div>
      <PageHeader
        eyebrow="Эффективность коррекции"
        title="Эффективность коррекционных сценариев"
        description="Экран оценивает изменение результатов после коррекции: снижение уверенности в выявленном заблуждении, рост освоения темы и успешный перенос знания на новое условие."
      />

      <KpiStrip items={[
        { label: 'Взвешенная эффективность', value: percent(weightedEffect), tone: weightedEffect >= .6 ? 'positive' : 'warning' },
        { label: 'Успешность переноса', value: percent(meanTransfer), tone: meanTransfer >= .65 ? 'positive' : 'warning' },
        { label: 'Средний прирост освоения', value: percent(meanGain), tone: meanGain > 0 ? 'positive' : 'danger' },
        { label: 'Снижение уверенности в заблуждении', value: percent(meanReduction), tone: meanReduction > 0 ? 'positive' : 'warning' },
        { label: 'Лучшая коррекция', value: top?.code ?? '—', hint: top ? percent(top.compositeEffectiveness) : undefined },
      ]} />

      <div className="two-column">
        <BarChartPanel title="Сводная эффективность" subtitle="Сводная эффективность по каждому типу заблуждения." data={effectivenessBars} xKey="name" series={[{ key: 'effectiveness', label: 'Эффективность' }]} percent horizontal height={360} />
        <ScatterChartPanel title="Снижение уверенности × рост освоения" subtitle="Коррекция считается сильной, если одновременно ослабляет ошибочную гипотезу и усиливает владение темой." data={scatter} xKey="confidenceReduction" yKey="masteryGain" xLabel="Снижение уверенности, п.п." yLabel="Прирост освоения, п.п." height={360} />
      </div>

      <section className="panel">
        <div className="panel-title"><div><h2>Результаты коррекционных модулей</h2><p className="muted chart-subtitle">Сортируйте по успешности переноса, снижению уверенности или итоговой оценке эффективности.</p></div><Target size={20} /></div>
        <DataTable rows={rows} columns={columns} rowKey={(row) => row.misconceptionId} searchText={(row) => `${row.code} ${row.title}`} pageSize={14} />
      </section>

      <section className="insight-grid">
        <article className="insight-card"><Repeat2 size={20} /><div><strong>До → после</strong><p>Снижение уверенности сравнивает состояние заблуждения до и после коррекционного сценария, а не только правильность последнего ответа.</p></div></article>
        <article className="insight-card"><Trophy size={20} /><div><strong>Проверка переноса</strong><p>Отдельное задание проверяет, может ли студент применить исправленное понимание в новом условии, а не просто запомнить ответ.</p></div></article>
      </section>
    </div>
  )
}
