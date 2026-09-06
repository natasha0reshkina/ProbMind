import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { useAuth } from '../auth/AuthContext'
import { PageHeader } from '../components/PageHeader'
import type { Achievement } from '../types/api'

export function AchievementsPage() {
  const { user } = useAuth()
  const query = useQuery({
    queryKey: ['achievements', user?.id],
    queryFn: async () => (await api.get<Achievement[]>('/edtech/achievements')).data,
    enabled: Boolean(user?.id),
    staleTime: 0,
    gcTime: 0,
    refetchOnMount: 'always',
  })
  if (query.isLoading) return <LoadingView />
  const items = query.data ?? []

  return (
    <div>
      <PageHeader
        eyebrow="Учебные достижения"
        title="Достижения"
        description="Это личные достижения текущего аккаунта. Прогресс считается только по вашим ответам, практике, освоению тем и учебной активности; данные других студентов здесь не используются."
      />
      <div className="achievement-grid">
        {items.map((item) => (
          <article className={`achievement-card ${item.unlocked ? 'achievement-card--unlocked' : ''}`} key={item.code}>
            <span className="achievement-state">{item.unlocked ? 'Ура! Получено' : `${item.progress} / ${item.target}`}</span>
            <h3>{item.title}</h3>
            <p>{item.description}</p>
            <div className="achievement-progress"><i style={{ width: `${item.unlocked ? 100 : Math.min(100, item.progress / item.target * 100)}%` }} /></div>
          </article>
        ))}
      </div>
    </div>
  )
}
