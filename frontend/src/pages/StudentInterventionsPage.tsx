import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import type { TeacherIntervention } from '../types/api'

export function StudentInterventionsPage() {
  const client = useQueryClient()
  const { user } = useAuth()

  const query = useQuery({
    queryKey: ['student-interventions', user?.id],
    queryFn: async () => (await api.get<TeacherIntervention[]>('/edtech/interventions')).data,
  })

  const complete = useMutation({
    mutationFn: async (id: string) => api.post(`/edtech/interventions/${id}/complete`),
    onSuccess: () => client.invalidateQueries({ queryKey: ['student-interventions', user?.id] }),
  })

  if (query.isLoading) return <LoadingView />

  const items = query.data ?? []

  return (
    <div>
      <PageHeader
        eyebrow="Работа с преподавателем"
        title="Индивидуальное задание"
        description="Персональные задания, которые преподаватель назначил с учётом вашего прогресса и сложных тем."
      />
      <section className="plain-section">
        {items.length === 0 ? (
          <p className="muted">Индивидуальных заданий пока нет.</p>
        ) : (
          <div className="edtech-card-list">
            {items.map((item) => (
              <article
                className={`edtech-card ${item.isCompleted ? 'edtech-card--completed' : ''}`}
                key={item.id}
              >
                <div className="edtech-card__meta">
                  {item.kind}
                  {item.dueAt ? ` · до ${new Date(item.dueAt).toLocaleDateString('ru-RU')}` : ''}
                </div>
                <h3>{item.title}</h3>
                <p>{item.body}</p>
                {item.isCompleted ? (
                  <strong>Выполнено</strong>
                ) : (
                  <button
                    type="button"
                    className="secondary-button"
                    onClick={() => complete.mutate(item.id)}
                  >
                    Отметить выполненным
                  </button>
                )}
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
