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
  const scheduled = items.filter((item) => item.nextReviewAt != null).length
  const reviewed = items.filter((item) => item.repetitions > 0).length

  return (
    <div>
      <PageHeader eyebrow="Интервальное повторение" title="Повторение по расписанию" />
      <section className="kpi-strip edtech-kpi-strip">
        <div><span>Повторить сегодня</span><strong>{due}</strong></div>
        <div><span>Тем в расписании</span><strong>{scheduled}</strong></div>
        <div><span>Уже повторялись</span><strong>{reviewed}</strong></div>
      </section>
      <section className="plain-section">
        <h2>Темы</h2>
        <div className="edtech-card-list">
          {items.map((item) => {
            const scheduleLabel = item.nextReviewAt == null
              ? 'Расписание появится после диагностики или практики'
              : item.isDue
                ? 'Повторить сегодня'
                : `Следующее повторение: ${new Date(item.nextReviewAt).toLocaleDateString('ru-RU')}`
            const intervalLabel = item.intervalDays > 0 ? `${item.intervalDays} дн.` : 'нет'
            return (
              <article className={`edtech-card ${item.isDue ? 'edtech-card--due' : ''}`} key={item.topicId}>
                <div className="edtech-card__main">
                  <div className="edtech-card__meta">{scheduleLabel}</div>
                  <h3>{item.topicName}</h3>
                  <p>Освоение: {Math.round(item.mastery * 100)}% · повторений: {item.repetitions} · текущий интервал: {intervalLabel}</p>
                </div>
                <div className="edtech-card__actions">
                  <Link className="secondary-button" to={`/practice?topic=${item.topicId}`}>Открыть практику</Link>
                  {item.nextReviewAt != null && item.isDue ? (
                    <div className="review-quality-actions" aria-label="Оценка повторения">
                      <button type="button" onClick={() => complete.mutate({ topicId: item.topicId, quality: 2 })}>Трудно</button>
                      <button type="button" onClick={() => complete.mutate({ topicId: item.topicId, quality: 4 })}>Нормально</button>
                      <button type="button" onClick={() => complete.mutate({ topicId: item.topicId, quality: 5 })}>Легко</button>
                    </div>
                  ) : null}
                </div>
              </article>
            )
          })}
        </div>
      </section>
    </div>
  )
}
