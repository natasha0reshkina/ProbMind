import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { BarChartPanel, DonutChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { PageHeader } from '../components/PageHeader'
import { ProgressBar } from '../components/ProgressBar'
import { StatusBadge } from '../components/StatusBadge'
import type { AdvancedSystemOverview, CohortAnalytics, ContentDrift, InterventionEffectiveness, Reliability } from '../types/api'
import { number, percent } from '../utils/format'

export function TeacherAnalyticsPage() {
  const cohort = useQuery({ queryKey: ['cohort'], queryFn: async () => (await api.get<CohortAnalytics>('/statistics/cohort')).data })
  const reliability = useQuery({ queryKey: ['teacher-reliability'], queryFn: async () => (await api.get<Reliability>('/teacher/diagnostics/reliability')).data })
  const interventions = useQuery({ queryKey: ['teacher-interventions'], queryFn: async () => (await api.get<InterventionEffectiveness[]>('/teacher/interventions/effectiveness')).data })
  const system = useQuery({ queryKey: ['advanced-system'], queryFn: async () => (await api.get<AdvancedSystemOverview>('/advanced-analytics/system')).data })
  const drift = useQuery({ queryKey: ['content-drift'], queryFn: async () => (await api.get<ContentDrift[]>('/advanced-analytics/content/drift')).data })

  const prevalence = (cohort.data?.misconceptions ?? []).slice(0, 12).map((item) => ({
    name: item.title.length > 24 ? `${item.title.slice(0, 24)}…` : item.title,
    prevalence: item.prevalence,
  }))

  const effectiveness = [...(interventions.data ?? [])]
    .sort((a, b) => b.compositeEffectiveness - a.compositeEffectiveness)
    .slice(0, 10)
    .map((item) => ({ code: item.code.replaceAll('_', ' '), effectiveness: item.compositeEffectiveness }))

  const driftFlagged = (drift.data ?? []).filter((item) => item.requiresReview)

  const driftColumns: DataColumn<ContentDrift>[] = [
    { key: 'code', title: 'Задание', render: (row) => <code>{row.code}</code>, sortValue: (row) => row.code },
    { key: 'shift', title: 'Изменение точности', render: (row) => percent(row.accuracyShift, 1), sortValue: (row) => row.accuracyShift, align: 'right' },
    { key: 'time', title: 'Изменение времени', render: (row) => number(row.responseTimeShift, 2), sortValue: (row) => row.responseTimeShift, align: 'right' },
    { key: 'entropy', title: 'Изменение энтропии', render: (row) => number(row.entropyShift, 2), sortValue: (row) => row.entropyShift, align: 'right' },
    { key: 'score', title: 'Оценка изменения', render: (row) => <strong>{number(row.driftScore, 2)}</strong>, sortValue: (row) => row.driftScore, align: 'right' },
    { key: 'review', title: 'Состояние', render: (row) => <StatusBadge value={row.requiresReview ? 'RequiresReview' : 'Stable'} />, sortValue: (row) => row.requiresReview ? 1 : 0 },
    { key: 'reason', title: 'Основание', render: (row) => <span className="table-wrap-text">{row.reason}</span> },
  ]

  return (
    <div>
      <PageHeader
        title="Сводка по группе"
        description="Результаты студентов, показатели качества диагностики и статистика по заданиям."
      />

      <section className="plain-section">
        <h2>Основные показатели</h2>
        <dl className="summary-list">
          <div><dt>Студентов</dt><dd>{cohort.data?.students ?? 0}</dd></div>
          <div><dt>Средняя точность</dt><dd>{percent(cohort.data?.meanAccuracy)}</dd></div>
          <div><dt>Среднее освоение</dt><dd>{percent(system.data?.meanMastery)}</dd></div>
          <div><dt>Коэффициент α Кронбаха</dt><dd>{number(reliability.data?.cronbachAlpha, 2)}</dd></div>
        </dl>
      </section>

      <section className="plain-section">
        <h2>Средний результат по темам</h2>
        <div className="topic-list">
          {(cohort.data?.meanTopicMastery ?? []).map((topic) => (
            <div className="topic-row" key={topic.topicId}>
              <div className="topic-row__meta">
                <strong>{topic.name}</strong>
                <span>{topic.observationCount} наблюдений · неопределённость {percent(topic.uncertainty)}</span>
              </div>
              <div className="topic-progress-cell"><ProgressBar value={topic.mastery} /><span>{percent(topic.mastery)}</span></div>
            </div>
          ))}
        </div>
      </section>

      <div className="two-column">
        <BarChartPanel
          title="Распространённость типичных ошибок"
          subtitle="Доля студентов, у которых обнаружен соответствующий тип ошибки."
          data={prevalence}
          xKey="name"
          series={[{ key: 'prevalence', label: 'Доля студентов' }]}
          percent
          horizontal
          height={360}
        />
        <BarChartPanel
          title="Результаты коррекционных заданий"
          subtitle="Сводный показатель по повторным заданиям и заданиям на перенос."
          data={effectiveness}
          xKey="code"
          series={[{ key: 'effectiveness', label: 'Результат' }]}
          percent
          horizontal
          height={360}
        />
      </div>

      <section className="plain-section">
        <h2>Состояние банка заданий</h2>
        <div className="two-column">
          <DonutChartPanel
            title="Стабильность заданий"
            subtitle="Сравнение стабильных заданий и заданий, которые стоит проверить."
            data={[
              { name: 'Стабильно', value: Math.max(0, (system.data?.questions ?? drift.data?.length ?? 0) - driftFlagged.length) },
              { name: 'Нужна проверка', value: driftFlagged.length },
            ]}
            height={280}
          />
          <dl className="summary-list summary-list--two">
            <div><dt>Обработано ответов</dt><dd>{system.data?.answers.toLocaleString('ru-RU') ?? cohort.data?.answers ?? '—'}</dd></div>
            <div><dt>Активных типичных ошибок</dt><dd>{system.data?.activeMisconceptions ?? '—'}</dd></div>
            <div><dt>Студентов, которым нужно внимание</dt><dd>{system.data?.atRiskLearners ?? '—'}</dd></div>
            <div><dt>Средняя точность диагностики</dt><dd>{percent(system.data?.meanDiagnosticAccuracy)}</dd></div>
          </dl>
        </div>
      </section>

      <section className="plain-section">
        <div className="section-heading-row">
          <div>
            <h2>Изменение характеристик заданий</h2>
            <p>Сравнение ранних и недавних ответов по точности, времени и распределению вариантов.</p>
          </div>
        </div>
        <DataTable rows={drift.data ?? []} columns={driftColumns} rowKey={(row) => row.questionId} searchText={(row) => `${row.code} ${row.reason}`} pageSize={12} />
      </section>
    </div>
  )
}
