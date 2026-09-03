import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { FormEvent, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { api } from '../api/client'
import { EmptyState } from '../components/EmptyState'
import { ErrorView } from '../components/ErrorView'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { ProgressBar } from '../components/ProgressBar'
import { statusLabel } from '../components/StatusBadge'
import type {
  AnswerFeedback,
  DiagnosticQuestion,
  MisconceptionCatalog,
  PracticeResult,
  PracticeSession,
  Topic,
  UserMisconception,
} from '../types/api'

export function PracticePage() {
  const client = useQueryClient()
  const navigate = useNavigate()
  const { sessionId: resumeSessionId } = useParams<{ sessionId?: string }>()
  const [params] = useSearchParams()
  const presetMisconception = params.get('misconception')
  const presetTopic = params.get('topic')
  const [topicId, setTopicId] = useState(presetTopic ?? '')
  const [misconceptionId, setMisconceptionId] = useState(presetMisconception ?? '')
  const [session, setSession] = useState<PracticeSession | null>(null)
  const [question, setQuestion] = useState<DiagnosticQuestion | null>(null)
  const [feedback, setFeedback] = useState<AnswerFeedback | null>(null)
  const [selected, setSelected] = useState<string | null>(null)
  const [step, setStep] = useState(0)
  const [lastResult, setLastResult] = useState<PracticeResult | null>(null)
  const [completionError, setCompletionError] = useState(false)
  const startedAt = useMemo(() => performance.now(), [question?.questionId])

  const topics = useQuery({
    queryKey: ['topics'],
    queryFn: async () => (await api.get<Topic[]>('/content/topics')).data,
  })

  const misconceptions = useQuery({
    queryKey: ['catalog-misconceptions'],
    queryFn: async () => (await api.get<MisconceptionCatalog[]>('/content/misconceptions')).data,
  })

  const learnerMisconceptions = useQuery({
    queryKey: ['my-misconceptions'],
    queryFn: async () => (await api.get<UserMisconception[]>('/learner/misconceptions')).data,
  })

  const history = useQuery({
    queryKey: ['practice-history'],
    queryFn: async () => (await api.get<PracticeSession[]>('/practice/history')).data,
  })

  const resumeSession = useQuery({
    queryKey: ['practice-session', resumeSessionId],
    enabled: Boolean(resumeSessionId),
    retry: false,
    queryFn: async () => (await api.get<PracticeSession>(`/practice/${resumeSessionId}`)).data,
  })

  const activeSession = useQuery({
    queryKey: ['practice-active'],
    enabled: !resumeSessionId,
    queryFn: async () => {
      const sessions = (await api.get<PracticeSession[]>('/practice/history')).data
      return sessions.find((item) => item.status === 'InProgress') ?? null
    },
  })

  const catalogById = useMemo(
    () => new Map((misconceptions.data ?? []).map((item) => [item.id, item])),
    [misconceptions.data],
  )
  const topicById = useMemo(
    () => new Map((topics.data ?? []).map((item) => [item.id, item])),
    [topics.data],
  )

  const recommended = useMemo(
    () => [...(learnerMisconceptions.data ?? [])]
      .filter((item) => item.confidence >= 0.15 && item.status !== 'Corrected' && item.status !== 'Unknown')
      .sort((a, b) => b.confidence - a.confidence)
      .slice(0, 3),
    [learnerMisconceptions.data],
  )

  useEffect(() => {
    if (presetMisconception && misconceptions.data) {
      const found = misconceptions.data.find((item) => item.id === presetMisconception)
      if (found) {
        setMisconceptionId(found.id)
        setTopicId(found.topicId)
      }
    }
  }, [presetMisconception, misconceptions.data])

  useEffect(() => {
    if (presetMisconception || presetTopic || topicId || !recommended.length) return
    const first = catalogById.get(recommended[0].misconceptionId)
    if (!first) return
    setMisconceptionId(first.id)
    setTopicId(first.topicId)
  }, [presetMisconception, presetTopic, topicId, recommended, catalogById])

  useEffect(() => {
    if (!resumeSessionId || !resumeSession.data || session) return

    if (resumeSession.data.status === 'InProgress') {
      setSession(resumeSession.data)
      setStep(resumeSession.data.completedExercises)
      return
    }
  }, [resumeSession.data, resumeSessionId, session])

  const start = useMutation({
    mutationFn: async () =>
      (await api.post<PracticeSession>('/practice', {
        topicId,
        misconceptionId: misconceptionId || null,
        targetExercises: 5,
      })).data,
    onSuccess: async (data) => {
      setSession(data)
      setQuestion(null)
      setFeedback(null)
      setSelected(null)
      setStep(0)
      setLastResult(null)
      setCompletionError(false)
      await Promise.all([
        client.invalidateQueries({ queryKey: ['practice-history'] }),
        client.invalidateQueries({ queryKey: ['practice-active'] }),
        client.invalidateQueries({ queryKey: ['dashboard'] }),
      ])
    },
  })

  const next = useMutation({
    mutationFn: async (id: string) =>
      (await api.get<DiagnosticQuestion | null>(`/practice/${id}/next`)).data,
    onSuccess: (data) => {
      setQuestion(data)
      setFeedback(null)
      setSelected(null)
    },
  })

  useEffect(() => {
    if (session && !question && !feedback) void next.mutateAsync(session.id)
  }, [session?.id])

  const submit = useMutation({
    mutationFn: async (answerOptionId: string) => {
      if (!session || !question) throw new Error('No practice question')
      const exerciseTypes = ['ConceptCheck', 'GuidedPractice', 'IndependentPractice', 'Transfer']
      return (await api.post<AnswerFeedback>('/practice/answers', {
        sessionId: session.id,
        questionId: question.questionId,
        questionVersionId: question.versionId,
        answerOptionId,
        exerciseType: exerciseTypes[Math.min(step, exerciseTypes.length - 1)],
        responseTimeMs: Math.max(0, Math.round(performance.now() - startedAt)),
      })).data
    },
    onSuccess: async (data) => {
      setFeedback(data)
      setSession((current) => current ? { ...current, completedExercises: current.completedExercises + 1 } : current)
      await Promise.all([
        client.invalidateQueries({ queryKey: ['dashboard'] }),
        client.invalidateQueries({ queryKey: ['my-misconceptions'] }),
        client.invalidateQueries({ queryKey: ['practice-history'] }),
        client.invalidateQueries({ queryKey: ['practice-active'] }),
      ])
    },
  })

  async function advance() {
    if (!session) return
    const nextStep = step + 1

    if (nextStep >= session.targetExercises) {
      setCompletionError(false)
      try {
        const result = (await api.post<PracticeResult>(`/practice/${session.id}/complete`)).data
        setLastResult(result)
        setSession(null)
        setQuestion(null)
        setFeedback(null)
        setSelected(null)
        setStep(0)
        await Promise.all([
          client.invalidateQueries({ queryKey: ['practice-history'] }),
          client.invalidateQueries({ queryKey: ['practice-active'] }),
          client.invalidateQueries({ queryKey: ['dashboard'] }),
          client.invalidateQueries({ queryKey: ['learning-path'] }),
          client.invalidateQueries({ queryKey: ['my-misconceptions'] }),
        ])
      } catch {
        setCompletionError(true)
        return
      }
      return
    }

    setStep(nextStep)
    setQuestion(null)
    setFeedback(null)
    setSelected(null)
    await next.mutateAsync(session.id)
  }

  function begin(event: FormEvent) {
    event.preventDefault()
    if (topicId) start.mutate()
  }

  function chooseRecommendation(item: UserMisconception) {
    const catalog = catalogById.get(item.misconceptionId)
    if (!catalog) return
    setTopicId(catalog.topicId)
    setMisconceptionId(catalog.id)
    setLastResult(null)
  }

  if (resumeSessionId && resumeSession.isLoading) {
    return <LoadingView text="Восстанавливаем незавершённую практику…" />
  }

  if (resumeSessionId && resumeSession.isError) {
    return <ErrorView message="Не удалось открыть практическую сессию. Вернитесь в историю и попробуйте ещё раз." />
  }

  if (resumeSessionId && resumeSession.data && resumeSession.data.status !== 'InProgress' && !session) {
    return (
      <div className="panel">
        <h2>Эту практику нельзя продолжить</h2>
        <p>Сессия уже завершена.</p>
        <button className="secondary-button" onClick={() => navigate('/practice')}>Вернуться к практике</button>
      </div>
    )
  }

  if (resumeSessionId && resumeSession.data && !session) {
    return <LoadingView text="Восстанавливаем прогресс…" />
  }

  if (!session) {
    const unfinished = activeSession.data

    return (
      <div>
        <PageHeader
          eyebrow="Коррекционная практика"
          title="Адаптивная практика"
          description="Практические задания подбираются по теме и, при наличии устойчивой ошибки, могут быть направлены на её коррекцию. Результаты учитываются в дальнейшем расчёте освоения и рекомендаций."
        />

        {lastResult ? (
          <section className="plain-section">
            <h2>Практика завершена</h2>
            <dl className="summary-list">
              <div><dt>Правильных ответов</dt><dd>{lastResult.correct} из {lastResult.total}</dd></div>
              <div><dt>Задание на перенос</dt><dd>{lastResult.transferPassed ? 'Пройдено' : 'Требует повторения'}</dd></div>
              <div><dt>Статус ошибки</dt><dd>{lastResult.misconceptionStatus ? statusLabel(lastResult.misconceptionStatus) : '—'}</dd></div>
            </dl>
          </section>
        ) : null}

        {unfinished ? (
          <section className="plain-section">
            <div className="section-heading-row">
              <div>
                <h2>Незавершённая практика</h2>
                <p>Ваш прогресс сохранён: выполнено {unfinished.completedExercises} из {unfinished.targetExercises} упражнений.</p>
              </div>
              <Link className="secondary-button" to={`/practice/${unfinished.id}/continue`}>Допройти</Link>
            </div>
          </section>
        ) : null}

        {recommended.length > 0 ? (
          <section className="plain-section">
            <h2>Что стоит потренировать</h2>
            <p className="muted">Предложения сформированы по сохранённым результатам диагностики и практики.</p>
            <div className="practice-recommendation-list">
              {recommended.map((item) => (
                <article className="practice-recommendation-row" key={item.misconceptionId}>
                  <div className="practice-recommendation-main">
                    <h3>{item.title}</h3>
                    <p>{item.description}</p>
                  </div>
                  <div className="practice-recommendation-side">
                    <div className="practice-confidence">
                      <span>Выраженность</span>
                      <strong>{Math.round(item.confidence * 100)}%</strong>
                    </div>
                    <button className="secondary-button" type="button" onClick={() => chooseRecommendation(item)}>Выбрать для практики</button>
                  </div>
                </article>
              ))}
            </div>
          </section>
        ) : null}

        <form className="panel practice-builder" onSubmit={begin}>
          <label>
            Тема
            <select value={topicId} onChange={(event) => { setTopicId(event.target.value); setMisconceptionId('') }} required>
              <option value="">Выберите тему</option>
              {(topics.data ?? []).map((topic) => <option key={topic.id} value={topic.id}>{topic.nameRu}</option>)}
            </select>
          </label>
          <label>
            Конкретное заблуждение
            <select value={misconceptionId} onChange={(event) => setMisconceptionId(event.target.value)}>
              <option value="">Общая практика по теме</option>
              {(misconceptions.data ?? []).filter((item) => item.topicId === topicId).map((item) => (
                <option key={item.id} value={item.id}>{item.title}</option>
              ))}
            </select>
          </label>
          <button className="primary-button" disabled={!topicId || start.isPending}>
            {start.isPending ? 'Создаём сессию…' : 'Начать практику'}
          </button>
          {start.isError ? <div className="form-error" role="alert">Не удалось создать практическую сессию. Проверьте выбранные параметры и попробуйте ещё раз.</div> : null}
        </form>

        <section className="plain-section">
          <h2>История тренировок</h2>
          {history.isError ? (
            <p className="form-error" role="alert">Не удалось загрузить историю тренировок.</p>
          ) : (history.data ?? []).length === 0 ? (
            <EmptyState title="Тренировок пока нет" description="После завершения практической сессии её результат появится здесь." />
          ) : (
            <div className="table-frame">
              <table className="academic-table">
                <thead><tr><th>Дата</th><th>Тема</th><th>Выполнено</th><th>Состояние</th><th>Действие</th></tr></thead>
                <tbody>
                  {(history.data ?? []).map((item) => (
                    <tr key={item.id}>
                      <td>{item.startedAt ? new Date(item.startedAt).toLocaleString('ru-RU') : '—'}</td>
                      <td>{topicById.get(item.topicId)?.nameRu ?? 'Неизвестная тема'}</td>
                      <td>{item.completedExercises} / {item.targetExercises}</td>
                      <td>{statusLabel(item.status)}</td>
                      <td>
                        {item.status === 'InProgress' ? (
                          <Link className="primary-button history-action-button" to={`/practice/${item.id}/continue`}>Продолжить</Link>
                        ) : (
                          '—'
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>
    )
  }

  return (
    <div>
      <PageHeader eyebrow="Практика" title={question?.topicName ?? 'Подбираем задание'} />
      <ProgressBar value={session.completedExercises / session.targetExercises} label={`Выполнено ${session.completedExercises} из ${session.targetExercises}`} />

      {next.isError ? (
        <div className="form-error" role="alert">
          Не удалось получить следующее упражнение.
          <button type="button" className="secondary-button" onClick={() => session && next.mutate(session.id)}>
            Повторить
          </button>
        </div>
      ) : null}
      {completionError ? <div className="form-error" role="alert">Не удалось завершить практику. Уже отправленные ответы сохранены, повторите завершение.</div> : null}

      {question ? (
        <section className="question-card">
          <span className="eyebrow">{question.isTransfer ? 'Задание на перенос' : statusLabel(question.difficulty)}</span>
          <h2>{question.prompt}</h2>
          <div className="answer-grid">
            {question.options.map((option, index) => (
              <button
                type="button"
                className={`answer-option ${selected === option.id ? 'selected' : ''}`}
                key={option.id}
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
                onClick={() => selected && submit.mutate(selected)}
              >
                {submit.isPending ? 'Проверяем ответ…' : 'Ответить'}
              </button>
              {!selected ? <span className="answer-hint">Выберите один вариант ответа</span> : null}
            </div>
          ) : null}

          {submit.isError ? <div className="form-error" role="alert">Ответ не был сохранён. Повторите отправку.</div> : null}

          {feedback ? (
            <div className={`feedback ${feedback.isCorrect ? 'feedback--correct' : 'feedback--wrong'}`}>
              <strong>{feedback.isCorrect ? 'Ответ верный' : 'Ответ требует разбора'}</strong>
              <p>{feedback.feedback}</p>
              <p>{feedback.correctExplanation}</p>
              {feedback.updatedConfidence != null ? <p>Результат учтён при обновлении оценки выявленного затруднения.</p> : null}
              <button className="primary-button" onClick={() => void advance()}>
                {session.completedExercises >= session.targetExercises ? 'Завершить практику' : 'Следующее упражнение'}
              </button>
            </div>
          ) : null}
        </section>
      ) : next.isPending ? <div className="panel">Подбираем следующее упражнение…</div> : null}
    </div>
  )
}
