import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { PageHeader } from '../components/PageHeader'
import { ProgressBar } from '../components/ProgressBar'
import { StatusBadge } from '../components/StatusBadge'
import { LoadingView } from '../components/LoadingView'
import type { DiagnosticReport } from '../types/api'

export function DiagnosticReportPage() {
  const { sessionId } = useParams()
  const { data, isLoading } = useQuery({
    queryKey: ['report', sessionId],
    enabled: Boolean(sessionId),
    queryFn: async () => (await api.get<DiagnosticReport>(`/diagnostics/${sessionId}/report`)).data,
  })

  if (isLoading || !data) return <LoadingView />

  return (
    <div>
      <PageHeader
        eyebrow="Результаты диагностики"
        title={`Результат: ${Math.round(data.accuracy * 100)}%`}
        description={data.summary}
        actions={<Link className="primary-button" to="/learning-path">Открыть траекторию</Link>}
      />

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
          <h2>Диагностические гипотезы</h2>
          <div className="misconception-list">
            {data.misconceptions.filter((m) => m.confidence >= .2).map((m) => (
              <Link className="misconception-row" key={m.misconceptionId} to={`/misconceptions/${m.misconceptionId}`}>
                <div className="misconception-row__main">
                  <strong>{m.title}</strong>
                  <span>{m.evidenceCount} сигналов</span>
                </div>
                <div className="misconception-row__right">
                  <strong>{Math.round(m.confidence * 100)}%</strong>
                  <StatusBadge value={m.status} />
                </div>
              </Link>
            ))}
          </div>
        </section>
      </div>
    </div>
  )
}
