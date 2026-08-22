export function LoadingView({ text = 'Загрузка…' }: { text?: string }) {
  return (
    <div className="loading-view">
      <span className="spinner" />
      <span>{text}</span>
    </div>
  )
}
