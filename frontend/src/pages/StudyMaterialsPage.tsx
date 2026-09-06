import { FormEvent, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { EmptyState } from '../components/EmptyState'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { useAuth } from '../auth/AuthContext'
import type { StudyItem } from '../types/api'
import { dateTime } from '../utils/format'

export function StudyMaterialsPage() {
  const { user } = useAuth()
  const client = useQueryClient()
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [shareWithTeacher, setShareWithTeacher] = useState(false)
  const [noteDrafts, setNoteDrafts] = useState<Record<string, string>>({})
  const [noteStates, setNoteStates] = useState<Record<string, 'idle' | 'saved' | 'error'>>({})

  const items = useQuery({
    queryKey: ['study-workspace-student', user?.id],
    queryFn: async () => (await api.get<StudyItem[]>('/study-workspace/student')).data,
  })

  const create = useMutation({
    mutationFn: async () => (await api.post<StudyItem>('/study-workspace/student', {
      title,
      body,
      shareWithTeacher,
    })).data,
    onSuccess: async () => {
      setTitle('')
      setBody('')
      setShareWithTeacher(false)
      await client.invalidateQueries({ queryKey: ['study-workspace-student', user?.id] })
    },
  })

  const saveNote = useMutation({
    mutationFn: async ({ itemId, note }: { itemId: string; note: string }) =>
      (await api.put<StudyItem>(`/study-workspace/student/${itemId}/note`, { body: note })).data,
    onSuccess: async (updated, variables) => {
      setNoteStates((current) => ({ ...current, [variables.itemId]: 'saved' }))
      setNoteDrafts((current) => ({ ...current, [variables.itemId]: updated.studentNote ?? '' }))
      client.setQueryData<StudyItem[]>(['study-workspace-student', user?.id], (current) =>
        (current ?? []).map((item) => item.id === updated.id ? updated : item),
      )
      await client.invalidateQueries({ queryKey: ['study-workspace-student', user?.id] })
    },
    onError: (_error, variables) => {
      setNoteStates((current) => ({ ...current, [variables.itemId]: 'error' }))
    },
  })

  const assignments = useMemo(
    () => (items.data ?? []).filter((item) => item.kind === 'TeacherAssignment'),
    [items.data],
  )
  const ownItems = useMemo(
    () => (items.data ?? []).filter((item) => item.kind === 'StudentMaterial'),
    [items.data],
  )

  function submit(event: FormEvent) {
    event.preventDefault()
    if (body.trim()) create.mutate()
  }

  function noteValue(item: StudyItem) {
    return noteDrafts[item.id] ?? item.studentNote ?? ''
  }

  if (items.isLoading) return <LoadingView />

  return (
    <div>
      <PageHeader
        eyebrow="Учебные материалы"
        title="Материалы и задачи"
        description="Сохраняйте задачи и материалы для себя или отправляйте их преподавателю. Здесь же отображаются задания для всей группы."
      />

      <form className="panel study-create-form" onSubmit={submit}>
        <div className="panel-title"><div><h2>Добавить материал или задачу</h2><p className="muted chart-subtitle">Текст останется только у вас, если не включить отправку преподавателю.</p></div></div>
        <label>
          Название
          <input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Например: задача по формуле Байеса" maxLength={300} />
        </label>
        <label>
          Текст
          <textarea value={body} onChange={(event) => setBody(event.target.value)} placeholder="Условие задачи, ссылка, конспект или другой материал" maxLength={10000} required />
        </label>
        <label className="checkbox-label study-share-option">
          <input type="checkbox" checked={shareWithTeacher} onChange={(event) => setShareWithTeacher(event.target.checked)} />
          <span><strong>Отправить преподавателю</strong><small>Преподаватель увидит этот материал и сможет ответить или отметить его для разбора на паре.</small></span>
        </label>
        <div className="button-row">
          <button className="primary-button" disabled={!body.trim() || create.isPending}>{create.isPending ? 'Сохраняем…' : 'Добавить'}</button>
        </div>
        {create.isError ? <div className="form-error" role="alert">Не удалось сохранить материал.</div> : null}
      </form>

      <section className="plain-section">
        <h2>Задания преподавателя</h2>
        {assignments.length === 0 ? (
          <EmptyState title="Заданий пока нет" description="Когда преподаватель добавит задание для группы, оно появится здесь." />
        ) : (
          <div className="study-list">
            {assignments.map((item) => (
              <article className="study-card" key={item.id}>
                <div className="study-card__heading">
                  <div><span className="eyebrow">Задание для группы</span><h3>{item.title}</h3></div>
                  <span className="muted">{dateTime(item.createdAt)}</span>
                </div>
                <p className="study-card__body">{item.body}</p>
                <label className="study-note-field">
                  Моя заметка
                  <textarea
                    value={noteValue(item)}
                    onChange={(event) => { setNoteDrafts((current) => ({ ...current, [item.id]: event.target.value })); setNoteStates((current) => ({ ...current, [item.id]: 'idle' })) }}
                    placeholder="Оставьте себе заметку по этому заданию"
                    maxLength={5000}
                  />
                </label>
                <button className="secondary-button" type="button" disabled={saveNote.isPending} onClick={() => saveNote.mutate({ itemId: item.id, note: noteValue(item) })}>Сохранить заметку</button>
                {noteStates[item.id] === 'saved' ? <span className="note-save-status note-save-status--saved">Заметка сохранена</span> : null}
                {noteStates[item.id] === 'error' ? <span className="note-save-status note-save-status--error">Не удалось сохранить заметку</span> : null}
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="plain-section">
        <h2>Мои материалы</h2>
        {ownItems.length === 0 ? (
          <EmptyState title="Материалов пока нет" description="Добавьте первую задачу или материал в форме выше." />
        ) : (
          <div className="study-list">
            {ownItems.map((item) => (
              <article className="study-card" key={item.id}>
                <div className="study-card__heading">
                  <div>
                    <span className="eyebrow">{item.visibility === 'SharedWithTeacher' ? 'Отправлено преподавателю' : 'Только мне'}</span>
                    <h3>{item.title}</h3>
                  </div>
                  <span className="muted">{dateTime(item.createdAt)}</span>
                </div>
                <p className="study-card__body">{item.body}</p>

                {item.visibility === 'SharedWithTeacher' && (item.teacherResponse || item.discussInClass) ? (
                  <div className="teacher-response-box">
                    {item.discussInClass ? <strong>Преподаватель отметил: разберём на паре</strong> : null}
                    {item.teacherResponse ? <p>{item.teacherResponse}</p> : null}
                    {item.teacherRespondedAt ? <small>{dateTime(item.teacherRespondedAt)}</small> : null}
                  </div>
                ) : item.visibility === 'SharedWithTeacher' ? (
                  <p className="muted">Материал отправлен преподавателю. Ответа пока нет.</p>
                ) : null}

                <label className="study-note-field">
                  Моя заметка
                  <textarea
                    value={noteValue(item)}
                    onChange={(event) => { setNoteDrafts((current) => ({ ...current, [item.id]: event.target.value })); setNoteStates((current) => ({ ...current, [item.id]: 'idle' })) }}
                    placeholder="Комментарий для себя"
                    maxLength={5000}
                  />
                </label>
                <button className="secondary-button" type="button" disabled={saveNote.isPending} onClick={() => saveNote.mutate({ itemId: item.id, note: noteValue(item) })}>Сохранить заметку</button>
                {noteStates[item.id] === 'saved' ? <span className="note-save-status note-save-status--saved">Заметка сохранена</span> : null}
                {noteStates[item.id] === 'error' ? <span className="note-save-status note-save-status--error">Не удалось сохранить заметку</span> : null}
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
