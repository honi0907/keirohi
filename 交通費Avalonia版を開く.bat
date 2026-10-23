@echo off
chcp 65001 >nul
cd /d "%~dp0"
dotnet run --project "Keirohi.Avalonia\Keirohi.Avalonia.csproj" -c Debug
pause
