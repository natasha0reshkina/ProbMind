import { ReactNode } from 'react'

export interface KpiItem {
  label: string
  value: ReactNode
  hint?: string
  tone?: 'default' | 'positive' | 'warning' | 'danger'
}

export function KpiStrip({ items }: { items: KpiItem[] }) {
  return (
    <div className="kpi-strip">
      {items.map((item) => (
        <div className={`kpi-strip__item kpi-strip__item--${item.tone ?? 'default'}`} key={item.label}>
          <span>{item.label}</span>
          <strong>{item.value}</strong>
          {item.hint && <small>{item.hint}</small>}
        </div>
      ))}
    </div>
  )
}
