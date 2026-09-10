import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { useAuth } from '../auth/AuthContext'
import type { ConfidenceSummary } from '../types/api'

const confidenceOptions = [
  { value: 1, label: 'Совсем не уверен' },
  { value: 2, label: 'Скорее не уверен' },
  { value: 3, label: 'Скорее уверен' },
  { value: 4, label: 'Полностью уверен' },
] as const

const allConfidenceLevels = confidenceOptions.map((item) => item.value)
const confidenceLabel = (value: number) => confidenceOptions.find((item) => item.value === value)?.label ?? '-'

export function ConfidencePage() {
  const { user } = useAuth()
  const [selectedLevels, setSelectedLevels] = useState<number[]>(allConfidenceLevels)
  const query = useQuery({
    queryKey: ['confidence-summary', user?.id],
    queryFn: async () => (await api.get<ConfidenceSummary>('/edtech/confidence')).data,
  })

  const filteredAnswers = useMemo(() => {
    const answers = query.data?.answers ?? []
    return answers.filter((answer) => selectedLevels.includes(answer.confidenceLevel))
  }, [query.data?.answers, selectedLevels])

  const groupedAnswers = useMemo(() => {
    const groups = new Map<string, typeof filteredAnswers>()
    filteredAnswers.forEach((answer) => {
      const current = groups.get(answer.topicName) ?? []
      current.push(answer)
      groups.set(answer.topicName, current)
    })
    return [...groups.entries()].sort(([a], [b]) => a.localeCompare(b, 'ru'))
  }, [filteredAnswers])

  function toggleLevel(level: number) {
    setSelectedLevels((current) => current.includes(level)
      ? current.filter((item) => item !== level)
      : [...current, level].sort())
  }

  if (query.isLoading) return <LoadingView />
  const data = query.data
  if (!data) return null

  return (
    <div>
      <PageHeader
        eyebrow="Метакогниция"
        title="Уверенность в ответах"
        description="Сравнивайте выбранную степень уверенности с фактической правильностью. Самооценка уверенности при решении необязательна."
      />
      <section className="kpi-strip edtech-kpi-strip">
        <div><span>Ответов с оценкой</span><strong>{data.answersWithConfidence}</strong></div>
        <div><span>Средняя уверенность</span><strong>{data.meanConfidence ? data.meanConfidence.toFixed(1) : '-'} / 4</strong></div>
        <div><span>Точность</span><strong>{Math.round(data.accuracy * 100)}%</strong></div>
        <div><span>Полностью уверен, но неверно</span><strong>{data.overconfidentWrong}</strong></div>
      </section>

      <section className="plain-section">
        <div className="confidence-filter-panel">
          <div>
            <h2>Фильтр по степени уверенности</h2>
            <p className="muted">Можно оставить одну или сразу несколько конкретных степеней.</p>
          </div>
          <div className="confidence-filter-actions">
            <button type="button" className="ghost-button" onClick={() => setSelectedLevels(allConfidenceLevels)}>Выбрать все</button>
            <button type="button" className="ghost-button" onClick={() => setSelectedLevels([])}>Снять все</button>
          </div>
        </div>
        <div className="confidence-filter-grid" role="group" aria-label="Степени уверенности">
          {confidenceOptions.map((item) => (
            <label className={`confidence-filter-chip ${selectedLevels.includes(item.value) ? 'selected' : ''}`} key={item.value}>
              <input
                type="checkbox"
                checked={selectedLevels.includes(item.value)}
                onChange={() => toggleLevel(item.value)}
              />
              <span>{item.label}</span>
            </label>
          ))}
        </div>
      </section>

      <section className="plain-section">
        <div className="section-heading-row">
          <div>
            <h2>История ответов по темам</h2>
            <p className="muted">Задания разделены на тематические блоки, чтобы было видно, в каких темах уверенность совпадает с результатом, а в каких - нет.</p>
          </div>
          <span className="topic-count-badge">{filteredAnswers.length} ответов · {groupedAnswers.length} тем</span>
        </div>

        {selectedLevels.length === 0 ? (
          <p className="muted">Выберите хотя бы одну степень уверенности в фильтре выше.</p>
        ) : groupedAnswers.length === 0 ? (
          <p className="muted">Для выбранных степеней уверенности ответов пока нет.</p>
        ) : (
          <div className="confidence-topic-groups">
            {groupedAnswers.map(([topicName, answers]) => (
              <section className="confidence-topic-group" key={topicName}>
                <div className="topic-group-heading">
                  <div><span>Тема</span><strong>{topicName}</strong></div>
                  <span>{answers.length} {answers.length === 1 ? 'ответ' : 'ответов'}</span>
                </div>
                <div className="table-frame">
                  <table className="academic-table">
                    <thead><tr><th>Дата</th><th>Источник</th><th>Задание</th><th>Результат</th><th>Степень уверенности</th></tr></thead>
                    <tbody>
                      {answers.slice(0, 100).map((answer) => (
                        <tr key={answer.id}>
                          <td>{new Date(answer.submittedAt).toLocaleString('ru-RU')}</td>
                          <td>{answer.source}</td>
                          <td className="confidence-prompt-cell">{answer.prompt}</td>
                          <td>{answer.isCorrect ? 'Верно' : 'Ошибка'}</td>
                          <td>{confidenceLabel(answer.confidenceLevel)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
