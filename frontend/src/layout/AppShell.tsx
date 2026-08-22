import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

const roleLabels = { Student: 'Студент', Teacher: 'Преподаватель', Admin: 'Администратор' } as const

type NavItem = readonly [string, string]

const studentItems: NavItem[] = [
  ['/', 'Обзор'],
  ['/diagnostic', 'Диагностика'],
  ['/diagnostics/history', 'История диагностик'],
  ['/misconceptions', 'Типичные ошибки'],
  ['/learning-path', 'План повторения'],
  ['/practice', 'Практика'],
  ['/statistics', 'Статистика'],
]

const teacherItems: NavItem[] = [
  ['/teacher/analytics', 'Сводка по группе'],
  ['/teacher/students', 'Студенты'],
  ['/teacher/benchmarks', 'Сравнение результатов'],
  ['/teacher/interventions', 'Коррекционная работа'],
  ['/teacher/questions/analytics', 'Статистика заданий'],
  ['/teacher/psychometrics', 'Показатели качества'],
  ['/teacher/research', 'Исследовательская статистика'],
  ['/teacher/content', 'Банк заданий'],
  ['/teacher/exports', 'Выгрузка данных'],
]

const adminItems: NavItem[] = [
  ['/admin/users', 'Пользователи'],
  ['/admin/audit', 'Журнал изменений'],
  ['/admin/operations', 'Состояние системы'],
  ['/teacher/analytics', 'Сводка по группе'],
  ['/teacher/students', 'Студенты'],
  ['/teacher/content', 'Банк заданий'],
  ['/teacher/research', 'Статистика'],
  ['/teacher/exports', 'Выгрузка данных'],
]

export function AppShell() {
  const { user, logout } = useAuth()
  const items = user?.role === 'Admin' ? adminItems : user?.role === 'Teacher' ? teacherItems : studentItems

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <strong>ProbMind</strong>
          <span>Учебный сервис по теории вероятностей</span>
        </div>

        <nav className="nav-list" aria-label="Основная навигация">
          {items.map(([to, label]) => (
            <NavLink key={to} to={to} end={to === '/'} className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>
              {label}
            </NavLink>
          ))}
        </nav>

        <div className="sidebar__bottom">
          <div className="account-links">
            <NavLink to="/notifications" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>Уведомления</NavLink>
            <NavLink to="/profile" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>Профиль</NavLink>
            <button className="nav-item nav-button" onClick={() => void logout()}>Выйти</button>
          </div>
          <div className="user-block">
            <strong>{user?.displayName}</strong>
            <span>{user ? roleLabels[user.role] : ''}</span>
          </div>
        </div>
      </aside>
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  )
}
