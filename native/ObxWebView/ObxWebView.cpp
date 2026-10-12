#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <objbase.h>
#include <windowsx.h>
#include <commctrl.h>
#include <d3d11.h>
#include <dcomp.h>
#include <dxgi.h>
#include <wrl.h>
#include <algorithm>
#include <deque>
#include <map>
#include <memory>
#include <string>
#include <utility>
#include <vector>
#include <WebView2.h>

using Microsoft::WRL::Callback;
using Microsoft::WRL::ComPtr;

namespace {

constexpr int kFlagDevTools = 1;
constexpr int kFlagWindowed = 2;
constexpr UINT_PTR kSubclassId = 1;
const HRESULT kBufferSmall = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
constexpr size_t kMaxMessage = 1 << 18;
constexpr size_t kMaxQueue = 512;
constexpr int kMaxReports = 64;

struct View {
    int id = 0;
    RECT bounds{};
    bool visible = false;
    bool started = false;
    int reports = 0;
    int pending = 0;
    std::wstring url;
    std::wstring shell;
    std::vector<std::pair<std::wstring, std::wstring>> mappings;
    std::vector<std::wstring> scripts;
    ComPtr<IDCompositionVisual> visual;
    ComPtr<ICoreWebView2CompositionController> comp;
    ComPtr<ICoreWebView2Controller> controller;
    ComPtr<ICoreWebView2> web;
};

struct QueuedMessage {
    int id = 0;
    bool native = false;
    std::wstring json;
};

struct State {
    HWND game = nullptr;
    DWORD thread = 0;
    int flags = 0;
    int epoch = 0;
    int hoverId = 0;
    int dragId = 0;
    int pressed = 0;
    HCURSOR cursor = nullptr;
    ComPtr<ID3D11Device> d3d;
    ComPtr<IDCompositionDevice> dcomp;
    ComPtr<IDCompositionTarget> target;
    ComPtr<IDCompositionVisual> root;
    ComPtr<ICoreWebView2Environment> env;
    std::map<int, std::unique_ptr<View>> views;
    std::deque<QueuedMessage> messages;
    bool profileCleared = false;
};

std::unique_ptr<State> gs;
std::wstring gLast;
int gNextId = 1;
int gEpoch = 0;

HRESULT Fail(HRESULT hr, const wchar_t* where) {
    gLast = std::wstring(where) + L" hr=" + std::to_wstring(static_cast<long>(hr));
    return hr;
}

void Emit(int id, const std::wstring& json, bool native, size_t limit = kMaxQueue) {
    if (gs && gs->messages.size() < limit) gs->messages.push_back(QueuedMessage{id, native, json});
}

void Notify(int id, const wchar_t* where, HRESULT hr, size_t limit) {
    try {
        Emit(id, L"{\"type\":\"obx.error\",\"view\":" + std::to_wstring(id) + L",\"where\":\"" + where + L"\",\"hr\":" + std::to_wstring(static_cast<long>(hr)) + L"}", true, limit);
    } catch (...) {
    }
}

HRESULT Report(int id, const wchar_t* where, HRESULT hr) {
    Notify(id, where, hr, kMaxQueue + 64);
    return Fail(hr, where);
}

View* Find(int id);

void Blocked(int id, const wchar_t* where) {
    View* v = Find(id);
    if (v && v->reports++ < kMaxReports) Notify(id, where, E_ACCESSDENIED, kMaxQueue);
}

void Status(int id, const wchar_t* key, const wchar_t* value) {
    Emit(id, L"{\"type\":\"obx.status\",\"view\":" + std::to_wstring(id) + L",\"" + key + L"\":\"" + value + L"\"}", true, kMaxQueue + 64);
}

template <class F>
HRESULT Guard(F f) {
    try {
        return f();
    } catch (...) {
        return Fail(E_FAIL, L"unexpected exception");
    }
}

// Constraint: every export runs on the thread that called Obx_Init (the Unity main thread).
template <class F>
int Run(F f) {
    if (gs && GetCurrentThreadId() != gs->thread) return Fail(RPC_E_WRONG_THREAD, L"called off the Obx_Init thread");
    return Guard(f);
}

View* Find(int id) {
    if (!gs) return nullptr;
    const auto it = gs->views.find(id);
    return it == gs->views.end() ? nullptr : it->second.get();
}

View* HitView(POINT p) {
    for (auto it = gs->views.rbegin(); it != gs->views.rend(); ++it) {
        View* v = it->second.get();
        if (v->visible && v->comp && PtInRect(&v->bounds, p)) return v;
    }
    return nullptr;
}

void EndDrag() {
    gs->dragId = 0;
    gs->pressed = 0;
    ReleaseCapture();
}

void SendLeave(int id) {
    View* v = Find(id);
    if (v && v->comp) v->comp->SendMouseInput(COREWEBVIEW2_MOUSE_EVENT_KIND_LEAVE, COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS_NONE, 0, POINT{0, 0});
}

bool IsDown(UINT msg) {
    return msg == WM_LBUTTONDOWN || msg == WM_LBUTTONDBLCLK || msg == WM_RBUTTONDOWN || msg == WM_RBUTTONDBLCLK ||
           msg == WM_MBUTTONDOWN || msg == WM_MBUTTONDBLCLK || msg == WM_XBUTTONDOWN || msg == WM_XBUTTONDBLCLK;
}

bool IsUp(UINT msg) {
    return msg == WM_LBUTTONUP || msg == WM_RBUTTONUP || msg == WM_MBUTTONUP || msg == WM_XBUTTONUP;
}

bool IsXButton(UINT msg) {
    return msg == WM_XBUTTONDOWN || msg == WM_XBUTTONUP || msg == WM_XBUTTONDBLCLK;
}

void ApplyScale(View* v, UINT dpi) {
    ComPtr<ICoreWebView2Controller3> controller3;
    if (v->comp && v->controller && SUCCEEDED(v->controller.As(&controller3))) controller3->put_RasterizationScale(dpi / 96.0);
}

bool Route(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp) {
    if (msg == WM_DPICHANGED) {
        for (auto& entry : gs->views) ApplyScale(entry.second.get(), HIWORD(wp));
        return false;
    }
    if (msg == WM_CANCELMODE || (msg == WM_CAPTURECHANGED && reinterpret_cast<HWND>(lp) != hwnd)) {
        if (gs->pressed) {
            SendLeave(gs->dragId);
            gs->hoverId = 0;
            EndDrag();
        }
        return false;
    }
    if (msg == WM_SETCURSOR) {
        POINT cursorPoint{};
        if (LOWORD(lp) != HTCLIENT || !gs->cursor || !GetCursorPos(&cursorPoint)) return false;
        ScreenToClient(hwnd, &cursorPoint);
        if (!HitView(cursorPoint)) return false;
        SetCursor(gs->cursor);
        return true;
    }
    if (msg < WM_MOUSEFIRST || msg > WM_MOUSEHWHEEL) return false;
    POINT p{GET_X_LPARAM(lp), GET_Y_LPARAM(lp)};
    const bool wheel = msg == WM_MOUSEWHEEL || msg == WM_MOUSEHWHEEL;
    if (wheel) ScreenToClient(hwnd, &p);
    View* target = gs->dragId ? Find(gs->dragId) : HitView(p);
    if (!target || !target->comp) {
        if (msg == WM_MOUSEMOVE && gs->hoverId) {
            SendLeave(gs->hoverId);
            gs->hoverId = 0;
        }
        return false;
    }
    if (IsUp(msg) && gs->pressed == 0) return false;
    if (gs->hoverId != target->id) {
        if (gs->hoverId) SendLeave(gs->hoverId);
        gs->hoverId = target->id;
    }
    UINT data = 0;
    if (wheel) data = static_cast<UINT>(GET_WHEEL_DELTA_WPARAM(wp));
    else if (IsXButton(msg)) data = HIWORD(wp);
    const POINT local{p.x - target->bounds.left, p.y - target->bounds.top};
    target->comp->SendMouseInput(static_cast<COREWEBVIEW2_MOUSE_EVENT_KIND>(msg), static_cast<COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS>(LOWORD(wp)), data, local);
    if (IsDown(msg)) {
        if (target->controller && !IsChild(hwnd, GetFocus())) target->controller->MoveFocus(COREWEBVIEW2_MOVE_FOCUS_REASON_PROGRAMMATIC);
        if (gs->pressed++ == 0) {
            gs->dragId = target->id;
            SetCapture(hwnd);
        }
    } else if (IsUp(msg) && --gs->pressed == 0) {
        EndDrag();
    }
    return true;
}

LRESULT CALLBACK Subclass(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp, UINT_PTR, DWORD_PTR) {
    bool handled = false;
    try {
        handled = gs && Route(hwnd, msg, wp, lp);
    } catch (...) {
        handled = false;
    }
    if (!handled) return DefSubclassProc(hwnd, msg, wp, lp);
    return msg == WM_SETCURSOR || IsXButton(msg) ? TRUE : 0;
}

HRESULT EnsureDComp() {
    if (gs->dcomp) return S_OK;
    HRESULT hr = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, D3D11_CREATE_DEVICE_BGRA_SUPPORT, nullptr, 0, D3D11_SDK_VERSION, gs->d3d.ReleaseAndGetAddressOf(), nullptr, nullptr);
    ComPtr<IDXGIDevice> dxgi;
    if (SUCCEEDED(hr)) hr = gs->d3d.As(&dxgi);
    if (SUCCEEDED(hr)) hr = DCompositionCreateDevice(dxgi.Get(), IID_PPV_ARGS(&gs->dcomp));
    if (SUCCEEDED(hr)) hr = gs->dcomp->CreateTargetForHwnd(gs->game, TRUE, &gs->target);
    if (SUCCEEDED(hr)) hr = gs->dcomp->CreateVisual(&gs->root);
    if (SUCCEEDED(hr)) hr = gs->target->SetRoot(gs->root.Get());
    if (SUCCEEDED(hr)) hr = gs->dcomp->Commit();
    if (FAILED(hr)) {
        gs->root.Reset();
        gs->target.Reset();
        gs->dcomp.Reset();
        gs->d3d.Reset();
    }
    return hr;
}

HRESULT AttachVisual(View* v) {
    HRESULT hr = EnsureDComp();
    if (SUCCEEDED(hr)) hr = gs->dcomp->CreateVisual(&v->visual);
    if (SUCCEEDED(hr)) hr = gs->root->AddVisual(v->visual.Get(), TRUE, nullptr);
    if (SUCCEEDED(hr)) hr = v->comp->put_RootVisualTarget(v->visual.Get());
    if (SUCCEEDED(hr)) hr = gs->dcomp->Commit();
    return hr;
}

void DetachVisual(View* v) {
    if (v->comp) v->comp->put_RootVisualTarget(nullptr);
    if (v->visual && gs->root) gs->root->RemoveVisual(v->visual.Get());
    if (gs->dcomp) gs->dcomp->Commit();
    v->visual.Reset();
    v->comp.Reset();
}

void ApplyBounds(View* v) {
    if (!v->controller) return;
    RECT r = v->bounds;
    if (v->visual) {
        v->visual->SetOffsetX(static_cast<float>(v->bounds.left));
        v->visual->SetOffsetY(static_cast<float>(v->bounds.top));
        gs->dcomp->Commit();
        r = RECT{0, 0, v->bounds.right - v->bounds.left, v->bounds.bottom - v->bounds.top};
    }
    v->controller->put_Bounds(r);
}

void ApplyVisible(View* v) {
    if (!v->controller) return;
    v->controller->put_IsVisible(v->visible ? TRUE : FALSE);
    if (v->visible) {
        v->controller->MoveFocus(COREWEBVIEW2_MOVE_FOCUS_REASON_PROGRAMMATIC);
    } else if (IsChild(gs->game, GetFocus())) {
        SetFocus(gs->game);
    }
}

HRESULT MapWebFolder(ICoreWebView2* web, const std::wstring& host, const std::wstring& folder) {
    ComPtr<ICoreWebView2_3> web3;
    HRESULT hr = web->QueryInterface(IID_PPV_ARGS(&web3));
    if (SUCCEEDED(hr)) hr = web3->SetVirtualHostNameToFolderMapping(host.c_str(), folder.c_str(), COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND_DENY_CORS);
    return hr;
}

void ReleasePending(View* v) {
    if (--v->pending == 0 && !v->url.empty()) v->web->Navigate(v->url.c_str());
}

void ScriptDone(int id, bool counted, HRESULT hr) {
    if (FAILED(hr)) Report(id, L"init-script", hr);
    View* v = Find(id);
    if (counted && v) ReleasePending(v);
}

HRESULT RegisterScript(int id, ICoreWebView2* web, const std::wstring& script, bool counted) {
    return web->AddScriptToExecuteOnDocumentCreated(script.c_str(), Callback<ICoreWebView2AddScriptToExecuteOnDocumentCreatedCompletedHandler>([id, counted](HRESULT result, LPCWSTR) -> HRESULT {
        return Guard([&] {
            ScriptDone(id, counted, result);
            return S_OK;
        });
    }).Get());
}

#include "ObxLockDown.h"

void CloseController(IUnknown* any) {
    ComPtr<ICoreWebView2Controller> controller;
    if (any && SUCCEEDED(any->QueryInterface(IID_PPV_ARGS(&controller)))) controller->Close();
}

HRESULT OnCursor(int id) {
    View* v = Find(id);
    HCURSOR cursor = nullptr;
    if (v && v->comp && SUCCEEDED(v->comp->get_Cursor(&cursor)) && cursor) {
        HCURSOR copy = CopyCursor(cursor);
        if (copy) {
            if (gs->cursor) DestroyCursor(gs->cursor);
            gs->cursor = copy;
        }
    }
    return S_OK;
}

void Attach(View* v) {
    ApplyBounds(v);
    ComPtr<ICoreWebView2Controller2> controller2;
    if (SUCCEEDED(v->controller.As(&controller2))) controller2->put_DefaultBackgroundColor(COREWEBVIEW2_COLOR{0, 0, 0, 0});
    ApplyScale(v, GetDpiForWindow(gs->game));
    HRESULT hr = v->controller->get_CoreWebView2(&v->web);
    if (FAILED(hr)) {
        Report(v->id, L"get_CoreWebView2", hr);
        return;
    }
    ComPtr<ICoreWebView2Settings> settings;
    v->web->get_Settings(&settings);
    settings->put_AreDefaultContextMenusEnabled(FALSE);
    settings->put_AreDevToolsEnabled((gs->flags & kFlagDevTools) != 0);
    settings->put_IsZoomControlEnabled(FALSE);
    settings->put_IsStatusBarEnabled(FALSE);
    settings->put_AreHostObjectsAllowed(FALSE);
    settings->put_AreDefaultScriptDialogsEnabled(FALSE);
    ComPtr<ICoreWebView2Settings3> settings3;
    if (SUCCEEDED(settings.As(&settings3))) settings3->put_AreBrowserAcceleratorKeysEnabled(FALSE);
    ComPtr<ICoreWebView2Settings4> settings4;
    if (SUCCEEDED(settings.As(&settings4))) {
        settings4->put_IsGeneralAutofillEnabled(FALSE);
        settings4->put_IsPasswordAutosaveEnabled(FALSE);
    }
    ComPtr<ICoreWebView2Settings6> settings6;
    if (SUCCEEDED(settings.As(&settings6))) settings6->put_IsSwipeNavigationEnabled(FALSE);
    if (FAILED(LockDown(v))) return;
    for (const auto& mapping : v->mappings) MapWebFolder(v->web.Get(), mapping.first, mapping.second);
    // Constraint: Attach holds a pending count until ReleasePending, so the first Navigate waits for every registration.
    ++v->pending;
    if (!gs->profileCleared) {
        gs->profileCleared = true;
        ClearServiceWorkers(v);
    }
    const HRESULT killHr = RegisterScript(v->id, v->web.Get(), kNoServiceWorker, true);
    if (FAILED(killHr)) ScriptDone(v->id, true, killHr);
    for (const auto& script : v->scripts) {
        const HRESULT scriptHr = RegisterScript(v->id, v->web.Get(), script, true);
        if (FAILED(scriptHr)) ScriptDone(v->id, true, scriptHr);
    }
    EventRegistrationToken messageToken{};
    v->web->add_WebMessageReceived(Callback<ICoreWebView2WebMessageReceivedEventHandler>([id = v->id](ICoreWebView2*, ICoreWebView2WebMessageReceivedEventArgs* args) -> HRESULT {
        return Guard([&] { return OnMessage(id, args); });
    }).Get(), &messageToken);
    if (v->comp) {
        EventRegistrationToken cursorToken{};
        v->comp->add_CursorChanged(Callback<ICoreWebView2CursorChangedEventHandler>([id = v->id](ICoreWebView2CompositionController*, IUnknown*) -> HRESULT {
            return Guard([&] { return OnCursor(id); });
        }).Get(), &cursorToken);
    }
    ApplyVisible(v);
    ReleasePending(v);
    Status(v->id, L"mode", v->comp ? L"composition" : L"windowed");
}

HRESULT OnWindowed(int id, HRESULT hr, ICoreWebView2Controller* controller);

HRESULT StartWindowed(View* v) {
    const int id = v->id;
    const HRESULT hr = gs->env->CreateCoreWebView2Controller(gs->game, Callback<ICoreWebView2CreateCoreWebView2ControllerCompletedHandler>([id](HRESULT result, ICoreWebView2Controller* controller) -> HRESULT {
        return Guard([&] { return OnWindowed(id, result, controller); });
    }).Get());
    return FAILED(hr) ? Report(id, L"windowed", hr) : S_OK;
}

HRESULT OnWindowed(int id, HRESULT hr, ICoreWebView2Controller* controller) {
    View* v = Find(id);
    if (!v) {
        CloseController(controller);
        return S_OK;
    }
    if (SUCCEEDED(hr) && !controller) hr = E_POINTER;
    if (FAILED(hr)) return Report(id, L"windowed", hr);
    v->controller = controller;
    Attach(v);
    return S_OK;
}

HRESULT OnComposition(int id, HRESULT hr, ICoreWebView2CompositionController* comp) {
    View* v = Find(id);
    if (!v) {
        CloseController(comp);
        return S_OK;
    }
    if (SUCCEEDED(hr) && !comp) hr = E_POINTER;
    if (SUCCEEDED(hr)) {
        v->comp = comp;
        hr = AttachVisual(v);
        if (SUCCEEDED(hr)) hr = v->comp.As(&v->controller);
        if (SUCCEEDED(hr)) {
            Attach(v);
            return S_OK;
        }
        const ComPtr<ICoreWebView2CompositionController> failed = v->comp;
        DetachVisual(v);
        v->controller.Reset();
        CloseController(failed.Get());
    }
    Report(id, L"composition", hr);
    return StartWindowed(v);
}

HRESULT StartView(View* v) {
    v->started = true;
    const int id = v->id;
    if (!(gs->flags & kFlagWindowed)) {
        ComPtr<ICoreWebView2Environment3> env3;
        HRESULT hr = gs->env.As(&env3);
        if (SUCCEEDED(hr)) hr = env3->CreateCoreWebView2CompositionController(gs->game, Callback<ICoreWebView2CreateCoreWebView2CompositionControllerCompletedHandler>([id](HRESULT result, ICoreWebView2CompositionController* comp) -> HRESULT {
            return Guard([&] { return OnComposition(id, result, comp); });
        }).Get());
        if (SUCCEEDED(hr)) return S_OK;
        Report(id, L"composition-start", hr);
    }
    return StartWindowed(v);
}

HRESULT OnEnvironment(int epoch, HRESULT hr, ICoreWebView2Environment* env) {
    if (!gs || gs->epoch != epoch) return S_OK;
    if (SUCCEEDED(hr) && !env) hr = E_POINTER;
    if (FAILED(hr)) return Report(0, L"environment", hr);
    gs->env = env;
    Status(0, L"env", L"ready");
    for (auto& entry : gs->views) {
        if (!entry.second->started) StartView(entry.second.get());
    }
    return S_OK;
}

void CloseView(View& v) {
    DetachVisual(&v);
    if (v.controller) v.controller->Close();
    v.web.Reset();
    v.controller.Reset();
}

HRESULT Init(HWND game, const wchar_t* userData, int flags) {
    if (gs) return S_OK;
    if (!game || !IsWindow(game)) return Fail(E_INVALIDARG, L"game window is not a valid HWND");
    HRESULT hr = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    if (hr == RPC_E_CHANGED_MODE) return Fail(hr, L"calling thread is not STA");
    if (FAILED(hr)) return Fail(hr, L"CoInitializeEx");
    LPWSTR version = nullptr;
    hr = GetAvailableCoreWebView2BrowserVersionString(nullptr, &version);
    const bool installed = SUCCEEDED(hr) && version != nullptr;
    CoTaskMemFree(version);
    if (!installed) return Fail(FAILED(hr) ? hr : E_FAIL, L"WebView2 runtime not installed");
    std::wstring folder = userData ? userData : L"";
    if (folder.empty()) {
        wchar_t base[MAX_PATH] = {};
        const DWORD length = GetEnvironmentVariableW(L"LOCALAPPDATA", base, MAX_PATH);
        if (length == 0 || length >= MAX_PATH) return Fail(E_FAIL, L"no LOCALAPPDATA for the default user data folder");
        folder = std::wstring(base) + L"\\ObenseuerCore\\WebView2";
    }
    const int epoch = ++gEpoch;
    auto state = std::make_unique<State>();
    state->game = game;
    state->thread = GetCurrentThreadId();
    state->flags = flags;
    state->epoch = epoch;
    gs = std::move(state);
    if (!SetWindowSubclass(game, Subclass, kSubclassId, 0)) {
        const HRESULT sub = HRESULT_FROM_WIN32(GetLastError());
        gs.reset();
        return Fail(sub, L"SetWindowSubclass");
    }
    hr = CreateCoreWebView2EnvironmentWithOptions(nullptr, folder.c_str(), nullptr, Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>([epoch](HRESULT result, ICoreWebView2Environment* env) -> HRESULT {
        return Guard([&] { return OnEnvironment(epoch, result, env); });
    }).Get());
    if (FAILED(hr)) {
        RemoveWindowSubclass(game, Subclass, kSubclassId);
        gs.reset();
        return Fail(hr, L"CreateCoreWebView2EnvironmentWithOptions");
    }
    return S_OK;
}

HRESULT Shutdown() {
    if (!gs) return S_OK;
    RemoveWindowSubclass(gs->game, Subclass, kSubclassId);
    EndDrag();
    for (auto& entry : gs->views) CloseView(*entry.second);
    gs->views.clear();
    gs->env.Reset();
    gs->root.Reset();
    gs->target.Reset();
    gs->dcomp.Reset();
    gs->d3d.Reset();
    if (gs->cursor) DestroyCursor(gs->cursor);
    gs.reset();
    return S_OK;
}

int CreateView(int x, int y, int w, int h, const wchar_t* url) {
    if (!gs) return Fail(E_UNEXPECTED, L"Obx_Init has not run");
    if (w <= 0 || h <= 0 || !url) return Fail(E_INVALIDARG, L"Obx_Create arguments");
    auto view = std::make_unique<View>();
    view->id = gNextId++;
    view->bounds = RECT{x, y, x + w, y + h};
    view->url = url;
    const int id = view->id;
    gs->views.emplace(id, std::move(view));
    if (gs->env) StartView(Find(id));
    return id;
}

HRESULT SetBounds(int id, int x, int y, int w, int h) {
    View* v = Find(id);
    if (!v) return Fail(E_INVALIDARG, L"unknown view");
    if (w <= 0 || h <= 0) return Fail(E_INVALIDARG, L"bounds size");
    v->bounds = RECT{x, y, x + w, y + h};
    ApplyBounds(v);
    return S_OK;
}

HRESULT SetVisible(int id, bool on) {
    View* v = Find(id);
    if (!v) return Fail(E_INVALIDARG, L"unknown view");
    v->visible = on;
    if (!on) {
        if (gs->hoverId == id) gs->hoverId = 0;
        if (gs->dragId == id) EndDrag();
    }
    ApplyVisible(v);
    return S_OK;
}

HRESULT Navigate(int id, const wchar_t* url) {
    View* v = Find(id);
    if (!v) return Fail(E_INVALIDARG, L"unknown view");
    if (!url) return Fail(E_INVALIDARG, L"url");
    v->url = url;
    return v->web ? v->web->Navigate(url) : S_OK;
}

HRESULT MapFolder(int id, const wchar_t* host, const wchar_t* folder) {
    View* v = Find(id);
    if (!v) return Fail(E_INVALIDARG, L"unknown view");
    if (!host || !folder) return Fail(E_INVALIDARG, L"host or folder");
    v->mappings.emplace_back(host, folder);
    return v->web ? MapWebFolder(v->web.Get(), host, folder) : S_OK;
}

HRESULT SetShellHost(int id, const wchar_t* host) {
    View* v = Find(id);
    if (!v) return Fail(E_INVALIDARG, L"unknown view");
    if (!host) return Fail(E_INVALIDARG, L"host");
    v->shell = host;
    return S_OK;
}

HRESULT PostJson(int id, const wchar_t* json) {
    View* v = Find(id);
    if (!v) return Fail(E_INVALIDARG, L"unknown view");
    if (!json) return Fail(E_INVALIDARG, L"json");
    if (!v->web) return Fail(E_PENDING, L"view not ready");
    return v->web->PostWebMessageAsJson(json);
}

HRESULT AddInitScript(int id, const wchar_t* script) {
    View* v = Find(id);
    if (!v) return Fail(E_INVALIDARG, L"unknown view");
    if (!script) return Fail(E_INVALIDARG, L"script");
    v->scripts.push_back(script);
    return v->web ? RegisterScript(v->id, v->web.Get(), v->scripts.back(), false) : S_OK;
}

HRESULT ExecuteScript(int id, const wchar_t* script) {
    View* v = Find(id);
    if (!v) return Fail(E_INVALIDARG, L"unknown view");
    if (!script) return Fail(E_INVALIDARG, L"script");
    if (!v->web) return Fail(E_PENDING, L"view not ready");
    return v->web->ExecuteScript(script, Callback<ICoreWebView2ExecuteScriptCompletedHandler>([](HRESULT, LPCWSTR) -> HRESULT {
        return S_OK;
    }).Get());
}

HRESULT DestroyView(int id) {
    View* v = Find(id);
    if (!v) return Fail(E_INVALIDARG, L"unknown view");
    if (gs->hoverId == id) gs->hoverId = 0;
    if (gs->dragId == id) EndDrag();
    CloseView(*v);
    gs->views.erase(id);
    return S_OK;
}

int PollMessage(wchar_t* buffer, int capacity, int* outId, int* outNative) {
    if (!buffer || capacity <= 0 || !outId || !outNative) return Fail(E_INVALIDARG, L"poll buffer");
    if (!gs || gs->messages.empty()) return 0;
    const QueuedMessage& front = gs->messages.front();
    const int length = static_cast<int>(front.json.size());
    if (length + 1 > capacity) return kBufferSmall;
    std::copy_n(front.json.c_str(), length + 1, buffer);
    *outId = front.id;
    *outNative = front.native ? 1 : 0;
    gs->messages.pop_front();
    return length;
}

int LastError(wchar_t* buffer, int capacity) {
    if (!buffer || capacity <= 0) return E_INVALIDARG;
    const int length = static_cast<int>(std::min(gLast.size(), static_cast<size_t>(capacity - 1)));
    std::copy_n(gLast.c_str(), length, buffer);
    buffer[length] = L'\0';
    return length;
}

}

extern "C" {

__declspec(dllexport) int __stdcall Obx_Init(HWND gameWindow, const wchar_t* userDataFolder, int flags) {
    return Run([&] { return Init(gameWindow, userDataFolder, flags); });
}

__declspec(dllexport) int __stdcall Obx_Create(int x, int y, int w, int h, const wchar_t* url) {
    return Run([&] { return CreateView(x, y, w, h, url); });
}

__declspec(dllexport) int __stdcall Obx_SetBounds(int id, int x, int y, int w, int h) {
    return Run([&] { return SetBounds(id, x, y, w, h); });
}

__declspec(dllexport) int __stdcall Obx_SetVisible(int id, int visible) {
    return Run([&] { return SetVisible(id, visible != 0); });
}

__declspec(dllexport) int __stdcall Obx_Navigate(int id, const wchar_t* url) {
    return Run([&] { return Navigate(id, url); });
}

__declspec(dllexport) int __stdcall Obx_MapFolder(int id, const wchar_t* host, const wchar_t* folder) {
    return Run([&] { return MapFolder(id, host, folder); });
}

__declspec(dllexport) int __stdcall Obx_SetShellHost(int id, const wchar_t* host) {
    return Run([&] { return SetShellHost(id, host); });
}

__declspec(dllexport) int __stdcall Obx_PostJson(int id, const wchar_t* json) {
    return Run([&] { return PostJson(id, json); });
}

__declspec(dllexport) int __stdcall Obx_ExecuteScript(int id, const wchar_t* script) {
    return Run([&] { return ExecuteScript(id, script); });
}

__declspec(dllexport) int __stdcall Obx_AddInitScript(int id, const wchar_t* script) {
    return Run([&] { return AddInitScript(id, script); });
}

__declspec(dllexport) int __stdcall Obx_Destroy(int id) {
    return Run([&] { return DestroyView(id); });
}

__declspec(dllexport) int __stdcall Obx_Shutdown() {
    return Run([] { return Shutdown(); });
}

__declspec(dllexport) int __stdcall Obx_PollMessage(wchar_t* buffer, int capacity, int* outId, int* outNative) {
    return Run([&] { return PollMessage(buffer, capacity, outId, outNative); });
}

__declspec(dllexport) int __stdcall Obx_LastError(wchar_t* buffer, int capacity) {
    return LastError(buffer, capacity);
}

}
