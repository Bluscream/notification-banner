@echo off
set EXE_NAME=banner.exe
set DLL_NAME=banner.dll

taskkill /F /IM "%EXE_NAME%" >nul 2>&1
taskkill /F /IM "%DLL_NAME%" >nul 2>&1
taskkill /F /IM "dotnet.exe" >nul 2>&1