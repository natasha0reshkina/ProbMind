import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Bell, BellRing, CheckCheck, Filter } from 'lucide-react'
import { api } from '../api/client'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import { statusLabel } from '../components/StatusBadge'
import type { NotificationItem } from '../types/api'
import { dateTime } from '../utils/format'
import { useState } from 'react'

export function NotificationCenterPage() {
  const [unreadOnly, setUnreadOnly] = useState(false)
  const client = useQueryClient()
  const notifications = useQuery({
    queryKey: ['notifications', unreadOnly],
    queryFn: async () => (await api.get<NotificationItem[]>('/notifications', { params: { unreadOnly } })).data,
  })
  const markRead = useMutation({
    mutationFn: async (id: string) => api.post(`/notifications/${id}/read`),
    onSuccess: () => client.invalidateQueries({ queryKey: ['notifications'] }),
  })
  const markAll = useMutation({
    mutationFn: async () => api.post('/notifications/read-all'),
    onSuccess: () => client.invalidateQueries({ queryKey: ['notifications'] }),
  })

  const rows = notifications.data ?? []
  const unread = rows.filter((item) => !item.isRead).length
  const types = new Set(rows.map((item) => item.type)).size

  return (
    <div>
      <PageHeader
        eyebrow="Уведомления"
        title="Центр уведомлений"
        description="Здесь собраны сообщения о диагностике, изменении учебной траектории, коррекции ошибок и административных событиях."
        actions={<button className="secondary-button" disabled={!unread} onClick={() => markAll.mutate()}><CheckCheck size={16} /> Прочитать всё</button>}
      />
      <KpiStrip items={[
        { label: 'Уведомлений', value: rows.length },
        { label: 'Непрочитанных', value: unread, tone: unread ? 'warning' : 'positive' },
        { label: 'Типов событий', value: types },
      ]} />

      <section className="panel filter-panel">
        <div className="filter-panel__title"><Filter size={18} /><strong>Фильтр</strong></div>
        <label className="checkbox-label"><input type="checkbox" checked={unreadOnly} onChange={(event) => setUnreadOnly(event.target.checked)} /> Только непрочитанные</label>
      </section>

      <section className="notification-list">
        {rows.map((item) => (
          <article className={`notification-card ${item.isRead ? '' : 'notification-card--unread'}`} key={item.id}>
            <div className="notification-card__icon">{item.isRead ? <Bell size={18} /> : <BellRing size={18} />}</div>
            <div className="notification-card__content">
              <div className="notification-card__top"><div><span className="soft-label">{statusLabel(item.type)}</span><h2>{item.title}</h2></div><time>{dateTime(item.createdAt)}</time></div>
              <p>{item.body}</p>
              {!item.isRead && <button className="ghost-button small" onClick={() => markRead.mutate(item.id)}>Отметить прочитанным</button>}
            </div>
          </article>
        ))}
        {!rows.length && <div className="empty-state">Уведомлений по выбранному фильтру нет.</div>}
      </section>
    </div>
  )
}
