@echo off
setlocal
set "ROOT=%~dp0"
set "WV2=%USERPROFILE%\.nuget\packages\microsoft.web.webview2\1.0.4022.49\build\native"
set "PATH=%SystemRoot%\System32;%PATH%"
call "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" >nul
if not exist "%ROOT%bin" mkdir "%ROOT%bin"
if not exist "%ROOT%obj" mkdir "%ROOT%obj"
cl /nologo /std:c++17 /EHsc /MT /O2 /W4 /WX /DUNICODE /D_UNICODE /external:anglebrackets /external:W0 /I"%WV2%\include" /Fo"%ROOT%obj\ObxWebView.obj" /LD "%ROOT%ObxWebView.cpp" /Fe:"%ROOT%bin\ObxWebView.dll" /link /IMPLIB:"%ROOT%obj\ObxWebView.lib" "%WV2%\x64\WebView2LoaderStatic.lib" user32.lib ole32.lib oleaut32.lib advapi32.lib comctl32.lib d3d11.lib dxgi.lib dcomp.lib
set "RC=%ERRORLEVEL%"
if exist "%ROOT%obj" rmdir /s /q "%ROOT%obj"
exit /b %RC%
