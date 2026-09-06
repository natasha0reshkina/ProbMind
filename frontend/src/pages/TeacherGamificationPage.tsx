import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { PageHeader } from '../components/PageHeader'
import type { Leaderboard, TeacherGamificationStudent } from '../types/api'
import { dateTime } from '../utils/format'

const categories = [
  ['overall', 'Общий результат'],
  ['mastery', 'Освоение тем'],
  ['diagnostics', 'Диагностика'],
  ['practice', 'Практика'],
  ['streak', 'Стрик'],
] as const

export function TeacherGamificationPage() {
  const [category, setCategory] = useState('overall')
  const [search, setSearch] = useState('')
  const queryClient = useQueryClient()

  const leaderboard = useQuery({
    queryKey: ['teacher-leaderboard', category],
    queryFn: async () => (await api.get<Leaderboard>('/gamification/teacher/leaderboard', { params: { category } })).data,
  })
  const students = useQuery({
    queryKey: ['teacher-gamification-students'],
    queryFn: async () => (await api.get<TeacherGamificationStudent[]>('/gamification/teacher/students')).data,
  })
  const toggle = useMutation({
    mutationFn: async (enabled: boolean) => (await api.put<{ enabled: boolean }>('/gamification/teacher/enabled', { enabled })).data,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['teacher-leaderboard'] })
    },
  })

  const enabled = leaderboard.data?.leaderboardEnabled ?? true
  const filteredStudents = (students.data ?? []).filter((student) => {
    const q = search.trim().toLowerCase()
    return !q || `${student.displayName} ${student.email}`.toLowerCase().includes(q)
  })

  return (
    <div>
      <PageHeader
        eyebrow="Преподаватель"
        title="Рейтинг и стрики"
        description="Управление рейтингом группы и обзор регулярности учебной активности студентов."
      />

      <section className="plain-section leaderboard-settings">
        <div>
          <h2>Рейтинг студентов</h2>
          <p className="muted">В рейтинг попадают только студенты, которые сами согласились участвовать.</p>
        </div>
        <label className="teacher-leaderboard-toggle">
          <input
            type="checkbox"
            checked={enabled}
            disabled={toggle.isPending}
            onChange={(event) => toggle.mutate(event.target.checked)}
          />
          <span><strong>{enabled ? 'Рейтинг включён' : 'Рейтинг отключён'}</strong><small>Преподаватель всё равно видит текущую таблицу и стрики.</small></span>
        </label>
      </section>

      <section className="panel leaderboard-panel">
        <div className="panel-title leaderboard-heading">
          <div>
            <h2>Текущий рейтинг</h2>
            <p className="muted">Участников: {leaderboard.data?.participants ?? 0}</p>
          </div>
          <label className="compact-select">Категория
            <select value={category} onChange={(event) => setCategory(event.target.value)}>
              {categories.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
        </div>
        <div className="leaderboard-list">
          {(leaderboard.data?.entries ?? []).map((entry) => (
            <div className="leaderboard-row" key={entry.userId}>
              <span className="leaderboard-rank">{entry.rank}</span>
              <div className="leaderboard-person"><strong>{entry.displayName}</strong><span>{entry.detail}</span></div>
              <strong className="leaderboard-score">{category === 'overall' ? Math.round(entry.score) : category === 'streak' ? `${Math.round(entry.score)} дн.` : `${entry.score.toFixed(0)}%`}</strong>
            </div>
          ))}
          {leaderboard.data && leaderboard.data.entries.length === 0 ? <div className="empty-inline">Пока ни один студент не включил участие в рейтинге.</div> : null}
        </div>
      </section>

      <section className="panel streak-roster-panel">
        <div className="panel-title streak-roster-heading">
          <div>
            <h2>Стрики студентов</h2>
            <p className="muted">Стрик считается по дням с учебной активностью в ProbMind.</p>
          </div>
          <input className="table-search compact-search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Найти студента" />
        </div>
        <div className="table-scroll">
          <table className="simple-table streak-table">
            <thead><tr><th>Студент</th><th>Текущий стрик</th><th>Лучший</th><th>Активных дней</th><th>Рейтинг</th><th>Последняя активность</th></tr></thead>
            <tbody>
              {filteredStudents.map((student) => (
                <tr key={student.userId}>
                  <td><strong>{student.displayName}</strong><span className="table-secondary">{student.email}</span></td>
                  <td><strong>{student.currentStreak} дн.</strong></td>
                  <td>{student.longestStreak} дн.</td>
                  <td>{student.activeDays}</td>
                  <td>{student.participatesInLeaderboard ? 'Участвует' : 'Не участвует'}</td>
                  <td>{dateTime(student.lastActivityAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {filteredStudents.length === 0 ? <div className="empty-inline">Студенты не найдены.</div> : null}
        </div>
      </section>
    </div>
  )
}
