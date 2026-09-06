import { FormEvent, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import type { DiagnosticTemplate, StudentGroup, TeacherMaterialCycleAnalytics } from '../types/api'

export function TeacherMaterialCyclesPage() {
  const client = useQueryClient()
  const [title, setTitle] = useState('')
  const [materialText, setMaterialText] = useState('')
  const [preId, setPreId] = useState('')
  const [postId, setPostId] = useState('')
  const [groupId, setGroupId] = useState('')

  const cycles = useQuery({
    queryKey: ['teacher-material-cycles'],
    queryFn: async () => (await api.get<TeacherMaterialCycleAnalytics[]>('/edtech/teacher/materials/cycles')).data,
  })

  const templates = useQuery({
    queryKey: ['diagnostic-templates-teacher'],
    queryFn: async () => (await api.get<DiagnosticTemplate[]>('/diagnostic-templates/teacher')).data,
  })

  const groups = useQuery({
    queryKey: ['student-groups'],
    queryFn: async () => (await api.get<StudentGroup[]>('/edtech/groups')).data,
  })

  const create = useMutation({
    mutationFn: async () => api.post('/edtech/teacher/materials/cycles', {
      title,
      materialText,
      preDiagnosticTemplateId: preId,
      postDiagnosticTemplateId: postId,
      groupId: groupId || null,
      isPublished: true,
    }),
    onSuccess: () => {
      setTitle('')
      setMaterialText('')
      client.invalidateQueries({ queryKey: ['teacher-material-cycles'] })
    },
  })

  if (cycles.isLoading || templates.isLoading || groups.isLoading) return <LoadingView />

  function submit(event: FormEvent) {
    event.preventDefault()
    if (title.trim() && materialText.trim() && preId && postId) create.mutate()
  }

  return (
    <div>
      <PageHeader
        eyebrow="Измерение эффекта материала"
        title="Диагностика до и после материала"
        description="Соберите учебный цикл: диагностический срез → материал → повторный срез. В таблице видно, сколько студентов дошли до каждого этапа и как изменился результат."
      />

      <section className="two-column edtech-two-column">
        <form className="panel" onSubmit={submit}>
          <h2>Новый учебный цикл</h2>
          <label>
            Название
            <input value={title} onChange={(event) => setTitle(event.target.value)} />
          </label>
          <label>
            Группа
            <select value={groupId} onChange={(event) => setGroupId(event.target.value)}>
              <option value="">Все студенты</option>
              {(groups.data ?? []).map((group) => (
                <option key={group.id} value={group.id}>{group.name}</option>
              ))}
            </select>
          </label>
          <label>
            Диагностика до
            <select value={preId} onChange={(event) => setPreId(event.target.value)}>
              <option value="">Выберите</option>
              {(templates.data ?? []).map((template) => (
                <option key={template.id} value={template.id}>{template.title}</option>
              ))}
            </select>
          </label>
          <label>
            Учебный материал
            <textarea
              rows={9}
              value={materialText}
              onChange={(event) => setMaterialText(event.target.value)}
              placeholder="Теория, пример, инструкция или текст для изучения"
            />
          </label>
          <label>
            Диагностика после
            <select value={postId} onChange={(event) => setPostId(event.target.value)}>
              <option value="">Выберите</option>
              {(templates.data ?? []).map((template) => (
                <option key={template.id} value={template.id}>{template.title}</option>
              ))}
            </select>
          </label>
          <button
            className="primary-button"
            disabled={!title.trim() || !materialText.trim() || !preId || !postId}
          >
            Опубликовать цикл
          </button>
        </form>

        <section className="panel">
          <h2>Результаты циклов</h2>
          <div className="edtech-card-list">
            {(cycles.data ?? []).map((cycle) => (
              <article className="edtech-card" key={cycle.id}>
                <div className="edtech-card__meta">
                  {cycle.groupName ?? 'Все студенты'} · {cycle.students} студентов
                </div>
                <h3>{cycle.title}</h3>
                <dl className="compact-dl">
                  <div><dt>До</dt><dd>{cycle.completedPre}</dd></div>
                  <div><dt>Материал открыт</dt><dd>{cycle.openedMaterial}</dd></div>
                  <div><dt>После</dt><dd>{cycle.completedPost}</dd></div>
                  <div>
                    <dt>Среднее изменение</dt>
                    <dd>
                      {cycle.meanDelta == null
                        ? '—'
                        : `${cycle.meanDelta >= 0 ? '+' : ''}${Math.round(cycle.meanDelta * 100)} п.п.`}
                    </dd>
                  </div>
                </dl>
              </article>
            ))}
          </div>
        </section>
      </section>
    </div>
  )
}
