import { FormEvent, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { EmptyState } from '../components/EmptyState'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import type { StudyItem, StudyItemComment } from '../types/api'
import { dateTime } from '../utils/format'

export function TeacherMaterialsPage() {
  const client = useQueryClient()
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [responseDrafts, setResponseDrafts] = useState<Record<string, string>>({})

  const items = useQuery({
    queryKey: ['study-workspace-teacher'],
    queryFn: async () => (await api.get<StudyItem[]>('/study-workspace/teacher')).data,
  })

  const comments = useQuery({
    queryKey: ['study-workspace-teacher-comments'],
    queryFn: async () => (await api.get<StudyItemComment[]>('/study-workspace/teacher/comments')).data,
  })

  const createAssignment = useMutation({
    mutationFn: async () => (await api.post<StudyItem>('/study-workspace/teacher/assignments', { title, body })).data,
    onSuccess: async () => {
      setTitle('')
      setBody('')
      await client.invalidateQueries({ queryKey: ['study-workspace-teacher'] })
    },
  })

  const respond = useMutation({
    mutationFn: async ({ itemId, response, discussInClass }: { itemId: string; response: string; discussInClass: boolean }) =>
      (await api.put<StudyItem>(`/study-workspace/teacher/${itemId}/response`, { response, discussInClass })).data,
    onSuccess: async () => {
      await client.invalidateQueries({ queryKey: ['study-workspace-teacher'] })
    },
  })

  const studentItems = useMemo(
    () => (items.data ?? []).filter((item) => item.kind === 'StudentMaterial'),
    [items.data],
  )
  const assignments = useMemo(
    () => (items.data ?? []).filter((item) => item.kind === 'TeacherAssignment'),
    [items.data],
  )

  function submit(event: FormEvent) {
    event.preventDefault()
    if (body.trim()) createAssignment.mutate()
  }

  function responseValue(item: StudyItem) {
    return responseDrafts[item.id] ?? item.teacherResponse ?? ''
  }

  if (items.isLoading || comments.isLoading) return <LoadingView />

  return (
    <div>
      <PageHeader
        eyebrow="Работа со студентами"
        title="Материалы и задачи"
        description="Здесь отображаются материалы, которые студенты отправили преподавателю, и задания для всей группы."
      />

      <form className="panel study-create-form" onSubmit={submit}>
        <div className="panel-title"><div><h2>Новое задание для всех студентов</h2><p className="muted chart-subtitle">Задание появится в разделе материалов у каждого студента.</p></div></div>
        <label>
          Название
          <input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="Название задания" maxLength={300} />
        </label>
        <label>
          Текст задания
          <textarea value={body} onChange={(event) => setBody(event.target.value)} placeholder="Условие, материалы или инструкция" maxLength={10000} required />
        </label>
        <div className="button-row"><button className="primary-button" disabled={!body.trim() || createAssignment.isPending}>{createAssignment.isPending ? 'Публикуем…' : 'Добавить всем студентам'}</button></div>
        {createAssignment.isError ? <div className="form-error" role="alert">Не удалось добавить задание.</div> : null}
      </form>

      <section className="plain-section teacher-comments-section">
        <div className="section-heading-row">
          <div>
            <h2>Комментарии студентов к заданиям</h2>
            <p>Здесь собраны заметки к общим заданиям и материалам, которые студенты отправили преподавателю. Личные материалы студентов сюда не попадают.</p>
          </div>
        </div>
        {(comments.data ?? []).length === 0 ? (
          <EmptyState title="Комментариев пока нет" description="Когда студент оставит комментарий к доступному преподавателю заданию, он появится здесь." />
        ) : (
          <div className="study-list">
            {(comments.data ?? []).map((comment) => (
              <article className="study-card study-comment-card" key={`${comment.itemId}-${comment.studentId}`}>
                <div className="study-card__heading">
                  <div>
                    <span className="eyebrow">{comment.studentName}</span>
                    <h3>{comment.itemTitle}</h3>
                  </div>
                  <span className="muted">{dateTime(comment.commentedAt)}</span>
                </div>
                <div className="study-comment-task">
                  <strong>Задание</strong>
                  <p>{comment.itemBody}</p>
                </div>
                <div className="study-comment-text">
                  <strong>Комментарий студента</strong>
                  <p>{comment.comment}</p>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="plain-section">
        <h2>От студентов</h2>
        {studentItems.length === 0 ? (
          <EmptyState title="Новых материалов нет" description="Здесь появятся задачи и материалы, которые студенты решат отправить преподавателю." />
        ) : (
          <div className="study-list">
            {studentItems.map((item) => (
              <article className="study-card" key={item.id}>
                <div className="study-card__heading">
                  <div><span className="eyebrow">{item.studentName ?? 'Студент'}</span><h3>{item.title}</h3></div>
                  <span className="muted">{dateTime(item.createdAt)}</span>
                </div>
                <p className="study-card__body">{item.body}</p>
                <label className="study-note-field">
                  Ответ студенту
                  <textarea
                    value={responseValue(item)}
                    onChange={(event) => setResponseDrafts((current) => ({ ...current, [item.id]: event.target.value }))}
                    placeholder="Напишите ответ или пояснение"
                    maxLength={5000}
                  />
                </label>
                <div className="button-row">
                  <button className="primary-button" type="button" disabled={respond.isPending} onClick={() => respond.mutate({ itemId: item.id, response: responseValue(item), discussInClass: item.discussInClass })}>Отправить ответ</button>
                  <button className="secondary-button" type="button" disabled={respond.isPending} onClick={() => respond.mutate({ itemId: item.id, response: responseValue(item), discussInClass: !item.discussInClass })}>{item.discussInClass ? 'Снять пометку разберём на паре' : 'Разберём на паре'}</button>
                </div>
                {item.discussInClass ? <div className="study-class-marker">Отмечено для разбора на паре</div> : null}
              </article>
            ))}
          </div>
        )}
      </section>

      <section className="plain-section">
        <h2>Задания для группы</h2>
        {assignments.length === 0 ? (
          <EmptyState title="Заданий пока нет" description="Добавьте первое общее задание в форме выше." />
        ) : (
          <div className="study-list">
            {assignments.map((item) => (
              <article className="study-card" key={item.id}>
                <div className="study-card__heading"><div><span className="eyebrow">Для всех студентов</span><h3>{item.title}</h3></div><span className="muted">{dateTime(item.createdAt)}</span></div>
                <p className="study-card__body">{item.body}</p>
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
