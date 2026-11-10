@echo off
title Desinstalador - SEA Biometrics Client Agent
cd /d "%~dp0"

echo ======================================================================
echo          DESINSTALADOR DEL AGENTE BIOMETRICO ZKTECO
echo ======================================================================
echo.

set "TARGET_DIR=%LOCALAPPDATA%\SEA-Biometrics-Bridge"

echo [1/3] Deteniendo agente...
taskkill /F /IM FingerprintBridgeServer.exe >nul 2>&1

echo [2/3] Eliminando inicio automatico de Windows...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "SEABiometricsBridge" /f >nul 2>&1

echo [3/3] Eliminando archivos locales...
if exist "%TARGET_DIR%" rmdir /S /Q "%TARGET_DIR%" >nul 2>&1

echo.
echo ======================================================================
echo   [OK] El agente ha sido desinstalado por completo de esta PC.
echo ======================================================================
echo.
pause
