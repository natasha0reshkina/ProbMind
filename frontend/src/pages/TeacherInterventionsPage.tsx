import { FormEvent, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import type { InterventionSuggestion, StudentListItem, TeacherIntervention } from '../types/api'

export function TeacherInterventionsPage() {
  const client = useQueryClient()
  const [studentId, setStudentId] = useState('')
  const [title, setTitle] = useState('')
  const [body, setBody] = useState('')
  const [kind, setKind] = useState('Practice')

  const suggestions = useQuery({
    queryKey: ['intervention-suggestions'],
    queryFn: async () => (await api.get<InterventionSuggestion[]>('/edtech/teacher/interventions/suggestions')).data,
  })

  const interventions = useQuery({
    queryKey: ['teacher-intervention-items'],
    queryFn: async () => (await api.get<TeacherIntervention[]>('/edtech/teacher/interventions')).data,
  })

  const students = useQuery({
    queryKey: ['teacher-students'],
    queryFn: async () => (await api.get<StudentListItem[]>('/teacher/students')).data,
  })

  const create = useMutation({
    mutationFn: async () => api.post('/edtech/teacher/interventions', {
      studentId,
      title,
      body,
      kind,
      dueAt: null,
    }),
    onSuccess: () => {
      setTitle('')
      setBody('')
      client.invalidateQueries({ queryKey: ['teacher-intervention-items'] })
    },
  })

  if (suggestions.isLoading || interventions.isLoading || students.isLoading) return <LoadingView />

  function chooseSuggestion(item: InterventionSuggestion) {
    setStudentId(item.studentId)
    setTitle(`Повторить: ${item.displayName}`)
    setBody(`Причина назначения: ${item.reason}. Рекомендуется короткая практика по слабой теме и повторная проверка.`)
  }

  function submit(event: FormEvent) {
    event.preventDefault()
    if (studentId && title.trim()) create.mutate()
  }

  return (
    <div>
      <PageHeader
        eyebrow="Преподавательское действие"
        title="Индивидуальное задание"
        description="Система подсказывает, кому может потребоваться дополнительная работа. Преподаватель выбирает студента и создаёт конкретное индивидуальное задание."
      />

      <section className="plain-section">
        <h2>Кому стоит уделить внимание</h2>
        <div className="edtech-card-list">
          {(suggestions.data ?? []).map((item) => (
            <article className="edtech-card edtech-card--warning" key={item.studentId}>
              <div>
                <h3>{item.displayName}</h3>
                <p>{item.reason}</p>
              </div>
              <button type="button" className="secondary-button" onClick={() => chooseSuggestion(item)}>
                Создать задание
              </button>
            </article>
          ))}
        </div>
      </section>

      <section className="two-column edtech-two-column">
        <form className="panel" onSubmit={submit}>
          <h2>Новое назначение</h2>
          <label>
            Студент
            <select value={studentId} onChange={(event) => setStudentId(event.target.value)}>
              <option value="">Выберите студента</option>
              {(students.data ?? []).map((student) => (
                <option value={student.userId} key={student.userId}>{student.displayName}</option>
              ))}
            </select>
          </label>
          <label>
            Тип
            <select value={kind} onChange={(event) => setKind(event.target.value)}>
              <option value="Practice">Практика</option>
              <option value="Review">Повторение</option>
              <option value="Material">Материал</option>
              <option value="Discussion">Разбор на паре</option>
            </select>
          </label>
          <label>
            Название
            <input value={title} onChange={(event) => setTitle(event.target.value)} />
          </label>
          <label>
            Что сделать
            <textarea value={body} onChange={(event) => setBody(event.target.value)} />
          </label>
          <button className="primary-button" disabled={!studentId || !title.trim()}>Назначить</button>
        </form>

        <section className="panel">
          <h2>История индивидуальных заданий</h2>
          <div className="edtech-card-list">
            {(interventions.data ?? []).map((item) => (
              <article className={`edtech-card ${item.isCompleted ? 'edtech-card--completed' : ''}`} key={item.id}>
                <div className="edtech-card__meta">{item.studentName} · {item.kind}</div>
                <h3>{item.title}</h3>
                <p>{item.body}</p>
                <strong>{item.isCompleted ? 'Выполнено студентом' : 'Ожидает выполнения'}</strong>
              </article>
            ))}
          </div>
        </section>
      </section>
    </div>
  )
}
