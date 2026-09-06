import { FormEvent, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import type { StudentGroup, StudentListItem } from '../types/api'

export function TeacherGroupsPage() {
  const client = useQueryClient()
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selected, setSelected] = useState<string[]>([])

  const groups = useQuery({
    queryKey: ['student-groups'],
    queryFn: async () => (await api.get<StudentGroup[]>('/edtech/groups')).data,
  })

  const students = useQuery({
    queryKey: ['teacher-students'],
    queryFn: async () => (await api.get<StudentListItem[]>('/teacher/students')).data,
  })

  const create = useMutation({
    mutationFn: async () => api.post('/edtech/groups', { name, description, studentIds: selected }),
    onSuccess: () => {
      setName('')
      setDescription('')
      setSelected([])
      client.invalidateQueries({ queryKey: ['student-groups'] })
    },
  })

  const sortedStudents = useMemo(
    () => [...(students.data ?? [])].sort((a, b) => a.displayName.localeCompare(b.displayName, 'ru')),
    [students.data],
  )

  if (groups.isLoading || students.isLoading) return <LoadingView />

  function submit(event: FormEvent) {
    event.preventDefault()
    if (name.trim()) create.mutate()
  }

  function toggle(id: string) {
    setSelected((current) => current.includes(id) ? current.filter((item) => item !== id) : [...current, id])
  }

  return (
    <div>
      <PageHeader
        eyebrow="Организация курса"
        title="Группы студентов"
        description="Создавайте группы, чтобы назначать диагностики, экзамены и учебные циклы не всем студентам сразу, а выбранной аудитории."
      />

      <section className="two-column edtech-two-column">
        <form className="panel" onSubmit={submit}>
          <h2>Новая группа</h2>
          <label>
            Название
            <input value={name} onChange={(event) => setName(event.target.value)} placeholder="Например, БИ-231" />
          </label>
          <label>
            Описание
            <textarea value={description} onChange={(event) => setDescription(event.target.value)} placeholder="Необязательно" />
          </label>
          <div className="student-picker">
            <strong>Студенты</strong>
            {sortedStudents.map((student) => (
              <label className="check-row" key={student.userId}>
                <input
                  type="checkbox"
                  checked={selected.includes(student.userId)}
                  onChange={() => toggle(student.userId)}
                />
                <span>
                  {student.displayName}
                  <small>{student.email}</small>
                </span>
              </label>
            ))}
          </div>
          <button className="primary-button" disabled={!name.trim() || create.isPending}>
            {create.isPending ? 'Создаём…' : 'Создать группу'}
          </button>
        </form>

        <section className="panel">
          <h2>Созданные группы</h2>
          {(groups.data ?? []).length === 0 ? (
            <p className="muted">Групп пока нет.</p>
          ) : (
            <div className="edtech-card-list">
              {(groups.data ?? []).map((group) => (
                <article className="edtech-card" key={group.id}>
                  <h3>{group.name}</h3>
                  {group.description ? <p>{group.description}</p> : null}
                  <div className="edtech-card__meta">{group.members.length} студентов</div>
                  <div className="chip-list">
                    {group.members.map((member) => (
                      <span className="soft-chip" key={member.studentId}>{member.displayName}</span>
                    ))}
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </section>
    </div>
  )
}
