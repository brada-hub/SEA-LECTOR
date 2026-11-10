@echo off
cd /d "%~dp0"
echo Compilando FingerprintBridgeServer.exe ...
"C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe" /platform:x86 /target:exe /nowarn:0168 /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll /r:AxInterop.ZKFPEngXControl.dll /r:Interop.ZKFPEngXControl.dll /out:FingerprintBridgeServer.exe BridgeServer.cs
if %ERRORLEVEL% EQU 0 (
    echo [EXITO] Compilacion completada correctamente!
) else (
    echo [ERROR] Fallo la compilacion.
)
pause
