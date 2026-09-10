import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { EmptyState } from '../components/EmptyState'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { ProgressBar } from '../components/ProgressBar'
import { StatusBadge } from '../components/StatusBadge'
import type { DiagnosticReport } from '../types/api'

export function DiagnosticReportPage() {
  const { sessionId } = useParams()
  const report = useQuery({
    queryKey: ['report', sessionId],
    enabled: Boolean(sessionId),
    queryFn: async () => (await api.get<DiagnosticReport>(`/diagnostics/${sessionId}/report`)).data,
  })

  if (report.isLoading) return <LoadingView />
  if (report.isError || !report.data) {
    return <div className="error-view"><strong>Не удалось открыть отчёт</strong><p>Результаты диагностики сейчас недоступны. Вернитесь к истории и попробуйте ещё раз.</p></div>
  }

  const data = report.data
  const orderedTopics = [...data.topics].sort((a, b) => b.mastery - a.mastery)
  const strongest = orderedTopics[0]
  const weakest = orderedTopics.at(-1)
  const visibleMisconceptions = data.misconceptions.filter((item) => item.confidence >= 0.2)

  return (
    <div className="diagnostic-report-page">
      <PageHeader
        eyebrow="Диагностика завершена"
        title="Результаты диагностики"
        description="Сводка по ответам, темам и выявленным затруднениям."
        actions={<Link className="secondary-button" to="/learning-path">План повторения</Link>}
      />

      <section className="diagnostic-report-overview">
        <div className="diagnostic-report-score">
          <span>Общий результат</span>
          <strong>{Math.round(data.accuracy * 100)}%</strong>
          <small>{data.correct} из {data.answered} правильных ответов</small>
        </div>
        <div className="diagnostic-report-summary">
          <p>Лучше всего освоена тема {strongest?.name ?? '-'}{strongest ? ` (${Math.round(strongest.mastery * 100)}%)` : ''}.</p>
          <p>В первую очередь стоит повторить тему {weakest?.name ?? '-'}{weakest ? ` (${Math.round(weakest.mastery * 100)}%)` : ''}.</p>
        </div>
      </section>

      <section className="plain-section diagnostic-report-results">
        <h2>Краткая сводка</h2>
        <dl className="summary-list diagnostic-report-summary-list">
          <div><dt>Правильные ответы</dt><dd>{data.correct} из {data.answered}</dd></div>
          <div><dt>Сильная тема</dt><dd>{strongest?.name ?? '-'}</dd></div>
          <div><dt>Тема для повторения</dt><dd>{weakest?.name ?? '-'}</dd></div>
          <div><dt>Выявлено типичных затруднений</dt><dd>{visibleMisconceptions.length}</dd></div>
        </dl>
      </section>

      <div className="two-column">
        <section className="panel">
          <h2>Освоение тем</h2>
          <div className="report-topic-list">
            {data.topics.map((topic) => (
              <div className="topic-row" key={topic.topicId}>
                <div className="topic-row__meta">
                  <strong>{topic.name}</strong>
                  <span>{topic.observationCount} наблюдений</span>
                </div>
                <ProgressBar value={topic.mastery} />
                <span className="topic-row__score">{Math.round(topic.mastery * 100)}%</span>
              </div>
            ))}
          </div>
        </section>

        <section className="panel">
          <h2>Выявленные типичные затруднения</h2>
          {visibleMisconceptions.length === 0 ? (
            <EmptyState
              title="Выраженных типичных затруднений не выявлено"
              description="В этой диагностике нет ошибок, которые требуют отдельного разбора."
            />
          ) : (
            <div className="misconception-list">
              {visibleMisconceptions.map((item) => (
                <Link className="misconception-row" key={item.misconceptionId} to={`/misconceptions/${item.misconceptionId}`}>
                  <div className="misconception-row__main">
                    <strong>{item.title}</strong>
                    <span>{item.evidenceCount} наблюдений</span>
                  </div>
                  <div className="misconception-row__right">
                    <strong>{Math.round(item.confidence * 100)}%</strong>
                    <StatusBadge value={item.status} />
                  </div>
                </Link>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  )
}
