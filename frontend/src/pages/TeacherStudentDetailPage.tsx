import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Brain, ClipboardCheck, Download } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { BarChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { ProgressBar } from '../components/ProgressBar'
import { StatusBadge, statusLabel } from '../components/StatusBadge'
import type { PathStability, StudentMistake, StudentOverview } from '../types/api'
import { dateTime, percent } from '../utils/format'

export function TeacherStudentDetailPage() {
  const { studentId = '' } = useParams()
  const student = useQuery({
    queryKey: ['teacher-student', studentId],
    queryFn: async () => (await api.get<StudentOverview>(`/teacher/students/${studentId}`)).data,
    enabled: Boolean(studentId),
  })
  const stability = useQuery({
    queryKey: ['path-stability', studentId],
    queryFn: async () => (await api.get<PathStability>(`/research/students/${studentId}/path-stability`)).data,
    enabled: Boolean(studentId),
  })
  const mistakes = useQuery({
    queryKey: ['teacher-student-mistakes', studentId],
    queryFn: async () => (await api.get<StudentMistake[]>(`/teacher/students/${studentId}/mistakes?limit=60`)).data,
    enabled: Boolean(studentId),
  })

  const data = student.data
  const observedTopics = (data?.topics ?? []).filter((topic) => topic.observationCount > 0)
  const hasTopicObservations = observedTopics.length > 0
  const hasStabilityHistory = Boolean(stability.data && stability.data.revisions >= 2 && stability.data.interpretation !== 'insufficient_history')
  const topicChart = observedTopics.map((topic) => ({
    topic: topic.name.length > 20 ? `${topic.name.slice(0, 20)}…` : topic.name,
    mastery: topic.mastery,
    uncertainty: topic.uncertainty,
  }))
  const misconceptionChart = (data?.misconceptions ?? []).map((item) => ({
    name: item.title,
    confidence: item.confidence,
  })).sort((a, b) => b.confidence - a.confidence).slice(0, 8)

  const mistakeGroups = Object.values((mistakes.data ?? []).reduce<Record<string, { topic: string; count: number; errors: Record<string, number> }>>((groups, item) => {
    const current = groups[item.topicName] ?? { topic: item.topicName, count: 0, errors: {} }
    current.count += 1
    const errorTitle = item.misconceptionTitle ?? 'Не классифицирована'
    current.errors[errorTitle] = (current.errors[errorTitle] ?? 0) + 1
    groups[item.topicName] = current
    return groups
  }, {})).sort((a, b) => b.count - a.count)

  const mistakeColumns: DataColumn<StudentMistake>[] = [
    { key: 'date', title: 'Дата', render: (row) => dateTime(row.submittedAt), sortValue: (row) => new Date(row.submittedAt).getTime() },
    { key: 'source', title: 'Источник', render: (row) => row.source === 'Practice' ? 'Тренировка' : row.source === 'Diagnostic' ? 'Диагностика' : row.source, sortValue: (row) => row.source },
    { key: 'topic', title: 'Тема', render: (row) => <strong>{row.topicName}</strong>, sortValue: (row) => row.topicName },
    { key: 'question', title: 'Задание', width: '30%', render: (row) => <span className="table-wrap-text">{row.prompt}</span>, sortValue: (row) => row.prompt },
    { key: 'selected', title: 'Ответ студента', width: '18%', render: (row) => <span className="table-wrap-text teacher-answer-wrong">{row.selectedAnswer}</span> },
    { key: 'correct', title: 'Правильный ответ', width: '18%', render: (row) => <span className="table-wrap-text teacher-answer-correct">{row.correctAnswer}</span> },
    { key: 'misconception', title: 'Типичная ошибка', width: '20%', render: (row) => row.misconceptionTitle ?? <span className="muted">Не классифицирована</span>, sortValue: (row) => row.misconceptionTitle ?? '' },
  ]

  async function downloadProfile() {
    const response = await api.get(`/exports/students/${studentId}`, { responseType: 'blob' })
    const url = URL.createObjectURL(response.data)
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = `rezultaty-studenta-${studentId}.xlsx`
    anchor.click()
    URL.revokeObjectURL(url)
  }

  return (
    <div>
      <div className="back-row"><Link to="/teacher/students"><ArrowLeft size={16} /> Все студенты</Link></div>
      <PageHeader
        eyebrow="Профиль студента"
        title={data?.displayName ?? 'Профиль студента'}
        description={data ? `${data.email} · последняя активность ${dateTime(data.lastActivityAt)}` : 'Загрузка профиля студента…'}
        actions={<button className="secondary-button" onClick={() => void downloadProfile()}><Download size={16} /> Скачать отчёт</button>}
      />

      <KpiStrip items={[
        { label: 'Общий уровень', value: hasTopicObservations ? percent(data?.overallMastery) : 'Нет данных', tone: hasTopicObservations ? ((data?.overallMastery ?? 0) > .7 ? 'positive' : 'warning') : undefined },
        { label: 'Активные заблуждения', value: data?.activeMisconceptions ?? '—', tone: data?.activeMisconceptions ? 'warning' : 'positive' },
        { label: 'Исправлено', value: data?.correctedMisconceptions ?? '—', tone: 'positive' },
        { label: 'Диагностик', value: data?.completedDiagnostics ?? '—' },
        { label: 'Практических сессий', value: data?.completedPracticeSessions ?? '—' },
      ]} />

      <div className="two-column">
        <section className="panel">
          <div className="panel-title"><h2>Прогресс по темам</h2></div>
          <div className="topic-list">
            {(data?.topics ?? []).map((topic) => (
              <div className="topic-row" key={topic.topicId}>
                <div className="topic-row__meta">
                  <strong>{topic.name}</strong>
                  <span>{topic.observationCount > 0 ? `${topic.observationCount} наблюдений · неопределённость ${percent(topic.uncertainty)}` : 'Нет данных'}</span>
                </div>
                {topic.observationCount > 0 ? <ProgressBar value={topic.mastery} /> : <span className="muted">Освоение ещё не рассчитывалось</span>}
              </div>
            ))}
          </div>
        </section>
        <section className="panel teacher-plan-card">
          <div className="panel-title"><h2>Изменение плана повторения</h2></div>
          {hasStabilityHistory ? (
            <>
              <div className="big-score compact-score">{percent(stability.data?.stability)}</div>
              <p className="muted">{stability.data ? statusLabel(stability.data.interpretation) : ''}</p>
              <div className="mini-metrics mini-metrics--spaced">
                <div><span>Изменений плана</span><strong>{stability.data?.revisions ?? '—'}</strong></div>
                <div><span>Сохранено шагов в среднем</span><strong>{percent(stability.data?.meanRetention)}</strong></div>
              </div>
            </>
          ) : (
            <div className="teacher-inline-empty">
              <strong>Пока недостаточно истории</strong>
              <p>Показатель появится после нескольких изменений персонального плана. До этого момента значение не рассчитывается.</p>
            </div>
          )}
        </section>
      </div>

      <div className="two-column">
        {hasTopicObservations ? (
          <BarChartPanel title="Освоение тем" data={topicChart} xKey="topic" series={[{ key: 'mastery', label: 'Уровень' }]} percent height={300} />
        ) : (
          <section className="panel teacher-inline-empty">
            <strong>Нет результатов по темам</strong>
            <p>График появится после первой диагностики или тренировочной сессии.</p>
          </section>
        )}
        {misconceptionChart.length ? (
          <BarChartPanel title="Выраженность активных ошибок" data={misconceptionChart} xKey="name" series={[{ key: 'confidence', label: 'Выраженность' }]} percent height={300} horizontal />
        ) : (
          <section className="panel teacher-inline-empty">
            <strong>Типичные ошибки пока не выявлены</strong>
            <p>Этот блок заполнится после появления диагностических результатов.</p>
          </section>
        )}
      </div>

      {mistakeGroups.length > 0 && (
        <section className="panel">
          <div className="panel-title"><div><h2>Ошибки по темам</h2><p className="muted chart-subtitle">Сводка показывает, в каких темах накопилось больше неверных ответов и какие затруднения повторяются.</p></div><ClipboardCheck size={20} /></div>
          <div className="teacher-topic-error-summary">
            {mistakeGroups.map((group) => (
              <div className="teacher-topic-error-row" key={group.topic}>
                <div><strong>{group.topic}</strong><span>{group.count === 1 ? '1 неверный ответ' : group.count < 5 ? `${group.count} неверных ответа` : `${group.count} неверных ответов`}</span></div>
                <div className="teacher-error-list">
                  {Object.entries(group.errors).sort((a, b) => b[1] - a[1]).slice(0, 4).map(([title, count]) => (
                    <span key={title}>{title} · {count}</span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      <section className="panel">
        <div className="panel-title"><div><h2>Неверные ответы</h2><p className="muted chart-subtitle">Конкретные задания, в которых студент ошибся: выбранный ответ, правильный вариант и связанный тип затруднения.</p></div><ClipboardCheck size={20} /></div>
        <DataTable
          rows={mistakes.data ?? []}
          columns={mistakeColumns}
          rowKey={(row) => row.id}
          searchText={(row) => `${row.topicName} ${row.prompt} ${row.selectedAnswer} ${row.correctAnswer} ${row.misconceptionTitle ?? ''}`}
          searchPlaceholder="Тема, задание или типичная ошибка…"
          pageSize={10}
          emptyText="У студента пока нет сохранённых неверных ответов"
        />
      </section>

      <section className="panel">
        <div className="panel-title"><div><h2>Состояние типичных ошибок</h2><p className="muted chart-subtitle">Ошибки, которые повторялись в ответах студента и учитываются при дальнейшей работе.</p></div><Brain size={20} /></div>
        <div className="cards-grid cards-grid--two">
          {(data?.misconceptions ?? []).map((item) => (
            <article className="analysis-card" key={item.misconceptionId}>
              <div className="analysis-card__head"><strong>{item.title}</strong><StatusBadge value={item.status} /></div>
              <p>{item.description}</p>
              <div className="confidence-row"><span>Выраженность</span><strong>{percent(item.confidence)}</strong></div>
              <div className="progress-track"><div className="progress-value" style={{ width: `${item.confidence * 100}%` }} /></div>
              <small>{item.evidenceCount} сигналов · последнее обнаружение {dateTime(item.lastDetectedAt)}</small>
            </article>
          ))}
          {!data?.misconceptions.length && <div className="empty-state">У студента пока нет сохранённых состояний заблуждений.</div>}
        </div>
      </section>

    </div>
  )
}
