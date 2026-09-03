import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { EmptyState } from '../components/EmptyState'
import { ErrorView } from '../components/ErrorView'
import { LoadingView } from '../components/LoadingView'
import { StatusBadge } from '../components/StatusBadge'
import type { DiagnosticComparison, DiagnosticSession } from '../types/api'

function percent(value: number) {
  return `${Math.round(value * 100)}%`
}

function signedPercent(value: number) {
  const rounded = Math.round(value * 100)
  return `${rounded > 0 ? '+' : ''}${rounded}%`
}

export function DiagnosticHistoryPage() {
  const [fromId, setFromId] = useState('')
  const [toId, setToId] = useState('')

  const history = useQuery({
    queryKey: ['diagnostic-history'],
    queryFn: async () => (await api.get<DiagnosticSession[]>('/diagnostics')).data,
  })

  const completed = useMemo(
    () => (history.data ?? [])
      .filter((item) => item.status === 'ReportReady')
      .sort((a, b) => new Date(a.completedAt ?? a.startedAt ?? 0).getTime() - new Date(b.completedAt ?? b.startedAt ?? 0).getTime()),
    [history.data],
  )

  const effectiveFrom = fromId || completed.at(-2)?.id || ''
  const effectiveTo = toId || completed.at(-1)?.id || ''

  const comparison = useQuery({
    queryKey: ['diagnostic-comparison', effectiveFrom, effectiveTo],
    enabled: Boolean(effectiveFrom && effectiveTo && effectiveFrom !== effectiveTo),
    queryFn: async () => (await api.get<DiagnosticComparison>('/statistics/diagnostics/compare', {
      params: { fromSessionId: effectiveFrom, toSessionId: effectiveTo },
    })).data,
  })

  if (history.isLoading) return <LoadingView />
  if (history.isError) return <ErrorView message="Не удалось загрузить историю диагностик." />

  return (
    <div className="academic-page">
      <header className="simple-page-header">
        <div>
          <h1>История диагностик</h1>
          <p>Здесь сохраняются завершённые и незавершённые диагностические сессии. Незавершённую диагностику можно продолжить с того места, где она была остановлена.</p>
        </div>
      </header>

      <section className="plain-section">
        <h2>Диагностические сессии</h2>
        {(history.data ?? []).length === 0 ? (
          <EmptyState title="Диагностик пока нет" description="После первой попытки здесь появятся дата, статус и итоговый результат." />
        ) : (
          <div className="table-frame">
            <table className="academic-table">
              <thead>
                <tr><th>Дата</th><th>Статус</th><th>Вопросы</th><th>Точность</th><th>Действие</th></tr>
              </thead>
              <tbody>
                {(history.data ?? []).map((item) => (
                  <tr key={item.id}>
                    <td>{item.startedAt ? new Date(item.startedAt).toLocaleString('ru-RU') : '—'}</td>
                    <td><StatusBadge value={item.status} /></td>
                    <td>{item.answeredQuestionCount} / {item.plannedQuestionCount}</td>
                    <td>{item.overallScore == null ? '—' : percent(item.overallScore)}</td>
                    <td>
                      {item.status === 'ReportReady' ? (
                        <Link className="secondary-button history-action-button" to={`/diagnostics/${item.id}/report`}>Открыть отчёт</Link>
                      ) : item.status === 'InProgress' ? (
                        <Link className="primary-button history-action-button" to={`/diagnostics/${item.id}/continue`}>Продолжить</Link>
                      ) : (
                        '—'
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {completed.length >= 2 ? (
        <section className="plain-section">
          <h2>Сравнение диагностик</h2>
          <p className="muted">Сравнение показывает, как изменились точность и уровень освоения тем между двумя завершёнными сессиями.</p>

          <div className="comparison-controls">
            <label>
              Первая диагностика
              <select value={effectiveFrom} onChange={(event) => setFromId(event.target.value)}>
                {completed.map((item) => (
                  <option key={item.id} value={item.id}>
                    {new Date(item.completedAt ?? item.startedAt ?? '').toLocaleString('ru-RU')} · {item.overallScore == null ? '—' : percent(item.overallScore)}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Вторая диагностика
              <select value={effectiveTo} onChange={(event) => setToId(event.target.value)}>
                {completed.map((item) => (
                  <option key={item.id} value={item.id}>
                    {new Date(item.completedAt ?? item.startedAt ?? '').toLocaleString('ru-RU')} · {item.overallScore == null ? '—' : percent(item.overallScore)}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {effectiveFrom === effectiveTo ? (
            <p className="muted">Выберите две разные диагностики.</p>
          ) : comparison.isLoading ? (
            <LoadingView />
          ) : comparison.data ? (
            <>
              <dl className="summary-list comparison-summary">
                <div><dt>Изменение точности</dt><dd>{signedPercent(comparison.data.accuracyDelta)}</dd></div>
                <div><dt>Типичных ошибок в отчёте</dt><dd>{comparison.data.from.detectedMisconceptions} → {comparison.data.to.detectedMisconceptions}</dd></div>
              </dl>
              <div className="table-frame">
                <table className="academic-table">
                  <thead><tr><th>Тема</th><th>Было</th><th>Стало</th><th>Изменение</th></tr></thead>
                  <tbody>
                    {comparison.data.topics.map((topic) => (
                      <tr key={topic.topicId}>
                        <td>{topic.name}</td>
                        <td>{topic.fromMastery == null ? '—' : percent(topic.fromMastery)}</td>
                        <td>{topic.toMastery == null ? '—' : percent(topic.toMastery)}</td>
                        <td>{topic.delta == null ? '—' : signedPercent(topic.delta)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          ) : null}
        </section>
      ) : null}
    </div>
  )
}
