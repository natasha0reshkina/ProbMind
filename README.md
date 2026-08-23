# ProbMind

Веб-приложение для диагностики типичных ошибок при изучении теории вероятностей и адаптивного повторения материала.

## Запуск

Для локального запуска нужен Docker Desktop.

```bash
./Start_ProbMind.command
```

Приложение: `http://127.0.0.1:8080`

Swagger: `http://127.0.0.1:8010/api/swagger`

Остановка:

```bash
./Stop_ProbMind.command
```

Сборка и тесты:

```bash
./Check_ProbMind.command
```

Если macOS блокирует `.command`-файлы:

```bash
xattr -dr com.apple.quarantine .
chmod +x *.command
```

Параметры локального окружения находятся в `.env.example`. При первом запуске создаётся `.env`, который не добавляется в Git.

## Стек

Backend: C# 12, ASP.NET Core 8, Entity Framework Core, PostgreSQL, Redis, xUnit.

Frontend: React, TypeScript, Vite, TanStack Query, Axios, Recharts.

Локальное окружение запускается через Docker Compose.

## Тестовые аккаунты

- `student@probmind.local` / `Student123!`
- `teacher@probmind.local` / `Teacher123!`
- `admin@probmind.local` / `Admin123!`
