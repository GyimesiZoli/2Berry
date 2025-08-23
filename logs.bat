@echo off
pushd %~dp0
docker compose logs -f
popd
