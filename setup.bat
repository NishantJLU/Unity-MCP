@echo off
echo ==========================================
echo    Unity-MCP Pro: One-Click Installer
echo ==========================================
echo.

echo [1/2] Installing MCP Server dependencies...
cd unity-mcp-server
call npm install
if %errorlevel% neq 0 (
    echo.
    echo ERROR: Failed to install server dependencies. Make sure Node.js is installed.
    pause
    exit /b %errorlevel%
)
cd ..

echo.
echo [2/2] Generating your custom configuration...
node get-config.js

echo.
echo ==========================================
echo SETUP COMPLETE!
echo Follow the "HOW TO USE" steps in the README.
echo ==========================================
pause
