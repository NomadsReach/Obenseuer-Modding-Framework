@echo off
setlocal
set "ROOT=%~dp0"
set "PATH=%SystemRoot%\System32;%PATH%"
call "%ROOT%..\build.cmd"
if errorlevel 1 exit /b 1
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
if not exist "%ROOT%bin" mkdir "%ROOT%bin"
cl /nologo /std:c++17 /EHsc /MT /O2 /W4 /WX /DUNICODE /D_UNICODE /Fo"%ROOT%bin\ObxHost.obj" "%ROOT%ObxHost.cpp" /Fe:"%ROOT%bin\ObxHost.exe" /link user32.lib d3d11.lib dxgi.lib psapi.lib ole32.lib
set "RC=%ERRORLEVEL%"
if exist "%ROOT%bin\ObxHost.obj" del "%ROOT%bin\ObxHost.obj"
if "%RC%"=="0" copy /y "%ROOT%..\bin\ObxWebView.dll" "%ROOT%bin\" >nul
exit /b %RC%
