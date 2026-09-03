import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import type { LearningPath } from '../types/api'

function localizedReason(reason: string) {
  return reason
    .replace(': confidence in «', ': уверенность в наличии ошибки «')
    .replace(/» is\s*([0-9]+)\s*%; mastery\s*([0-9]+)\s*%\.?/gi, '» — $1%; освоение темы — $2%.')
    .replace(': topic mastery is ', ': освоение темы — ')
    .replace(/; spaced-review priority\s*([0-9]+)\s*%\.?/gi, '; приоритет повторения — $1%.')
}

export function LearningPathPage() {
  const client = useQueryClient()
  const { data, isLoading } = useQuery({
    queryKey: ['learning-path'],
    queryFn: async () => (await api.get<LearningPath>('/learning-paths/current')).data,
  })

  const rebuild = useMutation({
    mutationFn: async () => (await api.post<LearningPath>('/learning-paths/rebuild')).data,
    onSuccess: (path) => client.setQueryData(['learning-path'], path),
  })

  const action = useMutation({
    mutationFn: async ({ stepId, kind }: { stepId: string; kind: 'complete' | 'skip' }) =>
      (await api.post<LearningPath>(`/learning-paths/steps/${stepId}/${kind}`)).data,
    onSuccess: (path) => client.setQueryData(['learning-path'], path),
  })

  if (isLoading || !data) return <LoadingView />

  return (
    <div>
      <PageHeader
        title="План повторения"
        description={`Обновлён ${new Date(data.builtAt).toLocaleString('ru-RU')}. ${data.buildReason}`}
        actions={<button className="secondary-button" onClick={() => rebuild.mutate()} disabled={rebuild.isPending}>Обновить план</button>}
      />

      <div className="table-frame">
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
