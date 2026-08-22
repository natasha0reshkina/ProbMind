import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { ErrorView } from '../components/ErrorView'
import { LoadingView } from '../components/LoadingView'
import type { Dashboard } from '../types/api'

function topicStatus(value: number) {
  if (value >= 0.8) return 'Освоено'
  if (value >= 0.65) return 'Стабильно'
  if (value >= 0.5) return 'Нужно повторить'
  return 'Требует внимания'
}

export function DashboardPage() {
  const query = useQuery({
    queryKey: ['dashboard'],
    queryFn: async () => (await api.get<Dashboard>('/statistics/dashboard')).data,
  })

  if (query.isLoading) return <LoadingView />
  if (!query.data) return <ErrorView />

  const data = query.data

  return (
    <div className="academic-page">
      <header className="simple-page-header">
        <div>
          <h1>Обзор</h1>
          <p>Текущие результаты по курсу и рекомендации для повторения.</p>
        </div>
        <Link className="primary-button" to="/diagnostic">Начать диагностику</Link>
      </header>

      <section className="plain-section">
        <h2>Краткая сводка</h2>
        <dl className="summary-list">
          <div><dt>Общий уровень освоения</dt><dd>{Math.round(data.overallMastery * 100)}%</dd></div>
          <div><dt>Типичных ошибок требуют внимания</dt><dd>{data.activeMisconceptions}</dd></div>
          <div><dt>Исправлено типичных ошибок</dt><dd>{data.correctedMisconceptions}</dd></div>
          <div><dt>Завершено диагностик</dt><dd>{data.completedDiagnostics}</dd></div>
        </dl>
      </section>

      <section className="plain-section">
        <div className="section-heading-row">
          <div>
            <h2>Темы курса</h2>
            <p>Уровень рассчитывается по результатам диагностики и практики.</p>
          </div>
          <Link to="/statistics">Подробная статистика</Link>
        </div>
        <div className="table-frame">
          <table className="academic-table">
            <thead>
              <tr>
                <th>Тема</th>
                <th>Наблюдений</th>
                <th>Освоение</th>
                <th>Состояние</th>
              </tr>
            </thead>
            <tbody>
              {data.topics.map((topic) => (
                <tr key={topic.topicId}>
                  <td><strong>{topic.name}</strong></td>
                  <td>{topic.observationCount}</td>
                  <td className="numeric-cell">{Math.round(topic.mastery * 100)}%</td>
                  <td><span className={`text-status text-status--${topicStatus(topic.mastery).replaceAll(' ', '-').toLowerCase()}`}>{topicStatus(topic.mastery)}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="plain-section">
        <h2>Рекомендации</h2>
        {data.recommendations.length ? (
          <ol className="recommendation-list-simple">
            {data.recommendations.map((item) => (
              <li key={item.id}>
                <div>
                  <strong>{item.title}</strong>
                  <p>{item.rationale}</p>
                </div>
                <span>приоритет {Math.round(item.priority * 100)}%</span>
              </li>
            ))}
          </ol>
        ) : (
          <p className="muted">После первой диагностики здесь появятся рекомендации по повторению материала.</p>
        )}
      </section>
    </div>
  )
}
