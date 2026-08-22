import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { ProgressBar } from '../components/ProgressBar'
import { StatusBadge } from '../components/StatusBadge'
import type { MisconceptionDetail } from '../types/api'

const evidenceLabels: Record<string, string> = {
  Distractor: 'Характерный ошибочный ответ',
  CorrectAnswer: 'Правильный ответ',
  CorrectionSuccess: 'Успешная коррекционная практика',
  TransferSuccess: 'Успешное задание на перенос',
  TransferFailure: 'Ошибка в задании на перенос',
  ManualTeacherEvidence: 'Наблюдение преподавателя',
}

export function MisconceptionDetailPage() {
  const { misconceptionId } = useParams()
  const { data, isLoading } = useQuery({
    queryKey: ['misconception-detail', misconceptionId],
    enabled: Boolean(misconceptionId),
    queryFn: async () => (await api.get<MisconceptionDetail>(`/learner/misconceptions/${misconceptionId}`)).data,
  })

  if (isLoading || !data) return <LoadingView />

  const mainContributions = data.reasoning.contributions.slice(0, 5)

  return (
    <div>
      <PageHeader
        title={data.state.title}
        description={data.state.description}
        actions={<StatusBadge value={data.state.status} />}
      />

      <section className="plain-section">
        <h2>Результат диагностики</h2>
        <div className="confidence-row">
          <strong className="big-score">{Math.round(data.state.confidence * 100)}%</strong>
          <ProgressBar value={data.state.confidence} />
        </div>
        <p>{data.reasoning.summary}</p>
        <p><strong>Следующий шаг:</strong> {data.reasoning.nextAction}</p>
      </section>

      <div className="two-column">
        <section className="plain-section">
          <h2>Что было замечено в ответах</h2>
          <p>{data.diagnosticRationale}</p>
          {data.reasoning.supportingReasons.length > 0 && (
            <ul className="simple-list">
              {data.reasoning.supportingReasons.map((item, index) => <li key={`${item}-${index}`}>{item}</li>)}
            </ul>
          )}
        </section>
        <section className="plain-section">
          <h2>Что стоит повторить</h2>
          <p>{data.correctiveExplanation}</p>
          <Link className="primary-button" to={`/practice?misconception=${data.state.misconceptionId}`}>Перейти к практике</Link>
        </section>
      </div>

      {mainContributions.length > 0 && (
        <section className="plain-section">
          <h2>На чём основан вывод</h2>
          <div className="table-frame">
            <table className="academic-table">
              <thead>
                <tr><th>Наблюдение</th><th>Роль в диагностике</th><th>Вклад</th></tr>
              </thead>
              <tbody>
                {mainContributions.map((item) => (
                  <tr key={item.evidenceId}>
                    <td>{item.reason}</td>
                    <td>{item.direction === 'supports' ? 'Подтверждает' : 'Опровергает'}</td>
                    <td className="numeric-cell">{Math.round(item.share * 100)}%</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      <section className="plain-section">
        <h2>История наблюдений</h2>
        <div className="timeline">
          {data.evidence.map((item) => (
            <article className="timeline-item" key={item.id}>
              <span className="timeline-dot" />
              <div>
                <div className="timeline-item__head">
                  <strong>{evidenceLabels[item.kind] ?? item.kind}</strong>
                  <span>{new Date(item.observedAt).toLocaleString('ru-RU')}</span>
                </div>
                <p>{item.explanation}</p>
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  )
}
