import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { api } from '../api/client'
import { BarChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import type { StudentGroup, StudentListItem } from '../types/api'
import { dateTime, percent } from '../utils/format'

type GroupSummary = {
  id: string
  name: string
  students: number
  studentsWithResults: number
  completedDiagnostics: number
  answeredQuestions: number
  wrongAnswers: number
  accuracy: number
  mastery: number
}

export function TeacherStudentsPage() {
  const navigate = useNavigate()
  const [viewMode, setViewMode] = useState<'students' | 'groups'>('students')
  const [groupFilter, setGroupFilter] = useState('all')
  const [showWithoutResults, setShowWithoutResults] = useState(false)

  const students = useQuery({
    queryKey: ['teacher-students'],
    queryFn: async () => (await api.get<StudentListItem[]>('/teacher/students')).data,
  })

  const groups = useQuery({
    queryKey: ['student-groups'],
    queryFn: async () => (await api.get<StudentGroup[]>('/edtech/groups')).data,
  })

  const rows = students.data ?? []
  const groupFilteredRows = groupFilter === 'all' ? rows : rows.filter((row) => row.groupNames.includes(groupFilter))
  const activeRows = groupFilteredRows.filter((row) => row.answeredQuestions > 0)
  const inactiveRows = groupFilteredRows.filter((row) => row.answeredQuestions === 0)
  const totalAnswers = rows.reduce((sum, row) => sum + row.answeredQuestions, 0)
  const totalWrong = rows.reduce((sum, row) => sum + row.wrongAnswers, 0)
  const completedDiagnostics = rows.reduce((sum, row) => sum + row.completedDiagnostics, 0)
  const meanAccuracy = totalAnswers === 0
    ? 0
    : rows.reduce((sum, row) => sum + row.diagnosticAccuracy * row.answeredQuestions, 0) / totalAnswers

  const groupRows = useMemo<GroupSummary[]>(() => {
    const byId = new Map(rows.map((row) => [row.userId, row]))
    return (groups.data ?? []).map((group) => {
      const members = group.members.map((member) => byId.get(member.studentId)).filter((item): item is StudentListItem => Boolean(item))
      const withResults = members.filter((item) => item.answeredQuestions > 0)
      const answers = members.reduce((sum, item) => sum + item.answeredQuestions, 0)
      const wrong = members.reduce((sum, item) => sum + item.wrongAnswers, 0)
      const observed = members.filter((item) => item.answeredQuestions > 0)
      return {
        id: group.id,
        name: group.name,
        students: members.length,
        studentsWithResults: withResults.length,
        completedDiagnostics: members.reduce((sum, item) => sum + item.completedDiagnostics, 0),
        answeredQuestions: answers,
        wrongAnswers: wrong,
        accuracy: answers === 0 ? 0 : members.reduce((sum, item) => sum + item.diagnosticAccuracy * item.answeredQuestions, 0) / answers,
        mastery: observed.length === 0 ? 0 : observed.reduce((sum, item) => sum + item.overallMastery, 0) / observed.length,
      }
    }).sort((a, b) => a.name.localeCompare(b.name, 'ru'))
  }, [groups.data, rows])

  const studentColumns: DataColumn<StudentListItem>[] = [
    {
      key: 'student',
      title: 'Студент',
      width: '24%',
      render: (row) => (
        <div>
          <strong>{row.displayName}</strong>
          <span className="table-secondary">{row.email}</span>
          <small className="table-secondary">{row.lastActivityAt ? `Активность: ${dateTime(row.lastActivityAt)}` : 'Нет учебной активности'}</small>
        </div>
      ),
      sortValue: (row) => row.displayName,
    },
    {
      key: 'group',
      title: 'Группа',
      width: '18%',
      render: (row) => row.groupNames.length ? row.groupNames.join(', ') : <span className="muted">Не назначена</span>,
      sortValue: (row) => row.groupNames.join(', '),
    },
    { key: 'diagnostics', title: 'Диагностик', render: (row) => row.completedDiagnostics, sortValue: (row) => row.completedDiagnostics, align: 'center' },
    { key: 'answers', title: 'Ответов', render: (row) => row.answeredQuestions || '-', sortValue: (row) => row.answeredQuestions, align: 'center' },
    { key: 'wrong', title: 'Неверно', render: (row) => row.answeredQuestions ? row.wrongAnswers : '-', sortValue: (row) => row.wrongAnswers, align: 'center' },
    { key: 'accuracy', title: 'Правильно', render: (row) => row.answeredQuestions ? percent(row.diagnosticAccuracy) : '-', sortValue: (row) => row.diagnosticAccuracy, align: 'right' },
    { key: 'mastery', title: 'Освоение', render: (row) => row.answeredQuestions ? percent(row.overallMastery) : '-', sortValue: (row) => row.overallMastery, align: 'right' },
  ]

  const groupColumns: DataColumn<GroupSummary>[] = [
    { key: 'group', title: 'Группа', width: '24%', render: (row) => <strong>{row.name}</strong>, sortValue: (row) => row.name },
    { key: 'students', title: 'Студентов', render: (row) => row.students, sortValue: (row) => row.students, align: 'center' },
    { key: 'with-results', title: 'С результатами', render: (row) => row.studentsWithResults, sortValue: (row) => row.studentsWithResults, align: 'center' },
    { key: 'diagnostics', title: 'Диагностик', render: (row) => row.completedDiagnostics, sortValue: (row) => row.completedDiagnostics, align: 'center' },
    { key: 'answers', title: 'Ответов', render: (row) => row.answeredQuestions || '-', sortValue: (row) => row.answeredQuestions, align: 'center' },
    { key: 'wrong', title: 'Неверно', render: (row) => row.answeredQuestions ? row.wrongAnswers : '-', sortValue: (row) => row.wrongAnswers, align: 'center' },
    { key: 'accuracy', title: 'Правильно', render: (row) => row.answeredQuestions ? percent(row.accuracy) : '-', sortValue: (row) => row.accuracy, align: 'right' },
    { key: 'mastery', title: 'Освоение', render: (row) => row.studentsWithResults ? percent(row.mastery) : '-', sortValue: (row) => row.mastery, align: 'right' },
  ]

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
      <PageHeader title="Студенты и группы" />

      <KpiStrip items={[
        { label: 'Студентов с результатами', value: activeRows.length },
        { label: 'Групп', value: groupRows.length },
        { label: 'Завершённых диагностик', value: completedDiagnostics },
        { label: 'Неверных ответов', value: totalWrong, tone: totalWrong ? 'warning' : 'positive' },
        { label: 'Средняя доля правильных', value: totalAnswers ? percent(meanAccuracy) : '-' },
      ]} />

      <section className="panel filter-panel">
        <div className="filters">
          <label>Показать
            <select value={viewMode} onChange={(event) => setViewMode(event.target.value as 'students' | 'groups')}>
              <option value="students">Студенты</option>
              <option value="groups">Группы</option>
            </select>
          </label>
          {viewMode === 'students' && (
            <label>Группа
              <select value={groupFilter} onChange={(event) => setGroupFilter(event.target.value)}>
                <option value="all">Все группы</option>
                {(groups.data ?? []).map((group) => <option key={group.id} value={group.name}>{group.name}</option>)}
              </select>
            </label>
          )}
          {viewMode === 'students' && inactiveRows.length > 0 && (
            <label className="inline-check filter-inline-check">
              <input type="checkbox" checked={showWithoutResults} onChange={(event) => setShowWithoutResults(event.target.checked)} />
              <span>Показывать без результатов</span>
            </label>
          )}
        </div>
      </section>

      <section className="panel">
        <div className="panel-title"><h2>{viewMode === 'students' ? 'Результаты по студентам' : 'Результаты по группам'}</h2></div>
        {viewMode === 'students' ? (
          <DataTable
            rows={showWithoutResults ? groupFilteredRows : activeRows}
            columns={studentColumns}
            rowKey={(row) => row.userId}
            searchText={(row) => `${row.displayName} ${row.email} ${row.groupNames.join(' ')} ${row.activeMisconceptionTitles.join(' ')}`}
            searchPlaceholder="Студент, группа или затруднение"
            pageSize={14}
            onRowClick={(row) => navigate(`/teacher/students/${row.userId}`)}
            emptyText="По выбранным условиям студентов нет"
          />
        ) : (
          <DataTable
            rows={groupRows}
            columns={groupColumns}
            rowKey={(row) => row.id}
            searchText={(row) => row.name}
            searchPlaceholder="Группа"
            pageSize={14}
            emptyText="Группы пока не созданы"
          />
        )}
      </section>

      {viewMode === 'students' && activeRows.length >= 2 && (
        <div className="two-column teacher-result-charts">
          <BarChartPanel
            title="Доля правильных ответов"
            data={accuracyChart}
            xKey="name"
            series={[{ key: 'value', label: 'Правильных ответов' }]}
            percent
            horizontal
            height={330}
          />
          <BarChartPanel
            title="Количество неверных ответов"
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
