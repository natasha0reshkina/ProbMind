import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { api } from '../api/client'
import { EmptyState } from '../components/EmptyState'
import { LoadingView } from '../components/LoadingView'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { useAuth } from '../auth/AuthContext'
import type { UserMisconception } from '../types/api'

export function MisconceptionsPage() {
  const { user } = useAuth()
  const { data, isLoading } = useQuery({
    queryKey: ['my-misconceptions', user?.id],
    queryFn: async () => (await api.get<UserMisconception[]>('/learner/misconceptions')).data,
  })

  if (isLoading) return <LoadingView />
  const items = (data ?? []).filter((item) => item.confidence >= .15)

  return (
    <div>
      <PageHeader
        title="Типичные ошибки"
        description="Ошибки, которые повторялись в диагностических и практических заданиях."
      />
      {items.length === 0 ? (
        <EmptyState title="Повторяющихся ошибок пока не выявлено" description="После диагностики здесь появятся темы, которые стоит разобрать подробнее." />
      ) : (
        <div className="table-frame">
          <table className="academic-table">
            <thead>
              <tr>
                <th>Тип ошибки</th>
                <th>Состояние</th>
                <th>Наблюдений</th>
                <th>Выраженность</th>
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr key={item.misconceptionId}>
                  <td><Link to={`/misconceptions/${item.misconceptionId}`}><strong>{item.title}</strong></Link><div className="table-secondary">{item.description}</div></td>
                  <td><StatusBadge value={item.status} /></td>
                  <td>{item.evidenceCount}</td>
                  <td className="numeric-cell">{Math.round(item.confidence * 100)}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
