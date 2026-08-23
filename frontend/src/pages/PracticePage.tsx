import { useMutation, useQuery } from '@tanstack/react-query'
import { FormEvent, useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api } from '../api/client'
import { PageHeader } from '../components/PageHeader'
import { statusLabel } from '../components/StatusBadge'
import { ProgressBar } from '../components/ProgressBar'
import type {
  AnswerFeedback,
  DiagnosticQuestion,
  MisconceptionCatalog,
  PracticeSession,
  Topic,
} from '../types/api'

export function PracticePage() {
  const [params] = useSearchParams()
  const presetMisconception = params.get('misconception')
  const [topicId, setTopicId] = useState('')
  const [misconceptionId, setMisconceptionId] = useState(presetMisconception ?? '')
  const [session, setSession] = useState<PracticeSession | null>(null)
  const [question, setQuestion] = useState<DiagnosticQuestion | null>(null)
  const [feedback, setFeedback] = useState<AnswerFeedback | null>(null)
  const [step, setStep] = useState(0)

  const topics = useQuery({
    queryKey: ['topics'],
    queryFn: async () => (await api.get<Topic[]>('/content/topics')).data,
  })

  const misconceptions = useQuery({
    queryKey: ['catalog-misconceptions'],
    queryFn: async () => (await api.get<MisconceptionCatalog[]>('/content/misconceptions')).data,
  })

  useEffect(() => {
    if (presetMisconception && misconceptions.data) {
      const found = misconceptions.data.find((x) => x.id === presetMisconception)
      if (found) setTopicId(found.topicId)
    }
  }, [presetMisconception, misconceptions.data])

  const start = useMutation({
    mutationFn: async () =>
      (await api.post<PracticeSession>('/practice', {
        topicId,
        misconceptionId: misconceptionId || null,
        targetExercises: 5,
      })).data,
    onSuccess: (data) => {
      setSession(data)
      setStep(0)
    },
  })

  const next = useMutation({
    mutationFn: async (id: string) =>
      (await api.get<DiagnosticQuestion | null>(`/practice/${id}/next`)).data,
    onSuccess: (data) => {
      setQuestion(data)
      setFeedback(null)
    },
  })

  useEffect(() => {
    if (session && !question) void next.mutateAsync(session.id)
  }, [session])

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
        responseTimeMs: 1000,
      })).data
    },
    onSuccess: (data) => setFeedback(data),
  })

  async function advance() {
    if (!session) return
    const nextStep = step + 1
    setStep(nextStep)
    setQuestion(null)
    setFeedback(null)
    if (nextStep >= session.targetExercises) {
      await api.post(`/practice/${session.id}/complete`)
      setSession(null)
      return
    }
    await next.mutateAsync(session.id)
  }

  function begin(event: FormEvent) {
    event.preventDefault()
    if (topicId) start.mutate()
  }

  if (!session) {
    return (
      <div>
        <PageHeader
          eyebrow="Коррекционная практика"
          title="Адаптивная практика"
          description="Коррекционный сценарий включает проверку понимания, тренировочные задания и задание на перенос, после которого пересчитывается уверенность диагностической модели."
        />
        <form className="panel practice-builder" onSubmit={begin}>
          <label>
            Тема
            <select value={topicId} onChange={(e) => { setTopicId(e.target.value); setMisconceptionId('') }} required>
              <option value="">Выберите тему</option>
              {(topics.data ?? []).map((topic) => <option key={topic.id} value={topic.id}>{topic.nameRu}</option>)}
            </select>
          </label>
          <label>
            Конкретное заблуждение
            <select value={misconceptionId} onChange={(e) => setMisconceptionId(e.target.value)}>
              <option value="">Общая практика по теме</option>
              {(misconceptions.data ?? []).filter((x) => x.topicId === topicId).map((mc) => (
                <option key={mc.id} value={mc.id}>{mc.title}</option>
              ))}
            </select>
          </label>
          <button className="primary-button" disabled={!topicId || start.isPending}>Создать практическую сессию</button>
        </form>
      </div>
    )
  }

  return (
    <div>
      <PageHeader eyebrow="Практика" title={question?.topicName ?? 'Подбираем задание'} />
      <ProgressBar value={step / session.targetExercises} label={`Шаг ${step + 1} из ${session.targetExercises}`} />
      {question ? (
        <section className="question-card">
          <span className="eyebrow">{question.isTransfer ? 'Задание на перенос' : statusLabel(question.difficulty)}</span>
          <h2>{question.prompt}</h2>
          <div className="answer-grid">
            {question.options.map((option, index) => (
              <button className="answer-option" key={option.id} disabled={Boolean(feedback)} onClick={() => submit.mutate(option.id)}>
                <span>{String.fromCharCode(65 + index)}</span>{option.text}
              </button>
            ))}
          </div>
          {feedback ? (
            <div className={`feedback ${feedback.isCorrect ? 'feedback--correct' : 'feedback--wrong'}`}>
              <strong>{feedback.isCorrect ? 'Ответ верный' : 'Ошибка повторилась'}</strong>
              <p>{feedback.feedback}</p>
              {feedback.updatedConfidence != null ? <p>Ответ учтён в результатах практики.</p> : null}
              <button className="primary-button" onClick={() => void advance()}>Продолжить</button>
            </div>
          ) : null}
        </section>
      ) : <div className="panel">Подбираем следующее упражнение…</div>}
    </div>
  )
}
