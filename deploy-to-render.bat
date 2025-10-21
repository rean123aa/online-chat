@echo off
echo ==============================
echo   🚀 Deploying to Render...
echo ==============================

cd /d "%~dp0"

git add .
echo ✅ Files added.

set /p msg=Enter commit message (press Enter for default): 
if "%msg%"=="" set msg=auto-deploy
git commit -m "%msg%"
echo 💾 Commit complete.

git pull origin main --rebase
git push origin main
if %errorlevel% neq 0 (
  echo ❌ Push failed! Check for conflicts or network issues.
  pause
  exit /b
)

echo ✅ Code pushed successfully!
echo 🌐 Render should start redeploying automatically.
timeout /t 10 >nul
echo ✅ Done! Check your Render app.
pause
