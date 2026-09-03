import { ReactNode, useMemo, useState } from 'react'
import { ArrowDown, ArrowUp, ArrowUpDown, Search } from 'lucide-react'

export interface DataColumn<T> {
  key: string
  title: string
  render: (row: T) => ReactNode
  sortValue?: (row: T) => string | number | null | undefined
  width?: string
  align?: 'left' | 'center' | 'right'
}

interface DataTableProps<T> {
  rows: T[]
  columns: DataColumn<T>[]
  rowKey: (row: T) => string
  emptyText?: string
  searchText?: (row: T) => string
  searchPlaceholder?: string
  pageSize?: number
  toolbar?: ReactNode
  onRowClick?: (row: T) => void
}

type SortState = { key: string; direction: 'asc' | 'desc' } | null

export function DataTable<T>({
  rows,
  columns,
  rowKey,
  emptyText = 'Нет данных для отображения',
  searchText,
  searchPlaceholder = 'Поиск…',
  pageSize = 12,
  toolbar,
  onRowClick,
}: DataTableProps<T>) {
  const [query, setQuery] = useState('')
  const [sort, setSort] = useState<SortState>(null)
  const [page, setPage] = useState(1)

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q || !searchText) return rows
    return rows.filter((row) => searchText(row).toLowerCase().includes(q))
  }, [rows, query, searchText])

  const sorted = useMemo(() => {
    if (!sort) return filtered
    const column = columns.find((candidate) => candidate.key === sort.key)
    if (!column?.sortValue) return filtered
    return [...filtered].sort((a, b) => {
      const left = column.sortValue?.(a)
      const right = column.sortValue?.(b)
      if (left == null && right == null) return 0
      if (left == null) return 1
      if (right == null) return -1
      const result = typeof left === 'number' && typeof right === 'number'
        ? left - right
        : String(left).localeCompare(String(right), 'ru')
      return sort.direction === 'asc' ? result : -result
    })
  }, [columns, filtered, sort])

  const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize))
  const safePage = Math.min(page, totalPages)
  const visible = sorted.slice((safePage - 1) * pageSize, safePage * pageSize)

  function toggleSort(column: DataColumn<T>) {
    if (!column.sortValue) return
    setPage(1)
    setSort((current) => {
      if (!current || current.key !== column.key) return { key: column.key, direction: 'asc' }
      if (current.direction === 'asc') return { key: column.key, direction: 'desc' }
      return null
    })
  }

  return (
    <div className="data-table-shell">
      {(searchText || toolbar) && (
        <div className="data-table-toolbar">
          {searchText && (
            <label className="search-field" aria-label="Поиск по таблице">
              <Search size={17} aria-hidden="true" />
              <input
                value={query}
                placeholder={searchPlaceholder}
                onChange={(event) => { setQuery(event.target.value); setPage(1) }}
              />
            </label>
          )}
          {toolbar ? <div className="data-table-toolbar__right">{toolbar}</div> : null}
        </div>
      )}

      <div className="data-table-scroll">
        <table>
          <thead>
            <tr>
              {columns.map((column) => {
                const active = sort?.key === column.key
                const icon = active
                  ? sort?.direction === 'asc' ? <ArrowUp size={14} /> : <ArrowDown size={14} />
                  : <ArrowUpDown size={14} />
                return (
                  <th key={column.key} style={{ width: column.width, textAlign: column.align ?? 'left' }}>
                    {column.sortValue ? (
                      <button
                        type="button"
                        className={`sortable-header${active ? ' sortable-header--active' : ''}`}
                        onClick={() => toggleSort(column)}
                      >
                        <span>{column.title}</span>
                        {icon}
                      </button>
                    ) : (
                      <span className="plain-header">{column.title}</span>
                    )}
                  </th>
                )
              })}
            </tr>
          </thead>
          <tbody>
            {visible.map((row) => (
              <tr
                key={rowKey(row)}
                className={onRowClick ? 'clickable-row' : undefined}
                onClick={() => onRowClick?.(row)}
              >
                {columns.map((column) => (
                  <td key={column.key} style={{ textAlign: column.align ?? 'left' }}>
                    {column.render(row)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
        {!visible.length && <div className="table-empty">{emptyText}</div>}
      </div>

      {sorted.length > pageSize && (
        <div className="table-pagination">
          <span>
            {`${(safePage - 1) * pageSize + 1}–${Math.min(safePage * pageSize, sorted.length)} из ${sorted.length}`}
          </span>
          <div className="pagination-buttons">
            <button type="button" className="ghost-button small" disabled={safePage <= 1} onClick={() => setPage(safePage - 1)}>Назад</button>
            <span>Страница {safePage} из {totalPages}</span>
            <button type="button" className="ghost-button small" disabled={safePage >= totalPages} onClick={() => setPage(safePage + 1)}>Далее</button>
          </div>
        </div>
      )}
    </div>
  )
}
