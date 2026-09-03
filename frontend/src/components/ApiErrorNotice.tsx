import { useEffect, useState } from 'react'

const EVENT_NAME = 'probmind:api-error'

export function ApiErrorNotice() {
  const [message, setMessage] = useState('')

  useEffect(() => {
    let timer: number | undefined

    const handler = (event: Event) => {
      const custom = event as CustomEvent<string>
      setMessage(custom.detail || 'Не удалось выполнить запрос. Попробуйте ещё раз.')
      if (timer) window.clearTimeout(timer)
      timer = window.setTimeout(() => setMessage(''), 5000)
    }

    window.addEventListener(EVENT_NAME, handler)
    return () => {
      window.removeEventListener(EVENT_NAME, handler)
      if (timer) window.clearTimeout(timer)
    }
  }, [])

  if (!message) return null

  return (
    <div className="api-error-notice" role="alert" aria-live="assertive">
      <span>{message}</span>
      <button type="button" aria-label="Закрыть сообщение" onClick={() => setMessage('')}>×</button>
    </div>
  )
}
