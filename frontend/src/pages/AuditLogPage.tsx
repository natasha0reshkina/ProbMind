import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { FileJson, Search, ShieldCheck } from 'lucide-react'
import { api } from '../api/client'
import { DataTable, type DataColumn } from '../components/DataTable'
import { KpiStrip } from '../components/KpiStrip'
import { Modal } from '../components/Modal'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge, statusLabel } from '../components/StatusBadge'
import type { AuditLog } from '../types/api'
import { dateTime } from '../utils/format'

function prettyJson(value: string) {
  if (!value) return '—'
  try { return JSON.stringify(JSON.parse(value), null, 2) } catch { return value }
}

export function AuditLogPage() {
  const [take, setTake] = useState(250)
  const [selected, setSelected] = useState<AuditLog | null>(null)
  const audit = useQuery({
    queryKey: ['admin-audit', take],
    queryFn: async () => (await api.get<AuditLog[]>('/admin/audit', { params: { take } })).data,
  })

  const rows = audit.data ?? []
  const mutations = rows.filter((row) => !['Read', 'Viewed'].includes(row.action)).length
  const actors = new Set(rows.map((row) => row.actorUserId).filter(Boolean)).size
  const entityTypes = new Set(rows.map((row) => row.entityType)).size

  const columns: DataColumn<AuditLog>[] = useMemo(() => [
    { key: 'time', title: 'Время', render: (row) => dateTime(row.createdAt), sortValue: (row) => new Date(row.createdAt).getTime(), width: '170px' },
    { key: 'action', title: 'Действие', render: (row) => <StatusBadge value={row.action} />, sortValue: (row) => row.action },
    { key: 'entity', title: 'Сущность', render: (row) => <div><strong>{row.entityType}</strong><span className="table-secondary mono-small">{row.entityId ?? '—'}</span></div>, sortValue: (row) => row.entityType },
    { key: 'actor', title: 'Инициатор', render: (row) => <code>{row.actorUserId?.slice(0, 8) ?? 'система'}</code>, sortValue: (row) => row.actorUserId ?? '' },
    { key: 'request', title: 'ID запроса', render: (row) => <code>{row.requestId?.slice(0, 12) || '—'}</code>, sortValue: (row) => row.requestId },
    { key: 'details', title: '', render: (row) => <button className="ghost-button small" onClick={(event) => { event.stopPropagation(); setSelected(row) }}><FileJson size={14} /> JSON</button> },
  ], [])

  return (
    <div>
      <PageHeader
        eyebrow="Администрирование"
        title="Журнал изменений"
        description="Журнал административных действий и изменений учебного контента с указанием пользователя, времени и идентификатора запроса."
      />
      <KpiStrip items={[
        { label: 'Записей загружено', value: rows.length },
        { label: 'Мутаций', value: mutations },
        { label: 'Уникальных инициаторов', value: actors },
        { label: 'Типов сущностей', value: entityTypes },
      ]} />

      <section className="panel filter-panel">
        <div className="filter-panel__title"><Search size={18} /><strong>Глубина журнала</strong></div>
        <label>Последние записей
          <select value={take} onChange={(event) => setTake(Number(event.target.value))}>
            <option value={100}>100</option>
            <option value={250}>250</option>
            <option value={500}>500</option>
            <option value={1000}>1000</option>
          </select>
        </label>
      </section>

      <section className="panel">
        <div className="panel-title"><div><h2>События аудита</h2><p className="muted chart-subtitle">Нажмите на строку, чтобы посмотреть состояние данных до и после изменения.</p></div><ShieldCheck size={20} /></div>
        <DataTable
          rows={rows}
          columns={columns}
          rowKey={(row) => row.id}
          searchText={(row) => `${row.action} ${row.entityType} ${row.entityId ?? ''} ${row.actorUserId ?? ''} ${row.requestId}`}
          searchPlaceholder="Действие, сущность, инициатор, ID запроса…"
          pageSize={18}
          onRowClick={setSelected}
        />
      </section>

      <Modal open={Boolean(selected)} title={`${selected ? statusLabel(selected.action) : ''} · ${selected?.entityType ?? ''}`} description={selected ? `${dateTime(selected.createdAt)} · запрос ${selected.requestId}` : undefined} onClose={() => setSelected(null)} wide>
        <div className="json-compare">
          <div><h3>До изменения</h3><pre>{prettyJson(selected?.oldValueJson ?? '')}</pre></div>
          <div><h3>После изменения</h3><pre>{prettyJson(selected?.newValueJson ?? '')}</pre></div>
        </div>
        <div className="audit-meta-grid">
          <div><span>Инициатор</span><code>{selected?.actorUserId ?? 'система'}</code></div>
          <div><span>ID объекта</span><code>{selected?.entityId ?? '—'}</code></div>
          <div><span>ID запроса</span><code>{selected?.requestId ?? '—'}</code></div>
        </div>
      </Modal>
    </div>
  )
}
