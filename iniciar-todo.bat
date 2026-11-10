@echo off
title SEA - Sistema Biometrico Completo (Next.js + NestJS + ZKTeco)
cd /d "%~dp0"

echo ======================================================================
echo    INICIANDO ECOSISTEMA BIOMETRICO COMPLETO (SEA LECTOR)
echo ======================================================================
echo.

echo 1. Iniciando Puente Biometrico C# (ZKTeco USB en puerto 8080)...
start "Puente ZKTeco (Puerto 8080)" /D "%~dp0sea-fingerprint-web-bridge" FingerprintBridgeServer.exe

timeout /t 2 >nul

echo 2. Iniciando Servidor Backend NestJS (Puerto 3001)...
start "Backend NestJS (Puerto 3001)" /D "%~dp0backend" cmd /k npm run start

timeout /t 3 >nul

echo 3. Iniciando Frontend Next.js (Puerto 3000)...
start "Frontend Next.js (Puerto 3000)" /D "%~dp0frontend" cmd /k npm run dev

timeout /t 4 >nul

echo.
echo ======================================================================
echo    TODO LISTO! Abriendo navegador en http://localhost:3000 ...
echo ======================================================================
start "" "http://localhost:3000"

exit
