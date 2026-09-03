import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Shield, UserCheck, UserCog, UserX } from 'lucide-react'
import { api } from '../api/client'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import type { AdminUser, UserRole } from '../types/api'
import { dateTime } from '../utils/format'

export function AdminUsersPage() {
  const client = useQueryClient()
  const [roleFilter, setRoleFilter] = useState('all')
  const [activeFilter, setActiveFilter] = useState('all')

  const users = useQuery({
    queryKey: ['admin-users'],
    queryFn: async () => (await api.get<AdminUser[]>('/admin/users')).data,
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
    return true
  }), [users.data, roleFilter, activeFilter])

  const all = users.data ?? []
  const counts = {
    active: all.filter((user) => user.isActive).length,
    inactive: all.filter((user) => !user.isActive).length,
    teachers: all.filter((user) => user.role === 'Teacher').length,
    admins: all.filter((user) => user.role === 'Admin').length,
  }

  const columns: DataColumn<AdminUser>[] = [
    {
      key: 'user', title: 'Пользователь', width: '27%',
      render: (row) => <div><strong>{row.displayName}</strong><span className="table-secondary">{row.email}</span></div>,
      sortValue: (row) => row.displayName,
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
    { key: 'created', title: 'Создан', render: (row) => dateTime(row.createdAt), sortValue: (row) => new Date(row.createdAt).getTime() },
    {
      key: 'action', title: '', align: 'right', render: (row) => (
        <button className={row.isActive ? 'ghost-button small danger-text' : 'secondary-button small'} onClick={(event) => { event.stopPropagation(); setActive.mutate({ userId: row.id, isActive: !row.isActive }) }}>
          {row.isActive ? <><UserX size={14} /> Деактивировать</> : <><UserCheck size={14} /> Активировать</>}
        </button>
      ),
    },
  ]

  return (
    <div>
      <PageHeader
        eyebrow="Администрирование"
        title="Управление пользователями"
        description="Администратор управляет ролями и состоянием аккаунтов. Изменения сохраняются в журнале действий."
      />
      <KpiStrip items={[
        { label: 'Всего аккаунтов', value: all.length },
        { label: 'Активных', value: counts.active, tone: 'positive' },
        { label: 'Отключённых', value: counts.inactive, tone: counts.inactive ? 'warning' : 'default' },
        { label: 'Преподавателей', value: counts.teachers },
        { label: 'Администраторов', value: counts.admins },
      ]} />

      <section className="panel filter-panel">
        <div className="filter-panel__title"><UserCog size={18} /><strong>Фильтры доступа</strong></div>
        <div className="filters">
          <label>Роль
            <select value={roleFilter} onChange={(event) => setRoleFilter(event.target.value)}><option value="all">Все роли</option><option value="Student">Студент</option><option value="Teacher">Преподаватель</option><option value="Admin">Администратор</option></select>
          </label>
          <label>Состояние
            <select value={activeFilter} onChange={(event) => setActiveFilter(event.target.value)}><option value="all">Все</option><option value="active">Активные</option><option value="inactive">Отключённые</option></select>
          </label>
          <button className="ghost-button" onClick={() => { setRoleFilter('all'); setActiveFilter('all') }}>Сбросить</button>
        </div>
      </section>

      <section className="panel">
        <div className="panel-title"><div><h2>Пользователи</h2><p className="muted chart-subtitle">Поиск, сортировка, смена роли и отключение аккаунтов.</p></div><Shield size={20} /></div>
        <DataTable rows={rows} columns={columns} rowKey={(row) => row.id} searchText={(row) => `${row.displayName} ${row.email} ${row.role} ${row.isActive ? 'active' : 'inactive'}`} pageSize={16} />
      </section>

    </div>
  )
}
