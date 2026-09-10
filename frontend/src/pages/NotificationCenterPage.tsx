import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCheck } from 'lucide-react'
import { api } from '../api/client'
import { PageHeader } from '../components/PageHeader'
import type { NotificationItem } from '../types/api'
import { dateTime } from '../utils/format'
import { useAuth } from '../auth/AuthContext'

export function NotificationCenterPage() {
  const { user } = useAuth()
  const [unreadOnly, setUnreadOnly] = useState(false)
  const client = useQueryClient()
  const notifications = useQuery({
    queryKey: ['notifications', user?.id, unreadOnly],
    queryFn: async () => (await api.get<NotificationItem[]>('/notifications', { params: { unreadOnly } })).data,
  })
  const markRead = useMutation({
    mutationFn: async (id: string) => api.post(`/notifications/${id}/read`),
    onSuccess: async () => {
      await Promise.all([
        client.invalidateQueries({ queryKey: ['notifications'] }),
        client.invalidateQueries({ queryKey: ['notifications-nav', user?.id] }),
      ])
    },
  })
  const markAll = useMutation({
    mutationFn: async () => api.post('/notifications/read-all'),
    onSuccess: async () => {
      await Promise.all([
        client.invalidateQueries({ queryKey: ['notifications'] }),
        client.invalidateQueries({ queryKey: ['notifications-nav', user?.id] }),
      ])
    },
  })

  const rows = notifications.data ?? []
  const unread = rows.filter((item) => !item.isRead).length

  return (
    <div>
      <PageHeader
        eyebrow="Уведомления"
        title="Уведомления"
        actions={<button className="secondary-button" disabled={!unread} onClick={() => markAll.mutate()}><CheckCheck size={16} /> Прочитать всё</button>}
      />

      <p className="notification-summary">Непрочитанных: <strong>{unread}</strong></p>
      <label className="checkbox-label"><input type="checkbox" checked={unreadOnly} onChange={(event) => setUnreadOnly(event.target.checked)} /> Только непрочитанные</label>

      <section className="notification-list" style={{ marginTop: 16 }}>
        {rows.map((item) => (
          <article className={`notification-card ${item.isRead ? 'notification-card--read' : 'notification-card--unread'}`} key={item.id}>
            <div className="notification-card__content">
              <div className="notification-card__top">
                <div>
                  {!item.isRead ? <div className="notification-unread-label">Непрочитано</div> : null}
                  <h2>{item.title}</h2>
                </div>
                <time>{dateTime(item.createdAt)}</time>
              </div>
              <p>{item.body}</p>
              {!item.isRead ? <button className="ghost-button small" onClick={() => markRead.mutate(item.id)}>Отметить прочитанным</button> : null}
            </div>
          </article>
        ))}
        {!rows.length ? <div className="empty-state">Уведомлений по выбранному фильтру нет.</div> : null}
      </section>
    </div>
  )
}
