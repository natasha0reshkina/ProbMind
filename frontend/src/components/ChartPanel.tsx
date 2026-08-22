import { ReactNode } from 'react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Scatter,
  ScatterChart,
  Tooltip,
  XAxis,
  YAxis,
  ZAxis,
} from 'recharts'

const chartColors = ['#4f46e5', '#7c3aed', '#0891b2', '#059669', '#d97706', '#dc2626', '#64748b']

interface ChartPanelProps {
  title: string
  subtitle?: string
  actions?: ReactNode
  children: ReactNode
  height?: number
}

export function ChartPanel({ title, subtitle, actions, children, height = 300 }: ChartPanelProps) {
  return (
    <section className="panel chart-panel">
      <div className="panel-title chart-panel__heading">
        <div>
          <h2>{title}</h2>
          {subtitle && <p className="muted chart-subtitle">{subtitle}</p>}
        </div>
        {actions}
      </div>
      <div style={{ height }}>{children}</div>
    </section>
  )
}

export interface SeriesConfig {
  key: string
  label: string
  colorIndex?: number
}

interface LineChartPanelProps {
  title: string
  subtitle?: string
  data: Record<string, string | number | null>[]
  xKey: string
  series: SeriesConfig[]
  yDomain?: [number | 'auto', number | 'auto']
  percent?: boolean
  height?: number
}

export function LineChartPanel({ title, subtitle, data, xKey, series, yDomain, percent, height }: LineChartPanelProps) {
  return (
    <ChartPanel title={title} subtitle={subtitle} height={height}>
      <ResponsiveContainer width="100%" height="100%">
        <LineChart data={data} margin={{ top: 8, right: 16, left: -10, bottom: 8 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#e8edf5" />
          <XAxis dataKey={xKey} tick={{ fontSize: 11 }} stroke="#94a3b8" />
          <YAxis domain={yDomain} tickFormatter={(value) => percent ? `${Math.round(value * 100)}%` : String(value)} tick={{ fontSize: 11 }} stroke="#94a3b8" />
          <Tooltip formatter={(value) => percent && typeof value === 'number' ? `${(value * 100).toFixed(1)}%` : value} />
          {series.length > 1 && <Legend />}
          {series.map((item, index) => (
            <Line
              key={item.key}
              type="monotone"
              dataKey={item.key}
              name={item.label}
              stroke={chartColors[item.colorIndex ?? index % chartColors.length]}
              strokeWidth={2.5}
              dot={false}
              activeDot={{ r: 5 }}
            />
          ))}
        </LineChart>
      </ResponsiveContainer>
    </ChartPanel>
  )
}

interface BarChartPanelProps {
  title: string
  subtitle?: string
  data: Record<string, string | number | null>[]
  xKey: string
  series: SeriesConfig[]
  percent?: boolean
  height?: number
  horizontal?: boolean
}

export function BarChartPanel({ title, subtitle, data, xKey, series, percent, height, horizontal }: BarChartPanelProps) {
  return (
    <ChartPanel title={title} subtitle={subtitle} height={height}>
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={data} layout={horizontal ? 'vertical' : 'horizontal'} margin={{ top: 6, right: 18, left: horizontal ? 38 : -10, bottom: 8 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#e8edf5" horizontal={!horizontal} vertical={horizontal} />
          {horizontal ? (
            <>
              <XAxis type="number" tickFormatter={(value) => percent ? `${Math.round(value * 100)}%` : String(value)} tick={{ fontSize: 11 }} />
              <YAxis type="category" dataKey={xKey} width={125} tick={{ fontSize: 11 }} />
            </>
          ) : (
            <>
              <XAxis dataKey={xKey} tick={{ fontSize: 11 }} />
              <YAxis tickFormatter={(value) => percent ? `${Math.round(value * 100)}%` : String(value)} tick={{ fontSize: 11 }} />
            </>
          )}
          <Tooltip formatter={(value) => percent && typeof value === 'number' ? `${(value * 100).toFixed(1)}%` : value} />
          {series.length > 1 && <Legend />}
          {series.map((item, index) => (
            <Bar key={item.key} dataKey={item.key} name={item.label} fill={chartColors[item.colorIndex ?? index % chartColors.length]} radius={[5, 5, 0, 0]} />
          ))}
        </BarChart>
      </ResponsiveContainer>
    </ChartPanel>
  )
}

interface DonutChartPanelProps {
  title: string
  subtitle?: string
  data: Array<{ name: string; value: number }>
  height?: number
  valueFormatter?: (value: number) => string
}

export function DonutChartPanel({ title, subtitle, data, height, valueFormatter }: DonutChartPanelProps) {
  return (
    <ChartPanel title={title} subtitle={subtitle} height={height}>
      <ResponsiveContainer width="100%" height="100%">
        <PieChart>
          <Pie data={data} dataKey="value" nameKey="name" innerRadius="58%" outerRadius="82%" paddingAngle={2}>
            {data.map((entry, index) => <Cell key={entry.name} fill={chartColors[index % chartColors.length]} />)}
          </Pie>
          <Tooltip formatter={(value) => valueFormatter && typeof value === 'number' ? valueFormatter(value) : value} />
          <Legend verticalAlign="bottom" height={28} />
        </PieChart>
      </ResponsiveContainer>
    </ChartPanel>
  )
}

interface ScatterChartPanelProps {
  title: string
  subtitle?: string
  data: Array<Record<string, string | number>>
  xKey: string
  yKey: string
  xLabel?: string
  yLabel?: string
  height?: number
}

export function ScatterChartPanel({ title, subtitle, data, xKey, yKey, xLabel, yLabel, height }: ScatterChartPanelProps) {
  return (
    <ChartPanel title={title} subtitle={subtitle} height={height}>
      <ResponsiveContainer width="100%" height="100%">
        <ScatterChart margin={{ top: 12, right: 18, bottom: 18, left: 2 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="#e8edf5" />
          <XAxis type="number" dataKey={xKey} name={xLabel ?? xKey} tick={{ fontSize: 11 }} />
          <YAxis type="number" dataKey={yKey} name={yLabel ?? yKey} tick={{ fontSize: 11 }} />
          <ZAxis range={[55, 55]} />
          <Tooltip cursor={{ strokeDasharray: '3 3' }} />
          <Scatter data={data} fill={chartColors[0]} />
        </ScatterChart>
      </ResponsiveContainer>
    </ChartPanel>
  )
}
