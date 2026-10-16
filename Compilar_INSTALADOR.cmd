@echo off
setlocal
cd /d "%~dp0"

echo Publicando Gestor de Comunicaciones DNET 1.10.1...
dotnet publish GestorComunicacionesDNET.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -o publicar-v1.10.1
if errorlevel 1 goto :error

set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 7\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 7\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"

if not exist "%ISCC%" (
  echo No se encontro Inno Setup 6 o 7.
  echo Descargalo desde https://jrsoftware.org/isdl.php
  pause
  exit /b 2
)

echo Creando el instalador multilingue...
"%ISCC%" Instalador\GestorComunicacionesDNET.iss
if errorlevel 1 goto :error

echo.
echo Instalador terminado en instalador-v1.10.1\Instalar_GestorComunicacionesDNET_1.10.1.exe
pause
exit /b 0

:error
echo.
echo No se pudo crear el instalador.
pause
exit /b 1
