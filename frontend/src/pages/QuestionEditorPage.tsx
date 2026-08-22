import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, CirclePlus, CopyPlus, Save, Send, Trash2 } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/client'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import type { MisconceptionCatalog, QuestionDetail, Topic } from '../types/api'

const kindLabels: Record<string, string> = { Diagnostic: 'Диагностическое', Corrective: 'Коррекционное', Transfer: 'Перенос', MasteryCheck: 'Контрольное' }
const difficultyLabels: Record<string, string> = { Introductory: 'Вводная', Basic: 'Базовая', Intermediate: 'Средняя', Advanced: 'Высокая', Transfer: 'Перенос' }

interface OptionDraft {
  text: string
  isCorrect: boolean
  misconceptionId: string
  feedback: string
  sortOrder: number
}

interface EditorState {
  topicId: string
  code: string
  kind: string
  prompt: string
  correctExplanation: string
  difficulty: string
  isTransfer: boolean
  testedMisconceptionIds: string[]
  options: OptionDraft[]
}

const blankOption = (sortOrder: number): OptionDraft => ({ text: '', isCorrect: false, misconceptionId: '', feedback: '', sortOrder })
const emptyState = (): EditorState => ({
  topicId: '', code: '', kind: 'Diagnostic', prompt: '', correctExplanation: '', difficulty: 'Intermediate', isTransfer: false,
  testedMisconceptionIds: [], options: [{ ...blankOption(0), isCorrect: true }, blankOption(1), blankOption(2), blankOption(3)],
})

export function QuestionEditorPage() {
  const { questionId } = useParams()
  const isNew = questionId === 'new'
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [state, setState] = useState<EditorState>(emptyState)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const topics = useQuery({ queryKey: ['content-topics'], queryFn: async () => (await api.get<Topic[]>('/content/topics')).data })
  const misconceptions = useQuery({ queryKey: ['content-misconceptions'], queryFn: async () => (await api.get<MisconceptionCatalog[]>('/content/misconceptions')).data })
  const detail = useQuery({
    queryKey: ['question-detail', questionId],
    queryFn: async () => (await api.get<QuestionDetail>(`/content/questions/${questionId}`)).data,
    enabled: Boolean(questionId && !isNew),
  })

  useEffect(() => {
    if (!detail.data) return
    const current = detail.data
    setState({
      topicId: current.question.topicId,
      code: current.question.code,
      kind: current.question.kind,
      prompt: current.currentVersion.prompt,
      correctExplanation: current.currentVersion.correctExplanation,
      difficulty: current.currentVersion.difficulty,
      isTransfer: current.currentVersion.isTransfer,
      testedMisconceptionIds: current.testedMisconceptionIds,
      options: current.currentVersion.options.map((option) => ({
        text: option.text,
        isCorrect: option.isCorrect,
        misconceptionId: option.misconceptionId ?? '',
        feedback: option.feedback,
        sortOrder: option.sortOrder,
      })),
    })
  }, [detail.data])

  const topicMisconceptions = useMemo(() => (misconceptions.data ?? []).filter((item) => !state.topicId || item.topicId === state.topicId), [misconceptions.data, state.topicId])
  const correctCount = state.options.filter((option) => option.isCorrect).length
  const invalidDiagnosticMappings = state.kind === 'Diagnostic' ? state.options.filter((option) => !option.isCorrect && !option.misconceptionId).length : 0
  const canSave = Boolean(state.topicId && state.code.trim() && state.prompt.trim() && state.correctExplanation.trim() && state.options.length >= 2 && correctCount === 1 && invalidDiagnosticMappings === 0 && (state.kind !== 'Diagnostic' || state.testedMisconceptionIds.length > 0))

  function updateOption(index: number, patch: Partial<OptionDraft>) {
    setState((current) => ({ ...current, options: current.options.map((option, i) => i === index ? { ...option, ...patch } : option) }))
  }

  function setCorrect(index: number) {
    setState((current) => ({ ...current, options: current.options.map((option, i) => ({ ...option, isCorrect: i === index, misconceptionId: i === index ? '' : option.misconceptionId })) }))
  }

  function addOption() {
    setState((current) => current.options.length >= 8 ? current : ({ ...current, options: [...current.options, blankOption(current.options.length)] }))
  }

  function removeOption(index: number) {
    setState((current) => ({ ...current, options: current.options.filter((_, i) => i !== index).map((option, i) => ({ ...option, sortOrder: i })) }))
  }

  function toggleTested(id: string) {
    setState((current) => ({ ...current, testedMisconceptionIds: current.testedMisconceptionIds.includes(id) ? current.testedMisconceptionIds.filter((item) => item !== id) : [...current.testedMisconceptionIds, id] }))
  }

  const save = useMutation({
    mutationFn: async () => {
      setError('')
      setMessage('')
      const options = state.options.map((option, index) => ({
        text: option.text,
        isCorrect: option.isCorrect,
        misconceptionId: option.misconceptionId || null,
        feedback: option.feedback,
        sortOrder: index,
      }))
      if (isNew) {
        return (await api.post<QuestionDetail>('/content/questions', {
          topicId: state.topicId, code: state.code, kind: state.kind, prompt: state.prompt,
          correctExplanation: state.correctExplanation, difficulty: state.difficulty, isTransfer: state.isTransfer,
          options, testedMisconceptionIds: state.testedMisconceptionIds,
        })).data
      }
      return (await api.post<QuestionDetail>('/content/questions/versions', {
        questionId, prompt: state.prompt, correctExplanation: state.correctExplanation,
        difficulty: state.difficulty, isTransfer: state.isTransfer, options,
      })).data
    },
    onSuccess: (result) => {
      setMessage(isNew ? 'Задание создано. Теперь его можно проверить и опубликовать.' : `Создана новая версия v${result.currentVersion.versionNumber}.`)
      queryClient.invalidateQueries({ queryKey: ['teacher-questions'] })
      queryClient.invalidateQueries({ queryKey: ['question-detail'] })
      if (isNew) navigate(`/teacher/content/${result.question.id}`, { replace: true })
    },
    onError: (reason) => setError(reason instanceof Error ? reason.message : 'Не удалось сохранить задание.'),
  })

  const publish = useMutation({
    mutationFn: async () => (await api.post<QuestionDetail>(`/content/questions/${questionId}/publish`)).data,
    onSuccess: () => { setMessage('Текущая версия опубликована.'); queryClient.invalidateQueries({ queryKey: ['question-detail'] }); queryClient.invalidateQueries({ queryKey: ['teacher-questions'] }) },
  })

  const selectedTopic = topics.data?.find((topic) => topic.id === state.topicId)

  return (
    <div>
      <div className="back-row"><Link to="/teacher/content"><ArrowLeft size={16} /> Банк заданий</Link></div>
      <PageHeader
        eyebrow="Редактор задания"
        title={isNew ? 'Новое задание' : `Редактор · ${state.code || '…'}`}
        description={isNew ? 'Создайте новое диагностическое или коррекционное задание.' : `Редактирование создаёт новую версию задания. Ранее опубликованная версия сохраняется для уже пройденных диагностик.`}
        actions={
          <div className="button-row button-row--top">
            {!isNew && detail.data && <StatusBadge value={detail.data.question.status} />}
            {!isNew && detail.data?.question.status === 'Draft' && <button className="secondary-button" onClick={() => publish.mutate()}><Send size={16} /> Опубликовать</button>}
            <button className="primary-button" disabled={!canSave || save.isPending} onClick={() => save.mutate()}>{isNew ? <Save size={16} /> : <CopyPlus size={16} />}{isNew ? 'Создать' : 'Создать новую версию'}</button>
          </div>
        }
      />

      {(message || error) && <div className={error ? 'alert alert--danger' : 'alert alert--success'}>{error || message}</div>}

      <div className="editor-layout">
        <div className="editor-main">
          <section className="panel form-section">
            <div className="panel-title"><div><h2>Метаданные</h2><p className="muted chart-subtitle">Стабильные свойства вопроса и классификация контента.</p></div></div>
            <div className="form-grid form-grid--three">
              <label>Тема
                <select value={state.topicId} disabled={!isNew} onChange={(event) => setState((current) => ({ ...current, topicId: event.target.value, testedMisconceptionIds: [] }))}>
                  <option value="">Выберите тему</option>
                  {(topics.data ?? []).map((topic) => <option key={topic.id} value={topic.id}>{topic.nameRu}</option>)}
                </select>
              </label>
              <label>Код
                <input value={state.code} disabled={!isNew} placeholder="COND_012" onChange={(event) => setState((current) => ({ ...current, code: event.target.value.toUpperCase() }))} />
              </label>
              <label>Тип
                <select value={state.kind} disabled={!isNew} onChange={(event) => setState((current) => ({ ...current, kind: event.target.value }))}>
                  {['Diagnostic', 'Corrective', 'Transfer', 'MasteryCheck'].map((kind) => <option key={kind} value={kind}>{kindLabels[kind]}</option>)}
                </select>
              </label>
              <label>Сложность
                <select value={state.difficulty} onChange={(event) => setState((current) => ({ ...current, difficulty: event.target.value }))}>
                  {['Introductory', 'Basic', 'Intermediate', 'Advanced', 'Transfer'].map((difficulty) => <option key={difficulty} value={difficulty}>{difficultyLabels[difficulty]}</option>)}
                </select>
              </label>
              <label className="checkbox-label editor-checkbox"><input type="checkbox" checked={state.isTransfer} onChange={(event) => setState((current) => ({ ...current, isTransfer: event.target.checked }))} /> Задание на перенос</label>
            </div>
          </section>

          <section className="panel form-section">
            <div className="panel-title"><div><h2>Формулировка</h2><p className="muted chart-subtitle">Текст задания и объяснение правильного рассуждения, которое увидит студент после ответа.</p></div></div>
            <label>Условие задания
              <textarea rows={6} value={state.prompt} placeholder="Введите условие диагностического задания…" onChange={(event) => setState((current) => ({ ...current, prompt: event.target.value }))} />
              <span className="field-counter">{state.prompt.length} / 8000</span>
            </label>
            <label>Объяснение правильного ответа
              <textarea rows={5} value={state.correctExplanation} placeholder="Объясните, почему правильный ответ корректен и какая концепция проверяется…" onChange={(event) => setState((current) => ({ ...current, correctExplanation: event.target.value }))} />
              <span className="field-counter">{state.correctExplanation.length} / 12000</span>
            </label>
          </section>

          <section className="panel form-section">
            <div className="panel-title"><div><h2>Варианты ответа и диагностика</h2><p className="muted chart-subtitle">Каждый диагностический дистрактор связывается с конкретным типом заблуждения. Правильный ответ, наоборот, уменьшает уверенность в проверяемой гипотезе.</p></div><button className="secondary-button small" disabled={state.options.length >= 8} onClick={addOption}><CirclePlus size={15} /> Вариант</button></div>
            <div className="option-editor-list">
              {state.options.map((option, index) => (
                <article className={`option-editor ${option.isCorrect ? 'option-editor--correct' : ''}`} key={index}>
                  <div className="option-editor__index">{String.fromCharCode(65 + index)}</div>
                  <div className="option-editor__body">
                    <div className="option-editor__top">
                      <label className="radio-label"><input type="radio" name="correct-option" checked={option.isCorrect} onChange={() => setCorrect(index)} /> Правильный ответ</label>
                      {state.options.length > 2 && <button className="icon-button" title="Удалить" onClick={() => removeOption(index)}><Trash2 size={16} /></button>}
                    </div>
                    <textarea rows={2} value={option.text} placeholder="Текст варианта ответа" onChange={(event) => updateOption(index, { text: event.target.value })} />
                    {!option.isCorrect && state.kind === 'Diagnostic' && (
                      <label>Какое заблуждение выражает этот вариант?
                        <select value={option.misconceptionId} onChange={(event) => updateOption(index, { misconceptionId: event.target.value })}>
                          <option value="">Выберите заблуждение</option>
                          {topicMisconceptions.map((item) => <option key={item.id} value={item.id}>{item.code} · {item.title}</option>)}
                        </select>
                      </label>
                    )}
                    <label>Пояснение для студента
                      <textarea rows={2} value={option.feedback} placeholder="Короткое объяснение выбора…" onChange={(event) => updateOption(index, { feedback: event.target.value })} />
                    </label>
                  </div>
                </article>
              ))}
            </div>
          </section>
        </div>

        <aside className="editor-side">
          <section className="panel sticky-panel">
            <h2>Проверяемые заблуждения</h2>
            <p className="muted">Для диагностического вопроса отметьте гипотезы, уверенность в которых должна снижаться после правильного ответа.</p>
            <div className="check-list">
              {topicMisconceptions.map((item) => (
                <label className="check-card" key={item.id}>
                  <input type="checkbox" checked={state.testedMisconceptionIds.includes(item.id)} disabled={!isNew} onChange={() => toggleTested(item.id)} />
                  <div><code>{item.code}</code><strong>{item.title}</strong><span>{item.description}</span></div>
                </label>
              ))}
              {!state.topicId && <div className="empty-state compact-empty">Сначала выберите тему.</div>}
            </div>

            <div className="editor-validation">
              <h3>Проверка перед сохранением</h3>
              <Validation ok={Boolean(state.topicId)} text="Тема выбрана" />
              <Validation ok={Boolean(state.prompt.trim())} text="Условие заполнено" />
              <Validation ok={Boolean(state.correctExplanation.trim())} text="Объяснение заполнено" />
              <Validation ok={correctCount === 1} text="Ровно один правильный ответ" />
              <Validation ok={invalidDiagnosticMappings === 0} text="Все диагностические дистракторы связаны с заблуждениями" />
              <Validation ok={state.kind !== 'Diagnostic' || state.testedMisconceptionIds.length > 0} text="Указаны проверяемые заблуждения" />
            </div>

            {!isNew && detail.data && (
              <div className="version-meta">
                <span>Текущая версия</span><strong>v{detail.data.currentVersion.versionNumber}</strong>
                <span>Тема</span><strong>{selectedTopic?.nameRu ?? '—'}</strong>
                <span>Статус</span><StatusBadge value={detail.data.question.status} />
              </div>
            )}
          </section>
        </aside>
      </div>
    </div>
  )
}

function Validation({ ok, text }: { ok: boolean; text: string }) {
  return <div className={`validation-row ${ok ? 'validation-row--ok' : ''}`}><span>{ok ? '✓' : '•'}</span>{text}</div>
}
