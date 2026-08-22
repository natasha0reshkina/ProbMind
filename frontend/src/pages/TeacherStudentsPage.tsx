import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, Brain, GraduationCap, SearchCheck, Users } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { BarChartPanel, ScatterChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge, statusLabel } from '../components/StatusBadge'
import type { CohortBenchmark, CohortSegment, StudentListItem, StudentRisk } from '../types/api'
import { dateTime, percent } from '../utils/format'

interface EnrichedStudent extends StudentListItem {
  risk?: StudentRisk
  benchmark?: CohortBenchmark
  segment?: CohortSegment
}

export function TeacherStudentsPage() {
  const navigate = useNavigate()
  const [riskFilter, setRiskFilter] = useState('all')
  const [segmentFilter, setSegmentFilter] = useState('all')

  const students = useQuery({
    queryKey: ['teacher-students'],
    queryFn: async () => (await api.get<StudentListItem[]>('/teacher/students')).data,
  })
  const risks = useQuery({
    queryKey: ['teacher-risk'],
    queryFn: async () => (await api.get<StudentRisk[]>('/advanced-analytics/cohort/risk')).data,
  })
  const benchmarks = useQuery({
    queryKey: ['teacher-benchmarks'],
    queryFn: async () => (await api.get<CohortBenchmark[]>('/advanced-analytics/cohort/benchmarks')).data,
  })
  const segments = useQuery({
    queryKey: ['teacher-segments'],
    queryFn: async () => (await api.get<CohortSegment[]>('/teacher/students/segments')).data,
  })

  const rows = useMemo<EnrichedStudent[]>(() => {
    const riskMap = new Map((risks.data ?? []).map((item) => [item.userId, item]))
    const benchmarkMap = new Map((benchmarks.data ?? []).map((item) => [item.userId, item]))
    const segmentMap = new Map((segments.data ?? []).map((item) => [item.userId, item]))
    return (students.data ?? []).map((student) => ({
      ...student,
      risk: riskMap.get(student.userId),
      benchmark: benchmarkMap.get(student.userId),
      segment: segmentMap.get(student.userId),
    })).filter((student) => {
      if (riskFilter !== 'all' && student.risk?.level !== riskFilter) return false
      if (segmentFilter !== 'all' && student.segment?.segment !== segmentFilter) return false
      return true
    })
  }, [students.data, risks.data, benchmarks.data, segments.data, riskFilter, segmentFilter])

  const riskLevels = Array.from(new Set((risks.data ?? []).map((item) => item.level))).sort()
  const segmentNames = Array.from(new Set((segments.data ?? []).map((item) => item.segment))).sort()
  const atRisk = (risks.data ?? []).filter((item) => item.level.toLowerCase().includes('high') || item.risk >= 0.6).length
  const meanMastery = rows.length ? rows.reduce((sum, row) => sum + row.overallMastery, 0) / rows.length : 0
  const activeMisconceptions = rows.reduce((sum, row) => sum + row.activeMisconceptions, 0)
  const inactive = (risks.data ?? []).filter((item) => item.daysInactive >= 7).length

  const columns: DataColumn<EnrichedStudent>[] = [
    {
      key: 'student', title: 'Студент', width: '25%',
      render: (row) => <div><strong>{row.displayName}</strong><span className="table-secondary">{row.email}</span></div>,
      sortValue: (row) => row.displayName,
    },
    {
      key: 'mastery', title: 'Уровень',
      render: (row) => <div className="compact-progress"><strong>{percent(row.overallMastery)}</strong><div className="progress-track"><div className="progress-value" style={{ width: `${row.overallMastery * 100}%` }} /></div></div>,
      sortValue: (row) => row.overallMastery,
    },
    {
      key: 'misconceptions', title: 'Активные ошибки',
      render: (row) => <strong>{row.activeMisconceptions}</strong>,
      sortValue: (row) => row.activeMisconceptions,
      align: 'center',
    },
    {
      key: 'risk', title: 'Риск',
      render: (row) => <div>{row.risk ? <><StatusBadge value={row.risk.level} /><span className="table-secondary">оценка {percent(row.risk.risk)}</span></> : '—'}</div>,
      sortValue: (row) => row.risk?.risk ?? 0,
    },
    {
      key: 'segment', title: 'Сегмент',
      render: (row) => <span className="soft-label">{row.segment ? statusLabel(row.segment.segment) : '—'}</span>,
      sortValue: (row) => row.segment?.segment ?? '',
    },
    {
      key: 'percentile', title: 'Перцентиль',
      render: (row) => row.benchmark ? percent(row.benchmark.percentile) : '—',
      sortValue: (row) => row.benchmark?.percentile ?? 0,
      align: 'right',
    },
    {
      key: 'last', title: 'Активность',
      render: (row) => dateTime(row.lastActivityAt),
      sortValue: (row) => row.lastActivityAt ? new Date(row.lastActivityAt).getTime() : 0,
    },
  ]

  const riskChart = (risks.data ?? [])
    .slice()
    .sort((a, b) => b.risk - a.risk)
    .slice(0, 12)
    .map((item) => ({ name: item.displayName.split(' ')[0], risk: item.risk }))

  const scatter = rows.map((item) => ({
    name: item.displayName,
    mastery: Math.round(item.overallMastery * 100),
    misconceptions: item.activeMisconceptions,
  }))

  return (
    <div>
      <PageHeader
        eyebrow="Группа студентов"
        title="Студенты и риск-профили"
        description="Список группы объединяет результаты по темам, активные заблуждения, перцентиль, сегментацию и оценку риска. Строка открывает подробный профиль студента."
      />

      <KpiStrip items={[
        { label: 'Студентов в выборке', value: rows.length, hint: 'после фильтров' },
        { label: 'Средний уровень', value: percent(meanMastery), tone: meanMastery >= .7 ? 'positive' : 'warning' },
        { label: 'Высокий риск', value: atRisk, hint: 'требуют внимания', tone: atRisk ? 'danger' : 'positive' },
        { label: 'Активных ошибок', value: activeMisconceptions, tone: activeMisconceptions > rows.length * 2 ? 'warning' : 'default' },
        { label: 'Неактивны 7+ дней', value: inactive, tone: inactive ? 'warning' : 'positive' },
      ]} />

      <section className="panel filter-panel">
        <div className="filter-panel__title"><SearchCheck size={18} /><strong>Фильтры группы</strong></div>
        <div className="filters">
          <label>Уровень риска
            <select value={riskFilter} onChange={(event) => setRiskFilter(event.target.value)}>
              <option value="all">Все уровни</option>
              {riskLevels.map((level) => <option key={level} value={level}>{statusLabel(level)}</option>)}
            </select>
          </label>
          <label>Сегмент
            <select value={segmentFilter} onChange={(event) => setSegmentFilter(event.target.value)}>
              <option value="all">Все сегменты</option>
              {segmentNames.map((segment) => <option key={segment} value={segment}>{statusLabel(segment)}</option>)}
            </select>
          </label>
          <button className="ghost-button" onClick={() => { setRiskFilter('all'); setSegmentFilter('all') }}>Сбросить</button>
        </div>
      </section>

      <section className="panel">
        <div className="panel-title">
          <div><h2>Реестр студентов</h2><p className="muted chart-subtitle">Сортировка по любой метрике и полнотекстовый поиск.</p></div>
          <div className="inline-badges"><span><Users size={14} /> {rows.length}</span><span><Brain size={14} /> {activeMisconceptions}</span></div>
        </div>
        <DataTable
          rows={rows}
          columns={columns}
          rowKey={(row) => row.userId}
          searchText={(row) => `${row.displayName} ${row.email} ${row.segment?.segment ?? ''} ${row.risk?.level ?? ''}`}
          searchPlaceholder="Имя, email, сегмент, риск…"
          pageSize={14}
          onRowClick={(row) => navigate(`/teacher/students/${row.userId}`)}
        />
      </section>

      <div className="two-column">
        <BarChartPanel
          title="Студенты с наибольшим риском"
          subtitle="Приоритизация преподавательского внимания."
          data={riskChart}
          xKey="name"
          series={[{ key: 'risk', label: 'Риск' }]}
          percent
          height={320}
        />
        <ScatterChartPanel
          title="Уровень освоения и устойчивые ошибки"
          subtitle="Студенты с большим числом устойчивых ошибок при сопоставимом уровне освоения выделяются в отдельную группу внимания."
          data={scatter}
          xKey="mastery"
          yKey="misconceptions"
          xLabel="Освоение, %"
          yLabel="Активных заблуждений"
          height={320}
        />
      </div>

      <section className="insight-grid">
        <article className="insight-card"><AlertTriangle size={20} /><div><strong>Список студентов, требующих внимания</strong><p>Оценка учитывает уровень освоения, количество активных ошибок и длительность неактивности. Она нужна для расстановки приоритетов и не заменяет диагностику.</p></div></article>
        <article className="insight-card"><GraduationCap size={20} /><div><strong>Сегментация</strong><p>Система относит студента к группе по совокупности показателей и сохраняет основание такой классификации.</p></div></article>
      </section>
    </div>
  )
}
