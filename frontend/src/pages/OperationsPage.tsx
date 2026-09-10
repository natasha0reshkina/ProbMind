import { useQuery } from '@tanstack/react-query'
import { Activity, Cpu, HardDrive, Server, TimerReset } from 'lucide-react'
import { api } from '../api/client'
import { BarChartPanel } from '../components/ChartPanel'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { PageHeader } from '../components/PageHeader'
import type { ApiEndpointMetric, ApiRuntimeSnapshot, ProcessSnapshot } from '../types/api'
import { bytes, dateTime, duration, number } from '../utils/format'

function prettifyRoute(method: string, route: string) {
  const cleaned = route
    .replace(/^GET HTTP:\s*/i, '')
    .replace(/^POST HTTP:\s*/i, '')
    .replace(/^PUT HTTP:\s*/i, '')
    .replace(/^DELETE HTTP:\s*/i, '')
    .replace(/^PATCH HTTP:\s*/i, '')
    .replace(/\s*\(ProbMind\.Api\)\s*$/i, '')
    .trim()

  if (cleaned.startsWith('/')) return cleaned

  const controllerMatch = cleaned.match(/(?:^|\.)(([A-Za-z]+)Controller)\.([A-Za-z0-9_]+)/)
  if (!controllerMatch) return cleaned

  const controllerName = controllerMatch[2].replace(/Controller$/, '')
  const action = controllerMatch[3]

  const controllerRoutes: Record<string, string> = {
    Auth: '/api/auth',
    Diagnostics: '/api/diagnostics',
    Statistics: '/api/statistics',
    Practice: '/api/practice',
    Teacher: '/api/teacher',
    LearningPaths: '/api/learning-paths',
    LearnerModel: '/api/learner',
    Content: '/api/content',
    Admin: '/api/admin',
    Operations: '/api/operations',
  }

  const actionRoutes: Record<string, string> = {
    List: '',
    Get: '',
    Me: '/me',
    Login: '/login',
    Logout: '/logout',
    Dashboard: '/dashboard',
    Next: '/next',
    Submit: '/submit',
    Report: '/report',
    Topics: '/topics',
    Misconceptions: '/misconceptions',
    Users: '/users',
    Process: '/process',
    RuntimeMetrics: '/runtime-metrics',
    CompareDiagnostics: '/diagnostics/compare',
    Students: '/students',
    Questions: '/questions',
    Current: '/current',
  }

  const base = controllerRoutes[controllerName] ?? `/${controllerName}`
  const suffix = actionRoutes[action] ?? `/${action.charAt(0).toLowerCase()}${action.slice(1)}`
  return `${base}${suffix}`
}

export function OperationsPage() {
  const runtime = useQuery({
    queryKey: ['ops-runtime'],
    queryFn: async () => (await api.get<ApiRuntimeSnapshot>('/operations/runtime-metrics?top=200')).data,
    refetchInterval: 8_000,
  })
  const process = useQuery({
    queryKey: ['ops-process'],
    queryFn: async () => (await api.get<ProcessSnapshot>('/operations/process')).data,
    refetchInterval: 8_000,
  })

  const endpointRows = (runtime.data?.endpoints ?? []).map((item) => ({
    ...item,
    route: prettifyRoute(item.method, item.route),
  }))

  const slowest = [...endpointRows]
    .sort((a, b) => b.meanMilliseconds - a.meanMilliseconds)
    .slice(0, 8)
    .map((item) => ({
      route: `${item.method} ${item.route}`,
      ms: Math.round(item.meanMilliseconds),
    }))

  const busiest = [...endpointRows]
    .sort((a, b) => b.requests - a.requests)
    .slice(0, 8)
    .map((item) => ({
      route: `${item.method} ${item.route}`,
      requests: item.requests,
    }))

  const columns: DataColumn<ApiEndpointMetric>[] = [
    { key: 'route', title: 'Endpoint', render: (row) => <div><span className="http-method">{row.method}</span> <code>{prettifyRoute(row.method, row.route)}</code></div>, sortValue: (row) => `${row.method} ${prettifyRoute(row.method, row.route)}` },
    { key: 'requests', title: 'Запросы', render: (row) => row.requests.toLocaleString('ru-RU'), sortValue: (row) => row.requests, align: 'right' },
    { key: 'failures', title: 'Ошибок', render: (row) => row.failures, sortValue: (row) => row.failures, align: 'right' },
    { key: 'mean', title: 'Среднее, мс', render: (row) => number(row.meanMilliseconds, 1), sortValue: (row) => row.meanMilliseconds, align: 'right' },
    { key: 'max', title: 'Макс., мс', render: (row) => number(row.maxMilliseconds, 1), sortValue: (row) => row.maxMilliseconds, align: 'right' },
    { key: 'last', title: 'Последний запрос', render: (row) => dateTime(row.lastSeenAt), sortValue: (row) => row.lastSeenAt ? new Date(row.lastSeenAt).getTime() : 0 },
  ]

  const failureRate = runtime.data?.totalRequests ? runtime.data.totalFailures / runtime.data.totalRequests : 0

  return (
    <div>
      <PageHeader
        eyebrow="Состояние системы"
        title="Состояние приложения"
        description="Панель состояния показывает работу API, время обработки запросов и накопленные ошибки. Данные обновляются каждые 8 секунд."
      />
      <KpiStrip items={[
        { label: 'Всего запросов', value: runtime.data?.totalRequests.toLocaleString('ru-RU') ?? '-' },
        { label: 'Ошибок', value: runtime.data?.totalFailures ?? '-', tone: runtime.data?.totalFailures ? 'danger' : 'positive' },
        { label: 'Доля ошибок', value: `${(failureRate * 100).toFixed(2)}%`, tone: failureRate > .01 ? 'danger' : 'positive' },
        { label: 'Среднее время ответа', value: runtime.data ? `${number(runtime.data.meanMilliseconds, 1)} ms` : '-' },
        { label: 'Время работы', value: duration(process.data?.uptimeSeconds) },
      ]} />

      <div className="system-grid">
        <article className="system-card"><Server size={19} /><div><span>Узел</span><strong>{process.data?.machineName ?? '-'}</strong><small>{process.data?.framework ?? ''}</small></div></article>
        <article className="system-card"><Cpu size={19} /><div><span>CPU</span><strong>{process.data?.processorCount ?? '-'} ядер</strong><small>{process.data?.threads ?? '-'} потоков процесса</small></div></article>
        <article className="system-card"><HardDrive size={19} /><div><span>Память процесса</span><strong>{bytes(process.data?.workingSetBytes)}</strong><small>частная память {bytes(process.data?.privateMemoryBytes)}</small></div></article>
        <article className="system-card"><TimerReset size={19} /><div><span>Запущен</span><strong>{dateTime(process.data?.startedAt)}</strong><small>PID {process.data?.processId ?? '-'}</small></div></article>
      </div>

      <div className="two-column">
        <BarChartPanel title="Самые медленные маршруты" subtitle="Среднее время обработки запроса." data={slowest} xKey="route" series={[{ key: 'ms', label: 'Среднее, мс' }]} height={360} horizontal />
        <BarChartPanel title="Самые используемые маршруты" subtitle="Накопленное число запросов с момента запуска процесса." data={busiest} xKey="route" series={[{ key: 'requests', label: 'Запросы' }]} height={360} horizontal />
      </div>

      <section className="panel">
        <div className="panel-title"><div><h2>Метрики API</h2><p className="muted chart-subtitle">Время ответа, количество ошибок и число запросов по маршрутам API.</p></div><Activity size={20} /></div>
        <DataTable rows={endpointRows} columns={columns} rowKey={(row) => `${row.method}-${row.route}`} searchText={(row) => `${row.method} ${row.route}`} pageSize={18} />
      </section>
    </div>
  )
}
