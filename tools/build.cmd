@echo off

taskkill /f /im banner.exe
taskkill /f /im dotnet.exe

REM Set flags for Windows, System.Drawing, System.Windows.Forms, System.Management
set USE_WINDOWS=true
set USE_SYSTEMDRAWING=true
set USE_SYSTEMWINDOWSFORMS=true
set USE_SYSTEMMANAGEMENT=true

REM Optionally, set other flags as needed
REM set USE_NEWTONSOFTJSON=true
REM set USE_SYSTEMTEXTJSON=true

REM Build the solution in Release mode
dotnet publish "P:\Visual Studio\source\repos\Blucream.Common\Blucream.Common.sln" -c Release ^
    /p:USE_WINDOWS=%USE_WINDOWS% ^
    /p:USE_SYSTEMDRAWING=%USE_SYSTEMDRAWING% ^
    /p:USE_SYSTEMWINDOWSFORMS=%USE_SYSTEMWINDOWSFORMS% ^
    /p:USE_SYSTEMMANAGEMENT=%USE_SYSTEMMANAGEMENT%

if %ERRORLEVEL% neq 0 (
    echo Bluscream.Common Build failed!
    exit /b %ERRORLEVEL%
) else (
    echo Bluscream.Common Build succeeded!
)

REM Build notification-banner.sln in Release mode
dotnet publish "P:\Visual Studio\source\repos\notification-banner\notification-banner.sln" -c Release

if %ERRORLEVEL% neq 0 (
    echo NotificationBanner build failed!
    exit /b %ERRORLEVEL%
) else (
    echo NotificationBanner build succeeded!
)

start "" dotnet run -- -console

echo Running dotnet processes:
tasklist /FI "IMAGENAME eq dotnet.exe"

echo Running banner processes:
tasklist /FI "IMAGENAME eq banner.exe"
