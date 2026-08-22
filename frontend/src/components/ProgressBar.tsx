export function ProgressBar({ value, label }: { value: number; label?: string }) {
  const percent = Math.round(Math.min(1, Math.max(0, value)) * 100)
  return (
    <div className="progress-wrap">
      {label ? (
        <div className="progress-label">
          <span>{label}</span>
          <span>{percent}%</span>
        </div>
      ) : null}
      <div className="progress-track">
        <div className="progress-value" style={{ width: `${percent}%` }} />
      </div>
    </div>
  )
}
