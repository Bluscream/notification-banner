@echo off
echo Testing Multi-Notification System
echo ================================

echo.
echo Sending 4 notifications to test spacing...
echo.

curl "http://localhost:14969/?message=First%20Notification&title=Test%201&time=8&max-notifications=4"
timeout /t 1 /nobreak >nul

curl "http://localhost:14969/?message=Second%20Notification&title=Test%202&time=8"
timeout /t 1 /nobreak >nul

curl "http://localhost:14969/?message=Third%20Notification&title=Test%203&time=8"
timeout /t 1 /nobreak >nul

curl "http://localhost:14969/?message=Fourth%20Notification&title=Test%204&time=8"
timeout /t 1 /nobreak >nul

echo.
echo All notifications sent! You should see 4 notifications spaced apart.
echo.
echo Testing bottom positioning...
echo.

curl "http://localhost:14969/?message=Bottom%20Test%201&title=Bottom%201&time=8&position=bottomright"
timeout /t 1 /nobreak >nul

curl "http://localhost:14969/?message=Bottom%20Test%202&title=Bottom%202&time=8&position=bottomright"
timeout /t 1 /nobreak >nul

echo.
echo Test completed!
pause 