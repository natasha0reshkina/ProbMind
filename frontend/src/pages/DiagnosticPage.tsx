import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { ErrorView } from '../components/ErrorView'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { ProgressBar } from '../components/ProgressBar'
import { statusLabel } from '../components/StatusBadge'
import type { AnswerFeedback, DiagnosticQuestion, DiagnosticSession } from '../types/api'

export function DiagnosticPage() {
  const client = useQueryClient()
  const navigate = useNavigate()
  const { sessionId: resumeSessionId } = useParams<{ sessionId?: string }>()
  const [session, setSession] = useState<DiagnosticSession | null>(null)
  const [question, setQuestion] = useState<DiagnosticQuestion | null>(null)
  const [feedback, setFeedback] = useState<AnswerFeedback | null>(null)
  const [selected, setSelected] = useState<string | null>(null)
  const startedAt = useMemo(() => performance.now(), [question?.questionId])

  const resumeSession = useQuery({
    queryKey: ['diagnostic-session', resumeSessionId],
    enabled: Boolean(resumeSessionId),
    retry: false,
    queryFn: async () => (await api.get<DiagnosticSession>(`/diagnostics/${resumeSessionId}`)).data,
  })

  const activeSession = useQuery({
    queryKey: ['diagnostic-active'],
    enabled: !resumeSessionId,
    queryFn: async () => {
      const sessions = (await api.get<DiagnosticSession[]>('/diagnostics')).data
      return sessions.find((item) => item.status === 'InProgress') ?? null
    },
  })

  const start = useMutation({
    mutationFn: async () => (await api.post<DiagnosticSession>('/diagnostics', { questionCount: 15 })).data,
    onSuccess: (data) => setSession(data),
  })

  const next = useMutation({
    mutationFn: async (sessionId: string) =>
      (await api.get<DiagnosticQuestion | null>(`/diagnostics/${sessionId}/next`)).data,
    onSuccess: (data) => {
      setQuestion(data)
      setFeedback(null)
      setSelected(null)
    },
  })

  const submit = useMutation({
    mutationFn: async (optionId: string) => {
      if (!session || !question) throw new Error('No active question')
      return (await api.post<AnswerFeedback>('/diagnostics/answers', {
        sessionId: session.id,
        questionId: question.questionId,
        questionVersionId: question.versionId,
        answerOptionId: optionId,
        responseTimeMs: Math.round(performance.now() - startedAt),
      })).data
    },
    onSuccess: async (data) => {
      setFeedback(data)
      if (session) {
        const refreshed = (await api.get<DiagnosticSession>(`/diagnostics/${session.id}`)).data
        setSession(refreshed)
        await Promise.all([
          client.invalidateQueries({ queryKey: ['dashboard'] }),
          client.invalidateQueries({ queryKey: ['diagnostic-history'] }),
          client.invalidateQueries({ queryKey: ['diagnostic-active'] }),
          client.invalidateQueries({ queryKey: ['diagnostic-session', session.id] }),
        ])
      }
    },
  })

  const complete = useMutation({
    mutationFn: async () => {
      if (!session) throw new Error('No active session')
      return (await api.post(`/diagnostics/${session.id}/complete`)).data
    },
    onSuccess: async () => {
      await Promise.all([
        client.invalidateQueries({ queryKey: ['dashboard'] }),
        client.invalidateQueries({ queryKey: ['diagnostic-history'] }),
        client.invalidateQueries({ queryKey: ['diagnostic-active'] }),
      ])
      if (session) navigate(`/diagnostics/${session.id}/report`)
    },
  })

  useEffect(() => {
    if (!resumeSessionId || !resumeSession.data || session) return

    if (resumeSession.data.id !== resumeSessionId && resumeSession.data.status === 'InProgress') {
      navigate(`/diagnostics/${resumeSession.data.id}/continue`, { replace: true })
      return
    }

    if (resumeSession.data.status === 'ReportReady') {
      navigate(`/diagnostics/${resumeSession.data.id}/report`, { replace: true })
      return
    }

    if (resumeSession.data.status === 'InProgress') {
      setSession(resumeSession.data)
    }
  }, [navigate, resumeSession.data, resumeSessionId, session])

  useEffect(() => {
    if (resumeSessionId || session || !activeSession.data) return
    navigate(`/diagnostics/${activeSession.data.id}/continue`, { replace: true })
  }, [activeSession.data, navigate, resumeSessionId, session])

  useEffect(() => {
    if (session && !question && !feedback) void next.mutateAsync(session.id)
  }, [session])

  function advance() {
    if (!session) return
    if (session.answeredQuestionCount >= session.plannedQuestionCount) {
      void complete.mutateAsync()
    } else {
      void next.mutateAsync(session.id)
    }
  }

  if (resumeSessionId && resumeSession.isLoading) {
    return <LoadingView text="Восстанавливаем незавершённую диагностику…" />
  }

  if (resumeSessionId && resumeSession.isError) {
    return <ErrorView message="Не удалось открыть незавершённую диагностику. Вернитесь в историю и попробуйте ещё раз." />
  }

  if (resumeSessionId && resumeSession.data?.status === 'ReportReady') {
    return <LoadingView text="Открываем готовый отчёт…" />
  }

  if (resumeSessionId && resumeSession.data && resumeSession.data.status !== 'InProgress') {
    return (
      <div className="panel">
        <h2>Эту диагностику нельзя продолжить</h2>
        <p>Сессия уже завершена или была отменена.</p>
        <button className="secondary-button" onClick={() => navigate('/diagnostics/history')}>Вернуться к истории</button>
      </div>
    )
  }

  if (resumeSessionId && resumeSession.data && !session) {
    return <LoadingView text="Восстанавливаем прогресс…" />
  }

  if (!session) {
    if (!resumeSessionId && activeSession.isLoading) {
      return <LoadingView text="Проверяем незавершённую диагностику…" />
    }

    const unfinished = activeSession.data

    return (
      <div>
        <PageHeader
          eyebrow="Адаптивная диагностика"
          title="Диагностика типичных заблуждений"
          description="15 вопросов выбираются адаптивно: система учитывает слабые темы, предыдущие ответы, сложность и давность предыдущих показов."
        />
        <section className="hero-panel diagnostic-intro-panel">
          <div>
            <h2>Что будет измеряться</h2>
            <ul className="feature-list">
              <li>условная вероятность и базовые частоты;</li>
              <li>независимость и отличие от несовместности;</li>
              <li>интерпретация p-value и статистической значимости;</li>
              <li>закон больших чисел и ошибка игрока;</li>
              <li>интуитивное понимание случайности.</li>
            </ul>
          </div>
          <div className="diagnostic-start-actions">
            {unfinished ? (
              <>
                <p><strong>Есть незавершённая сессия:</strong> {unfinished.answeredQuestionCount} из {unfinished.plannedQuestionCount} вопросов уже отвечено.</p>
                <button
                  className="primary-button large"
                  onClick={() => navigate(`/diagnostics/${unfinished.id}/continue`)}
                >
                  Продолжить диагностику
                </button>
              </>
            ) : (
              <button className="primary-button large" onClick={() => start.mutate()} disabled={start.isPending}>
                {start.isPending ? 'Создаём сессию…' : 'Начать диагностику'}
              </button>
            )}
            {start.isError ? <div className="form-error" role="alert">Не удалось начать диагностику. Попробуйте ещё раз.</div> : null}
          </div>
        </section>
      </div>
    )
  }

  if (!question && next.isPending) return <div className="panel">Подбираем следующий диагностический вопрос…</div>

  if (!question && next.isError) {
    return (
      <div className="panel">
        <h2>Не удалось получить следующий вопрос</h2>
        <p>Текущая диагностическая сессия сохранена. Можно повторить загрузку вопроса.</p>
        <button className="primary-button" onClick={() => session && next.mutate(session.id)}>Повторить</button>
      </div>
    )
  }

  if (!question) {
    return (
      <div className="panel">
        <h2>Вопросы закончились</h2>
        <button className="primary-button" onClick={() => complete.mutate()} disabled={complete.isPending}>
          {complete.isPending ? 'Формируем отчёт…' : 'Построить отчёт'}
        </button>
        {complete.isError ? <div className="form-error" role="alert">Не удалось сформировать отчёт. Ответы сохранены, попробуйте ещё раз.</div> : null}
      </div>
    )
  }

  return (
    <div className="diagnostic-page">
      <PageHeader
        eyebrow={`Сессия ${session.id.slice(0, 8)}`}
        title={question.topicName}
        description={`${statusLabel(question.difficulty)}${question.isTransfer ? ' · проверка переноса' : ''}`}
      />
      <ProgressBar
        value={session.answeredQuestionCount / session.plannedQuestionCount}
        label={`${session.answeredQuestionCount} из ${session.plannedQuestionCount}`}
      />

      <p className="selection-note"><strong>Основание выбора:</strong> {question.selectionExplanation}</p>

      <section className="question-card">
        <h2>{question.prompt}</h2>
        <div className="answer-grid">
          {question.options.map((option, index) => (
            <button
              key={option.id}
              type="button"
              className={`answer-option ${selected === option.id ? 'selected' : ''}`}
              disabled={Boolean(feedback) || submit.isPending}
              aria-pressed={selected === option.id}
              onClick={() => setSelected(option.id)}
            >
              <span className="answer-letter">{String.fromCharCode(65 + index)}.</span>
              <span className="answer-text">{option.text}</span>
            </button>
          ))}
        </div>

        {!feedback ? (
          <div className="answer-actions">
            <button
              type="button"
              className="primary-button"
              disabled={!selected || submit.isPending}
              onClick={() => selected && void submit.mutateAsync(selected)}
            >
              {submit.isPending ? 'Проверяем ответ…' : 'Ответить'}
            </button>
            {!selected ? <span className="answer-hint">Выберите один вариант ответа</span> : null}
          </div>
        ) : null}

        {submit.isError ? (
          <div className="form-error" role="alert">
            Не удалось отправить ответ. Попробуйте ещё раз.
          </div>
        ) : null}

        {feedback ? (
          <div className={`feedback ${feedback.isCorrect ? 'feedback--correct' : 'feedback--wrong'}`}>
            <strong>{feedback.isCorrect ? 'Верно' : 'Ответ требует разбора'}</strong>
            <p>{feedback.feedback}</p>
            <p>{feedback.correctExplanation}</p>
            {feedback.suspectedMisconceptionCode ? (
              <div className="feedback-meta">Этот ответ будет учтён при формировании итогового разбора.</div>
            ) : null}
            <button className="primary-button feedback-action" onClick={advance} disabled={next.isPending || complete.isPending}>
              {complete.isPending
                ? 'Формируем отчёт…'
                : next.isPending
                  ? 'Подбираем вопрос…'
                  : session.answeredQuestionCount >= session.plannedQuestionCount
                    ? 'Завершить диагностику'
                    : 'Следующий вопрос'}
            </button>
            {complete.isError ? <div className="form-error" role="alert">Не удалось завершить диагностику. Все отправленные ответы сохранены.</div> : null}
          </div>
        ) : null}
      </section>


    </div>
  )
}
