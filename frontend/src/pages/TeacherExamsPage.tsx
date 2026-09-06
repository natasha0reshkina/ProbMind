import { FormEvent, useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import type { DiagnosticTemplate, Exam, StudentGroup, StudentListItem } from '../types/api'

type AudienceKind = 'all' | 'student' | 'group'

export function TeacherExamsPage() {
  const client = useQueryClient()
  const [templateId, setTemplateId] = useState('')
  const [audienceKind, setAudienceKind] = useState<AudienceKind>('all')
  const [studentId, setStudentId] = useState('')
  const [groupId, setGroupId] = useState('')
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [minutes, setMinutes] = useState(30)

  const exams = useQuery({ queryKey: ['teacher-exams'], queryFn: async () => (await api.get<Exam[]>('/edtech/teacher/exams')).data })
  const templates = useQuery({ queryKey: ['diagnostic-templates-teacher'], queryFn: async () => (await api.get<DiagnosticTemplate[]>('/diagnostic-templates/teacher')).data })
  const groups = useQuery({ queryKey: ['student-groups'], queryFn: async () => (await api.get<StudentGroup[]>('/edtech/groups')).data })
  const students = useQuery({ queryKey: ['teacher-students'], queryFn: async () => (await api.get<StudentListItem[]>('/teacher/students')).data })

  const create = useMutation({
    mutationFn: async () => api.post('/edtech/teacher/exams', {
      diagnosticTemplateId: templateId,
      groupId: audienceKind === 'group' ? groupId || null : null,
      studentId: audienceKind === 'student' ? studentId || null : null,
      title,
      description,
      timeLimitMinutes: minutes,
      isPublished: true,
      availableFrom: null,
      availableUntil: null,
    }),
    onSuccess: () => {
      setTitle('')
      setDescription('')
      client.invalidateQueries({ queryKey: ['teacher-exams'] })
    },
  })

  if (exams.isLoading || templates.isLoading || groups.isLoading || students.isLoading) return <LoadingView />

  function submit(e: FormEvent) {
    e.preventDefault()
    const audienceReady = audienceKind === 'all' || (audienceKind === 'student' && !!studentId) || (audienceKind === 'group' && !!groupId)
    if (templateId && title.trim() && audienceReady) create.mutate()
  }

  const audienceReady = audienceKind === 'all' || (audienceKind === 'student' && !!studentId) || (audienceKind === 'group' && !!groupId)

  return (
    <div>
      <PageHeader eyebrow="Контроль знаний" title="Режим «Экзамен»" description="Используйте готовую диагностику как экзамен: назначьте всех студентов, конкретного студента или учебную группу и задайте лимит времени." />
      <section className="two-column edtech-two-column">
        <form className="panel" onSubmit={submit}>
          <h2>Создать экзамен</h2>
          <label>Диагностика
            <select value={templateId} onChange={(e) => setTemplateId(e.target.value)}>
              <option value="">Выберите набор задач</option>
              {(templates.data ?? []).map((t) => <option key={t.id} value={t.id}>{t.title} · {t.questionCount} заданий</option>)}
            </select>
          </label>

          <fieldset className="audience-fieldset">
            <legend>Для кого</legend>
            <div className="audience-options">
              <label className="audience-option">
                <input type="radio" name="audience" checked={audienceKind === 'all'} onChange={() => { setAudienceKind('all'); setStudentId(''); setGroupId('') }} />
                <span><strong>Все студенты</strong><small>Экзамен увидят все студенты</small></span>
              </label>
              <label className="audience-option">
                <input type="radio" name="audience" checked={audienceKind === 'student'} onChange={() => { setAudienceKind('student'); setGroupId('') }} />
                <span><strong>Конкретный студент</strong><small>Назначить экзамен одному студенту</small></span>
              </label>
              <label className="audience-option">
                <input type="radio" name="audience" checked={audienceKind === 'group'} onChange={() => { setAudienceKind('group'); setStudentId('') }} />
                <span><strong>Группа</strong><small>Назначить экзамен выбранной группе</small></span>
              </label>
            </div>
          </fieldset>

          {audienceKind === 'student' ? (
            <label>Студент
              <select value={studentId} onChange={(e) => setStudentId(e.target.value)}>
                <option value="">Выберите студента</option>
                {(students.data ?? []).map((student) => <option key={student.userId} value={student.userId}>{student.displayName} · {student.email}</option>)}
              </select>
            </label>
          ) : null}

          {audienceKind === 'group' ? (
            <label>Группа
              <select value={groupId} onChange={(e) => setGroupId(e.target.value)}>
                <option value="">Выберите группу</option>
                {(groups.data ?? []).map((group) => <option key={group.id} value={group.id}>{group.name} · {group.members.length} студентов</option>)}
              </select>
            </label>
          ) : null}

          <label>Название<input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Контрольная №1" /></label>
          <label>Описание<textarea value={description} onChange={(e) => setDescription(e.target.value)} /></label>
          <label>Лимит времени, минут<input type="number" min={1} max={300} value={minutes} onChange={(e) => setMinutes(Number(e.target.value))} /></label>
          <button className="primary-button" disabled={!templateId || !title.trim() || !audienceReady}>Опубликовать экзамен</button>
        </form>

        <section className="panel">
          <h2>Экзамены</h2>
          <div className="edtech-card-list">
            {(exams.data ?? []).map((exam) => {
              const audience = exam.studentName ? `Студент: ${exam.studentName}` : exam.groupName ? `Группа: ${exam.groupName}` : 'Все студенты'
              return <article className="edtech-card" key={exam.id}>
                <div className="edtech-card__meta">{audience} · {exam.questionCount} заданий · {exam.timeLimitMinutes} мин.</div>
                <h3>{exam.title}</h3>
                {exam.description ? <p>{exam.description}</p> : null}
                <div className="exam-card-progress">
                  <strong>{exam.isPublished ? 'Опубликован' : 'Черновик'}</strong>
                  <span>Решили: {exam.completedStudents} / {exam.assignedStudents}</span>
                </div>
                {exam.allCompleted ? (
                  <Link className="secondary-button exam-review-link" to={`/teacher/exams/${exam.id}`}>Посмотреть ответы</Link>
                ) : (
                  <div className="exam-review-waiting">
                    {exam.assignedStudents === 0 ? 'Нет назначенных студентов' : 'Ответы откроются после завершения экзамена всеми назначенными студентами'}
                  </div>
                )}
              </article>
            })}
          </div>
        </section>
      </section>
    </div>
  )
}
