@echo off
rem Packages the Visual Studio project template for the Obenseuer Modding Framework.
rem The zip is flat: MyTemplate.vstemplate sits at its root, as Visual Studio expects.
setlocal
for %%I in ("%~dp0..") do set "ROOT=%%~fI"
set "SRC=%ROOT%\templates\VisualStudio\ObenseuerMod"
set "DIST=%ROOT%\dist"
set "TAR=%SystemRoot%\System32\tar.exe"
set "FIND=%SystemRoot%\System32\findstr.exe"
set "NAME=ObenseuerModTemplate.zip"
set "ZIP=%DIST%\%NAME%"
set "VSIX=%DIST%\ObenseuerMod.vsix-template.zip"
set "LIST=%TEMP%\obenseuer-template-list.txt"
set "TP=%USERPROFILE%\Documents\Visual Studio 2022\Templates\ProjectTemplates"
set "FILES=MyTemplate.vstemplate $safeprojectname$.csproj Plugin.cs Directory.Build.props README.txt"

if not exist "%SRC%\MyTemplate.vstemplate" (
  echo ERROR: "%SRC%\MyTemplate.vstemplate" not found.
  exit /b 1
)
for %%F in (%FILES%) do (
  if not exist "%SRC%\%%F" (
    echo ERROR: "%SRC%\%%F" not found.
    exit /b 1
  )
)
if not exist "%DIST%" mkdir "%DIST%" || goto fail
if exist "%ZIP%" del "%ZIP%" || goto fail

"%TAR%" -a -c -f "%ZIP%" -C "%SRC%" %FILES% || goto fail

set "FIRST="
"%TAR%" -tf "%ZIP%" > "%LIST%" || goto fail
for /f "usebackq delims=" %%L in ("%LIST%") do if not defined FIRST set "FIRST=%%L"
if /i not "%FIRST%"=="MyTemplate.vstemplate" (
  echo ERROR: "%ZIP%" does not start with MyTemplate.vstemplate, it starts with "%FIRST%".
  goto fail
)
for %%F in (%FILES%) do (
  "%FIND%" /x /c:"%%F" "%LIST%" >nul || (
    echo ERROR: "%ZIP%" does not contain %%F.
    goto fail
  )
)
del "%LIST%" >nul 2>nul

copy /y "%ZIP%" "%VSIX%" >nul || goto fail
echo Wrote "%ZIP%"
echo Wrote "%VSIX%" (the same bytes, under the release name)
echo.
echo Install: copy "%NAME%" to:
echo   %TP%
echo Then choose "Obenseuer Mod (Framework)" in Visual Studio's New Project dialog.
exit /b 0

:fail
if exist "%LIST%" del "%LIST%" >nul 2>nul
echo ERROR: packaging the Visual Studio template failed.
exit /b 1
