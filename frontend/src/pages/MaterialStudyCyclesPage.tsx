import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import type { DiagnosticSession, MaterialStudyCycle } from '../types/api'

export function MaterialStudyCyclesPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const client = useQueryClient()

  const query = useQuery({
    queryKey: ['material-cycles', user?.id],
    queryFn: async () => (await api.get<MaterialStudyCycle[]>('/edtech/materials/cycles')).data,
  })

  const start = useMutation({
    mutationFn: async ({ id, stage }: { id: string; stage: 'pre' | 'post' }) => (
      await api.post<DiagnosticSession>(`/edtech/materials/cycles/${id}/${stage}/start`)
    ).data,
    onSuccess: (session) => navigate(`/diagnostics/${session.id}/continue`),
  })

  const open = useMutation({
    mutationFn: async (id: string) => api.post(`/edtech/materials/cycles/${id}/open`),
    onSuccess: () => client.invalidateQueries({ queryKey: ['material-cycles', user?.id] }),
  })

  if (query.isLoading) return <LoadingView />

  const cycles = query.data ?? []

  return (
    <div>
      <PageHeader
        eyebrow="Проверка эффекта обучения"
        title="До и после материала"
        description="Сначала короткая диагностика, затем материал и повторная проверка. Так видно не только итог, но и изменение знания после изучения."
      />

      <section className="plain-section">
        {cycles.length === 0 ? (
          <p className="muted">Преподаватель пока не назначил учебные циклы.</p>
        ) : (
          <div className="edtech-card-list">
            {cycles.map((cycle) => {
              const preDone = cycle.preStatus === 'ReportReady'
              const postDone = cycle.postStatus === 'ReportReady'

              return (
                <article className="edtech-card material-cycle-card" key={cycle.id}>
                  <div className="edtech-card__meta">{cycle.groupName ?? 'Для всех студентов'}</div>
                  <h3>{cycle.title}</h3>

                  <div className="cycle-steps">
                    <div className={preDone ? 'cycle-step done' : 'cycle-step'}>
                      <strong>1. До материала</strong>
                      <span>{cycle.preScore != null ? `${Math.round(cycle.preScore * 100)}%` : 'не пройдено'}</span>
                      {!preDone ? (
                        <button
                          type="button"
                          className="secondary-button"
                          onClick={() => cycle.preSessionId
                            ? navigate(`/diagnostics/${cycle.preSessionId}/continue`)
                            : start.mutate({ id: cycle.id, stage: 'pre' })}
                        >
                          {cycle.preSessionId ? 'Продолжить' : 'Начать'}
                        </button>
                      ) : null}
                    </div>

                    <div className={cycle.materialOpenedAt ? 'cycle-step done' : 'cycle-step'}>
                      <strong>2. Материал</strong>
                      {preDone ? (
                        <>
                          <div className="material-preview">{cycle.materialText}</div>
                          {!cycle.materialOpenedAt ? (
                            <button
                              type="button"
                              className="secondary-button"
                              onClick={() => open.mutate(cycle.id)}
                            >
                              Отметить как изученный
                            </button>
                          ) : (
                            <span>изучен</span>
                          )}
                        </>
                      ) : (
                        <span>доступен после первой диагностики</span>
                      )}
                    </div>

                    <div className={postDone ? 'cycle-step done' : 'cycle-step'}>
                      <strong>3. После материала</strong>
                      <span>{cycle.postScore != null ? `${Math.round(cycle.postScore * 100)}%` : 'не пройдено'}</span>
                      {cycle.materialOpenedAt && !postDone ? (
                        <button
                          type="button"
                          className="secondary-button"
                          onClick={() => cycle.postSessionId
                            ? navigate(`/diagnostics/${cycle.postSessionId}/continue`)
                            : start.mutate({ id: cycle.id, stage: 'post' })}
                        >
                          {cycle.postSessionId ? 'Продолжить' : 'Начать'}
                        </button>
                      ) : null}
                    </div>
                  </div>

                  {cycle.scoreDelta != null ? (
                    <div className="cycle-result">
                      <strong>Изменение:</strong>{' '}
                      {cycle.scoreDelta >= 0 ? '+' : ''}{Math.round(cycle.scoreDelta * 100)} п.п.
                    </div>
                  ) : null}
                </article>
              )
            })}
          </div>
        )}
      </section>
    </div>
  )
}
