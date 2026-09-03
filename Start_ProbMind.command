#!/bin/zsh
set -e
cd "${0:A:h}"

fail() {
  echo ""
  echo "Ошибка: $1"
  echo ""
  echo "Последние логи API:"
  docker compose logs --tail=120 api 2>/dev/null || true
  echo ""
  echo "Последние логи frontend:"
  docker compose logs --tail=80 frontend 2>/dev/null || true
  exit 1
}

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker не найден. Установите и откройте Docker Desktop."
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Docker Engine ещё не готов. Дождитесь полной загрузки Docker Desktop."
  exit 1
fi

if [[ ! -f .env ]]; then
  cp .env.example .env
fi

set -a
source .env
set +a

API_PORT=${API_PORT:-8010}
WEB_PORT=${WEB_PORT:-8080}

echo "Останавливаю предыдущий запуск проекта..."
docker compose down --remove-orphans >/dev/null 2>&1 || true

echo "Проверяю docker-compose..."
docker compose config >/dev/null

echo "1/4 Собираю ASP.NET Core API..."
docker compose build api || fail "не удалось собрать C# API"

echo "2/4 Собираю .NET Worker..."
docker compose build worker || fail "не удалось собрать Worker"

echo "3/4 Собираю React + TypeScript frontend..."
docker compose build --no-cache frontend || fail "не удалось собрать frontend"

echo "4/4 Запускаю PostgreSQL, Redis, API, Worker и frontend..."
docker compose up -d --force-recreate postgres redis api worker frontend || fail "не удалось запустить контейнеры"

printf "Ожидаю готовность API"
api_ready=0
for i in {1..120}; do
  if curl -fsS "http://127.0.0.1:${API_PORT}/api/ready" >/dev/null 2>&1; then
    api_ready=1
    break
  fi
  printf "."
  sleep 2
done

echo ""
if [[ "$api_ready" -ne 1 ]]; then
  fail "API не перешёл в состояние ready"
fi

printf "Ожидаю frontend"
frontend_ready=0
for i in {1..60}; do
  if curl -fsS "http://127.0.0.1:${WEB_PORT}/" >/dev/null 2>&1; then
    frontend_ready=1
    break
  fi
  printf "."
  sleep 2
done

echo ""
if [[ "$frontend_ready" -ne 1 ]]; then
  fail "frontend не ответил"
fi

if ! docker compose ps --status running --services | grep -qx "worker"; then
  echo "Ошибка: Worker не остался в состоянии running."
  docker compose logs --tail=120 worker || true
  exit 1
fi

echo ""
echo "ProbMind запущен."
echo "Приложение: http://127.0.0.1:${WEB_PORT}"
echo "Swagger:     http://127.0.0.1:${API_PORT}/api/swagger"
echo ""
open "http://127.0.0.1:${WEB_PORT}/"
