import { FormEvent, useState } from 'react'
import { api } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { PageHeader } from '../components/PageHeader'

const roleLabels = { Student: 'Студент', Teacher: 'Преподаватель', Admin: 'Администратор' } as const

export function ProfilePage() {
  const { user, refreshProfile } = useAuth()
  const [name, setName] = useState(user?.displayName ?? '')
  const [saved, setSaved] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    await api.patch('/auth/me', { displayName: name })
    await refreshProfile()
    setSaved(true)
    window.setTimeout(() => setSaved(false), 1500)
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
