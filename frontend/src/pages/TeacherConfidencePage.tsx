import { useQuery } from '@tanstack/react-query'
import { api } from '../api/client'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import type { TeacherConfidenceStudent } from '../types/api'

export function TeacherConfidencePage() {
  const query = useQuery({
    queryKey: ['teacher-confidence'],
    queryFn: async () => (await api.get<TeacherConfidenceStudent[]>('/edtech/teacher/confidence')).data,
  })

  if (query.isLoading) return <LoadingView />

  return (
    <div>
      <PageHeader
        eyebrow="Метакогниция группы"
        title="Уверенность студентов"
        description="Сравнение субъективной уверенности и фактической правильности. Особенно полезны уверенные ошибки: они часто указывают на устойчивое заблуждение."
      />
      <section className="plain-section">
        <div className="table-frame">
          <table className="academic-table">
            <thead>
              <tr>
                <th>Студент</th>
                <th>Ответов</th>
                <th>Средняя уверенность</th>
                <th>Точность</th>
                <th>Уверенно, но неверно</th>
                <th>Верно при низкой уверенности</th>
              </tr>
            </thead>
            <tbody>
              {(query.data ?? []).map((item) => (
                <tr key={item.studentId}>
                  <td><strong>{item.displayName}</strong></td>
                  <td>{item.answersWithConfidence}</td>
                  <td>{item.answersWithConfidence ? `${item.meanConfidence.toFixed(1)} / 4` : '-'}</td>
                  <td>{item.answersWithConfidence ? `${Math.round(item.accuracy * 100)}%` : '-'}</td>
                  <td>{item.overconfidentWrong}</td>
                  <td>{item.lowConfidenceCorrect}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  )
}
