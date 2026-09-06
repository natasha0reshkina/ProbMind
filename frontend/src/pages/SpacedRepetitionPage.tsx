import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { useAuth } from '../auth/AuthContext'
import type { SpacedReview } from '../types/api'

export function SpacedRepetitionPage() {
  const { user } = useAuth()
  const client = useQueryClient()
  const query = useQuery({
    queryKey: ['spaced-repetition', user?.id],
    queryFn: async () => (await api.get<SpacedReview[]>('/edtech/repetition')).data,
  })
  const complete = useMutation({
    mutationFn: async ({ topicId, quality }: { topicId: string; quality: number }) =>
      (await api.post<SpacedReview>(`/edtech/repetition/${topicId}/complete`, { quality })).data,
    onSuccess: () => client.invalidateQueries({ queryKey: ['spaced-repetition', user?.id] }),
  })

  if (query.isLoading) return <LoadingView />
  const items = query.data ?? []
  const due = items.filter((item) => item.isDue).length

  return (
    <div>
      <PageHeader eyebrow="Интервальное повторение" title="Повторение по расписанию" description="Темы возвращаются в работу через увеличивающиеся интервалы. После повторения отметьте, насколько легко материал вспомнился." />
      <section className="kpi-strip edtech-kpi-strip">
        <div><span>К повторению сейчас</span><strong>{due}</strong></div>
        <div><span>Всего тем в расписании</span><strong>{items.length}</strong></div>
        <div><span>Следующий шаг</span><strong>{due ? 'Повторить' : 'По плану'}</strong></div>
      </section>
      <section className="plain-section">
        <h2>Очередь повторения</h2>
        <div className="edtech-card-list">
          {items.map((item) => (
            <article className={`edtech-card ${item.isDue ? 'edtech-card--due' : ''}`} key={item.topicId}>
              <div className="edtech-card__main">
                <div className="edtech-card__meta">{item.isDue ? 'Пора повторить' : `Следующее повторение: ${new Date(item.nextReviewAt).toLocaleDateString('ru-RU')}`}</div>
                <h3>{item.topicName}</h3>
                <p>Освоение: {Math.round(item.mastery * 100)}% · текущий интервал: {item.intervalDays} дн. · повторений: {item.repetitions}</p>
              </div>
              <div className="edtech-card__actions">
                <Link className="secondary-button" to={`/practice?topic=${item.topicId}`}>Открыть практику</Link>
                <div className="review-quality-actions">
                  <button type="button" onClick={() => complete.mutate({ topicId: item.topicId, quality: 2 })}>Трудно</button>
                  <button type="button" onClick={() => complete.mutate({ topicId: item.topicId, quality: 4 })}>Нормально</button>
                  <button type="button" onClick={() => complete.mutate({ topicId: item.topicId, quality: 5 })}>Легко</button>
                </div>
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  )
}
