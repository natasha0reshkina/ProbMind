import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { api } from '../api/client'
import type { NotificationItem } from '../types/api'

const roleLabels = { Student: 'Студент', Teacher: 'Преподаватель', Admin: 'Администратор' } as const

type NavItem = readonly [string, string]
type NavSection = { title: string; items: NavItem[] }

const studentSections: NavSection[] = [
  {
    title: 'Профиль',
    items: [
      ['/profile', 'Личный кабинет'],
    ],
  },
  {
    title: 'Обучение',
    items: [
      ['/diagnostic', 'Диагностика'],
      ['/practice', 'Практика'],
      ['/repetition', 'Интервальное повторение'],
      ['/exams', 'Экзамены'],
      ['/material-cycles', 'До и после материала'],
    ],
  },
  {
    title: 'Прогресс',
    items: [
      ['/diagnostics/history', 'История диагностик'],
      ['/confidence', 'Уверенность в ответах'],
      ['/misconceptions', 'Типичные ошибки'],
      ['/learning-path', 'План повторения'],
      ['/statistics', 'Статистика'],
    ],
  },
  {
    title: 'Мотивация',
    items: [
      ['/achievements', 'Достижения'],
    ],
  },
  {
    title: 'Связь с преподавателем',
    items: [
      ['/interventions', 'Индивидуальное задание'],
      ['/materials', 'Материалы и задачи'],
    ],
  },
]

const teacherSections: NavSection[] = [
  {
    title: 'Группа',
    items: [
      ['/teacher/analytics', 'Сводка по группе'],
      ['/teacher/students', 'Студенты'],
      ['/teacher/groups', 'Группы студентов'],
      ['/teacher/interventions', 'Индивидуальное задание'],
    ],
  },
  {
    title: 'Учебный процесс',
    items: [
      ['/teacher/exams', 'Экзамены'],
      ['/teacher/material-cycles', 'До и после материала'],
      ['/teacher/diagnostics', 'Конструктор диагностик'],
      ['/teacher/materials', 'Материалы и задачи'],
    ],
  },
  {
    title: 'Аналитика',
    items: [
      ['/teacher/confidence', 'Уверенность студентов'],
      ['/teacher/questions/analytics', 'Статистика заданий'],
      ['/teacher/gamification', 'Рейтинг и стрики'],
    ],
  },
  {
    title: 'Контент и данные',
    items: [
      ['/teacher/content', 'Банк заданий'],
      ['/teacher/exports', 'Выгрузка данных'],
    ],
  },
]

const adminSections: NavSection[] = [
  {
    title: 'Администрирование',
    items: [
      ['/admin/users', 'Пользователи'],
      ['/admin/audit', 'Журнал изменений'],
      ['/admin/operations', 'Состояние системы'],
    ],
  },
  {
    title: 'Группа',
    items: [
      ['/teacher/analytics', 'Сводка по группе'],
      ['/teacher/students', 'Результаты студентов'],
      ['/teacher/groups', 'Группы студентов'],
      ['/teacher/interventions', 'Индивидуальное задание'],
    ],
  },
  {
    title: 'Учебный процесс',
    items: [
      ['/teacher/exams', 'Экзамены'],
      ['/teacher/material-cycles', 'До и после материала'],
      ['/teacher/diagnostics', 'Конструктор диагностик'],
      ['/teacher/materials', 'Материалы и задачи'],
    ],
  },
  {
    title: 'Аналитика',
    items: [
      ['/teacher/confidence', 'Уверенность студентов'],
      ['/teacher/questions/analytics', 'Статистика заданий'],
      ['/teacher/gamification', 'Рейтинг и стрики'],
      ['/teacher/research', 'Расширенная статистика'],
    ],
  },
  {
    title: 'Контент и данные',
    items: [
      ['/teacher/content', 'Банк заданий'],
      ['/teacher/exports', 'Выгрузка данных'],
    ],
  },
]

export function AppShell() {
  const { user, logout } = useAuth()
  const [collapsed, setCollapsed] = useState(() => window.localStorage.getItem('probmind-sidebar-collapsed') === '1')
  const sections = user?.role === 'Admin' ? adminSections : user?.role === 'Teacher' ? teacherSections : studentSections
  const unreadQuery = useQuery({
    queryKey: ['notifications-nav', user?.id],
    enabled: Boolean(user),
    queryFn: async () => (await api.get<NotificationItem[]>('/notifications', { params: { unreadOnly: true } })).data,
    refetchInterval: 30000,
  })
  const unreadCount = unreadQuery.data?.length ?? 0

  function toggleSidebar() {
    setCollapsed((current) => {
      const next = !current
      window.localStorage.setItem('probmind-sidebar-collapsed', next ? '1' : '0')
      return next
    })
  }

  return (
    <div className={`app-shell${collapsed ? ' app-shell--collapsed' : ''}`}>
      <aside className={`sidebar${collapsed ? ' sidebar--collapsed' : ''}`}>
        <button className="sidebar-toggle" type="button" onClick={toggleSidebar} aria-label={collapsed ? 'Открыть меню' : 'Скрыть меню'}>
          <span aria-hidden="true">{collapsed ? '›' : '‹'}</span>
        </button>

        {!collapsed ? (
          <>
            <div className="brand">
              <strong>ProbMind</strong>
            </div>

            <nav className="nav-list" aria-label="Основная навигация">
              {sections.map((section) => (
                <div className="nav-section" key={section.title}>
                  <div className="nav-section__title">{section.title}</div>
                  {section.items.map(([to, label]) => (
                    <NavLink key={to} to={to} end={to === '/'} className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>
                      {label}
                    </NavLink>
                  ))}
                </div>
              ))}
            </nav>

            <div className="sidebar__bottom">
              <div className="nav-section__title nav-section__title--account">Аккаунт</div>
              <div className="account-links">
                <NavLink to="/notifications" className={({ isActive }) => isActive ? 'nav-item active nav-item--with-badge' : 'nav-item nav-item--with-badge'}>
                  <span>Уведомления</span>
                  {unreadCount > 0 ? <span className="nav-unread-badge" aria-label={`Непрочитанных: ${unreadCount}`}>{unreadCount > 99 ? '99+' : unreadCount}</span> : null}
                </NavLink>
                {user?.role !== 'Student' ? <NavLink to="/profile" className={({ isActive }) => isActive ? 'nav-item active' : 'nav-item'}>Профиль</NavLink> : null}
                <button className="nav-item nav-button" onClick={() => void logout()}>Выйти</button>
              </div>
              <div className="user-block">
                <strong>{user?.displayName}</strong>
                <span>{user ? roleLabels[user.role] : ''}</span>
              </div>
            </div>
          </>
        ) : null}
      </aside>
      <main className="main-content">
        <Outlet />
      </main>
    </div>
  )
}
