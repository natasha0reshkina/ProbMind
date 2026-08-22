import { Navigate } from 'react-router-dom'
import { useAuth } from './AuthContext'
import { LoadingView } from '../components/LoadingView'
import type { UserRole } from '../types/api'
import { PropsWithChildren } from 'react'

export function ProtectedRoute({
  roles,
  children,
}: PropsWithChildren<{ roles?: UserRole[] }>) {
  const { user, loading } = useAuth()

  if (loading) return <LoadingView text="Проверяем сессию…" />
  if (!user) return <Navigate to="/login" replace />
  if (roles && !roles.includes(user.role)) return <Navigate to="/" replace />

  return children
}
