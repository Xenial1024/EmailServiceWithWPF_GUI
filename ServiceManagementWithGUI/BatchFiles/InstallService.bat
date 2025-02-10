@echo off
setlocal enabledelayedexpansion

:: Ustawienia
set SERVICE_NAME=ReportService
set "SERVICE_FILE=%~dp0..\..\ReportService\bin\Debug\ReportService.exe"

:: Zmienna do przechowywania ścieżki do InstallUtil
set INSTALL_UTIL=

:: Znalezienie najnowszego InstallUtil.exe
for /f "delims=" %%I in ('dir /b /ad /o-n "%WINDIR%\Microsoft.NET\Framework\v*"') do (
    set INSTALL_UTIL=%WINDIR%\Microsoft.NET\Framework\%%I\InstallUtil.exe
    if exist "!INSTALL_UTIL!" goto Install
)

exit /b 1

:Install
:: Instalacja usługi
"!INSTALL_UTIL!" "%SERVICE_FILE%" >nul || exit /b %errorlevel%

:: Nadawanie uprawnień do włączenia usługi
sc config ReportService obj= "LocalSystem" >nul

endlocal