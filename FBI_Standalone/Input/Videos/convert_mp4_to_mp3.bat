@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul

where ffmpeg >nul 2>nul
if errorlevel 1 (
    echo [ERREUR] ffmpeg n'est pas installe, ou pas accessible dans le PATH.
    echo.
    echo Installe-le avec la commande suivante dans un terminal :
    echo     winget install ffmpeg
    echo puis relance ce script.
    echo.
    pause
    exit /b 1
)

if "%~1"=="" goto convertfolder

:convertdropped
echo Fichier(s) glisse-depose detecte(s).
for %%f in (%*) do (
    call :convert "%%~f"
)
goto end

:convertfolder
echo Aucun fichier glisse-depose : conversion de tous les .mp4 du dossier de ce script.
echo.
set found=0
for %%f in ("%~dp0*.mp4") do (
    set found=1
    call :convert "%%~f"
)
if "!found!"=="0" (
    echo Aucun fichier .mp4 trouve dans ce dossier.
)
goto end

:convert
echo Conversion de "%~nx1"...
ffmpeg -y -i "%~1" -vn -acodec libmp3lame -q:a 2 "%~dpn1.mp3" -loglevel error
if errorlevel 1 (
    echo   -^> Echec pour "%~nx1"
) else (
    echo   -^> OK : "%~n1.mp3"
)
exit /b 0

:end
echo.
echo ===============================
echo Termine !
echo ===============================
pause
