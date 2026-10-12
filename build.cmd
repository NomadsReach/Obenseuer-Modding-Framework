@echo off
setlocal
cd /d "%~dp0"
set "GAME=C:\Program Files (x86)\Steam\steamapps\common\Obenseuer"
set "MANAGED=%GAME%\Obenseuer_Data\Managed"
set "BEPINEX=%GAME%\BepInEx\core"
set "CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
call "%~dp0tools\Publicize\build.cmd"
if errorlevel 1 exit /b 1
"%~dp0tools\Publicize\bin\Publicize.exe" "%MANAGED%\Assembly-CSharp.dll" "%~dp0lib"
if errorlevel 1 exit /b 1
call "%~dp0native\ObxWebView\build.cmd"
if errorlevel 1 exit /b 1
if not exist "%~dp0bin" mkdir "%~dp0bin"
copy /y "%~dp0native\ObxWebView\bin\ObxWebView.dll" "%~dp0bin\" >nul
if errorlevel 1 exit /b 1
if exist "%~dp0bin\web" rd /s /q "%~dp0bin\web"
"%SystemRoot%\System32\xcopy.exe" /e /i /q /y "%~dp0web" "%~dp0bin\web\" >nul
if errorlevel 1 exit /b 1
for /f "usebackq delims=" %%L in (`%PS% -NoProfile -Command "$m=[regex]::Match((Get-Content -Raw -LiteralPath '%~dp0native\ObxWebView\build.cmd'),'WV2=([^\x22]+)'); $p=[Environment]::ExpandEnvironmentVariables($m.Groups[1].Value); Split-Path -Parent (Split-Path -Parent $p)"`) do set "WV2PKG=%%L"
copy /y "%WV2PKG%\LICENSE.txt" "%~dp0bin\ThirdPartyNotices-WebView2.txt" >nul
if errorlevel 1 exit /b 1
"%CSC%" /noconfig /nologo /nostdlib+ /target:library /optimize+ /langversion:5 /out:"%~dp0bin\ObenseuerCore.dll" ^
 /r:"%MANAGED%\mscorlib.dll" /r:"%MANAGED%\netstandard.dll" /r:"%MANAGED%\System.dll" /r:"%MANAGED%\System.Core.dll" ^
 /r:"%MANAGED%\UnityEngine.dll" /r:"%MANAGED%\UnityEngine.CoreModule.dll" /r:"%MANAGED%\UnityEngine.PhysicsModule.dll" /r:"%MANAGED%\UnityEngine.JSONSerializeModule.dll" /r:"%MANAGED%\UnityEngine.UI.dll" /r:"%MANAGED%\UnityEngine.IMGUIModule.dll" /r:"%MANAGED%\UnityEngine.InputLegacyModule.dll" /r:"%MANAGED%\UnityEngine.UIModule.dll" /r:"%MANAGED%\UnityEngine.AnimationModule.dll" /r:"%MANAGED%\Unity.TextMeshPro.dll" /r:"%MANAGED%\UnityEngine.TextRenderingModule.dll" /r:"%MANAGED%\UnityEngine.AudioModule.dll" /r:"%MANAGED%\UnityEngine.AIModule.dll" /r:"%MANAGED%\UnityEngine.ImageConversionModule.dll" /r:"%MANAGED%\AstarPathfindingProject.dll" ^
 /r:"%MANAGED%\Assembly-CSharp-firstpass.dll" /r:"%~dp0lib\Assembly-CSharp.publicized.dll" ^
 /r:"%BEPINEX%\BepInEx.dll" /r:"%BEPINEX%\0Harmony.dll" ^
 "src\Core.cs" "src\AccessProbe.cs" "src\CoreLog.cs" "src\CorePatcher.cs" "src\Events.cs" "src\GameTime.cs" "src\Wallet.cs" "src\SaveData.cs" ^
 "src\Survival.cs" "src\SurvivalStat.cs" "src\SurvivalDecay.cs" "src\SurvivalConsumables.cs" "src\SurvivalPatches.cs" ^
 "src\Tweaks.cs" "src\TweaksNeeds.cs" "src\TweaksStacks.cs" "src\TweaksSpoilage.cs" "src\TweaksCrafting.cs" "src\TweaksDishwasher.cs" "src\TweaksRenovations.cs" "src\TweaksTime.cs" "src\CameraView.cs" "src\TweaksCamera.cs" "src\CoreInput.cs" "src\TweakState.cs" ^
 "src\ModMenu.cs" "src\ModMenuWindow.cs" "src\ModMenuRows.cs" "src\ModMenuTab.cs" "src\ModMenuMainMenu.cs" "src\ModMenuWidgets.cs" "src\ModMenuInput.cs" "src\ModMenuJson.cs" "src\ModMenuWebKeys.cs" "src\ModMenuWebModel.cs" "src\ModMenuWebConfig.cs" "src\ModMenuWebHost.cs" "src\ConfigTheme.cs" "src\ComputerApps.cs" "src\ComputerAppsDesk.cs" "src\PlayerLook.cs" "src\PlayerLookTable.cs" ^
 "src\WebUi.cs" "src\WebView.cs" "src\WebUiNative.cs" ^
 "src\Ui\UiTheme.cs" "src\Ui\UiSprites.cs" "src\Ui\ModWindow.cs" "src\Ui\ModUi.cs" "src\Ui\ModControls.cs" "src\Ui\ModInput.cs" "src\Ui\ModPickers.cs" "src\Ui\ModTabs.cs" "src\Ui\ModelPreview.cs" ^
 /recurse:"src\Story\*.cs" /recurse:"src\Api\*.cs"
exit /b %ERRORLEVEL%
