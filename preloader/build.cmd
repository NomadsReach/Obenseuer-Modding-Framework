@echo off
setlocal
set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Obenseuer"
set "MANAGED=%GAME%\Obenseuer_Data\Managed"
set "BEPINEX=%GAME%\BepInEx\core"
set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%~dp0bin" mkdir "%~dp0bin"
"%CSC%" /noconfig /nologo /nostdlib+ /langversion:5 /target:library /optimize+ /nowarn:649 /out:"%~dp0bin\ObenseuerCore.Preloader.dll" ^
 /r:"%MANAGED%\mscorlib.dll" /r:"%MANAGED%\netstandard.dll" /r:"%MANAGED%\System.dll" /r:"%MANAGED%\System.Core.dll" ^
 /r:"%BEPINEX%\BepInEx.dll" /r:"%BEPINEX%\Mono.Cecil.dll" ^
 "%~dp0ObenseuerCore.Preloader.cs"
exit /b %ERRORLEVEL%
