import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { useAuth } from '../auth/AuthContext'
import type { LearningPath } from '../types/api'
import { cleanUiText } from '../utils/format'

function localizedReason(reason: string) {
  return cleanUiText(reason)
    .replace('уверенность в наличии ошибки ', 'ошибка ')
    .replace('Диагностическая уверенность', 'Уровень подтверждения')
}

export function LearningPathPage() {
  const { user } = useAuth()
  const client = useQueryClient()
  const { data, isLoading } = useQuery({
    queryKey: ['learning-path', user?.id],
    queryFn: async () => (await api.get<LearningPath | null>('/learning-paths/current')).data,
  })

  const rebuild = useMutation({
    mutationFn: async () => (await api.post<LearningPath>('/learning-paths/rebuild')).data,
    onSuccess: (path) => client.setQueryData(['learning-path', user?.id], path),
  })

  const action = useMutation({
    mutationFn: async ({ stepId, kind }: { stepId: string; kind: 'complete' | 'skip' }) =>
      (await api.post<LearningPath>(`/learning-paths/steps/${stepId}/${kind}`)).data,
    onSuccess: (path) => client.setQueryData(['learning-path', user?.id], path),
  })

  if (isLoading) return <LoadingView />

  if (!data) {
    return (
      <div>
        <PageHeader title="План повторения" />
        <section className="plain-section">
          <h2>План ещё не сформирован</h2>
          <p className="muted">Сформируйте его после диагностики или практики.</p>
          <button className="primary-button" onClick={() => rebuild.mutate()} disabled={rebuild.isPending}>{rebuild.isPending ? 'Формируем…' : 'Сформировать план'}</button>
        </section>
      </div>
    )
  }

  return (
    <div>
      <PageHeader
        title="План повторения"
        description={`Обновлён ${new Date(data.builtAt).toLocaleString('ru-RU')}. ${data.buildReason}`}
        actions={<button className="secondary-button" onClick={() => rebuild.mutate()} disabled={rebuild.isPending}>Обновить план</button>}
      />

      <div className="table-frame learning-path-frame">
        <table className="academic-table learning-path-table">
          <thead>
            <tr>
              <th>№</th>
              <th>Тема</th>
              <th>Причина</th>
              <th>Приоритет</th>
              <th>Состояние</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {data.steps.map((step) => (
              <tr key={step.id}>
                <td className="learning-path-position">{step.position}</td>
                <td><strong>{step.topicName}</strong>{step.misconceptionTitle ? <div className="table-secondary">{step.misconceptionTitle}</div> : null}</td>
                <td><span className="table-wrap-text">{localizedReason(step.reason)}</span></td>
                <td className="numeric-cell">{Math.round(step.priority * 100)}%</td>
                <td><StatusBadge value={step.status} /></td>
                <td>
                  {step.status === 'InProgress' || step.status === 'Pending' ? (
                    <div className="row-actions">
                      <button className="secondary-button" onClick={() => action.mutate({ stepId: step.id, kind: 'complete' })}>Выполнено</button>
                      <button className="ghost-button" onClick={() => action.mutate({ stepId: step.id, kind: 'skip' })}>Пропустить</button>
                    </div>
                  ) : null}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
