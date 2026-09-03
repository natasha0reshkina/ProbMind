import { useQuery } from '@tanstack/react-query'
import { Database, Download, FileSpreadsheet, UserRound } from 'lucide-react'
import { useState } from 'react'
import { api } from '../api/client'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import type { StudentListItem, Topic } from '../types/api'

async function download(path: string, filename: string) {
  const response = await api.get(path, { responseType: 'blob' })
  const url = URL.createObjectURL(response.data)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  document.body.appendChild(anchor)
  anchor.click()
  anchor.remove()
  URL.revokeObjectURL(url)
}

export function ExportCenterPage() {
  const [studentId, setStudentId] = useState('')
  const [topicId, setTopicId] = useState('')
  const students = useQuery({ queryKey: ['teacher-students'], queryFn: async () => (await api.get<StudentListItem[]>('/teacher/students')).data })
  const topics = useQuery({ queryKey: ['content-topics'], queryFn: async () => (await api.get<Topic[]>('/content/topics')).data })

  return (
    <div>
      <PageHeader
        eyebrow="Экспорт данных"
        title="Экспорт данных"
        description="Выгрузка результатов и статистики в формате Excel для анализа и подготовки отчётов. Раздел доступен преподавателю и администратору."
      />
      <KpiStrip items={[
        { label: 'Доступных студентов', value: students.data?.length ?? 0 },
        { label: 'Тем контента', value: topics.data?.length ?? 0 },
        { label: 'Формат', value: 'Excel', hint: '.xlsx' },
      ]} />

      <div className="export-grid">
        <article className="export-card">
          <div className="export-card__icon"><UserRound size={22} /></div>
          <div><h2>Профиль студента</h2><p>Персональная выгрузка результатов по темам, выявленным заблуждениям и учебной активности.</p></div>
          <label>Студент
            <select value={studentId} onChange={(event) => setStudentId(event.target.value)}>
              <option value="">Выберите студента</option>
              {(students.data ?? []).map((student) => <option key={student.userId} value={student.userId}>{student.displayName} · {student.email}</option>)}
            </select>
          </label>
          <button className="primary-button" disabled={!studentId} onClick={() => void download(`/exports/students/${studentId}`, `rezultaty-studenta-${studentId}.xlsx`)}><Download size={16} /> Скачать таблицу</button>
        </article>

        <article className="export-card">
          <div className="export-card__icon"><Database size={22} /></div>
          <div><h2>Заблуждения по группе</h2><p>Агрегированная распространённость и уровень уверенности по типичным заблуждениям всей группы.</p></div>
          <div className="export-spacer" />
          <button className="primary-button" onClick={() => void download('/exports/cohort/misconceptions', 'tipichnye-oshibki-gruppy.xlsx')}><Download size={16} /> Скачать таблицу по группе</button>
        </article>

        <article className="export-card">
          <div className="export-card__icon"><FileSpreadsheet size={22} /></div>
          <div><h2>Аналитика заданий</h2><p>Метрики качества банка заданий: число ответов, доля правильных ответов, различающая способность, энтропия вариантов и время ответа.</p></div>
          <label>Тема
            <select value={topicId} onChange={(event) => setTopicId(event.target.value)}>
              <option value="">Все темы</option>
              {(topics.data ?? []).map((topic) => <option key={topic.id} value={topic.id}>{topic.nameRu}</option>)}
            </select>
          </label>
          <button className="primary-button" onClick={() => void download(`/exports/questions${topicId ? `?topicId=${topicId}` : ''}`, 'statistika-zadaniy.xlsx')}><Download size={16} /> Скачать таблицу</button>
        </article>
      </div>

      <section className="panel">
        <h2>Контроль экспорта</h2>
        <ul className="feature-list">
          <li>Выгрузки формируются на сервере из текущего состояния базы данных.</li>
          <li>Доступ к выгрузкам ограничен ролями: студент не может получить данные группы или чужой персональный профиль.</li>
          <li>Выгруженный файл можно открыть в табличном редакторе или использовать для дополнительного анализа.</li>
        </ul>
      </section>
    </div>
  )
}
