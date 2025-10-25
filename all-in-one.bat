@echo off
title Chat App Monitor

REM ===========================
REM ASCII Art + Title
REM ===========================
echo.
echo     ****     ****          Hello Ethan
echo   ******** ********
echo  *******************
echo   *****************
echo    ***************
echo      ***********
echo        *******
echo          ***
echo           *
echo.
echo Press any key to open your code in Cursor...
pause >nul

REM ======== Path to Cursor executable ========
set "cursorPath=C:\Program Files\cursor\Cursor.exe"
REM ===========================================

REM Project folder (folder containing this bat)
set "folder=%~dp0"

REM Paths to files
set "htmlFile=%folder%public\index.html"
set "jsFile=%folder%server.js"

REM Check if files exist
if not exist "%htmlFile%" (
    echo ERROR: index.html not found at "%htmlFile%"
    pause
    exit /b
)
if not exist "%jsFile%" (
    echo ERROR: server.js not found at "%jsFile%"
    pause
    exit /b
)

REM Open files in Cursor minimized
start "" /min "%cursorPath%" "%htmlFile%" >nul 2>&1
start "" /min "%cursorPath%" "%jsFile%" >nul 2>&1

echo.
echo === Files opened in Cursor ===
echo.

REM Ask which files to monitor
:ask
echo Did you modify only HTML, only server.js, or both? (html/server/both)
set /p choice=

if /i "%choice%"=="html" (
    set "checkHtml=1"
    set "checkServer=0"
) else if /i "%choice%"=="server" (
    set "checkHtml=0"
    set "checkServer=1"
) else if /i "%choice%"=="both" (
    set "checkHtml=1"
    set "checkServer=1"
) else (
    echo Invalid choice. Type html, server, or both.
    goto ask
)

REM ====== Git setup if repo missing ======
if not exist "%folder%.git" (
    echo [%time%] Git repo not found. Initializing...
    git init
    git remote add origin https://github.com/rean123aa/online-chat.git
    echo # Ignore everything except project files > "%folder%.gitignore"
    echo !index.html >> "%folder%.gitignore"
    echo !server.js >> "%folder%.gitignore"
    git add .
    git commit -m "Initial commit"
    echo [%time%] Git initialized and initial commit done.
)

REM ====== Generate initial hashes ======
if %checkHtml%==1 for /f "tokens=1,*" %%a in ('certutil -hashfile "%htmlFile%" SHA256 ^| findstr /v /c:"hash" /c:"CertUtil"') do set "hhash=%%a%%b"
if %checkServer%==1 for /f "tokens=1,*" %%a in ('certutil -hashfile "%jsFile%" SHA256 ^| findstr /v /c:"hash" /c:"CertUtil"') do set "shash=%%a%%b"

echo Monitoring for changes every 1 second...
echo.

:monitor
set "change=0"

if %checkHtml%==1 (
    for /f "tokens=1,*" %%a in ('certutil -hashfile "%htmlFile%" SHA256 ^| findstr /v /c:"hash" /c:"CertUtil"') do set "newh=%%a%%b"
    if not "%hhash%"=="%newh%" (
        set "change=1"
        set "hhash=%newh%"
    )
)

if %checkServer%==1 (
    for /f "tokens=1,*" %%a in ('certutil -hashfile "%jsFile%" SHA256 ^| findstr /v /c:"hash" /c:"CertUtil"') do set "news=%%a%%b"
    if not "%shash%"=="%news%" (
        set "change=1"
        set "shash=%news%"
    )
)

if %change%==0 (
    timeout /t 1 >nul
    goto monitor
)

REM ======= Deployment =======
echo [%time%] Changes detected! Deploying...
git add .
git commit -m "Auto-update" >nul 2>&1
git pull origin main --rebase --quiet
git push origin main --quiet

if %errorlevel% neq 0 (
    echo [%time%] Push failed! Resolve conflicts manually.
    pause
    exit /b
)

REM ======= Open Render link =======
echo [%time%] Deployed! Opening Render link...
start "" "https://online-chat-1-dd3k.onrender.com"

REM ======= Notification beep =======
powershell -c "[console]::beep(800,200); Start-Sleep -Milliseconds 100; [console]::beep(1000,200)"

echo.
echo [%time%] Monitoring for more changes...
timeout /t 1 >nul
goto monitor
