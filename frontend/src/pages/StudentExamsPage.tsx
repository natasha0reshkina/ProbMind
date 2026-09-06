import { useMutation, useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import type { Exam, ExamStart } from '../types/api'

export function StudentExamsPage() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const query = useQuery({
    queryKey: ['student-exams', user?.id],
    queryFn: async () => (await api.get<Exam[]>('/edtech/exams')).data,
  })

  const start = useMutation({
    mutationFn: async (examId: string) => (await api.post<ExamStart>(`/edtech/exams/${examId}/start`)).data,
    onSuccess: (data) => navigate(`/diagnostics/${data.sessionId}/continue?exam=1`),
  })

  if (query.isLoading) return <LoadingView />

  const exams = query.data ?? []

  return (
    <div>
      <PageHeader
        eyebrow="Контроль знаний"
        title="Экзамены"
        description="Экзамены имеют фиксированный набор вопросов и лимит времени. Подробный разбор ответов показывается после завершения."
      />
      <section className="plain-section">
        {exams.length === 0 ? (
          <p className="muted">Сейчас нет доступных экзаменов.</p>
        ) : (
          <div className="edtech-card-list">
            {exams.map((exam) => (
              <article className="edtech-card" key={exam.id}>
                <div className="edtech-card__meta">
                  {exam.studentName ? 'Назначен лично' : exam.groupName ?? 'Для всех студентов'} · {exam.questionCount} заданий · {exam.timeLimitMinutes} мин.
                </div>
                <h3>{exam.title}</h3>
                {exam.description ? <p>{exam.description}</p> : null}
                <div className="edtech-card__actions">
                  {exam.sessionStatus === 'ReportReady' ? (
                    <span>
                      <strong>Завершён</strong>
                      {exam.score != null ? ` · ${Math.round(exam.score * 100)}%` : ''}
                    </span>
                  ) : (
                    <button
                      className="primary-button"
                      type="button"
                      disabled={start.isPending}
                      onClick={() => exam.sessionId
                        ? navigate(`/diagnostics/${exam.sessionId}/continue?exam=1`)
                        : start.mutate(exam.id)}
                    >
                      {exam.sessionId ? 'Продолжить экзамен' : 'Начать экзамен'}
                    </button>
                  )}
                </div>
              </article>
            ))}
          </div>
        )}
        {start.isError ? <div className="form-error">Не удалось начать экзамен.</div> : null}
      </section>
    </div>
  )
}
