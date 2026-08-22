import { FormEvent, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ displayName: '', email: '', password: '' })
  const [error, setError] = useState('')
  const [pending, setPending] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setPending(true)
    setError('')
    try {
      await register(form.email, form.password, form.displayName)
      navigate('/')
    } catch {
      setError('Регистрация не удалась. Проверьте заполненные поля.')
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
        <h1>Регистрация</h1>
        <form onSubmit={submit}>
          <label>Имя<input value={form.displayName} onChange={(event) => setForm({ ...form, displayName: event.target.value })} required /></label>
          <label>E-mail<input type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} required /></label>
          <label>Пароль<input type="password" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} minLength={10} required /></label>
          <p className="field-hint">Не менее 10 символов, включая заглавную и строчную буквы и цифру.</p>
          {error ? <div className="form-error">{error}</div> : null}
          <button className="primary-button" disabled={pending}>{pending ? 'Регистрация…' : 'Зарегистрироваться'}</button>
        </form>
        <p className="auth-link">Уже есть аккаунт? <Link to="/login">Войти</Link></p>
      </main>
    </div>
  )
}
