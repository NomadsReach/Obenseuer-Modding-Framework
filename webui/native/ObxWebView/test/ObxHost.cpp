#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <objbase.h>
#include <d3d11.h>
#include <dxgi1_2.h>
#include <psapi.h>
#include <tlhelp32.h>
#include <wrl/client.h>
#include <cmath>
#include <cstdio>
#include <cstdlib>
#include <cwchar>
#include <filesystem>
#include <set>
#include <system_error>
#include <string>
#include <vector>
#include "FrameHostChecks.h"
#include "TestInput.h"

using Microsoft::WRL::ComPtr;

namespace {

typedef int(__stdcall* InitFn)(HWND, const wchar_t*, int);
typedef int(__stdcall* CreateFn)(int, int, int, int, const wchar_t*);
typedef int(__stdcall* BoundsFn)(int, int, int, int, int);
typedef int(__stdcall* VisibleFn)(int, int);
typedef int(__stdcall* MapFn)(int, const wchar_t*, const wchar_t*);
typedef int(__stdcall* PostFn)(int, const wchar_t*);
typedef int(__stdcall* ShellFn)(int, const wchar_t*);
typedef int(__stdcall* FrameFn)(int, const wchar_t*);
typedef int(__stdcall* ScriptFn)(int, const wchar_t*);
typedef int(__stdcall* PollFn)(wchar_t*, int, int*, int*);
typedef int(__stdcall* TextFn)(wchar_t*, int);
typedef int(__stdcall* IdFn)(int);
typedef int(__stdcall* NoArgFn)();

struct Api {
    InitFn init = nullptr;
    CreateFn create = nullptr;
    BoundsFn bounds = nullptr;
    VisibleFn visible = nullptr;
    MapFn map = nullptr;
    PostFn post = nullptr;
    ShellFn shell = nullptr;
    FrameFn allowFrame = nullptr;
    ScriptFn script = nullptr;
    ScriptFn addInit = nullptr;
    PollFn poll = nullptr;
    TextFn lastError = nullptr;
    IdFn destroy = nullptr;
    IdFn setSuspended = nullptr;
    NoArgFn shutdown = nullptr;
};

enum class Phase { Boot, Echo, Mouse, Key, FrameProbe, Page, Done };

Api gApi;
HMODULE gDll = nullptr;
HWND gWnd = nullptr;
ComPtr<ID3D11Device> gDev;
ComPtr<ID3D11DeviceContext> gCtx;
ComPtr<IDXGISwapChain1> gSwap;
ComPtr<ID3D11RenderTargetView> gRtv;
std::wstring gDir;
std::wstring gPage;
bool gPageMode = false;
bool gWindowed = false;
bool gComposition = false;
bool gDefaultData = false;
bool gMta = false;
bool gLockdown = false;
bool gKeepProfile = false;
bool gFrameMessage = false;
bool gNoFrameHost = false;
bool gFrameLoaded = false;
bool gStalburgCancelled = false;
bool gOtherFrameCancelled = false;
bool gShellFrameCancelled = false;
bool gSameHostTop = false;
bool gRemoteImg = false;
bool gRemoteOpened = false;
bool gRemoteBridgeAccepted = false;
bool gRemoteSourceBlocked = false;
bool gAliveEnd = false;
bool gLongFrame = false;
bool gFrameUrlLong = false;
int gFrameFailCount = 0;
bool gHostsOk = true;
int gNavBlocks = 0;
int gSameMark = -1;
bool gWorkerImpossible = false;
bool gSwTopAbsent = false;
bool gSwFrameAbsent = false;
bool gInitTop = false;
bool gFrameInit = false;
bool gFakeSeen = false;
bool gFakeNative = false;
bool gNativeSeen = false;
bool gKeySeen = false;
bool gTraversalLeak = true;
bool gWindowBlocked = false;
std::set<std::wstring> gBlocked;
int gWidth = 1280;
int gHeight = 720;
int gView = 0;
Phase gPhase = Phase::Boot;
ULONGLONG gDeadline = 0;
long long gToken = 0;
int gErrors = 0;
bool gEchoOk = false;
bool gRunning = true;
int gExit = 1;
int gMouseStep = 0;
bool gMouseDown = false;
ULONGLONG gProbeAt = 0;

void Say(const std::wstring& text) {
    const int bytes = WideCharToMultiByte(CP_UTF8, 0, text.c_str(), static_cast<int>(text.size()), nullptr, 0, nullptr, nullptr);
    std::string utf8(static_cast<size_t>(bytes), '\0');
    WideCharToMultiByte(CP_UTF8, 0, text.c_str(), static_cast<int>(text.size()), &utf8[0], bytes, nullptr, nullptr);
    fwrite(utf8.data(), 1, utf8.size(), stdout);
    fputc('\n', stdout);
    fflush(stdout);
}

std::wstring Num(long long value) {
    return std::to_wstring(value);
}

std::wstring FullPath(const std::wstring& path) {
    wchar_t full[MAX_PATH * 2] = {};
    GetFullPathNameW(path.c_str(), MAX_PATH * 2, full, nullptr);
    return full;
}

bool Has(const std::wstring& text, const std::wstring& needle) {
    return text.find(needle) != std::wstring::npos;
}

long long NumberAfter(const std::wstring& text, const std::wstring& key) {
    const size_t at = text.find(key);
    if (at == std::wstring::npos) return -1;
    return wcstoll(text.c_str() + at + key.size(), nullptr, 10);
}

std::wstring FocusName() {
    wchar_t name[256] = {};
    HWND focus = GetFocus();
    if (!focus) return L"none";
    GetClassNameW(focus, name, 256);
    return name;
}

#include "SmokeChecks.h"

void ReportMemory() {
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (snapshot == INVALID_HANDLE_VALUE) return;
    std::vector<PROCESSENTRY32W> all;
    PROCESSENTRY32W entry{};
    entry.dwSize = sizeof(entry);
    for (BOOL ok = Process32FirstW(snapshot, &entry); ok; ok = Process32NextW(snapshot, &entry)) all.push_back(entry);
    CloseHandle(snapshot);
    std::set<DWORD> tree{GetCurrentProcessId()};
    for (bool grew = true; grew;) {
        grew = false;
        for (const auto& e : all) {
            if (tree.count(e.th32ParentProcessID) && !tree.count(e.th32ProcessID)) {
                tree.insert(e.th32ProcessID);
                grew = true;
            }
        }
    }
    SIZE_T total = 0;
    int count = 0;
    for (const auto& e : all) {
        if (!tree.count(e.th32ProcessID) || _wcsicmp(e.szExeFile, L"msedgewebview2.exe") != 0) continue;
        HANDLE process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, e.th32ProcessID);
        if (!process) continue;
        PROCESS_MEMORY_COUNTERS counters{};
        counters.cb = sizeof(counters);
        if (GetProcessMemoryInfo(process, &counters, sizeof(counters))) {
            total += counters.WorkingSetSize;
            ++count;
        }
        CloseHandle(process);
    }
    wchar_t mb[64] = {};
    swprintf_s(mb, L"%.1f", static_cast<double>(total) / 1048576.0);
    Say(L"MEMORY: msedgewebview2 processes=" + Num(count) + L" working_set=" + mb + L" MB (descendants of this process)");
}

void LastErrorLine(const wchar_t* label) {
    wchar_t text[512] = {};
    gApi.lastError(text, 512);
    Say(std::wstring(label) + L" last=" + text);
}

void Finish(bool ok, const std::wstring& verdict) {
    if (gDll) {
        ReportMemory();
        LastErrorLine(L"DIAG");
    }
    Say(verdict);
    gExit = ok ? 0 : 1;
    gPhase = Phase::Done;
    gRunning = false;
}

void LeftButton(bool down) {
    if (down) {
        TakeForeground(gWnd);
        if (!ClickTargetOk(gWnd)) {
            Say(L"CLICK NOT SENT: window not under the cursor/foreground");
            return;
        }
    } else if (!gMouseDown) {
        return;
    }
    gMouseDown = down;
    INPUT input{};
    input.type = INPUT_MOUSE;
    input.mi.dwFlags = down ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
    SendInput(1, &input, sizeof(INPUT));
}

void PressKey(WORD key) {
    TakeForeground(gWnd);
    if (GetForegroundWindow() != gWnd) {
        Say(L"KEY NOT SENT: this window is not in the foreground (focus=" + FocusName() + L")");
        return;
    }
    gApi.visible(gView, 1);
    INPUT inputs[2] = {};
    inputs[0].type = INPUT_KEYBOARD;
    inputs[0].ki.wVk = key;
    inputs[1].type = INPUT_KEYBOARD;
    inputs[1].ki.wVk = key;
    inputs[1].ki.dwFlags = KEYEVENTF_KEYUP;
    SendInput(2, inputs, sizeof(INPUT));
}

void StartKeyProbe() {
    gPhase = Phase::Key;
    gDeadline = GetTickCount64() + 4000;
    Say(L"KEYBOARD: pressing 'A' (focus before=" + FocusName() + L")");
    PressKey('A');
}

const wchar_t* const kProbe = LR"JS(
(function () {
  var $ = function (id) { return document.getElementById(id); };
  var box = function (sel) {
    var e = document.querySelector(sel);
    if (!e) return null;
    var b = e.getBoundingClientRect();
    return [Math.round(b.left), Math.round(b.top), Math.round(b.width), Math.round(b.height)];
  };
  var out = {};
  var bar = document.querySelector('.site-head');
  var center = document.querySelector('.center');
  var watchScroll = document.querySelector('.watch-scroll');
  var posScroll = document.querySelector('.pos-scroll');
  var body = getComputedStyle(document.body);
  out.viewport = [innerWidth, innerHeight, devicePixelRatio];
  out.window = box('.obx-window');
  out.topbar = [bar.clientWidth, bar.scrollWidth, Array.prototype.map.call(bar.children, function (e) { return Math.round(e.getBoundingClientRect().right); })];
  out.center = [center.clientHeight, center.scrollHeight];
  out.watch = [document.querySelectorAll('.watch-row').length, watchScroll.clientHeight, watchScroll.scrollHeight];
  out.positions = [document.querySelectorAll('.pos-row').length, posScroll.clientHeight, posScroll.scrollHeight];
  out.ticket = box('.ticket');
  out.submit = box('#submit');
  out.cards = [document.querySelectorAll('.obx-card').length, box('.stats')];
  out.chart = box('#chart');
  out.status = [box('.obx-statusbar'), $('clock').textContent];
  out.chrome = [$('title').textContent, $('url').textContent, box('.obx-titlebar'), box('.obx-chrome')];
  out.theme = [
    getComputedStyle(document.querySelector('.obx-titlebar')).backgroundColor,
    getComputedStyle(document.querySelector('.obx-statusbar')).backgroundColor,
    getComputedStyle(document.querySelector('.obx-brand-a')).color,
    getComputedStyle(document.querySelector('.obx-brand-b')).color,
    getComputedStyle(document.querySelector('.obx-btn.primary')).backgroundColor
  ];
  out.font = [body.fontFamily, body.letterSpacing, body.fontSize];
  var minFont = 99;
  document.querySelectorAll('.obx-window *').forEach(function (e) {
    var text = Array.prototype.some.call(e.childNodes, function (n) { return n.nodeType === 3 && n.textContent.trim() !== ''; });
    if (text && e.offsetParent !== null) minFont = Math.min(minFont, parseFloat(getComputedStyle(e).fontSize));
  });
  out.minFont = minFont;
  out.start = [$('cash').textContent, $('power').textContent, $('submit').textContent, $('submit').disabled, $('note').textContent];
  document.querySelectorAll('#chips button')[2].click();
  var cv = $('chart'), c = cv.getBoundingClientRect();
  cv.dispatchEvent(new MouseEvent('mousemove', { clientX: c.left + c.width / 2, clientY: c.top + c.height / 2 }));
  out.hover = [$('tip').className, $('tip').textContent, cv.width, cv.height];
  cv.dispatchEvent(new MouseEvent('mouseleave'));
  out.left = $('tip').className;
  document.querySelector('.side[data-side="sell"]').click();
  out.sell = [$('submit').textContent, $('submit').disabled, $('submit').className, $('note').textContent];
  document.querySelector('.presets button[data-q="max"]').click();
  out.sellMax = [$('qty').value, $('est').textContent, $('note').textContent, $('submit').disabled];
  document.querySelector('.side[data-side="buy"]').click();
  document.querySelector('.presets button[data-q="max"]').click();
  out.buyMax = [$('qty').value, $('est').textContent, $('power').textContent, $('submit').disabled, $('submit').className];
  $('submit').click();
  out.sent = $('note').textContent;
  document.querySelector('.watch-row[data-sym="RBF"]').click();
  out.selected = [$('sym').textContent, $('name').textContent, $('price').textContent, $('change').textContent];
  $('search').value = 'os';
  $('search').dispatchEvent(new Event('input', { bubbles: true }));
  out.filter = document.querySelectorAll('.watch-row:not([hidden])').length;
  $('search').value = '';
  $('search').dispatchEvent(new Event('input', { bubbles: true }));
  document.querySelector('.obx-tab[data-tab="portfolio"]').click();
  out.portfolio = [document.body.dataset.tab, getComputedStyle($('soon')).display, $('soon').textContent, $('url').textContent, $('title').textContent];
  document.querySelector('.obx-tab[data-tab="markets"]').click();
  out.markets = [document.body.dataset.tab, getComputedStyle($('soon')).display, $('url').textContent];
  chrome.webview.postMessage(Object.assign({ type: 'probe' }, out));
  $('close').click();
  document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
  var paint = function (name) {
    var style = function (sel, prop) { var e = document.querySelector(sel); return e ? getComputedStyle(e)[prop] : null; };
    return {
      theme: name,
      attr: document.documentElement.dataset.theme || 'none',
      toggle: document.querySelector('.theme-toggle [aria-pressed="true"]').dataset.themeMode,
      osDark: matchMedia('(prefers-color-scheme: dark)').matches,
      scheme: getComputedStyle(document.documentElement).colorScheme,
      window: [style('.obx-window', 'backgroundColor'), box('.obx-window')],
      header: style('.site-head', 'backgroundColor'),
      text: style('.site-head .obx-tab.active', 'color'),
      muted: style('.obx-label', 'color'),
      card: style('.obx-card', 'backgroundColor'),
      input: [style('#search', 'backgroundColor'), style('#search', 'borderTopColor')],
      primary: [style('#submit', 'backgroundColor'), style('#submit', 'color')],
      brand: [style('.obx-brand-a', 'color'), style('.obx-brand-b', 'color')],
      badge: [style('.obx-badge.obx-up', 'backgroundColor'), style('.obx-badge.obx-up', 'color')],
      strip: [style('.obx-statusbar', 'backgroundColor'), style('.obx-statusbar', 'color')],
      chart: box('#chart')
    };
  };
  chrome.webview.postMessage(Object.assign({ type: 'probe' }, paint('light')));
  document.querySelector('.theme-toggle [data-theme-mode="dark"]').click();
  setTimeout(function () {
    chrome.webview.postMessage(Object.assign({ type: 'probe' }, paint('dark')));
    obx.theme.set('system');
    setTimeout(function () { chrome.webview.postMessage(Object.assign({ type: 'probe' }, paint('system'))); }, 300);
  }, 300);
})();
)JS";

void Probe() {
    if (!gProbeAt || GetTickCount64() < gProbeAt) return;
    gProbeAt = 0;
    gApi.script(gView, kProbe);
}

void HideAndFinish();

const wchar_t* const kInitScript = L"window.__obxRan = true; if (window === window.top) window.__obxInit = 'top';";

const wchar_t* const kRemoteFrameScript = LR"JS(
(function () {
  if (window === window.top || location.hostname !== "stalburg.net") return;
  var report = function (o) { parent.postMessage(Object.assign({ remote: true }, o), "*"); };
  var bridge = !!(window.chrome && window.chrome.webview);
  setTimeout(function () { var posted = "ok"; try { chrome.webview.postMessage({ type: "remote-bridge" }); } catch (e) { posted = "threw " + e; } report({ posted: posted }); }, 300);
  setTimeout(function () { if (!location.search) location.href = "https://stalburg.net/Currencies?" + "A".repeat(100000); }, 1000);
  var img = new Image();
  img.onload = function () { report({ bridge: bridge, img: "load" }); };
  img.onerror = function () { report({ bridge: bridge, img: "error" }); };
  img.src = "https://example.invalid/pixel.png";
  var opened = window.open("https://example.invalid/");
  report({ bridge: bridge, opened: opened === null ? "null" : "window" });
  setTimeout(function () { location.href = "https://obx.page/index.html"; }, 1500);
})();
)JS";

const wchar_t* const kExpectedBlocks[] = {L"new-window-blocked", L"navigation-blocked", L"download-blocked", L"message-too-large", L"resource-blocked", L"frame-navigation-blocked"};

void FinishLockdown() {
    std::wstring missing;
    for (const wchar_t* name : kExpectedBlocks) {
        if (!gBlocked.count(name)) missing += std::wstring(L" ") + name;
    }
    if (gTraversalLeak) missing += L" traversal";
    if (!gWindowBlocked) missing += L" window.open";
    if (!gKeySeen) missing += L" escape-key";
    if (!gSwTopAbsent) missing += L" sw-top-or-blank-frame-present";
    if (!gSwFrameAbsent) missing += L" sw-frame-present";
    if (!gWorkerImpossible) missing += L" worker-register-possible";
    if (gFrameMessage) missing += L" frame-message-accepted";
    if (!gBlocked.count(L"message-source-blocked")) missing += L" message-source-blocked";
    if (gRemoteBridgeAccepted) missing += L" remote-frame-bridge-accepted";
    if (!gSameHostTop) missing += L" same-host-top-cancelled";
    if (!gAliveEnd) missing += L" queue-stalled";
    if (gFrameUrlLong) missing += L" frame-url-over-256";
    if (gFrameFailCount != 64) missing += L" frame-cap-64(" + Num(gFrameFailCount) + L")";
    if (!gHostsOk) missing += L" frame-host-validation";
    if (!gNoFrameHost && !gLongFrame) missing += L" long-frame-not-reported";
    if (gNoFrameHost) {
        if (gFrameLoaded) missing += L" frame-loaded-without-allow";
        if (!gStalburgCancelled) missing += L" frame-not-cancelled";
    } else {
        if (!gFrameLoaded) missing += L" allowed-frame-loads";
        if (!gOtherFrameCancelled) missing += L" other-frame-cancelled";
        if (!gShellFrameCancelled) missing += L" frame-to-shell-cancelled";
        if (!gRemoteImg) missing += L" remote-frame-subresource-403";
        if (!gRemoteOpened) missing += L" remote-frame-window.open";
        if (!gRemoteSourceBlocked) missing += L" remote-frame-message-source-blocked";
    }
    Finish(missing.empty(), missing.empty() ? std::wstring(L"LOCKDOWN PASS") : L"LOCKDOWN FAIL: missing" + missing);
}

bool FrameUrlTooLong(const std::wstring& json) {
    const size_t begin = json.find(L"\"url\":\"");
    if (begin == std::wstring::npos) return true;
    const size_t end = json.find(L'"', begin + 7);
    return end == std::wstring::npos || end - begin - 7 > 256;
}

void CollectLockdown(const std::wstring& json) {
    for (const wchar_t* name : kExpectedBlocks) {
        if (Has(json, std::wstring(L"\"where\":\"") + name + L"\"")) gBlocked.insert(name);
    }
    if (Has(json, L"\"name\":\"traversal\"") && Has(json, L"\"leaked\":false")) gTraversalLeak = false;
    if (Has(json, L"\"name\":\"window.open\"") && Has(json, L"\"result\":\"null\"")) gWindowBlocked = true;
    if (Has(json, L"\"where\":\"message-source-blocked\"")) gBlocked.insert(L"message-source-blocked");
    if (Has(json, L"\"type\":\"frame\"")) gFrameMessage = true;
    if (Has(json, L"\"type\":\"obx.key\"") && Has(json, L"\"vk\":27")) gKeySeen = true;
    if (Has(json, L"\"name\":\"sw-top\"") && Has(json, L"\"absent\":true")) gSwTopAbsent = true;
    if (Has(json, L"\"name\":\"frame\"") && Has(json, L"\"sw\":true")) gSwFrameAbsent = true;
    if (Has(json, L"\"name\":\"worker-register\"") && Has(json, L"\"impossible\":true")) gWorkerImpossible = true;
    if (Has(json, L"\"name\":\"alive end\"")) gAliveEnd = true;
    if (Has(json, L"\"name\":\"alive end\"")) gDeadline = GetTickCount64() + 2500;
    if (Has(json, L"\"where\":\"navigation-blocked\"")) ++gNavBlocks;
    if (Has(json, L"\"name\":\"top same host\"")) {
        if (Has(json, L"\"phase\":\"start\"")) gSameMark = gNavBlocks;
        else if (gSameMark >= 0 && gNavBlocks > gSameMark) gSameHostTop = true;
    }
    if (Has(json, L"\"type\":\"obx.frame\"")) {
        if (Has(json, L"\"ok\":false")) ++gFrameFailCount;
        if (FrameUrlTooLong(json)) gFrameUrlLong = true;
        if (Has(json, L"\"url\":\"https://stalburg.net/Currencies?AAAA")) gLongFrame = true;
        if (Has(json, L"\"ok\":true") && Has(json, L"\"url\":\"https://stalburg.net/Currencies\"")) gFrameLoaded = true;
        if (Has(json, L"\"ok\":false") && Has(json, L"\"url\":\"https://stalburg.net/Currencies\"")) gStalburgCancelled = true;
        if (Has(json, L"\"ok\":false") && Has(json, L"\"url\":\"https://example.invalid/\"")) gOtherFrameCancelled = true;
        if (Has(json, L"\"ok\":false") && Has(json, L"\"url\":\"https://obx.page/index.html\"")) gShellFrameCancelled = true;
    }
    if (Has(json, L"\"name\":\"remote frame\"")) {
        if (Has(json, L"\"img\":\"error\"")) gRemoteImg = true;
        if (Has(json, L"\"opened\":\"null\"")) gRemoteOpened = true;
    }
    if (Has(json, L"\"type\":\"remote-bridge\"")) gRemoteBridgeAccepted = true;
    if (Has(json, L"\"where\":\"message-source-blocked\"") && Has(json, L"\"source\":\"https://stalburg.net/")) gRemoteSourceBlocked = true;
}

void CollectSelfTest(const std::wstring& json, bool native) {
    SmokeNote(json, native);
    if (Has(json, L"\"type\":\"init-probe\"")) {
        gInitTop = Has(json, L"\"ran\":true") && Has(json, L"\"init\":\"top\"");
        Say(std::wstring(L"INIT: ") + (gInitTop ? L"PASS" : L"FAIL") + L" top document ran the init script before the page script (" + json + L")");
    }
    if (Has(json, L"\"type\":\"frame-probe\"")) {
        gFrameInit = Has(json, L"\"ran\":true") && Has(json, L"\"init\":\"undefined\"");
        Say(std::wstring(L"INIT FRAME: ") + (gFrameInit ? L"PASS" : L"FAIL") + L" frame ran the init script but not the top-only part (" + json + L")");
    }
    if (Has(json, L"\"fake\":\"page\"")) {
        if (native) gFakeNative = true;
        else gFakeSeen = true;
    }
    if (native && Has(json, L"\"type\":\"obx.status\"")) gNativeSeen = true;
}

void HandleMessage(int id, const std::wstring& json) {
    if (!(gLockdown && Has(json, L"\"type\":\"flood\""))) Say(L"[obx view=" + Num(id) + L"] " + json);
    if (Has(json, L"\"type\":\"error\"") || Has(json, L"\"type\":\"obx.error\"")) ++gErrors;
    if (gLockdown) CollectLockdown(json);
    if (gPageMode) {
        if (gPhase == Phase::Boot && Has(json, L"\"type\":\"ready\"")) {
            gPhase = Phase::Page;
            gDeadline = GetTickCount64() + (gLockdown ? 20000 : 8000);
            gApi.post(gView, L"{\"type\":\"wallet\",\"cash\":1975}");
            if (!gLockdown) gProbeAt = GetTickCount64() + 1500;
            else {
                Say(L"ESCAPE: pressing Escape (focus before=" + FocusName() + L")");
                PressKey(VK_ESCAPE);
            }
        }
        return;
    }
    switch (gPhase) {
    case Phase::Boot:
        if (Has(json, L"\"type\":\"ready\"")) {
            gToken = static_cast<long long>(GetTickCount64());
            gApi.post(gView, (L"{\"type\":\"ping\",\"token\":" + Num(gToken) + L"}").c_str());
            gPhase = Phase::Echo;
            gDeadline = GetTickCount64() + 8000;
        }
        break;
    case Phase::Echo:
        if (Has(json, L"\"type\":\"echo\"") && Has(json, L"\"token\":" + Num(gToken))) {
            gEchoOk = true;
            Say(L"ECHO: PASS (page echoed the posted JSON through chrome.webview.postMessage)");
            gPhase = Phase::Mouse;
            gMouseStep = 1;
            gDeadline = GetTickCount64() + 400;
            MoveCursorInWindow(gWnd, 300, 200);
        }
        break;
    case Phase::Mouse:
        if (Has(json, L"\"type\":\"mouse\"")) {
            const long long x = NumberAfter(json, L"\"x\":");
            const long long y = NumberAfter(json, L"\"y\":");
            const bool ok = x == 200 && y == 140;
            Say(std::wstring(L"MOUSE: ") + (ok ? L"PASS" : L"FAIL") + L" page clientX=" + Num(x) + L" clientY=" + Num(y) + L" (window click 300,200, view at 100,60, expected 200,140)");
            StartKeyProbe();
        }
        break;
    case Phase::Key:
        if (Has(json, L"\"type\":\"key\"")) {
            Say(L"KEYBOARD: PASS (page received keydown)");
            gPhase = Phase::FrameProbe;
            gDeadline = GetTickCount64() + 10000;
            if (gFrameInit) HideAndFinish();
        }
        break;
    case Phase::FrameProbe:
        if (gFrameInit) HideAndFinish();
        break;
    default:
        break;
    }
}

void PollMessages() {
    static wchar_t buffer[65536];
    int id = 0;
    int native = 0;
    for (;;) {
        const int n = gApi.poll(buffer, 65536, &id, &native);
        if (n == 0) break;
        if (n < 0) {
            Say(L"poll failed hr=" + Num(n));
            break;
        }
        const std::wstring json(buffer, static_cast<size_t>(n));
        CollectSelfTest(json, native != 0);
        HandleMessage(id, json);
    }
}

void HideAndFinish() {
    gApi.visible(gView, 0);
    Say(L"FOCUS after hide=" + FocusName());
    const bool kind = gFakeSeen && !gFakeNative && gNativeSeen;
    Say(std::wstring(L"KIND: ") + (kind ? L"PASS" : L"FAIL") + L" page-sent obx.error and obx.key arrive as page kind, native obx.status arrives as native kind");
    const bool ok = gEchoOk && gInitTop && gFrameInit && kind;
    Finish(ok, ok ? L"SELFTEST PASS" : L"SELFTEST FAIL");
}

void CheckTimeouts() {
    const ULONGLONG now = GetTickCount64();
    if (gPhase == Phase::Done || now < gDeadline) return;
    if (gLockdown) {
        FinishLockdown();
    } else if (gPageMode) {
        Finish(gErrors == 0 && gPhase == Phase::Page, L"PAGE RESULT ready=" + std::wstring(gPhase == Phase::Page ? L"1" : L"0") + L" errors=" + Num(gErrors));
    } else if (gPhase == Phase::Boot) {
        Finish(false, L"SELFTEST FAIL: no ready message from the page");
    } else if (gPhase == Phase::Echo) {
        Finish(false, L"SELFTEST FAIL: page did not echo the posted JSON");
    } else if (gPhase == Phase::Mouse) {
        if (gMouseStep == 1) {
            LeftButton(true);
            gMouseStep = 2;
            gDeadline = now + 100;
        } else if (gMouseStep == 2) {
            LeftButton(false);
            gMouseStep = 3;
            gDeadline = now + 3000;
        } else {
            Say(L"MOUSE: NOT RECEIVED by the page");
            StartKeyProbe();
        }
    } else if (gPhase == Phase::Key) {
        Say(L"KEYBOARD: NOT RECEIVED by the page (focus=" + FocusName() + L")");
        HideAndFinish();
    } else if (gPhase == Phase::FrameProbe) {
        Say(L"INIT FRAME: NOT RECEIVED by the harness");
        HideAndFinish();
    }
}

void Frame() {
    const float t = static_cast<float>(GetTickCount64()) / 1000.0f;
    const float color[4] = {0.5f + 0.5f * std::sin(t), 0.2f, 0.5f + 0.5f * std::cos(t), 1.0f};
    ID3D11RenderTargetView* rtv = gRtv.Get();
    gCtx->OMSetRenderTargets(1, &rtv, nullptr);
    gCtx->ClearRenderTargetView(gRtv.Get(), color);
    gSwap->Present(1, 0);
}

LRESULT CALLBACK WndProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp) {
    NoteActivation(msg, wp, lp);
    if (msg == WM_CLOSE) { gRunning = false; return 0; }
    return DefWindowProcW(hwnd, msg, wp, lp);
}

bool MakeWindow() {
    WNDCLASSEXW wc{};
    wc.cbSize = sizeof(wc);
    wc.lpfnWndProc = WndProc;
    wc.hInstance = GetModuleHandleW(nullptr);
    wc.lpszClassName = L"ObxHostWindow";
    wc.hCursor = LoadCursorW(nullptr, IDC_ARROW);
    RegisterClassExW(&wc);
    gWnd = CreateWindowExW(0, L"ObxHostWindow", L"ObxHost", WS_POPUP | WS_VISIBLE, 50, 50, gWidth, gHeight, nullptr, nullptr, wc.hInstance, nullptr);
    return gWnd != nullptr;
}

bool InitD3D() {
    if (FAILED(D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, D3D11_CREATE_DEVICE_BGRA_SUPPORT, nullptr, 0, D3D11_SDK_VERSION, gDev.ReleaseAndGetAddressOf(), nullptr, gCtx.ReleaseAndGetAddressOf()))) return false;
    ComPtr<IDXGIDevice> dxgi;
    ComPtr<IDXGIAdapter> adapter;
    ComPtr<IDXGIFactory2> factory;
    gDev.As(&dxgi);
    dxgi->GetAdapter(adapter.ReleaseAndGetAddressOf());
    adapter->GetParent(IID_PPV_ARGS(factory.ReleaseAndGetAddressOf()));
    DXGI_SWAP_CHAIN_DESC1 desc{};
    desc.Width = gWidth;
    desc.Height = gHeight;
    desc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
    desc.SampleDesc.Count = 1;
    desc.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    desc.BufferCount = 2;
    desc.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
    if (FAILED(factory->CreateSwapChainForHwnd(gDev.Get(), gWnd, &desc, nullptr, nullptr, gSwap.ReleaseAndGetAddressOf()))) return false;
    ComPtr<ID3D11Texture2D> back;
    gSwap->GetBuffer(0, IID_PPV_ARGS(back.ReleaseAndGetAddressOf()));
    return SUCCEEDED(gDev->CreateRenderTargetView(back.Get(), nullptr, gRtv.ReleaseAndGetAddressOf()));
}

template <class T>
bool Load(const char* name, T& out) {
    out = reinterpret_cast<T>(GetProcAddress(gDll, name));
    return out != nullptr;
}

bool LoadApi() {
    return Load("Obx_Init", gApi.init) && Load("Obx_Create", gApi.create) && Load("Obx_SetBounds", gApi.bounds) &&
           Load("Obx_SetVisible", gApi.visible) && Load("Obx_MapFolder", gApi.map) && Load("Obx_PostJson", gApi.post) && Load("Obx_SetShellHost", gApi.shell) &&
           Load("Obx_PollMessage", gApi.poll) && Load("Obx_LastError", gApi.lastError) && Load("Obx_Destroy", gApi.destroy) &&
           Load("Obx_Shutdown", gApi.shutdown) && Load("Obx_ExecuteScript", gApi.script) && Load("Obx_AddInitScript", gApi.addInit) && Load("Obx_AllowFrameHost", gApi.allowFrame) && Load("Obx_SetSuspended", gApi.setSuspended);
}

}

int wmain(int argc, wchar_t** argv) {
    bool selftest = false;
    for (int i = 1; i < argc; ++i) {
        if (!wcscmp(argv[i], L"--selftest")) selftest = true;
        else if (!wcscmp(argv[i], L"--windowed")) gWindowed = true;
        else if (!wcscmp(argv[i], L"--composition")) gComposition = true;
        else if (!wcscmp(argv[i], L"--default-data")) gDefaultData = true;
        else if (!wcscmp(argv[i], L"--mta")) gMta = true;
        else if (!wcscmp(argv[i], L"--lockdown")) { gLockdown = true; gPageMode = true; }
        else if (!wcscmp(argv[i], L"--keep-profile")) gKeepProfile = true;
        else if (!wcscmp(argv[i], L"--no-frame-host")) gNoFrameHost = true;
        else if (!wcscmp(argv[i], L"--size") && i + 1 < argc) swscanf_s(argv[++i], L"%dx%d", &gWidth, &gHeight);
        else if (!wcscmp(argv[i], L"--page") && i + 1 < argc) { gPage = argv[++i]; gPageMode = true; }
    }
    if (selftest && !gWindowed) gComposition = true;
    if (!selftest && !gPageMode) {
        Say(L"usage: ObxHost --selftest [--windowed] [--default-data] [--mta] | ObxHost --lockdown [--windowed] [--keep-profile] | ObxHost --page <folder> [--composition|--windowed] [--default-data]");
        return 2;
    }
    wchar_t exePath[MAX_PATH] = {};
    GetModuleFileNameW(nullptr, exePath, MAX_PATH);
    gDir = exePath;
    gDir = gDir.substr(0, gDir.find_last_of(L'\\') + 1);
    gDll = LoadLibraryW((gDir + L"ObxWebView.dll").c_str());
    if (!gDll) {
        Say(L"LoadLibrary ObxWebView.dll failed, GetLastError=" + Num(GetLastError()));
        return 1;
    }
    if (!LoadApi()) {
        Say(L"an Obx_ export is missing");
        return 1;
    }
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
    CoInitializeEx(nullptr, gMta ? COINIT_MULTITHREADED : COINIT_APARTMENTTHREADED);
    if (!MakeWindow() || !InitD3D()) {
        Say(L"window or D3D11 swapchain setup failed");
        return 1;
    }
    Say(L"ObxHost: mode=" + std::wstring(gWindowed ? L"windowed" : gComposition ? L"composition" : L"thread") + L" dpi=" + Num(GetDpiForWindow(gWnd)) + L" exe=" + gDir);
    const std::wstring host = gPageMode ? L"obx.page" : L"obx.test";
    const std::wstring folder = FullPath(gLockdown ? gDir + L"..\\lockdown" : gPageMode ? gPage : gDir + L"..\\web");
    const std::wstring url = L"https://" + host + L"/index.html";
    const int flags = gWindowed ? 2 : gComposition ? 4 : 0;
    const std::wstring userData = gDir + L"userdata";
    std::error_code ignored;
    if (gLockdown && !gKeepProfile) std::filesystem::remove_all(userData, ignored);
    const int init = gApi.init(gWnd, gDefaultData ? nullptr : userData.c_str(), flags);
    Say(L"Obx_Init hr=" + Num(init));
    gView = gPageMode ? gApi.create(0, 0, gWidth, gHeight, url.c_str()) : gApi.create(100, 60, 800, 500, url.c_str());
    if (gView <= 0) {
        Say(L"Obx_Create failed hr=" + Num(gView));
        LastErrorLine(L"DIAG");
        return 1;
    }
    Say(L"Obx_Create view=" + Num(gView) + L" url=" + url + L" folder=" + folder);
    std::wstring initScript = std::wstring(kInitScript) + (gLockdown ? kRemoteFrameScript : L"");
    if (gApi.addInit(gView, initScript.c_str()) < 0) {
        Say(L"Obx_AddInitScript failed");
        LastErrorLine(L"DIAG");
        return 1;
    }
    gApi.map(gView, host.c_str(), folder.c_str());
    if (gLockdown) gApi.map(gView, L"obx.other", FullPath(gDir + L"..\\other").c_str());
    gApi.map(gView, L"obx.frame", FullPath(gDir + L"..\\web").c_str());
    gApi.shell(gView, host.c_str());
    if (gLockdown && !gNoFrameHost) gApi.allowFrame(gView, L"stalburg.net");
    if (gLockdown) gHostsOk = FrameHostsValid(gApi.allowFrame, gView);
    Say(L"SHOW hr=" + Num(gApi.visible(gView, 1)));
    gDeadline = GetTickCount64() + 40000;
    const ULONGLONG start = GetTickCount64();
    while (gRunning) {
        MSG msg;
        while (PeekMessageW(&msg, nullptr, 0, 0, PM_REMOVE)) {
            if (msg.message == WM_QUIT) gRunning = false;
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }
        if (!gRunning) break;
        Frame();
        PollMessages();
        SmokeStep();
        Probe();
        CheckTimeouts();
        if (GetTickCount64() - start > 120000) Finish(false, L"SELFTEST FAIL: global timeout");
    }
    return StopTimed();
}
