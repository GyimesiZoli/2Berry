@echo off
pushd %~dp0
docker compose down
docker compose up --build -d
popd
