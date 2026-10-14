@echo off
setlocal
for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "DLL=%ROOT%\bin\ObenseuerCore.dll"
set "WEBUI=%ROOT%\bin\webui"
set "DLL5=%ROOT%\bin\be5\ObenseuerCore.dll"
set "WEBUI5=%ROOT%\bin\be5\webui"
set "CORE=%ROOT%\src\Core.cs"
set "STAGE=%ROOT%\dist\stage"
set "VERSION=%~1"
set "SUFFIX=%~2"
if "%SUFFIX%"=="" set "SUFFIX=-beta"
set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"

if not exist "%DLL%" (
  echo ERROR: "%DLL%" not found. Run dotnet build ObenseuerCore.slnx -c Release first.
  exit /b 1
)
if not exist "%WEBUI%\ObenseuerCore.WebUi.dll" (
  echo ERROR: "%WEBUI%\ObenseuerCore.WebUi.dll" not found. Run dotnet build ObenseuerCore.slnx -c Release first.
  exit /b 1
)
if not exist "%DLL5%" (
  echo ERROR: "%DLL5%" not found. Run dotnet build ObenseuerCore.slnx -c Release -p:Loader=BepInEx5 first.
  exit /b 1
)
if not exist "%WEBUI5%\ObenseuerCore.WebUi.dll" (
  echo ERROR: "%WEBUI5%\ObenseuerCore.WebUi.dll" not found. Run dotnet build ObenseuerCore.slnx -c Release -p:Loader=BepInEx5 first.
  exit /b 1
)
if "%VERSION%"=="" for /f "usebackq delims=" %%V in (`%PS% -NoProfile -Command "[regex]::Match((Get-Content -Raw -LiteralPath '%CORE%'),'BepInPlugin\(\x22obenseuer\.moddingframework\x22,\s*\x22[^\x22]*\x22,\s*\x22([^\x22]+)\x22').Groups[1].Value"`) do set "VERSION=%%V"
if "%VERSION%"=="" (
  echo ERROR: no version. Pass it as the first argument, or check the BepInPlugin attribute in "%CORE%".
  exit /b 1
)

set "ZIP=%ROOT%\dist\ObenseuerModdingFramework-%VERSION%%SUFFIX%.zip"
set "PLUGIN=%STAGE%\BepInEx\plugins\ObenseuerCore"
if exist "%STAGE%" rd /s /q "%STAGE%"
mkdir "%PLUGIN%" || goto fail
copy /y "%DLL%" "%PLUGIN%\" >nul || goto fail
copy /y "%DLL5%" "%PLUGIN%\ObenseuerCore.BepInEx5.dll" >nul || goto fail
copy /y "%ROOT%\LICENSE" "%PLUGIN%\" >nul || goto fail
copy /y "%ROOT%\README.md" "%PLUGIN%\" >nul || goto fail
"%SystemRoot%\System32\tar.exe" -a -c -f "%ZIP%" -C "%STAGE%" BepInEx || goto fail
rd /s /q "%STAGE%"
echo Wrote "%ZIP%"

set "WZIP=%ROOT%\dist\ObenseuerModdingFramework-WebUi-%VERSION%%SUFFIX%.zip"
set "WPLUGIN=%STAGE%\BepInEx\plugins\ObenseuerCore.WebUi"
mkdir "%WPLUGIN%" || goto fail
copy /y "%WEBUI%\ObenseuerCore.WebUi.dll" "%WPLUGIN%\" >nul || goto fail
copy /y "%WEBUI5%\ObenseuerCore.WebUi.dll" "%WPLUGIN%\ObenseuerCore.WebUi.BepInEx5.dll" >nul || goto fail
copy /y "%WEBUI%\ObxWebView.dll" "%WPLUGIN%\" >nul || goto fail
copy /y "%WEBUI%\ThirdPartyNotices-WebView2.txt" "%WPLUGIN%\" >nul || goto fail
copy /y "%ROOT%\LICENSE" "%WPLUGIN%\" >nul || goto fail
mkdir "%WPLUGIN%\web" || goto fail
"%SystemRoot%\System32\xcopy.exe" /e /i /q /y "%WEBUI%\web" "%WPLUGIN%\web\" >nul || goto fail
"%SystemRoot%\System32\tar.exe" -a -c -f "%WZIP%" -C "%STAGE%" BepInEx || goto fail
rd /s /q "%STAGE%"
echo Wrote "%WZIP%"
exit /b 0

:fail
if exist "%STAGE%" rd /s /q "%STAGE%"
echo ERROR: packaging failed.
exit /b 1
