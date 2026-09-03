import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronRight, Search } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { BarChartPanel } from '../components/ChartPanel'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import type { StudentListItem } from '../types/api'
import { dateTime, percent } from '../utils/format'

export function TeacherStudentsPage() {
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const [showWithoutResults, setShowWithoutResults] = useState(false)
  const students = useQuery({
    queryKey: ['teacher-students'],
    queryFn: async () => (await api.get<StudentListItem[]>('/teacher/students')).data,
  })

  const rows = students.data ?? []
  const activeRows = rows.filter((row) => row.answeredQuestions > 0)
  const inactiveRows = rows.filter((row) => row.answeredQuestions === 0)
  const totalAnswers = rows.reduce((sum, row) => sum + row.answeredQuestions, 0)
  const totalWrong = rows.reduce((sum, row) => sum + row.wrongAnswers, 0)
  const completedDiagnostics = rows.reduce((sum, row) => sum + row.completedDiagnostics, 0)
  const meanAccuracy = totalAnswers === 0
    ? 0
    : rows.reduce((sum, row) => sum + row.diagnosticAccuracy * row.answeredQuestions, 0) / totalAnswers

  const visibleRows = useMemo(() => {
    const base = showWithoutResults ? rows : activeRows
    const normalized = query.trim().toLocaleLowerCase('ru-RU')
    const filtered = normalized
      ? base.filter((row) => `${row.displayName} ${row.email} ${row.activeMisconceptionTitles.join(' ')}`.toLocaleLowerCase('ru-RU').includes(normalized))
      : base
    return [...filtered].sort((a, b) => b.wrongAnswers - a.wrongAnswers || a.diagnosticAccuracy - b.diagnosticAccuracy || a.displayName.localeCompare(b.displayName, 'ru'))
  }, [activeRows, query, rows, showWithoutResults])

  const accuracyChart = [...activeRows]
    .sort((a, b) => a.diagnosticAccuracy - b.diagnosticAccuracy)
    .slice(0, 10)
    .map((row) => ({ name: row.displayName, value: row.diagnosticAccuracy }))

  const wrongChart = [...activeRows]
    .sort((a, b) => b.wrongAnswers - a.wrongAnswers)
    .slice(0, 10)
    .map((row) => ({ name: row.displayName, value: row.wrongAnswers }))

  return (
    <div>
      <PageHeader
        eyebrow="Результаты группы"
        title="Студенты"
        description="Здесь показаны только учебные результаты: сколько заданий выполнено, где были ошибки и какие затруднения повторяются у каждого студента."
      />

      <KpiStrip items={[
        { label: 'Студентов с результатами', value: activeRows.length },
        { label: 'Ещё не начинали', value: inactiveRows.length },
        { label: 'Завершённых диагностик', value: completedDiagnostics },
        { label: 'Неверных ответов', value: totalWrong, tone: totalWrong ? 'warning' : 'positive' },
        { label: 'Средняя доля правильных', value: totalAnswers ? percent(meanAccuracy) : '—' },
      ]} />

      <section className="panel teacher-students-panel">
        <div className="panel-title teacher-section-heading">
          <div>
            <h2>Результаты по студентам</h2>
            <p className="muted chart-subtitle">Сначала показаны студенты с наибольшим числом неверных ответов. Нажмите на строку, чтобы увидеть конкретные задания и ответы.</p>
          </div>
        </div>

        <div className="teacher-student-toolbar">
          <label className="search-field teacher-student-search" aria-label="Поиск студента">
            <Search size={17} aria-hidden="true" />
            <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Найти студента или типичную ошибку" />
          </label>
          {inactiveRows.length > 0 && (
            <button type="button" className="secondary-button" onClick={() => setShowWithoutResults((value) => !value)}>
              {showWithoutResults ? 'Скрыть студентов без результатов' : `Показать без результатов (${inactiveRows.length})`}
            </button>
          )}
        </div>

        <div className="teacher-student-list">
          {visibleRows.map((row) => (
            <button type="button" className="teacher-student-row" key={row.userId} onClick={() => navigate(`/teacher/students/${row.userId}`)}>
              <div className="teacher-student-identity">
                <strong>{row.displayName}</strong>
                <span>{row.email}</span>
                <small>{row.lastActivityAt ? `Последняя активность: ${dateTime(row.lastActivityAt)}` : 'Учебной активности пока нет'}</small>
              </div>

              <div className="teacher-student-metrics" aria-label="Результаты студента">
                <div><span>Диагностик</span><strong>{row.completedDiagnostics}</strong></div>
                <div><span>Ответов</span><strong>{row.answeredQuestions || '—'}</strong></div>
                <div><span>Неверно</span><strong className={row.wrongAnswers > 0 ? 'metric-warning' : undefined}>{row.answeredQuestions ? row.wrongAnswers : '—'}</strong></div>
                <div><span>Правильно</span><strong>{row.answeredQuestions ? percent(row.diagnosticAccuracy) : '—'}</strong></div>
                <div><span>Освоение</span><strong>{row.answeredQuestions ? percent(row.overallMastery) : '—'}</strong></div>
              </div>

              <div className="teacher-student-errors">
                <span className="teacher-student-errors__label">Повторяющиеся затруднения</span>
                {row.activeMisconceptionTitles.length ? (
                  <div className="teacher-error-list">
                    {row.activeMisconceptionTitles.slice(0, 4).map((title) => <span key={title}>{title}</span>)}
                    {row.activeMisconceptionTitles.length > 4 && <small>Ещё {row.activeMisconceptionTitles.length - 4}</small>}
                  </div>
                ) : (
                  <span className="muted teacher-no-errors">{row.answeredQuestions ? 'Устойчивые ошибки не выявлены' : 'Нет результатов для анализа'}</span>
                )}
              </div>
              <ChevronRight className="teacher-student-row__arrow" size={19} aria-hidden="true" />
            </button>
          ))}
          {!visibleRows.length && (
            <div className="teacher-list-empty">По выбранным условиям студентов не найдено.</div>
          )}
        </div>
      </section>

      {activeRows.length >= 2 && (
        <div className="two-column teacher-result-charts">
          <BarChartPanel
            title="Доля правильных ответов"
            subtitle="Студенты с более низким результатом находятся выше списка."
            data={accuracyChart}
            xKey="name"
            series={[{ key: 'value', label: 'Правильных ответов' }]}
            percent
            horizontal
            height={330}
          />
          <BarChartPanel
            title="Количество неверных ответов"
            subtitle="Показывает общий объём ошибок в сохранённых ответах."
            data={wrongChart}
            xKey="name"
            series={[{ key: 'value', label: 'Неверных ответов' }]}
            horizontal
            height={330}
          />
        </div>
      )}
    </div>
  )
}
