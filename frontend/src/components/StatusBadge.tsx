const labels: Record<string, string> = {
  Published: 'Опубликовано',
  Draft: 'Черновик',
  Archived: 'Архив',
  Active: 'Активен',
  Inactive: 'Отключён',
  Completed: 'Завершено',
  InProgress: 'В процессе',
  Detected: 'Обнаружено',
  Suspected: 'Подозрение',
  CorrectionInProgress: 'Коррекция',
  Corrected: 'Исправлено',
  RecheckRequired: 'Нужна повторная проверка',
  Unknown: 'Нет данных',
  Created: 'Создано',
  Analyzing: 'Анализируется',
  ReportReady: 'Отчёт готов',
  Rebuilt: 'Обновлено',
  Pending: 'Ожидает',
  Skipped: 'Пропущено',
  Stable: 'Стабильно',
  RequiresReview: 'Требует проверки',
  Student: 'Студент',
  Teacher: 'Преподаватель',
  Admin: 'Администратор',
  DiagnosticReady: 'Диагностика завершена',
  LearningPathChanged: 'План повторения обновлён',
  MisconceptionDetected: 'Обнаружена типичная ошибка',
  CorrectionCompleted: 'Коррекция завершена',
  Diagnostic: 'Диагностическое',
  Corrective: 'Коррекционное',
  Transfer: 'На перенос',
  Introductory: 'Вводная',
  Basic: 'Базовая',
  Intermediate: 'Средняя',
  Advanced: 'Высокая',
  Explanation: 'Объяснение',
  WorkedExample: 'Разобранный пример',
  ConceptCheck: 'Проверка понимания',
  GuidedPractice: 'Практика с подсказками',
  IndependentPractice: 'Самостоятельная практика',
  Updated: 'Изменено',
  RoleChanged: 'Изменена роль',
  excellent: 'Отлично',
  good: 'Хорошо',
  acceptable: 'Допустимо',
  review: 'Требует проверки',
  no_data: 'Нет данных',
  insufficient_sample: 'Мало данных',
  improving: 'Улучшается',
  declining: 'Снижается',
  stable: 'Стабильно',
  insufficient_data: 'Мало данных',
  insufficient_items: 'Мало заданий',
  zero_total_variance: 'Нет вариативности',
  questionable: 'Сомнительно',
  poor: 'Низкое',
  drift_detected: 'Есть изменение',
  unknown: 'Нет данных',
  top_10: 'Верхние 10%',
  upper_quartile: 'Верхний квартиль',
  middle: 'Средний диапазон',
  lower_middle: 'Ниже среднего',
  support_needed: 'Нужна поддержка',
  critical: 'Критический',
  high: 'Высокий',
  moderate: 'Средний',
  low: 'Низкий',
  minimal: 'Минимальный',
  advanced: 'Высокий уровень',
  low_engagement: 'Низкая активность',
  misconception_intensive: 'Много устойчивых ошибок',
  foundational_support: 'Нужна базовая поддержка',
  rapidly_improving: 'Быстрый прогресс',
  developing: 'Развивается',
  moderately_stable: 'Умеренно стабильно',
  unstable: 'Нестабильно',
  insufficient_history: 'Недостаточно истории',
  adaptive: 'Адаптивно изменяется',
  highly_dynamic: 'Часто изменяется',
  systemic_misconceptions: 'Системные затруднения',
  several_persistent_patterns: 'Несколько устойчивых ошибок',
  targeted_support: 'Нужна точечная поддержка',
  low_misconception_burden: 'Небольшое число устойчивых ошибок',
  empty: 'Нет данных',
}

export function statusLabel(value: string) {
  return labels[value] ?? value
}

function badgeIntent(value: string) {
  const normalized = value.toLowerCase()
  const success = ['excellent', 'good', 'improving', 'stable']
  const warning = ['review', 'moderate', 'high', 'support_needed', 'insufficient_sample']
  const danger = ['critical', 'declining', 'unstable']

  if (normalized.includes('correct') || normalized.includes('complete') || normalized.includes('published') || success.includes(normalized)) return 'success'
  if (normalized.includes('detect') || normalized.includes('required') || normalized.includes('active') || warning.includes(normalized)) return 'warning'
  if (normalized.includes('archive') || normalized.includes('inactive') || danger.includes(normalized)) return 'danger'
  return 'neutral'
}

export function StatusBadge({ value }: { value: string }) {
  return <span className={`badge badge--${badgeIntent(value)}`}>{statusLabel(value)}</span>
}
