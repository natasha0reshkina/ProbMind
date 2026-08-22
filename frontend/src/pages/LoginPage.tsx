import { FormEvent, useState } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function LoginPage() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('student@probmind.local')
  const [password, setPassword] = useState('Student123!')
  const [error, setError] = useState('')
  const [pending, setPending] = useState(false)

  if (user) return <Navigate to="/" replace />

  async function submit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError('')
    try {
      await login(email, password)
      navigate('/')
    } catch {
      setError('Не удалось войти. Проверьте e-mail и пароль.')
    } finally {
      setPending(false)
    }
  }

  return (
    <div className="auth-page">
      <main className="auth-card">
        <div className="auth-heading">
          <strong>ProbMind</strong>
          <span>Учебный сервис по теории вероятностей</span>
        </div>
        <h1>Вход в систему</h1>
        <form onSubmit={submit}>
          <label>E-mail<input value={email} onChange={(event) => setEmail(event.target.value)} type="email" required /></label>
          <label>Пароль<input value={password} onChange={(event) => setPassword(event.target.value)} type="password" required /></label>
          {error ? <div className="form-error">{error}</div> : null}
          <button className="primary-button" disabled={pending}>{pending ? 'Вход…' : 'Войти'}</button>
        </form>
        <p className="auth-note">Для демонстрации в форме указаны данные тестового студента.</p>
        <p className="auth-link">Нет аккаунта? <Link to="/register">Зарегистрироваться</Link></p>
      </main>
    </div>
  )
}
