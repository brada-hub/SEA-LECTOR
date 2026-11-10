@echo off
title Instalador - SEA Biometrics Client Agent
cd /d "%~dp0"

echo ======================================================================
echo           INSTALADOR DEL AGENTE BIOMETRICO ZKTECO
echo ======================================================================
echo.

set "TARGET_DIR=%LOCALAPPDATA%\SEA-Biometrics-Bridge"

echo [1/4] Cerrando versiones anteriores si existen...
taskkill /F /IM FingerprintBridgeServer.exe >nul 2>&1

echo [2/4] Creando directorio en: %TARGET_DIR% ...
if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"

echo [3/4] Copiando archivos del puente...
copy /Y "FingerprintBridgeServer.exe" "%TARGET_DIR%\" >nul
copy /Y "AxInterop.ZKFPEngXControl.dll" "%TARGET_DIR%\" >nul
copy /Y "Interop.ZKFPEngXControl.dll" "%TARGET_DIR%\" >nul

echo [4/4] Configurando inicio automatico con Windows...
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "SEABiometricsBridge" /t REG_SZ /d "\"%TARGET_DIR%\FingerprintBridgeServer.exe\"" /f >nul

echo.
echo Iniciando agente biometrico...
start "" "%TARGET_DIR%\FingerprintBridgeServer.exe"

echo.
echo ======================================================================
echo   [EXITO] INSTALACION COMPLETADA CORRECTAMENTE!
echo ======================================================================
echo.
echo El lector de huellas ya esta conectado y el agente se ejecutara
echo automaticamente cada vez que encienda esta computadora.
echo.
echo Ya puede abrir su navegador y usar el CRM normalmente.
echo.
pause
