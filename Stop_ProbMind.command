#!/bin/zsh
set -e
cd "${0:A:h}"
docker compose down --remove-orphans
