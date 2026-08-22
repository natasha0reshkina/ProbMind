#!/bin/zsh
set -e
cd "${0:A:h}"

if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
  echo "Docker Desktop не запущен."
  exit 1
fi

echo "Проверяю конфигурацию..."
docker compose config >/dev/null

echo "Собираю backend..."
docker compose build api worker

echo "Проверяю frontend..."
docker build --target typecheck -f frontend/Dockerfile frontend
docker compose build frontend

echo "Запускаю тесты..."
docker compose --profile check build tests
docker compose up -d postgres redis
docker compose --profile check run --rm tests

echo "Запускаю приложение..."
docker compose up -d api worker frontend

for i in {1..120}; do
  if curl -fsS http://127.0.0.1:8000/api/ready >/dev/null 2>&1; then
    break
  fi
  sleep 2
done

curl -fsS http://127.0.0.1:8000/api/ready >/dev/null
curl -fsS http://127.0.0.1:8000/api/health >/dev/null
curl -fsS http://127.0.0.1:8080/ >/dev/null

if ! docker compose ps --status running --services | grep -qx "worker"; then
  docker compose logs --tail=120 worker
  exit 1
fi

echo "Проверка завершена."
