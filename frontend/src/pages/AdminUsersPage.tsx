import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { UserCheck, UserX } from 'lucide-react'
import { api } from '../api/client'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import type { AdminUser, StudentGroup, StudentListItem, UserRole } from '../types/api'
import { dateTime, percent } from '../utils/format'

type AdminGroupSummary = {
  id: string
  name: string
  description: string
  students: number
  activeStudents: number
  members: string
  completedDiagnostics: number
  answeredQuestions: number
  accuracy: number
  mastery: number
}

export function AdminUsersPage() {
  const client = useQueryClient()
  const [viewMode, setViewMode] = useState<'users' | 'groups'>('users')
  const [roleFilter, setRoleFilter] = useState('all')
  const [activeFilter, setActiveFilter] = useState('all')
  const [groupFilter, setGroupFilter] = useState('all')

  const users = useQuery({
    queryKey: ['admin-users'],
    queryFn: async () => (await api.get<AdminUser[]>('/admin/users')).data,
  })

  const groups = useQuery({
    queryKey: ['student-groups'],
    queryFn: async () => (await api.get<StudentGroup[]>('/edtech/groups')).data,
  })

  const studentResults = useQuery({
    queryKey: ['teacher-students'],
    queryFn: async () => (await api.get<StudentListItem[]>('/teacher/students')).data,
  })

  const setRole = useMutation({
    mutationFn: async ({ userId, role }: { userId: string; role: UserRole }) =>
      (await api.post<AdminUser>('/admin/users/role', { userId, role })).data,
    onSuccess: () => client.invalidateQueries({ queryKey: ['admin-users'] }),
  })

  const setActive = useMutation({
    mutationFn: async ({ userId, isActive }: { userId: string; isActive: boolean }) =>
      (await api.post<AdminUser>('/admin/users/active', { userId, isActive })).data,
    onSuccess: () => client.invalidateQueries({ queryKey: ['admin-users'] }),
  })

  const rows = useMemo(() => (users.data ?? []).filter((user) => {
    if (roleFilter !== 'all' && user.role !== roleFilter) return false
    if (activeFilter === 'active' && !user.isActive) return false
    if (activeFilter === 'inactive' && user.isActive) return false
    if (groupFilter !== 'all' && !user.groupNames.includes(groupFilter)) return false
    return true
  }), [users.data, roleFilter, activeFilter, groupFilter])

  const all = users.data ?? []
  const activeStudentIds = new Set(all.filter((user) => user.role === 'Student' && user.isActive).map((user) => user.id))
  const resultById = new Map((studentResults.data ?? []).map((student) => [student.userId, student]))
  const groupRows = useMemo<AdminGroupSummary[]>(() => (groups.data ?? []).map((group) => {
    const results = group.members.map((member) => resultById.get(member.studentId)).filter((item): item is StudentListItem => Boolean(item))
    const answers = results.reduce((sum, item) => sum + item.answeredQuestions, 0)
    const observed = results.filter((item) => item.answeredQuestions > 0)
    return {
      id: group.id,
      name: group.name,
      description: group.description,
      students: group.members.length,
      activeStudents: group.members.filter((member) => activeStudentIds.has(member.studentId)).length,
      members: group.members.map((member) => member.displayName).join(', '),
      completedDiagnostics: results.reduce((sum, item) => sum + item.completedDiagnostics, 0),
      answeredQuestions: answers,
      accuracy: answers === 0 ? 0 : results.reduce((sum, item) => sum + item.diagnosticAccuracy * item.answeredQuestions, 0) / answers,
      mastery: observed.length === 0 ? 0 : observed.reduce((sum, item) => sum + item.overallMastery, 0) / observed.length,
    }
  }).sort((a, b) => a.name.localeCompare(b.name, 'ru')), [groups.data, all, studentResults.data])

  const counts = {
    active: all.filter((user) => user.isActive).length,
    inactive: all.filter((user) => !user.isActive).length,
    teachers: all.filter((user) => user.role === 'Teacher').length,
    admins: all.filter((user) => user.role === 'Admin').length,
  }

  const columns: DataColumn<AdminUser>[] = [
    {
      key: 'user', title: 'Пользователь', width: '24%',
      render: (row) => <div><strong>{row.displayName}</strong><span className="table-secondary">{row.email}</span></div>,
      sortValue: (row) => row.displayName,
    },
    {
      key: 'group', title: 'Группа', width: '17%',
      render: (row) => row.role === 'Student'
        ? row.groupNames.length ? row.groupNames.join(', ') : <span className="muted">Не назначена</span>
        : <span className="muted">Не применяется</span>,
      sortValue: (row) => row.groupNames.join(', '),
    },
    {
      key: 'role', title: 'Роль', render: (row) => (
        <select className="compact-select" value={row.role} onClick={(event) => event.stopPropagation()} onChange={(event) => setRole.mutate({ userId: row.id, role: event.target.value as UserRole })}>
          <option value="Student">Студент</option><option value="Teacher">Преподаватель</option><option value="Admin">Администратор</option>
        </select>
      ), sortValue: (row) => row.role,
    },
    { key: 'state', title: 'Состояние', render: (row) => <StatusBadge value={row.isActive ? 'Active' : 'Inactive'} />, sortValue: (row) => row.isActive ? 1 : 0 },
    { key: 'login', title: 'Последний вход', render: (row) => dateTime(row.lastLoginAt), sortValue: (row) => row.lastLoginAt ? new Date(row.lastLoginAt).getTime() : 0 },
    {
      key: 'action', title: '', align: 'right', render: (row) => (
        <button className={row.isActive ? 'ghost-button small danger-text' : 'secondary-button small'} onClick={(event) => { event.stopPropagation(); setActive.mutate({ userId: row.id, isActive: !row.isActive }) }}>
          {row.isActive ? <><UserX size={14} /> Деактивировать</> : <><UserCheck size={14} /> Активировать</>}
        </button>
      ),
    },
  ]

  const groupColumns: DataColumn<AdminGroupSummary>[] = [
    { key: 'name', title: 'Группа', width: '18%', render: (row) => <strong>{row.name}</strong>, sortValue: (row) => row.name },
    { key: 'students', title: 'Студентов', render: (row) => row.students, sortValue: (row) => row.students, align: 'center' },
    { key: 'active', title: 'Активных', render: (row) => row.activeStudents, sortValue: (row) => row.activeStudents, align: 'center' },
    { key: 'diagnostics', title: 'Диагностик', render: (row) => row.completedDiagnostics, sortValue: (row) => row.completedDiagnostics, align: 'center' },
    { key: 'accuracy', title: 'Правильно', render: (row) => row.answeredQuestions ? percent(row.accuracy) : '-', sortValue: (row) => row.accuracy, align: 'right' },
    { key: 'mastery', title: 'Освоение', render: (row) => row.answeredQuestions ? percent(row.mastery) : '-', sortValue: (row) => row.mastery, align: 'right' },
    { key: 'members', title: 'Состав', width: '30%', render: (row) => row.members || <span className="muted">Нет студентов</span>, sortValue: (row) => row.members },
  ]

  return (
    <div>
      <PageHeader title="Пользователи и группы" />
      <KpiStrip items={[
        { label: 'Всего аккаунтов', value: all.length },
        { label: 'Активных', value: counts.active, tone: 'positive' },
        { label: 'Групп', value: groupRows.length },
        { label: 'Преподавателей', value: counts.teachers },
        { label: 'Администраторов', value: counts.admins },
      ]} />

      <section className="panel filter-panel">
        <div className="filters">
          <label>Показать
            <select value={viewMode} onChange={(event) => setViewMode(event.target.value as 'users' | 'groups')}>
              <option value="users">Пользователи</option>
              <option value="groups">Группы</option>
            </select>
          </label>
          {viewMode === 'users' && (
            <>
              <label>Роль
                <select value={roleFilter} onChange={(event) => setRoleFilter(event.target.value)}><option value="all">Все роли</option><option value="Student">Студент</option><option value="Teacher">Преподаватель</option><option value="Admin">Администратор</option></select>
              </label>
              <label>Состояние
                <select value={activeFilter} onChange={(event) => setActiveFilter(event.target.value)}><option value="all">Все</option><option value="active">Активные</option><option value="inactive">Отключённые</option></select>
              </label>
              <label>Группа
                <select value={groupFilter} onChange={(event) => setGroupFilter(event.target.value)}>
                  <option value="all">Все группы</option>
                  {(groups.data ?? []).map((group) => <option key={group.id} value={group.name}>{group.name}</option>)}
                </select>
              </label>
              <button className="ghost-button" onClick={() => { setRoleFilter('all'); setActiveFilter('all'); setGroupFilter('all') }}>Сбросить</button>
            </>
          )}
        </div>
      </section>

      <section className="panel">
        <div className="panel-title"><h2>{viewMode === 'users' ? 'Пользователи' : 'Группы'}</h2></div>
        {viewMode === 'users' ? (
          <DataTable
            rows={rows}
            columns={columns}
            rowKey={(row) => row.id}
            searchText={(row) => `${row.displayName} ${row.email} ${row.role} ${row.groupNames.join(' ')}`}
            searchPlaceholder="Пользователь или группа"
            pageSize={16}
          />
        ) : (
          <DataTable
            rows={groupRows}
            columns={groupColumns}
            rowKey={(row) => row.id}
            searchText={(row) => `${row.name} ${row.members} ${row.description}`}
            searchPlaceholder="Группа или студент"
            pageSize={16}
            emptyText="Группы пока не созданы"
          />
        )}
      </section>
    </div>
  )
}
