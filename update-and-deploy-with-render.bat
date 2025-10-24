@echo off
echo ==============================
echo   Updating and Deploying Chat App...
echo ==============================

REM Set updates folder path
set "newFilesPath=%~dp0updates"

REM Check if updates folder exists
if not exist "%newFilesPath%" (
    echo Updates folder does not exist!
    pause
    exit /b
)

REM Copy index.html
if exist "%newFilesPath%\index.html" (
    copy /Y "%newFilesPath%\index.html" "public\index.html"
    echo index.html updated
)

REM Copy server.js
if exist "%newFilesPath%\server.js" (
    copy /Y "%newFilesPath%\server.js" "server.js"
    echo server.js updated
)

REM Git add & commit
git add .
git commit -m "Auto-update chat files"

REM Pull first to avoid conflicts
git pull origin main --rebase

REM Push to GitHub
git push origin main
if %errorlevel% neq 0 (
  echo Push failed! Please resolve conflicts.
  pause
  exit /b
)

echo Code pushed successfully!
echo Render should auto-deploy.

REM Clear updates folder
echo Clearing updates folder...
del /Q "%newFilesPath%\*"
echo Updates folder cleared.

REM Open Render link
start https://online-chat-1-dd3k.onrender.com

echo ==============================
echo   Update & Deploy Complete!
echo ==============================
pause
