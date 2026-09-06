import { createContext, PropsWithChildren, useContext, useEffect, useMemo, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { api, tokenStorage } from '../api/client'
import type { AuthResponse, AuthUser } from '../types/api'

interface AuthContextValue {
  user: AuthUser | null
  loading: boolean
  login(email: string, password: string): Promise<void>
  register(email: string, password: string, displayName: string): Promise<void>
  logout(): Promise<void>
  refreshProfile(): Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient()
  const [user, setUser] = useState<AuthUser | null>(null)
  const [loading, setLoading] = useState(true)

  async function refreshProfile() {
    const { data } = await api.get<AuthUser>('/auth/me')
    setUser(data)
  }

  useEffect(() => {
    refreshProfile()
      .catch(() => setUser(null))
      .finally(() => setLoading(false))
  }, [])

  async function login(email: string, password: string) {
    const { data } = await api.post<AuthResponse>('/auth/login', { email, password })
    tokenStorage.save(data.accessToken, data.refreshToken)
    queryClient.clear()
    setUser(data.user)
  }

  async function register(email: string, password: string, displayName: string) {
    const { data } = await api.post<AuthResponse>('/auth/register', { email, password, displayName })
    tokenStorage.save(data.accessToken, data.refreshToken)
    queryClient.clear()
    setUser(data.user)
  }

  async function logout() {
    const refreshToken = tokenStorage.refreshToken()
    try {
      if (refreshToken) await api.post('/auth/logout', { refreshToken })
    } finally {
      tokenStorage.clear()
      queryClient.clear()
      setUser(null)
    }
  }

  const value = useMemo(
    () => ({ user, loading, login, register, logout, refreshProfile }),
    [user, loading],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) throw new Error('useAuth must be used inside AuthProvider')
  return value
}
