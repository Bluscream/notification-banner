@echo off
echo Building notification-banner as single executable...

call tools\build.cmd

REM Build as framework-dependent single file (smaller, requires .NET runtime)
echo.
echo Option 1: Framework-dependent single file (requires .NET runtime)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
if %ERRORLEVEL% equ 0 (
    echo.
    echo Success! Single file created at: bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\banner.exe
    echo Size: ~24MB (requires .NET 8.0 runtime)
    copy /Y "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\banner.exe" "bin\banner.exe" >nul 2>&1
) else (
    echo Build failed! %ERRORLEVEL%
)

echo.
echo Option 2: Self-contained single file (larger, no runtime required)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if %ERRORLEVEL% equ 0 (
    echo.
    echo Success! Self-contained single file created at: bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\banner.exe
    echo Size: ~171MB (includes .NET runtime)
    copy /Y "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\banner.exe" "bin\banner.standalone.exe"
) else (
    echo Self-contained build failed! %ERRORLEVEL%
)

echo.
echo Build complete! Check the bin directory for your executables.
pause 