@echo off
sc query ReportService | findstr /I "RUNNING" >nul
if %errorlevel%==0 sc stop ReportService >nul
sc delete ReportService >nul