import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { BarChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { ProgressBar } from '../components/ProgressBar'
import type { CohortAnalytics, InterventionEffectiveness, StudentListItem } from '../types/api'
import { percent } from '../utils/format'

export function TeacherAnalyticsPage() {
  const navigate = useNavigate()
  const cohort = useQuery({ queryKey: ['cohort'], queryFn: async () => (await api.get<CohortAnalytics>('/statistics/cohort')).data })
  const students = useQuery({ queryKey: ['teacher-students'], queryFn: async () => (await api.get<StudentListItem[]>('/teacher/students')).data })
  const interventions = useQuery({ queryKey: ['teacher-interventions'], queryFn: async () => (await api.get<InterventionEffectiveness[]>('/teacher/interventions/effectiveness')).data })

  const studentRows = [...(students.data ?? [])]
    .filter((row) => row.answeredQuestions > 0)
    .sort((a, b) => b.wrongAnswers - a.wrongAnswers || a.diagnosticAccuracy - b.diagnosticAccuracy)
    .slice(0, 10)

  const totalWrong = (students.data ?? []).reduce((sum, row) => sum + row.wrongAnswers, 0)
  const totalAnswers = (students.data ?? []).reduce((sum, row) => sum + row.answeredQuestions, 0)
  const weightedAccuracy = totalAnswers === 0 ? 0 : (students.data ?? []).reduce((sum, row) => sum + row.diagnosticAccuracy * row.answeredQuestions, 0) / totalAnswers
  const studentsWithResults = (students.data ?? []).filter((row) => row.answeredQuestions > 0).length
  const studentsWithErrors = (students.data ?? []).filter((row) => row.activeMisconceptions > 0).length

  const prevalence = studentsWithResults >= 3
    ? (cohort.data?.misconceptions ?? [])
      .filter((item) => item.studentsAffected > 0)
      .slice(0, 10)
      .map((item) => ({ name: item.title, prevalence: item.prevalence }))
    : []

  const topicRows = (cohort.data?.meanTopicMastery ?? []).filter((topic) => topic.observationCount > 0)

  const effectiveness = [...(interventions.data ?? [])]
    .filter((item) => item.learners > 0)
    .sort((a, b) => b.compositeEffectiveness - a.compositeEffectiveness)
    .slice(0, 8)
    .map((item) => ({ name: item.title, effectiveness: item.compositeEffectiveness }))

  const studentColumns: DataColumn<StudentListItem>[] = useMemo(() => [
    {
      key: 'student',
      title: 'Студент',
      width: '21%',
      render: (row) => <div><strong>{row.displayName}</strong><span className="table-secondary">{row.email}</span></div>,
      sortValue: (row) => row.displayName,
    },
    {
      key: 'result',
      title: 'Правильных ответов',
      render: (row) => percent(row.diagnosticAccuracy),
      sortValue: (row) => row.diagnosticAccuracy,
      align: 'right',
    },
    {
      key: 'wrong',
      title: 'Неверно',
      render: (row) => <strong>{row.wrongAnswers}</strong>,
      sortValue: (row) => row.wrongAnswers,
      align: 'center',
    },
    {
      key: 'errors',
      title: 'Что вызывает затруднение',
      width: '45%',
      render: (row) => row.activeMisconceptionTitles.length
        ? <div className="teacher-error-list">{row.activeMisconceptionTitles.slice(0, 3).map((title) => <span key={title}>{title}</span>)}</div>
        : <span className="muted">Устойчивые ошибки не выявлены</span>,
      sortValue: (row) => row.activeMisconceptions,
    },
  ], [])

  return (
    <div>
      <PageHeader
        eyebrow="Преподаватель"
        title="Сводка по группе"
        description="Здесь собраны показатели, которые помогают понять результаты студентов: кто уже прошёл диагностику, где больше неверных ответов и какие типичные затруднения встречаются чаще."
      />

      <KpiStrip items={[
        { label: 'Студентов в группе', value: cohort.data?.students ?? 0 },
        { label: 'Есть результаты', value: studentsWithResults },
        { label: 'Доля правильных ответов', value: totalAnswers > 0 ? percent(weightedAccuracy) : '-' },
        { label: 'Неверных ответов', value: totalWrong },
        { label: 'Есть устойчивые ошибки', value: studentsWithErrors },
      ]} />

      <section className="panel">
        <div className="panel-title">
          <div>
            <h2>Студенты, которым стоит уделить внимание</h2>
            <p className="muted chart-subtitle">Сначала показаны студенты с большим числом неверных ответов. Строка открывает подробный разбор конкретного студента.</p>
          </div>
        </div>
        <DataTable
          rows={studentRows}
          columns={studentColumns}
          rowKey={(row) => row.userId}
          searchText={(row) => `${row.displayName} ${row.email} ${row.activeMisconceptionTitles.join(' ')}`}
          searchPlaceholder="Студент или типичное затруднение…"
          pageSize={10}
          onRowClick={(row) => navigate(`/teacher/students/${row.userId}`)}
          emptyText="Пока недостаточно результатов студентов"
        />
      </section>

      <section className="plain-section">
        <h2>Средний результат по темам</h2>
        {topicRows.length > 0 ? (
          <div className="topic-list">
            {topicRows.map((topic) => (
              <div className="topic-row" key={topic.topicId}>
                <div className="topic-row__meta">
                  <strong>{topic.name}</strong>
                  <span>{topic.observationCount} наблюдений</span>
                </div>
                <div className="topic-progress-cell"><ProgressBar value={topic.mastery} /><span>{percent(topic.mastery)}</span></div>
              </div>
            ))}
          </div>
        ) : (
          <div className="teacher-inline-empty"><strong>Пока нет результатов по темам</strong><p>Показатели появятся после прохождения студентами диагностических заданий.</p></div>
        )}
      </section>

      <div className="two-column teacher-summary-charts">
        {studentsWithResults >= 3 ? (
          <BarChartPanel
            title="Наиболее частые типичные затруднения"
            subtitle="Доля студентов с результатами диагностики, у которых сейчас сохраняется соответствующее затруднение."
            data={prevalence}
            xKey="name"
            series={[{ key: 'prevalence', label: 'Доля студентов' }]}
            percent
            horizontal
            height={350}
          />
        ) : (
          <section className="teacher-data-note">
            <div className="teacher-data-note__title"><h2>Наиболее частые типичные затруднения</h2></div>
            <div className="teacher-data-note__body">
              <strong>Для группового сравнения пока мало данных</strong>
              <p>Нужно получить результаты как минимум трёх студентов. До этого момента ориентируйтесь на индивидуальные разборы в разделе Студенты.</p>
            </div>
          </section>
        )}
        {effectiveness.length > 0 ? (
          <BarChartPanel
            title="Результаты повторной работы"
            subtitle="Показано, насколько успешно студенты справляются с заданиями после разбора конкретных затруднений."
            data={effectiveness}
            xKey="name"
            series={[{ key: 'effectiveness', label: 'Результат' }]}
            percent
            horizontal
            height={350}
          />
        ) : (
          <section className="teacher-data-note">
            <div className="teacher-data-note__title"><h2>Результаты повторной работы</h2></div>
            <div className="teacher-data-note__body">
              <strong>Коррекционных попыток пока недостаточно</strong>
              <p>Этот блок появится после того, как студенты выполнят дополнительные задания по выявленным затруднениям.</p>
            </div>
          </section>
        )}
      </div>
    </div>
  )
}
