import { FormEvent, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { PageHeader } from '../components/PageHeader'
import type { GamificationProfile, Leaderboard } from '../types/api'
import { DashboardPage } from './DashboardPage'

const roleLabels = { Student: 'Студент', Teacher: 'Преподаватель', Admin: 'Администратор' } as const

const leaderboardCategories = [
  ['overall', 'Общий результат'],
  ['mastery', 'Освоение тем'],
  ['diagnostics', 'Диагностика'],
  ['practice', 'Практика'],
  ['streak', 'Стрик'],
] as const

export function ProfilePage() {
  const { user, refreshProfile } = useAuth()
  const [name, setName] = useState(user?.displayName ?? '')
  const [saved, setSaved] = useState(false)
  const [category, setCategory] = useState('overall')
  const queryClient = useQueryClient()

  const gamification = useQuery({
    queryKey: ['gamification-profile', user?.id],
    queryFn: async () => (await api.get<GamificationProfile>('/gamification/profile')).data,
    enabled: user?.role === 'Student',
  })

  const leaderboard = useQuery({
    queryKey: ['leaderboard', user?.id, category],
    queryFn: async () => (await api.get<Leaderboard>('/gamification/leaderboard', { params: { category } })).data,
    enabled: user?.role === 'Student' && Boolean(gamification.data?.leaderboardEnabled),
  })

  const participation = useMutation({
    mutationFn: async (participate: boolean) => (await api.put<GamificationProfile>('/gamification/participation', { participate })).data,
    onSuccess: async (data) => {
      queryClient.setQueryData(['gamification-profile', user?.id], data)
      await queryClient.invalidateQueries({ queryKey: ['leaderboard'] })
    },
  })

  async function submit(event: FormEvent) {
    event.preventDefault()
    await api.patch('/auth/me', { displayName: name })
    await refreshProfile()
    setSaved(true)
    window.setTimeout(() => setSaved(false), 1500)
  }

  if (user?.role === 'Student') {
    const game = gamification.data
    return (
      <div>
        <PageHeader title="Личный кабинет" description="Аккаунт, прогресс, учебная активность и рекомендации в одном месте." />

        <div className="cabinet-top-grid">
          <section className="plain-section cabinet-account-section">
            <h2>Аккаунт</h2>
            <form className="profile-form cabinet-profile-form" onSubmit={submit}>
              <label>Имя<input value={name} onChange={(event) => setName(event.target.value)} /></label>
              <label>E-mail<input value={user.email} disabled /></label>
              <label>Роль<input value={roleLabels[user.role]} disabled /></label>
              <div className="button-row"><button className="primary-button">{saved ? 'Сохранено' : 'Сохранить'}</button></div>
            </form>
          </section>

          <section className="plain-section streak-section">
            <div className="section-heading-row">
              <div>
                <h2>Учебный стрик</h2>
                <p className="muted">Дни с учебной активностью подряд.</p>
              </div>
            </div>
            <div className="streak-value">{game?.currentStreak ?? 0}</div>
            <div className="streak-label">дней подряд</div>
            <div className="streak-stats">
              <div><strong>{game?.longestStreak ?? 0}</strong><span>лучший стрик</span></div>
              <div><strong>{game?.activeDays ?? 0}</strong><span>активных дней</span></div>
              <div><strong>{game?.overallPoints ?? 0}</strong><span>баллов</span></div>
              <div><strong>{game?.participatesInLeaderboard ? game.overallRank ?? '—' : '—'}</strong><span>место в общем рейтинге</span></div>
            </div>
            <label className="leaderboard-opt-in">
              <input
                type="checkbox"
                checked={Boolean(game?.participatesInLeaderboard)}
                disabled={participation.isPending}
                onChange={(event) => participation.mutate(event.target.checked)}
              />
              <span>
                <strong>Участвовать в рейтинге</strong>
                <small>Ваше имя и результат будут видны другим участникам рейтинга.</small>
              </span>
            </label>
            {game && !game.leaderboardEnabled ? <p className="inline-notice">Рейтинг временно отключён преподавателем. Стрик продолжает считаться.</p> : null}
          </section>
        </div>

        {game?.leaderboardEnabled ? (
          <section className="panel leaderboard-panel">
            <div className="panel-title leaderboard-heading">
              <div>
                <h2>Рейтинг</h2>
                <p className="muted">{game.participatesInLeaderboard ? 'Вы участвуете в рейтинге.' : 'Вы видите рейтинг, но ваш результат в нём не публикуется.'}</p>
              </div>
              <label className="compact-select">Категория
                <select value={category} onChange={(event) => setCategory(event.target.value)}>
                  {leaderboardCategories.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
                </select>
              </label>
            </div>
            <div className="leaderboard-list">
              {(leaderboard.data?.entries ?? []).slice(0, 20).map((entry) => (
                <div className={`leaderboard-row${entry.userId === user.id ? ' leaderboard-row--me' : ''}`} key={entry.userId}>
                  <span className="leaderboard-rank">{entry.rank}</span>
                  <div className="leaderboard-person"><strong>{entry.displayName}</strong><span>{entry.detail}</span></div>
                  <strong className="leaderboard-score">{category === 'overall' ? Math.round(entry.score) : category === 'streak' ? `${Math.round(entry.score)} дн.` : `${entry.score.toFixed(0)}%`}</strong>
                </div>
              ))}
              {leaderboard.data && leaderboard.data.entries.length === 0 ? <div className="empty-inline">Пока никто не участвует в рейтинге.</div> : null}
            </div>
          </section>
        ) : null}

        <DashboardPage embedded />
      </div>
    )
  }

  return (
    <div>
      <PageHeader title="Профиль" description="Основные данные текущего аккаунта." />
      <form className="profile-form" onSubmit={submit}>
        <label>Имя<input value={name} onChange={(event) => setName(event.target.value)} /></label>
        <label>E-mail<input value={user?.email ?? ''} disabled /></label>
        <label>Роль<input value={user ? roleLabels[user.role] : ''} disabled /></label>
        <div className="button-row"><button className="primary-button">{saved ? 'Сохранено' : 'Сохранить'}</button></div>
      </form>
    </div>
  )
}
