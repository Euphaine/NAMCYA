@echo off
title NAMCYA Server Engine
cd /d "%~dp0"

:: Start the self-contained executable minimized in the background
start /min "" EventScoringSystem.exe

:: Loop and wait until port 5000 is active and ready
:checkport
netstat -ano | findstr :5000 >nul
if errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto checkport
)

:: Launch Microsoft Edge in clean App Mode (looks like a standalone desktop app)
start /wait msedge --app=http://localhost:5000

:: Automatically terminate the background server engine when the app window is closed
taskkill /FI "WINDOWTITLE eq NAMCYA Server Engine*" /F