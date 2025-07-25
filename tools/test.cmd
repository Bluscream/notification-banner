@echo off
echo Testing notification-banner with various argument combinations...
echo.

set EXE_NAME=banner.dll
set EXE_PATH=bin\Debug\net8.0-windows10.0.19041.0\%EXE_NAME%

set START_CMD=start "" dotnet %EXE_PATH%
set START_CMD_MIN=start "" /min dotnet %EXE_PATH%
set DEFAULT_TIME=1

REM Check if /build argument was given
set BUILD_ARG=false
for %%A in (%*) do (
    if /I "%%A"=="/build" (
        call tools\build.cmd
        if %ERRORLEVEL% neq 0 (
            exit /b 1
        )
    )
)

REM Allow user to jump to a specific test by passing /test:X or --test:X or -test:X
for %%A in (%*) do (
    if /I "%%A"=="/test:1" set "TEST_JUMP=1"
    if /I "%%A"=="/test:3b" set "TEST_JUMP=3b"
    if /I "%%A"=="/test:3c" set "TEST_JUMP=3c"
    if /I "%%A"=="/test:3d" set "TEST_JUMP=3d"
    if /I "%%A"=="/test:3e" set "TEST_JUMP=3e"
    if /I "%%A"=="/test:3f" set "TEST_JUMP=3f"
    if /I "%%A"=="/test:3g" set "TEST_JUMP=3g"
    if /I "%%A"=="/test:5" set "TEST_JUMP=5"
    if /I "%%A"=="/test:6" set "TEST_JUMP=6"
    if /I "%%A"=="/test:7" set "TEST_JUMP=7"
    if /I "%%A"=="/test:7b" set "TEST_JUMP=7b"
    if /I "%%A"=="/test:7c" set "TEST_JUMP=7c"
    if /I "%%A"=="/test:8" set "TEST_JUMP=8"
    if /I "%%A"=="/test:9" set "TEST_JUMP=9"
    if /I "%%A"=="/test:10" set "TEST_JUMP=10"
    if /I "%%A"=="/test:11" set "TEST_JUMP=11"
    if /I "%%A"=="/test:11b" set "TEST_JUMP=11b"
    if /I "%%A"=="/test:12" set "TEST_JUMP=12"
    if /I "%%A"=="/test:13" set "TEST_JUMP=13"
    if /I "%%A"=="/test:13b" set "TEST_JUMP=13b"
    if /I "%%A"=="/test:14" set "TEST_JUMP=14"
    if /I "%%A"=="/test:14a" set "TEST_JUMP=14a"
    if /I "%%A"=="/test:14b" set "TEST_JUMP=14b"
    if /I "%%A"=="/test:14c" set "TEST_JUMP=14c"
    if /I "%%A"=="/test:14d" set "TEST_JUMP=14d"
    if /I "%%A"=="/test:14e" set "TEST_JUMP=14e"
    if /I "%%A"=="/test:15" set "TEST_JUMP=15"
    if /I "%%A"=="/test:15b" set "TEST_JUMP=15b"
    if /I "%%A"=="/test:16" set "TEST_JUMP=16"
    if /I "%%A"=="/test:16b" set "TEST_JUMP=16b"
    if /I "%%A"=="/test:16c" set "TEST_JUMP=16c"
    if /I "%%A"=="/test:16d" set "TEST_JUMP=16d"
    if /I "%%A"=="/test:17" set "TEST_JUMP=17"
    if /I "%%A"=="/test:18" set "TEST_JUMP=18"
    if /I "%%A"=="/test:19" set "TEST_JUMP=19"
)

REM Jump to the requested test if specified
if defined TEST_JUMP (
    echo Jumping to test %TEST_JUMP%...
    goto TEST_%TEST_JUMP%
)

:TEST_1
REM Test 1: Basic notification
echo Test 1: Basic notification
%START_CMD% --message "This is a basic test notification" --title "Basic Test" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_3b
echo Test 3b: Top-center position
%START_CMD_MIN% --message "Top-center notification" --title "Position Test" --position topcenter --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul

:TEST_3c
echo Test 3c: Top-right position
%START_CMD_MIN% --message "Top-right notification" --title "Position Test" --position topright --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul

:TEST_3d
echo Test 3d: Bottom-left position
%START_CMD_MIN% --message "Bottom-left notification" --title "Position Test" --position bottomleft --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul

:TEST_3e
echo Test 3e: Bottom-center position
%START_CMD_MIN% --message "Bottom-center notification" --title "Position Test" --position bottomcenter --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul

:TEST_3f
echo Test 3f: Bottom-right position
%START_CMD_MIN% --message "Bottom-right notification" --title "Position Test" --position bottomright --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul

:TEST_3g
echo Test 3g: Center position
%START_CMD_MIN% --message "Center notification" --title "Position Test" --position center --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_5
REM Test 5: Long messages
echo Test 5: Long message test
%START_CMD_MIN% --message "This is a very long notification message that should wrap to multiple lines and test how the banner handles longer text content" --title "Long Message Test" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_6
REM Test 6: Special characters (simplified)
echo Test 6: Special characters test
%START_CMD_MIN% --message "Special chars: !@#^$%%^&*()_+-=[]{}^|;':^\",./^<^>?" --title "Special Chars" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_7
REM Test 7: Unicode characters
echo Test 7: Unicode characters test
%START_CMD_MIN% --message "Unicode: 你好世界 🌍 🚀 ⭐" --title "Unicode Test" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_7b
REM Test 7b: Base64 encoded image
echo Test 7b: Base64 encoded image test
set /p BASE64_IMAGE=<tools\test.image.b64
%START_CMD_MIN% --message "This notification has a base64 encoded image" --title "Base64 Image Test" --image "%BASE64_IMAGE%" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_7c
REM Test 7c: Image URL test
echo Test 7c: Image URL test
%START_CMD_MIN% --message "This notification has an image from URL" --title "Image URL Test" --image "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/assets/Info/SVG/ic_fluent_info_24_regular.svg" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_8
REM Test 8: Multiple notifications (should queue)
echo Test 8: Multiple notifications (should queue)
%START_CMD_MIN% --message "First notification" --title "Queue Test 1" --time %DEFAULT_TIME%
timeout /t 1 /nobreak >nul
%START_CMD_MIN% --message "Second notification" --title "Queue Test 2" --time %DEFAULT_TIME%
timeout /t 1 /nobreak >nul
%START_CMD_MIN% --message "Third notification" --title "Queue Test 3" --time %DEFAULT_TIME%
timeout /t 5 /nobreak >nul
echo.

:TEST_9
REM Test 9: Command line arguments (positional)
echo Test 9: Positional arguments
%START_CMD_MIN% "This is a message" "This is a title" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_10
REM Test 10: Minimal arguments
echo Test 10: Minimal arguments (just message)
%START_CMD_MIN% --message "Minimal notification" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_11
REM Test 11: Different time formats
echo Test 11: Very short timeout
%START_CMD_MIN% --message "Very short notification" --title "Short Timeout" --time 1
timeout /t 2 /nobreak >nul

:TEST_11b
echo Test 11b: Long timeout
%START_CMD_MIN% --message "Long timeout notification" --title "Long Timeout" --time 15
timeout /t 5 /nobreak >nul
echo.

:TEST_12
REM Test 12: Error cases
echo Test 12: No message (should show error)
%START_CMD_MIN% --title "No Message Test" --time %DEFAULT_TIME%
timeout /t 2 /nobreak >nul
echo.

:TEST_14
REM Test 14: Color and opacity tests
echo Test 14: Color
%START_CMD_MIN% --message "Some color" --title "Color Test" --color "#AABBCC" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_14a
REM Test 14: Color and opacity tests
echo Test 14a: Fully opaque black
%START_CMD_MIN% --message "Opaque black" --title "Color Test" --color "#FF000000" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_14b
echo Test 14b: Fully opaque white
%START_CMD_MIN% --message "Opaque white" --title "Color Test" --color "#FFFFFFFF" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_14c
echo Test 14c: 50% transparent red
%START_CMD_MIN% --message "50%% transparent red" --title "Color Test" --color "#80FF0000" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_14d
echo Test 14d: 25% transparent green
%START_CMD_MIN% --message "25%% transparent green" --title "Color Test" --color "#40FF00FF" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_14e
echo Test 14e: Fully transparent (should be invisible)
%START_CMD_MIN% --message "Fully transparent" --title "Color Test" --color "#00000000" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_15
REM Test 15: Sound tests
echo Test 15: Sound from local file (if available)
if exist "C:\Windows\Media\chimes.wav" (
    %START_CMD_MIN% --message "Playing chimes sound" --title "Sound Test" --sound "C:\Windows\Media\chimes.wav" --time %DEFAULT_TIME%
) else (
    echo Note: No test WAV file found at C:\Windows\Media\chimes.wav
    %START_CMD_MIN% --message "Sound test (no file)" --title "Sound Test" --time %DEFAULT_TIME%
)
timeout /t 3 /nobreak >nul
echo.

:TEST_15b
echo Test 15b: Sound from URL (if available)
%START_CMD_MIN% --message "Sound from URL test" --title "Sound URL Test" --sound "https://www.soundjay.com/misc/sounds/bell-ringing-05.wav" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_16
REM Test 16: Size scaling tests
echo Test 16: Large size (150%%)
%START_CMD_MIN% --message "Large notification" --title "Size Test" --size 150 --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_16b
echo Test 16b: Small size (75%%)
%START_CMD_MIN% --message "Small notification" --title "Size Test" --size 75 --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_16c
echo Test 16c: Extra large size (200%%)
%START_CMD_MIN% --message "Extra large notification" --title "Size Test" --size 200 --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_16d
echo Test 16d: Tiny size (50%%)
%START_CMD_MIN% --message "Tiny notification" --title "Size Test" --size 50 --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_17
REM Test 17: Primary screen test
echo Test 17: Primary screen notification
%START_CMD_MIN% --message "This should appear on the primary screen" --title "Primary Screen Test" --primary --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_18
REM Test 18: Important notification (should show even in Do Not Disturb mode)
echo Test 18: Important notification test
%START_CMD_MIN% --message "This is an important notification that should show even in Do Not Disturb mode" --title "Important Test" --important --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_19
REM Test 19: Regular notification (should be blocked in Do Not Disturb mode)
echo Test 19: Regular notification test (may be blocked if Do Not Disturb is active)
%START_CMD_MIN% --message "This is a regular notification that should be blocked in Do Not Disturb mode" --title "Regular Test" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:TEST_13
@REM echo Test 13: Overfill the notification queue (should skip some notifications)
@REM for /L %%i in (1,1,120) do (
@REM     %START_CMD_MIN% --message "Notification %%i" --title "Queue Overfill Test" --time 1
@REM )
@REM timeout /t 5 /nobreak >nul
@REM echo.

:TEST_14
echo Test 14: Exit after notification (should exit immediately)
%START_CMD_MIN% --message "This notification should exit the app" --title "Exit Test" --exit --time %DEFAULT_TIME%
timeout /t 2 /nobreak >nul
echo.

:TEST_14b
echo Test 14b: Notification after exit
%START_CMD_MIN% --message "this is after exit" --title "After Exit Test" --time %DEFAULT_TIME%
timeout /t 3 /nobreak >nul
echo.

:END
echo All tests completed!
echo.

@REM pause
call tools\kill.cmd

goto :EOF