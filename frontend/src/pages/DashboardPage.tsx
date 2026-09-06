import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { ErrorView } from '../components/ErrorView'
import { LoadingView } from '../components/LoadingView'
import type { Dashboard, DiagnosticSession, PracticeSession } from '../types/api'

function topicStatus(value: number) {
  if (value >= 0.8) return 'Освоено'
  if (value >= 0.65) return 'Стабильно'
  if (value >= 0.5) return 'Нужно повторить'
  return 'Требует внимания'
}

export function DashboardPage({ embedded = false }: { embedded?: boolean }) {
  const query = useQuery({
    queryKey: ['dashboard'],
    queryFn: async () => (await api.get<Dashboard>('/statistics/dashboard')).data,
  })

  const diagnostics = useQuery({
    queryKey: ['diagnostic-history'],
    queryFn: async () => (await api.get<DiagnosticSession[]>('/diagnostics')).data,
  })

  const practiceHistory = useQuery({
    queryKey: ['practice-history'],
    queryFn: async () => (await api.get<PracticeSession[]>('/practice/history')).data,
  })

  if (query.isLoading || diagnostics.isLoading || practiceHistory.isLoading) return <LoadingView />
  if (!query.data) return <ErrorView />

  const data = query.data
  const activeDiagnostic = (diagnostics.data ?? []).find((item) => item.status === 'InProgress')
  const activePractice = (practiceHistory.data ?? []).find((item) => item.status === 'InProgress')
  const hasObservations = data.topics.some((topic) => topic.observationCount > 0)

  return (
    <div className="academic-page">
      <header className={embedded ? 'cabinet-progress-header' : 'simple-page-header'}>
        <div>
          {embedded ? <h2>Учебный прогресс</h2> : <h1>Обзор</h1>}
          <p>Текущие результаты по курсу и рекомендации для повторения.</p>
        </div>
        {activeDiagnostic ? (
          <Link className="primary-button" to={`/diagnostics/${activeDiagnostic.id}/continue`}>Продолжить диагностику</Link>
        ) : activePractice ? (
          <Link className="primary-button" to={`/practice/${activePractice.id}/continue`}>Продолжить практику</Link>
        ) : (
          <Link className="primary-button" to="/diagnostic">Начать диагностику</Link>
        )}
      </header>

      {activeDiagnostic ? (
        <section className="plain-section">
          <div className="section-heading-row">
            <div>
              <h2>Незавершённая диагностика</h2>
              <p>Ваш прогресс сохранён: отвечено {activeDiagnostic.answeredQuestionCount} из {activeDiagnostic.plannedQuestionCount} вопросов.</p>
            </div>
            <Link className="secondary-button" to={`/diagnostics/${activeDiagnostic.id}/continue`}>Допройти</Link>
          </div>
        </section>
      ) : null}

      {activePractice ? (
        <section className="plain-section">
          <div className="section-heading-row">
            <div>
              <h2>Незавершённая практика</h2>
              <p>Ваш прогресс сохранён: выполнено {activePractice.completedExercises} из {activePractice.targetExercises} упражнений.</p>
            </div>
            <Link className="secondary-button" to={`/practice/${activePractice.id}/continue`}>Допройти</Link>
          </div>
        </section>
      ) : null}

      <section className="plain-section">
        <h2>Краткая сводка</h2>
        <dl className="summary-list">
          <div><dt>Общий уровень освоения</dt><dd>{hasObservations ? `${Math.round(data.overallMastery * 100)}%` : 'Нет данных'}</dd></div>
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
                  <td className="numeric-cell">{topic.observationCount > 0 ? `${Math.round(topic.mastery * 100)}%` : '—'}</td>
                  <td>{topic.observationCount > 0 ? <span className={`text-status text-status--${topicStatus(topic.mastery).replaceAll(' ', '-').toLowerCase()}`}>{topicStatus(topic.mastery)}</span> : <span className="muted">Нет данных</span>}</td>
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
                  <Link
                    to={item.misconceptionId
                      ? `/practice?misconception=${item.misconceptionId}`
                      : item.topicId
                        ? `/practice?topic=${item.topicId}`
                        : '/diagnostic'}
                  >
                    Перейти к следующему шагу
                  </Link>
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
