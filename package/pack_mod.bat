@echo off
setlocal enabledelayedexpansion

:: --- CONFIGURATION ---
set MOD_NAME=PirMod
set DLL_PATH=PirMod.dll
:: (Adjust the DLL_PATH above to where your actual compiled DLL is)

:: --- EXTRACT VERSION FROM MANIFEST.JSON ---
if not exist manifest.json (
    echo Error: manifest.json not found!
    pause
    exit /b
)

:: Changed tokens=4 to tokens=2 to correctly grab the value after the colon
for /f "tokens=2 delims=:," %%a in ('findstr "version_number" manifest.json') do (
    set VERSION=%%a
    set VERSION=!VERSION:"=!
    set VERSION=!VERSION: =!
)

if "!VERSION!"=="" (
    echo Error: Could not detect version_number in manifest.json
    pause
    exit /b
)

set ZIP_NAME=!MOD_NAME!-!VERSION!.zip

echo Detected Version: !VERSION!
echo Creating !ZIP_NAME!...

:: --- ZIP PROCESS ---
powershell -Command "Compress-Archive -Path 'manifest.json', 'README.md', 'CHANGELOG.md', 'icon.png', '%DLL_PATH%' -DestinationPath '!ZIP_NAME!' -Force"

if %ERRORLEVEL% equ 0 (
    echo.
    echo Successfully created !ZIP_NAME!
) else (
    echo.
    echo Error: Failed to create zip file.
)

pause