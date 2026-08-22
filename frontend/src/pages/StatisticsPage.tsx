import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { CartesianGrid, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { api } from '../api/client'
import { PageHeader } from '../components/PageHeader'
import type { Dashboard, TimelinePoint } from '../types/api'

export function StatisticsPage() {
  const dashboard = useQuery({
    queryKey: ['dashboard'],
    queryFn: async () => (await api.get<Dashboard>('/statistics/dashboard')).data,
  })
  const [topicId, setTopicId] = useState('')

  const selected = topicId || dashboard.data?.topics[0]?.topicId || ''
  const timeline = useQuery({
    queryKey: ['mastery-timeline', selected],
    enabled: Boolean(selected),
    queryFn: async () => (await api.get<TimelinePoint[]>(`/statistics/mastery/${selected}/timeline`)).data,
  })

  const chartData = (timeline.data ?? []).map((item) => ({
    date: new Date(item.at).toLocaleDateString('ru-RU'),
    mastery: Math.round(item.value * 100),
  }))

  return (
    <div>
      <PageHeader
        title="Статистика"
        description="Изменение результатов по темам на основе выполненных диагностических и практических заданий."
      />

      <section className="plain-section">
        <div className="section-heading-row">
          <div><h2>Динамика по теме</h2></div>
          <select className="compact-select" value={selected} onChange={(event) => setTopicId(event.target.value)}>
            {(dashboard.data?.topics ?? []).map((topic) => <option key={topic.topicId} value={topic.topicId}>{topic.name}</option>)}
          </select>
        </div>
        <div className="chart-panel">
          <div className="chart-box">
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={chartData} margin={{ top: 10, right: 18, bottom: 4, left: 0 }}>
                <CartesianGrid stroke="#e6e9ec" vertical={false} />
                <XAxis dataKey="date" tick={{ fontSize: 11 }} stroke="#89939d" />
                <YAxis domain={[0, 100]} tick={{ fontSize: 11 }} stroke="#89939d" />
                <Tooltip />
                <Line type="monotone" dataKey="mastery" stroke="#4d6a7d" strokeWidth={2} dot={{ r: 2 }} activeDot={{ r: 3 }} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>
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
                  <td className="numeric-cell">{Math.round(topic.uncertainty * 100)}%</td>
                  <td className="numeric-cell"><strong>{Math.round(topic.mastery * 100)}%</strong></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}
