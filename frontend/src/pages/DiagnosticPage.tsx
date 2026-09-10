import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { ErrorView } from '../components/ErrorView'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { ProgressBar } from '../components/ProgressBar'
import { useAuth } from '../auth/AuthContext'
import { statusLabel } from '../components/StatusBadge'
import type { AnswerFeedback, DiagnosticQuestion, DiagnosticSession, DiagnosticTemplate, ExamSessionContext } from '../types/api'

export function DiagnosticPage() {
  const { user } = useAuth()
  const client = useQueryClient()
  const navigate = useNavigate()
  const { sessionId: resumeSessionId } = useParams<{ sessionId?: string }>()
  const [session, setSession] = useState<DiagnosticSession | null>(null)
  const [question, setQuestion] = useState<DiagnosticQuestion | null>(null)
  const [feedback, setFeedback] = useState<AnswerFeedback | null>(null)
  const [selected, setSelected] = useState<string | null>(null)
  const [studentNote, setStudentNote] = useState('')
  const [confidenceLevel, setConfidenceLevel] = useState<number | null>(null)
  const [reasoning, setReasoning] = useState('')
  const [examClock, setExamClock] = useState(() => Date.now())
  const startedAt = useMemo(() => performance.now(), [question?.questionId])

  const resumeSession = useQuery({
    queryKey: ['diagnostic-session', user?.id, resumeSessionId],
    enabled: Boolean(resumeSessionId),
    retry: false,
    queryFn: async () => (await api.get<DiagnosticSession>(`/diagnostics/${resumeSessionId}`)).data,
  })

  const examContext = useQuery({
    queryKey: ['exam-session-context', user?.id, resumeSessionId],
    enabled: Boolean(resumeSessionId),
    retry: false,
    queryFn: async () => (await api.get<ExamSessionContext>(`/edtech/exams/session/${resumeSessionId}`)).data,
  })

  const activeSession = useQuery({
    queryKey: ['diagnostic-active', user?.id],
    enabled: !resumeSessionId,
    queryFn: async () => {
      const sessions = (await api.get<DiagnosticSession[]>('/diagnostics')).data
      return sessions.find((item) => item.status === 'InProgress') ?? null
    },
  })

  const teacherTemplates = useQuery({
    queryKey: ['diagnostic-templates-student', user?.id],
    enabled: !resumeSessionId,
    queryFn: async () => (await api.get<DiagnosticTemplate[]>('/diagnostic-templates/student')).data,
  })

  const diagnosticHistory = useQuery({
    queryKey: ['diagnostic-history', user?.id],
    enabled: !resumeSessionId,
    queryFn: async () => (await api.get<DiagnosticSession[]>('/diagnostics')).data,
  })

  const start = useMutation({
    mutationFn: async () => (await api.post<DiagnosticSession>('/diagnostics', { questionCount: 15 })).data,
    onSuccess: (data) => setSession(data),
  })

  const startTemplate = useMutation({
    mutationFn: async (templateId: string) =>
      (await api.post<DiagnosticSession>(`/diagnostic-templates/${templateId}/start`)).data,
    onSuccess: async (data) => {
      setSession(data)
      await Promise.all([
        client.invalidateQueries({ queryKey: ['diagnostic-active'] }),
        client.invalidateQueries({ queryKey: ['diagnostic-history'] }),
      ])
      navigate(`/diagnostics/${data.id}/continue`)
    },
  })

  const cancel = useMutation({
    mutationFn: async (sessionId: string) => (await api.post<DiagnosticSession>(`/diagnostics/${sessionId}/cancel`)).data,
    onSuccess: async () => {
      await Promise.all([
        client.invalidateQueries({ queryKey: ['diagnostic-active'] }),
        client.invalidateQueries({ queryKey: ['diagnostic-history'] }),
      ])
    },
  })

  const next = useMutation({
    mutationFn: async (sessionId: string) =>
      (await api.get<DiagnosticQuestion | null>(`/diagnostics/${sessionId}/next`)).data,
    onSuccess: (data) => {
      setQuestion(data)
      setFeedback(null)
      setSelected(null)
      setStudentNote('')
      setConfidenceLevel(null)
      setReasoning('')
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
        studentNote: studentNote.trim() || null,
        confidenceLevel,
        reasoning: reasoning.trim() || null,
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
          client.invalidateQueries({ queryKey: ['diagnostic-session', user?.id, session.id] }),
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

  const examExpiresAtMs = examContext.data?.expiresAt ? new Date(examContext.data.expiresAt).getTime() : null
  const examSecondsLeft = examExpiresAtMs == null ? null : Math.max(0, Math.ceil((examExpiresAtMs - examClock) / 1000))
  const examTimeExpired = Boolean(examContext.data?.isExam && examSecondsLeft === 0)

  useEffect(() => {
    if (!examExpiresAtMs) return
    setExamClock(Date.now())
    const timer = window.setInterval(() => setExamClock(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [examExpiresAtMs])

  useEffect(() => {
    if (!session || !examTimeExpired || complete.isPending || session.status !== 'InProgress') return
    void complete.mutateAsync()
  }, [examTimeExpired, session?.id])

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
          title="Диагностика"
          description="15 вопросов выбираются адаптивно: система учитывает слабые темы, предыдущие ответы, сложность и давность предыдущих показов."
        />
        {(diagnosticHistory.data ?? []).some((item) => item.status === 'ReportReady') ? (
          <section className="plain-section">
            <div className="section-heading-row"><h2>История попыток</h2><Link to="/diagnostics/history">Все попытки</Link></div>
            <div className="attempt-history-list">
              {(diagnosticHistory.data ?? [])
                .filter((item) => item.status === 'ReportReady')
                .slice(0, 5)
                .map((item) => (
                  <button key={item.id} type="button" className="attempt-history-row" onClick={() => navigate(`/diagnostics/${item.id}/report`)}>
                    <span>{item.completedAt ? new Date(item.completedAt).toLocaleString('ru-RU') : 'Завершена'}</span>
                    <strong>{item.overallScore == null ? '-' : `${Math.round(item.overallScore * 100)}%`}</strong>
                  </button>
                ))}
            </div>
          </section>
        ) : null}

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
                <div className="button-row">
                  <button
                    className="primary-button large"
                    onClick={() => navigate(`/diagnostics/${unfinished.id}/continue`)}
                  >
                    Продолжить диагностику
                  </button>
                  <button className="secondary-button" type="button" disabled={cancel.isPending} onClick={() => cancel.mutate(unfinished.id)}>Отменить</button>
                </div>
              </>
            ) : (
              <button className="primary-button large" onClick={() => start.mutate()} disabled={start.isPending}>
                {start.isPending ? 'Создаём сессию…' : 'Начать диагностику'}
              </button>
            )}
            {start.isError ? <div className="form-error" role="alert">Не удалось начать диагностику. Попробуйте ещё раз.</div> : null}
          </div>
        </section>

        {(teacherTemplates.data ?? []).length > 0 ? (
          <section className="plain-section teacher-diagnostic-list-section">
            <h2>Диагностики преподавателя</h2>
            {unfinished ? <p className="muted">Сначала завершите или отмените текущую диагностику.</p> : null}
            <div className="study-list">
              {(teacherTemplates.data ?? []).map((template) => {
                const attempts = (diagnosticHistory.data ?? [])
                  .filter((item) => item.diagnosticTemplateId === template.id && item.status === 'ReportReady')
                  .sort((a, b) => new Date(b.completedAt ?? 0).getTime() - new Date(a.completedAt ?? 0).getTime())
                return (
                  <article className="study-card" key={template.id}>
                    <div className="study-card__heading">
                      <div><span className="eyebrow">{template.questionCount} заданий</span><h3>{template.title}</h3></div>
                    </div>
                    {template.description ? <p className="study-card__body">{template.description}</p> : null}
                    {attempts.length > 0 ? (
                      <div className="template-attempts">
                        <strong>Предыдущие попытки: {attempts.length}</strong>
                        {attempts.slice(0, 3).map((attempt) => (
                          <button key={attempt.id} type="button" className="attempt-link" onClick={() => navigate(`/diagnostics/${attempt.id}/report`)}>
                            {attempt.completedAt ? new Date(attempt.completedAt).toLocaleDateString('ru-RU') : 'Завершена'}, {attempt.overallScore == null ? '-' : `${Math.round(attempt.overallScore * 100)}%`}
                          </button>
                        ))}
                      </div>
                    ) : null}
                    <button
                      type="button"
                      className="secondary-button"
                      disabled={startTemplate.isPending || Boolean(unfinished)}
                      onClick={() => startTemplate.mutate(template.id)}
                    >
                      {startTemplate.isPending ? 'Открываем…' : attempts.length > 0 ? 'Пройти ещё раз' : 'Начать диагностику'}
                    </button>
                  </article>
                )
              })}
            </div>
            {startTemplate.isError ? <div className="form-error">Не удалось начать диагностику преподавателя.</div> : null}
          </section>
        ) : null}

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

  const isExam = Boolean(examContext.data?.isExam)
  const examExpiresAt = examContext.data?.expiresAt ? new Date(examContext.data.expiresAt) : null
  const examTimerLabel = examSecondsLeft == null ? null : `${String(Math.floor(examSecondsLeft / 60)).padStart(2, '0')}:${String(examSecondsLeft % 60).padStart(2, '0')}`

  return (
    <div className="diagnostic-page">
      <PageHeader
        eyebrow={`Сессия ${session.id.slice(0, 8)}`}
        title={question.topicName}
        description={`${statusLabel(question.difficulty)}${question.isTransfer ? ' · применение в новой ситуации' : ''}`}
      />
      <ProgressBar
        value={session.answeredQuestionCount / session.plannedQuestionCount}
        label={`${session.answeredQuestionCount} из ${session.plannedQuestionCount}`}
      />

      {isExam ? <div className={`exam-mode-banner ${examTimeExpired ? 'exam-mode-banner--expired' : ''}`}><strong>Экзаменационный режим</strong><span>{examContext.data?.title}{examTimerLabel ? ` · осталось ${examTimerLabel}` : examExpiresAt ? ` · завершить до ${examExpiresAt.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}` : ''}</span></div> : <p className="selection-note"><strong>Основание выбора:</strong> {question.selectionExplanation}</p>}

      <section className="question-card">
        <div className="question-topic-banner"><span>Тема задания</span><strong>{question.topicName}</strong><em>{statusLabel(question.difficulty)}{question.isTransfer ? ' · новая ситуация' : ''}</em></div>
        <h2>{question.prompt}</h2>
        <div className="answer-grid">
          {question.options.map((option, index) => (
            <button
              key={option.id}
              type="button"
              className={`answer-option ${selected === option.id ? 'selected' : ''}`}
              disabled={Boolean(feedback) || submit.isPending || examTimeExpired}
              aria-pressed={selected === option.id}
              onClick={() => setSelected(option.id)}
            >
              <span className="answer-letter">{String.fromCharCode(65 + index)}.</span>
              <span className="answer-text">{option.text}</span>
            </button>
          ))}
        </div>

        <div className="metacognition-block">
          <div>
            <strong>Насколько вы уверены в ответе? <span className="optional-mark">Необязательно</span></strong>
            <div className="confidence-choice">
              {[{ value: 1, label: 'Совсем не уверен' }, { value: 2, label: 'Скорее не уверен' }, { value: 3, label: 'Скорее уверен' }, { value: 4, label: 'Полностью уверен' }].map((item) => (
                <button key={item.value} type="button" className={confidenceLevel === item.value ? 'selected' : ''} disabled={Boolean(feedback) || examTimeExpired} onClick={() => setConfidenceLevel((current) => current === item.value ? null : item.value)}>{item.label}</button>
              ))}
              {confidenceLevel != null ? <button type="button" className="confidence-clear" disabled={Boolean(feedback) || examTimeExpired} onClick={() => setConfidenceLevel(null)}>Не указывать</button> : null}
            </div>
          </div>
          <label>
            Ход рассуждения
            <textarea value={reasoning} onChange={(event) => setReasoning(event.target.value)} placeholder="Необязательно: коротко опишите, как вы пришли к ответу" maxLength={4000} disabled={Boolean(feedback) || examTimeExpired} />
          </label>
        </div>

        <label className="question-note-field">
          Комментарий преподавателю
          <textarea
            value={studentNote}
            onChange={(event) => setStudentNote(event.target.value)}
            placeholder="Необязательно: напишите, что было непонятно, как вы рассуждали или что хотите уточнить"
            maxLength={2000}
            disabled={Boolean(feedback) || submit.isPending || examTimeExpired}
          />
          <span className="field-hint">Комментарий сохранится вместе с ответом и будет виден преподавателю.</span>
        </label>

        {examTimeExpired ? <div className="form-error" role="status">Время экзамена истекло. Формируем итоговый отчёт…</div> : null}

        {!feedback ? (
          <div className="answer-actions">
            <button
              type="button"
              className="primary-button"
              disabled={!selected || submit.isPending || examTimeExpired}
              onClick={() => selected && void submit.mutateAsync(selected)}
            >
              {submit.isPending ? 'Проверяем ответ…' : 'Ответить'}
            </button>
            {!selected ? <span className="answer-hint">Выберите один вариант ответа</span> : <span className="answer-hint">Степень уверенности можно не указывать</span>}
          </div>
        ) : null}

        {submit.isError ? (
          <div className="form-error" role="alert">
            Не удалось отправить ответ. Попробуйте ещё раз.
          </div>
        ) : null}

        {feedback ? (
          <div className={`feedback ${isExam ? '' : feedback.isCorrect ? 'feedback--correct' : 'feedback--wrong'}`}>
            {isExam ? (
              <>
                <strong>Ответ сохранён</strong>
                <p>В экзаменационном режиме правильность и разбор будут доступны после завершения.</p>
              </>
            ) : (
              <>
                <strong>{feedback.isCorrect ? 'Верно' : 'Ответ требует разбора'}</strong>
                <p>{feedback.feedback}</p>
                <p>{feedback.correctExplanation}</p>
                {feedback.suspectedMisconceptionCode ? <div className="feedback-meta">Этот ответ будет учтён при формировании итогового разбора.</div> : null}
              </>
            )}
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
