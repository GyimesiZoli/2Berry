@echo off
pushd %~dp0
docker compose up --build -d
popd
