export function ErrorView({ message = 'Не удалось загрузить данные.' }: { message?: string }) {
  return (
    <div className="error-view">
      <strong>Ошибка</strong>
      <p>{message}</p>
    </div>
  )
}
