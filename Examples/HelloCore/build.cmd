@echo off
setlocal
set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Obenseuer"
set "MANAGED=%GAME%\Obenseuer_Data\Managed"
set "BEPINEX=%GAME%\BepInEx\core"
set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%~dp0bin" mkdir "%~dp0bin"
"%CSC%" /noconfig /nologo /nostdlib+ /target:library /optimize+ /langversion:5 /out:"%~dp0bin\HelloCore.dll" ^
 /r:"%MANAGED%\mscorlib.dll" /r:"%MANAGED%\netstandard.dll" /r:"%MANAGED%\System.dll" /r:"%MANAGED%\System.Core.dll" ^
 /r:"%MANAGED%\UnityEngine.dll" /r:"%MANAGED%\UnityEngine.CoreModule.dll" ^
 /r:"%BEPINEX%\BepInEx.dll" /r:"%~dp0..\..\bin\ObenseuerCore.dll" ^
 "%~dp0HelloCore.cs"
exit /b %ERRORLEVEL%
