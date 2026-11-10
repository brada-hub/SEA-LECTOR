@echo off
title SEA - ZKTeco Fingerprint Web Bridge
cd /d "%~dp0"
echo ========================================================
echo   SEA - ZKTeco Fingerprint Web Bridge Server
echo   Servidor Puente para Aplicaciones Web y CRM
echo ========================================================
echo.
echo Iniciando servidor en http://localhost:8080 ...
echo Mantenga esta ventana abierta.
echo.
start "" "FingerprintBridgeServer.exe"
timeout /t 2 >nul
start "" "http://localhost:8080"
exit
