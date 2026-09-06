import { useQuery } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { api } from '../api/client'
import { PageHeader } from '../components/PageHeader'
import { useAuth } from '../auth/AuthContext'
import type { Dashboard, DiagnosticSession, TimelinePoint } from '../types/api'

function formatSessionLabel(index: number, value?: string | null) {
  if (!value) return `Диагностика ${index + 1}`

  const date = new Date(value)
  return `№${index + 1} · ${date.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit' })} ${date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}`
}

export function StatisticsPage() {
  const { user } = useAuth()
  const dashboard = useQuery({
    queryKey: ['dashboard', user?.id],
    queryFn: async () => (await api.get<Dashboard>('/statistics/dashboard')).data,
  })
  const diagnostics = useQuery({
    queryKey: ['diagnostic-history-for-statistics', user?.id],
    queryFn: async () => (await api.get<DiagnosticSession[]>('/diagnostics')).data,
  })
  const [topicId, setTopicId] = useState('')

  const topicsWithData = (dashboard.data?.topics ?? []).filter((topic) => topic.observationCount > 0)
  const selected = topicId || topicsWithData[0]?.topicId || ''
  const timeline = useQuery({
    queryKey: ['mastery-timeline', user?.id, selected],
    enabled: Boolean(selected),
    queryFn: async () => (await api.get<TimelinePoint[]>(`/statistics/mastery/${selected}/timeline`)).data,
  })

  const completedDiagnostics = useMemo(
    () => (diagnostics.data ?? [])
      .filter((item) => item.status === 'ReportReady' && item.completedAt)
      .sort((a, b) => new Date(a.completedAt ?? 0).getTime() - new Date(b.completedAt ?? 0).getTime()),
    [diagnostics.data],
  )

  const chartData = useMemo(() => {
    const history = [...(timeline.data ?? [])]
      .sort((a, b) => new Date(a.at).getTime() - new Date(b.at).getTime())

    return completedDiagnostics.flatMap((session, index) => {
      const completedAt = new Date(session.completedAt ?? 0).getTime()
      const point = history
        .filter((item) => new Date(item.at).getTime() <= completedAt)
        .at(-1)

      if (!point) return []

      return [{
        session: formatSessionLabel(index, session.completedAt),
        mastery: Math.round(point.value * 100),
        score: session.overallScore == null ? null : Math.round(session.overallScore * 100),
      }]
    })
  }, [completedDiagnostics, timeline.data])

  return (
    <div>
      <PageHeader
        title="Статистика"
        description="Изменение результатов по темам на основе завершённых диагностик и практических заданий."
      />

      <section className="plain-section">
        <div className="section-heading-row">
          <div>
            <h2>Динамика освоения между диагностиками</h2>
            <p>Одна точка соответствует одной завершённой диагностике. Если несколько диагностик пройдены в один день, они различаются по номеру и времени завершения.</p>
          </div>
          <select className="compact-select" value={selected} onChange={(event) => setTopicId(event.target.value)}>
            {topicsWithData.length ? topicsWithData.map((topic) => <option key={topic.topicId} value={topic.topicId}>{topic.name}</option>) : <option value="">Нет данных</option>}
          </select>
        </div>
        {chartData.length ? (
          <div className="chart-panel">
            <div className="chart-box">
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={chartData} margin={{ top: 10, right: 24, bottom: 16, left: 0 }}>
                  <CartesianGrid stroke="#e6e9ec" vertical={false} />
                  <XAxis dataKey="session" tick={{ fontSize: 11 }} stroke="#89939d" interval={0} />
                  <YAxis domain={[0, 100]} tick={{ fontSize: 11 }} stroke="#89939d" />
                  <Tooltip formatter={(value: number) => [`${value}%`, 'Освоение']} />
                  <Line type="monotone" dataKey="mastery" stroke="#4d6a7d" strokeWidth={2} dot={{ r: 4 }} activeDot={{ r: 5 }} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </div>
        ) : (
          <div className="teacher-inline-empty">
            <strong>Пока недостаточно завершённых диагностик</strong>
            <p>График появится после завершения хотя бы одной диагностики, по которой рассчитано освоение выбранной темы.</p>
          </div>
        )}
      </section>

      <section className="plain-section">
        <h2>Текущие результаты</h2>
        <div className="table-frame">
          <table className="academic-table">
            <thead><tr><th>Тема</th><th>Наблюдений</th><th>Неопределённость</th><th>Освоение</th></tr></thead>
            <tbody>
              {(dashboard.data?.topics ?? []).map((topic) => (
                <tr key={topic.topicId}>
                  <td><strong>{topic.name}</strong></td>
                  <td>{topic.observationCount}</td>
                  <td className="numeric-cell">{topic.observationCount > 0 ? `${Math.round(topic.uncertainty * 100)}%` : '—'}</td>
                  <td className="numeric-cell"><strong>{topic.observationCount > 0 ? `${Math.round(topic.mastery * 100)}%` : '—'}</strong></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}
