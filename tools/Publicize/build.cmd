@echo off
setlocal
set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Obenseuer"
set "BEPINEX=%GAME%\BepInEx\core"
set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%~dp0bin" mkdir "%~dp0bin"
copy /Y "%BEPINEX%\Mono.Cecil.dll" "%~dp0bin\" >nul
if errorlevel 1 exit /b 1
"%CSC%" /nologo /target:exe /optimize+ /out:"%~dp0bin\Publicize.exe" /r:"%BEPINEX%\Mono.Cecil.dll" "%~dp0Publicize.cs"
exit /b %ERRORLEVEL%
