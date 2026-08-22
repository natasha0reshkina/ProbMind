#!/bin/zsh
cd "${0:A:h}"
docker compose down
echo "ProbMind остановлен."
