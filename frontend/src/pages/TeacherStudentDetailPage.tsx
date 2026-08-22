import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Brain, ClipboardCheck, Download, Route, Sparkles } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { BarChartPanel } from '../components/ChartPanel'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { ProgressBar } from '../components/ProgressBar'
import { StatusBadge } from '../components/StatusBadge'
import type { PathStability, StudentOverview } from '../types/api'
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

  const data = student.data
  const topicChart = (data?.topics ?? []).map((topic) => ({
    topic: topic.name.length > 20 ? `${topic.name.slice(0, 20)}…` : topic.name,
    mastery: topic.mastery,
    uncertainty: topic.uncertainty,
  }))
  const misconceptionChart = (data?.misconceptions ?? []).map((item) => ({
    name: item.code.replaceAll('_', ' '),
    confidence: item.confidence,
  })).sort((a, b) => b.confidence - a.confidence).slice(0, 8)

  async function downloadProfile() {
    const response = await api.get(`/exports/students/${studentId}`, { responseType: 'blob' })
    const url = URL.createObjectURL(response.data)
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = `probmind-student-${studentId}.csv`
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
        actions={<button className="secondary-button" onClick={() => void downloadProfile()}><Download size={16} /> CSV профиль</button>}
      />

      <KpiStrip items={[
        { label: 'Общий уровень', value: percent(data?.overallMastery), tone: (data?.overallMastery ?? 0) > .7 ? 'positive' : 'warning' },
        { label: 'Активные заблуждения', value: data?.activeMisconceptions ?? '—', tone: data?.activeMisconceptions ? 'warning' : 'positive' },
        { label: 'Исправлено', value: data?.correctedMisconceptions ?? '—', tone: 'positive' },
        { label: 'Диагностик', value: data?.completedDiagnostics ?? '—' },
        { label: 'Практических сессий', value: data?.completedPracticeSessions ?? '—' },
      ]} />

      <div className="two-column">
        <section className="panel">
          <div className="panel-title"><h2>Прогресс по темам</h2><Sparkles size={18} /></div>
          <div className="topic-list">
            {(data?.topics ?? []).map((topic) => (
              <div className="topic-row" key={topic.topicId}>
                <div className="topic-row__meta"><strong>{topic.name}</strong><span>{topic.observationCount} наблюдений · неопределённость {percent(topic.uncertainty)}</span></div>
                <ProgressBar value={topic.mastery} />
              </div>
            ))}
          </div>
        </section>
        <section className="panel">
          <div className="panel-title"><h2>Стабильность плана повторения</h2><Route size={18} /></div>
          <div className="big-score compact-score">{percent(stability.data?.stability)}</div>
          <p className="muted">{stability.data?.interpretation ?? 'Стабильность показывает, насколько последовательна персональная траектория между перестроениями.'}</p>
          <div className="mini-metrics">
            <div><span>Ревизий</span><strong>{stability.data?.revisions ?? '—'}</strong></div>
            <div><span>Среднее сохранение шагов</span><strong>{percent(stability.data?.meanRetention)}</strong></div>
          </div>
        </section>
      </div>

      <div className="two-column">
        <BarChartPanel title="Освоение тем" data={topicChart} xKey="topic" series={[{ key: 'mastery', label: 'Уровень' }]} percent height={300} />
        <BarChartPanel title="Выраженность активных ошибок" data={misconceptionChart} xKey="name" series={[{ key: 'confidence', label: 'Выраженность' }]} percent height={300} horizontal />
      </div>

      <section className="panel">
        <div className="panel-title"><div><h2>Состояние заблуждений</h2><p className="muted chart-subtitle">Типичные ошибки, которые повторялись в ответах студента.</p></div><Brain size={20} /></div>
        <div className="cards-grid cards-grid--two">
          {(data?.misconceptions ?? []).map((item) => (
            <article className="analysis-card" key={item.misconceptionId}>
              <div className="analysis-card__head"><code>{item.code}</code><StatusBadge value={item.status} /></div>
              <h3>{item.title}</h3>
              <p>{item.description}</p>
              <div className="confidence-row"><span>Выраженность</span><strong>{percent(item.confidence)}</strong></div>
              <div className="progress-track"><div className="progress-value" style={{ width: `${item.confidence * 100}%` }} /></div>
              <small>{item.evidenceCount} сигналов · последнее обнаружение {dateTime(item.lastDetectedAt)}</small>
            </article>
          ))}
          {!data?.misconceptions.length && <div className="empty-state">У студента пока нет сохранённых состояний заблуждений.</div>}
        </div>
      </section>

      <section className="insight-grid">
        <article className="insight-card"><ClipboardCheck size={20} /><div><strong>Диагностическая история</strong><p>В профиле учитываются результаты нескольких диагностик и практических сессий, поэтому единичная ошибка не приравнивается к повторяющейся.</p></div></article>
        <article className="insight-card"><Brain size={20} /><div><strong>Основание для разбора</strong><p>Оценка выраженности ошибки рассчитывается по накопленным ответам. Преподаватель видит её вместе с уровнем освоения темы и количеством наблюдений.</p></div></article>
      </section>
    </div>
  )
}
