@echo off
setlocal
set "HERE=%~dp0"
set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Obenseuer"
set "MANAGED=%GAME%\Obenseuer_Data\Managed"
set "BEPINEX=%GAME%\BepInEx\core"
set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%HERE%..\..\bin\ObenseuerCore.dll" (
  echo ERROR: build the Core first, "%HERE%..\..\bin\ObenseuerCore.dll" is missing
  exit /b 1
)
if exist "%HERE%bin\web" rd /s /q "%HERE%bin\web"
"%SystemRoot%\System32\xcopy.exe" /e /i /q /y "%HERE%web" "%HERE%bin\web\" >nul
if errorlevel 1 exit /b 1
"%CSC%" /noconfig /nologo /nostdlib+ /target:library /optimize+ /warnaserror+ /langversion:5 /out:"%HERE%bin\WebUiHello.dll" ^
 /r:"%MANAGED%\mscorlib.dll" /r:"%MANAGED%\netstandard.dll" /r:"%MANAGED%\System.dll" /r:"%MANAGED%\System.Core.dll" ^
 /r:"%MANAGED%\UnityEngine.dll" /r:"%MANAGED%\UnityEngine.CoreModule.dll" /r:"%MANAGED%\UnityEngine.InputLegacyModule.dll" ^
 /r:"%BEPINEX%\BepInEx.dll" /r:"%HERE%..\..\bin\ObenseuerCore.dll" ^
 "%HERE%WebUiHello.cs"
exit /b %ERRORLEVEL%
