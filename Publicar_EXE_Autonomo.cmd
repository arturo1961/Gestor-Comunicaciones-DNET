@echo off
chcp 65001 >nul
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Falta el SDK de .NET 8.
  pause
  exit /b 1
)
dotnet publish GestorComunicacionesDNET.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -o publicar
if errorlevel 1 (
  echo ERROR de publicacion.
  pause
  exit /b 1
)
echo.
echo EXE creado en publicar\GestorComunicacionesDNET.exe
pause
