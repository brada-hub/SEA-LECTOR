@echo off
title SEA - Detener Todos los Servicios
echo ======================================================================
echo    DETENIENDO ECOSISTEMA BIOMETRICO SEA
echo ======================================================================
echo.

echo 1. Cerrando Puente Biometrico C# (Puerto 8080)...
taskkill /F /IM FingerprintBridgeServer.exe >nul 2>&1

echo 2. Deteniendo servidores Node.js...
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :3001') do taskkill /F /PID %%a >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :3000') do taskkill /F /PID %%a >nul 2>&1

echo.
echo ======================================================================
echo    TODOS LOS SERVICIOS HAN SIDO DETENIDOS LIMPIAMENTE.
echo ======================================================================
timeout /t 2 >nul
exit
