import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios'

const ACCESS_KEY = 'probmind.access'
const REFRESH_KEY = 'probmind.refresh'

export const api = axios.create({
  baseURL: '/api',
  timeout: 20_000,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (config.data instanceof FormData) config.headers.delete('Content-Type')
  const token = localStorage.getItem(ACCESS_KEY)
  if (token) config.headers.Authorization = `Bearer ${token}`
  config.headers['X-Request-Id'] = crypto.randomUUID()

  const method = config.method?.toUpperCase()
  if (method && ['POST', 'PUT', 'PATCH'].includes(method) && !config.headers['X-Idempotency-Key']) {
    config.headers['X-Idempotency-Key'] = crypto.randomUUID()
  }

  return config
})


function requestErrorMessage(error: AxiosError) {
  const status = error.response?.status
  const serverMessage = (error.response?.data as { message?: string } | undefined)?.message
  if (serverMessage) return serverMessage
  if (!status) return 'Не удалось связаться с сервером. Проверьте соединение и повторите попытку.'
  if (status === 400) return 'Запрос содержит некорректные данные. Проверьте введённую информацию.'
  if (status === 403) return 'Для этой операции недостаточно прав.'
  if (status === 404) return 'Запрошенные данные не найдены.'
  if (status === 409) return 'Операция уже была выполнена или конфликтует с текущим состоянием.'
  if (status >= 500) return 'Сервис временно не может выполнить операцию. Попробуйте ещё раз.'
  return 'Не удалось выполнить запрос. Попробуйте ещё раз.'
}

function notifyRequestError(error: AxiosError) {
  window.dispatchEvent(new CustomEvent('probmind:api-error', { detail: requestErrorMessage(error) }))
}

let refreshing = false
let waiters: Array<(token: string | null) => void> = []

function notifyWaiters(token: string | null) {
  waiters.forEach((resolve) => resolve(token))
  waiters = []
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined
    if (!original || error.response?.status !== 401 || original._retried) {
      if (error.response?.status !== 401) notifyRequestError(error)
      throw error
    }

    const refreshToken = localStorage.getItem(REFRESH_KEY)
    if (!refreshToken) {
      window.dispatchEvent(new CustomEvent('probmind:api-error', { detail: 'Сеанс завершён. Выполните вход снова.' }))
      throw error
    }

    if (refreshing) {
      const token = await new Promise<string | null>((resolve) => waiters.push(resolve))
      if (!token) throw error
      original.headers.Authorization = `Bearer ${token}`
      original._retried = true
      return api(original)
    }

    refreshing = true
    original._retried = true

    try {
      const response = await axios.post('/api/auth/refresh', { refreshToken })
      const token = response.data.accessToken as string
      const nextRefresh = response.data.refreshToken as string
      localStorage.setItem(ACCESS_KEY, token)
      localStorage.setItem(REFRESH_KEY, nextRefresh)
      notifyWaiters(token)
      original.headers.Authorization = `Bearer ${token}`
      return api(original)
    } catch (refreshError) {
      localStorage.removeItem(ACCESS_KEY)
      localStorage.removeItem(REFRESH_KEY)
      notifyWaiters(null)
      window.location.assign('/login')
      throw refreshError
    } finally {
      refreshing = false
    }
  },
)

export const tokenStorage = {
  save(accessToken: string, refreshToken: string) {
    localStorage.setItem(ACCESS_KEY, accessToken)
    localStorage.setItem(REFRESH_KEY, refreshToken)
  },
  clear() {
    localStorage.removeItem(ACCESS_KEY)
    localStorage.removeItem(REFRESH_KEY)
  },
  refreshToken() {
    return localStorage.getItem(REFRESH_KEY)
  },
}
