#!/bin/zsh
set -e
cd "${0:A:h}"

if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
  echo "Docker Desktop не запущен."
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

echo "Проверяю конфигурацию..."
docker compose config >/dev/null

echo "Собираю backend..."
docker compose build api worker

echo "Собираю frontend..."
docker compose build frontend

echo "Запускаю тесты..."
docker compose --profile check build tests
docker compose up -d postgres redis
docker compose --profile check run --rm tests

echo "Запускаю приложение..."
docker compose up -d api worker frontend

for i in {1..120}; do
  if curl -fsS "http://127.0.0.1:${API_PORT}/api/ready" >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

curl -fsS "http://127.0.0.1:${API_PORT}/api/ready" >/dev/null
curl -fsS "http://127.0.0.1:${API_PORT}/api/health" >/dev/null
curl -fsS "http://127.0.0.1:${WEB_PORT}/" >/dev/null

if ! docker compose ps --status running --services | grep -qx "worker"; then
  docker compose logs --tail=120 worker
  exit 1
fi

echo "Проверка завершена."
