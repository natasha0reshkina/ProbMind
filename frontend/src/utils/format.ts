export function percent(value?: number | null, digits = 0) {
  if (value == null || Number.isNaN(value)) return '—'
  return `${(value * 100).toFixed(digits)}%`
}

export function number(value?: number | null, digits = 1) {
  if (value == null || Number.isNaN(value)) return '—'
  return value.toLocaleString('ru-RU', { maximumFractionDigits: digits, minimumFractionDigits: digits })
}

export function dateTime(value?: string | null) {
  if (!value) return '—'
  return new Intl.DateTimeFormat('ru-RU', {
    day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  }).format(new Date(value))
}

export function duration(seconds?: number | null) {
  if (seconds == null || Number.isNaN(seconds)) return '—'
  if (seconds < 60) return `${Math.round(seconds)} сек.`
  if (seconds < 3600) return `${Math.floor(seconds / 60)} мин ${Math.round(seconds % 60)} сек`
  return `${Math.floor(seconds / 3600)} ч ${Math.round((seconds % 3600) / 60)} мин`
}

export function bytes(value?: number | null) {
  if (value == null || Number.isNaN(value)) return '—'
  const units = ['Б', 'КБ', 'МБ', 'ГБ']
  let size = value
  let unit = 0
  while (size >= 1024 && unit < units.length - 1) {
    size /= 1024
    unit += 1
  }
  return `${size.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}`
}
