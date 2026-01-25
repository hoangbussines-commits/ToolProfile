@echo off
chcp 65001 >nul
echo ========================================
echo   TOOLPROFILE PUBLISH - FIXED
echo ========================================
echo.

REM Clean
echo Cleaning...
dotnet clean

REM Publish WITHOUT trimming
echo Publishing...
dotnet publish -c Release ^
-r win-x64 ^
--self-contained true ^
-p:PublishSingleFile=true ^
-p:IncludeNativeLibrariesForSelfExtract=true ^
-p:DebugType=None ^
-p:DebugSymbols=false ^
--output ".\Publish"

echo.
echo ========================================
echo   DONE!
echo ========================================
echo.
echo Output: %cd%\Publish
pause