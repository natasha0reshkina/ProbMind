import { ReactNode } from 'react'

export function PageHeader({
  title,
  actions,
}: {
  eyebrow?: string
  title: string
  description?: string
  actions?: ReactNode
}) {
  return (
    <header className="page-header">
      <h1>{title}</h1>
      {actions ? <div className="page-header__actions">{actions}</div> : null}
    </header>
  )
}
