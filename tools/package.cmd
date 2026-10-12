@echo off
setlocal
for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "DLL=%ROOT%\bin\ObenseuerCore.dll"
set "CORE=%ROOT%\src\Core.cs"
set "STAGE=%ROOT%\dist\stage"
set "VERSION=%~1"
set "PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"

if not exist "%DLL%" (
  echo ERROR: "%DLL%" not found. Run build.cmd first.
  exit /b 1
)
if "%VERSION%"=="" for /f "usebackq delims=" %%V in (`%PS% -NoProfile -Command "$t=(Get-Content -Raw -LiteralPath '%CORE%') -replace [char]34,''''; $m=[regex]::Match($t,'BepInPlugin\(''obenseuer\.moddingframework'', ''[^'']*'', ''([^'']+)''\)'); if ($m.Success) { $m.Groups[1].Value }"`) do set "VERSION=%%V"
if "%VERSION%"=="" (
  echo ERROR: no version. Pass it as the first argument, or check the BepInPlugin attribute in "%CORE%".
  exit /b 1
)

set "ZIP=%ROOT%\dist\ObenseuerModdingFramework-%VERSION%-beta.zip"
set "PLUGIN=%STAGE%\BepInEx\plugins\ObenseuerCore"
if exist "%STAGE%" rd /s /q "%STAGE%"
mkdir "%PLUGIN%" || goto fail
copy /y "%DLL%" "%PLUGIN%\" >nul || goto fail
copy /y "%ROOT%\LICENSE" "%PLUGIN%\" >nul || goto fail
copy /y "%ROOT%\README.md" "%PLUGIN%\" >nul || goto fail
copy /y "%ROOT%\bin\ObxWebView.dll" "%PLUGIN%\" >nul || goto fail
copy /y "%ROOT%\bin\ThirdPartyNotices-WebView2.txt" "%PLUGIN%\" >nul || goto fail
mkdir "%PLUGIN%\web" || goto fail
"%SystemRoot%\System32\xcopy.exe" /e /i /q /y "%ROOT%\bin\web" "%PLUGIN%\web\" >nul || goto fail
"%SystemRoot%\System32\tar.exe" -a -c -f "%ZIP%" -C "%STAGE%" BepInEx || goto fail
rd /s /q "%STAGE%"
echo Wrote "%ZIP%"
exit /b 0

:fail
if exist "%STAGE%" rd /s /q "%STAGE%"
echo ERROR: packaging failed.
exit /b 1
