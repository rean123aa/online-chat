@echo off
title Opening Chat App – Auto Deploy Monitor

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
REM ==========================================

REM Project folder
set "folder=%~dp0"

REM Remove any old temporary files
del /q "%folder%*.tmp" >nul 2>&1

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

REM Open files in Cursor
start "" "%cursorPath%" "%htmlFile%" >nul 2>&1
start "" "%cursorPath%" "%jsFile%" >nul 2>&1

echo.
echo ==============================
echo   Both files are open in Cursor.
echo ==============================
echo.

REM Ask which files to monitor
:ask
echo Did you modify only HTML, only server.js, or both? (type html / server / both)
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
    echo Invalid choice. Please type html, server, or both.
    goto ask
)

REM Generate initial hashes
if %checkHtml%==1 certutil -hashfile "%htmlFile%" SHA256 >"%folder%htmlhash.tmp"
if %checkServer%==1 certutil -hashfile "%jsFile%" SHA256 >"%folder%jshash.tmp"

echo Monitoring selected files every 5 seconds...
echo.

:monitor
set "changed=0"

if %checkHtml%==1 (
    certutil -hashfile "%htmlFile%" SHA256 >"%folder%htmlhash_new.tmp"
    fc /b "%folder%htmlhash.tmp" "%folder%htmlhash_new.tmp" >nul
    if %errorlevel% neq 0 (
        set "changed=1"
        copy /y "%folder%htmlhash_new.tmp" "%folder%htmlhash.tmp" >nul
    )
)

if %checkServer%==1 (
    certutil -hashfile "%jsFile%" SHA256 >"%folder%jshash_new.tmp"
    fc /b "%folder%jshash.tmp" "%folder%jshash_new.tmp" >nul
    if %errorlevel% neq 0 (
        set "changed=1"
        copy /y "%folder%jshash_new.tmp" "%folder%jshash.tmp" >nul
    )
)

if %changed%==0 (
    timeout /t 5 >nul
    goto monitor
)

echo [%time%] Changes detected! Deploying...

REM Git deployment
git add .
git commit -m "Auto-update chat files" >nul 2>&1
git pull origin main --rebase --quiet
git push origin main --quiet

if %errorlevel% neq 0 (
    echo [%time%] Push failed! Resolve conflicts manually.
    pause
    exit /b
)

echo [%time%] Deployment complete!
echo [%time%] Render link: https://online-chat-1-dd3k.onrender.com

REM Notification beep
powershell -c "[console]::beep(800,300); Start-Sleep -Milliseconds 100; [console]::beep(1000,300)"

REM Clean temporary files after deploy
del /q "%folder%*.tmp" >nul 2>&1

echo.
echo [%time%] Monitoring for more changes...
timeout /t 5 >nul
goto monitor
