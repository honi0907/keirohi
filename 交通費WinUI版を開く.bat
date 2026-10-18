@echo off
setlocal EnableExtensions
cd /d "%~dp0Keirohi.WinUI"
if errorlevel 1 goto ERR_CD

where dotnet >nul 2>&1
if errorlevel 1 goto ERR_DOTNET

echo Building Keirohi WinUI...
dotnet build -c Debug
if errorlevel 1 goto ERR_BUILD

set "EXE=bin\Debug\net8.0-windows10.0.26100.0\win-x64\Keirohi.App.exe"
if not exist "%EXE%" goto ERR_EXE

echo Starting...
start "" "%CD%\%EXE%"
exit /b 0

:ERR_CD
echo [ERROR] Cannot find Keirohi.WinUI folder.
pause
exit /b 1

:ERR_DOTNET
echo [ERROR] dotnet not found. Install .NET 8 SDK.
pause
exit /b 1

:ERR_BUILD
echo [ERROR] Build failed. Close Keirohi.App if it is running, then retry.
pause
exit /b 1

:ERR_EXE
echo [ERROR] EXE not found:
echo %CD%\%EXE%
pause
exit /b 1
