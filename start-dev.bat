@echo off
echo ========================================
echo   Lumina Tutors - Dev Launcher
echo ========================================

:: Kill any existing dotnet process on port 60481
for /f "tokens=5" %%a in ('netstat -aon ^| find "60481" ^| find "LISTENING"') do (
    taskkill /f /pid %%a >nul 2>&1
)

:: Start backend minimized (runs in background)
start /min "Lumina Backend" cmd /k "cd /d "%~dp0" && dotnet run --project src/LuminaTutors.Web"

echo [1/2] Backend dang khoi dong...
timeout /t 8 /nobreak >nul

:: Start Expo in normal window
start "Lumina Expo" cmd /k "cd /d "%~dp0lumina-mobile" && npx expo start --clear"

echo.
echo ========================================
echo   DA KHOI DONG XONG!
echo.
echo   Web (may tinh): http://localhost:60481
echo   Web (dien thoai): http://10.21.3.60:60481
echo   Mobile: Quet QR trong cua so Expo
echo ========================================
echo.
pause
