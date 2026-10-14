#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <objbase.h>
#include <windowsx.h>
#include <commctrl.h>
#include <d3d11.h>
#include <dcomp.h>
#include <dxgi.h>
#include <process.h>
#include <wrl.h>
#include <algorithm>
#include <atomic>
#include <deque>
#include <functional>
#include <map>
#include <memory>
#include <mutex>
#include <set>
#include <string>
#include <utility>
#include <vector>
#include <WebView2.h>
using Microsoft::WRL::Callback;
using Microsoft::WRL::ComPtr;

namespace {

constexpr int kFlagDevTools = 1;
constexpr int kFlagWindowed = 2;
constexpr int kFlagComposition = 4;
constexpr UINT_PTR kSubclassId = 1;
const HRESULT kBufferSmall = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
// Constraint: webui/src/WebUi.cs MaxBuffer (1 << 20) must stay above kMaxMessage.
constexpr size_t kMaxMessage = 1 << 14;
constexpr size_t kMaxQueue = 512;
constexpr int kMaxReports = 64;
constexpr size_t kMaxFrameUrls = 32;
constexpr size_t kMaxSourceChars = 256;

struct View {
    int id = 0;
    RECT bounds{};
    bool visible = false;
    bool started = false;
    int reports = 0;
    ULONGLONG reportAt = 0;
    int frameOkReports = 0;
    int frameFailReports = 0;
    int pending = 0;
    bool focused = false;
    bool typing = false;
    HWND popup = nullptr;
    std::wstring url;
    std::wstring shell;
    std::vector<std::pair<std::wstring, std::wstring>> mappings;
    std::vector<std::wstring> frameHosts;
    std::map<UINT64, std::wstring> frameUrls;
    std::wstring script;
    std::wstring scriptId;
    int scriptSeq = 0;
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
    bool threaded = false;
    bool stopping = false;
    bool suspended = false;
    bool profileCleared = false;
    std::wstring folder;
    HANDLE shim = nullptr;
    unsigned shimId = 0;
    HANDLE ready = nullptr;
    RECT placed{};
    int hoverId = 0;
    int dragId = 0;
    int pressed = 0;
    POINT moveAt{};
    HCURSOR cursor = nullptr;
    ComPtr<ID3D11Device> d3d;
    ComPtr<IDCompositionDevice> dcomp;
    ComPtr<IDCompositionTarget> target;
    ComPtr<IDCompositionVisual> root;
    ComPtr<ICoreWebView2Environment> env;
    std::map<int, std::unique_ptr<View>> views;
    std::deque<QueuedMessage> messages;
    size_t dropped = 0;
};

std::unique_ptr<State> gs;
std::mutex gLock;
std::wstring gLast;
std::deque<std::function<void()>> gTasks;
std::set<int> gKnown;
int gNextId = 1;
int gEpoch = 0;

HRESULT Fail(HRESULT hr, const wchar_t* where) {
    std::lock_guard<std::mutex> hold(gLock);
    gLast = std::wstring(where) + L" hr=" + std::to_wstring(static_cast<long>(hr));
    return hr;
}

void Emit(int id, const std::wstring& json, bool native, size_t limit = kMaxQueue) {
    if (!gs) return;
    std::lock_guard<std::mutex> hold(gLock);
    if (json.size() <= kMaxMessage && gs->messages.size() < limit) gs->messages.push_back(QueuedMessage{id, native, json});
    else ++gs->dropped;
}

void Notify(int id, const wchar_t* where, HRESULT hr, size_t limit, const std::wstring& extra = std::wstring()) {
    try {
        Emit(id, L"{\"type\":\"obx.error\",\"view\":" + std::to_wstring(id) + L",\"where\":\"" + where + L"\",\"hr\":" + std::to_wstring(static_cast<long>(hr)) + extra + L"}", true, limit);
    } catch (...) {
    }
}

HRESULT Report(int id, const wchar_t* where, HRESULT hr) {
    Notify(id, where, hr, kMaxQueue + 64);
    return Fail(hr, where);
}

View* Find(int id);

void Blocked(int id, const wchar_t* where, const std::wstring& extra = std::wstring()) {
    View* v = Find(id);
    if (!v) return;
    const ULONGLONG now = GetTickCount64();
    if (now - v->reportAt >= 1000) {
        v->reportAt = now;
        v->reports = 0;
    }
    if (v->reports++ < kMaxReports) Notify(id, where, E_ACCESSDENIED, kMaxQueue, extra);
}

void Status(int id, const wchar_t* key, const wchar_t* value) {
    Emit(id, L"{\"type\":\"obx.status\",\"view\":" + std::to_wstring(id) + L",\"" + key + L"\":\"" + value + L"\"}", true, kMaxQueue + 64);
}

#include "ObxTiming.h"

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

// Constraint: gKnown is only read and written on the Obx_Init thread.
bool Known(int id) {
    return gKnown.count(id) != 0;
}

#include "ObxComposition.h"
#include "ObxThread.h"

void ApplyBounds(View* v) {
    if (v->popup) {
        PlacePopup(v);
        return;
    }
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

void ApplyVisible(View* v, bool raise = false) {
    if (!v->controller) return;
    const bool shown = v->visible && !(v->popup && gs->suspended);
    if (v->popup) ShowPopup(v, shown, raise);
    v->controller->put_IsVisible(v->visible ? TRUE : FALSE);
    if (shown) {
        if (!v->popup && !v->focused) Timed(v->id, L"MoveFocus", [&] { v->controller->MoveFocus(COREWEBVIEW2_MOVE_FOCUS_REASON_PROGRAMMATIC); });
    } else if (v->focused) {
        v->focused = false;
        if (!v->popup) SetFocus(gs->game);
    }
}

HRESULT MapWebFolder(ICoreWebView2* web, const std::wstring& host, const std::wstring& folder) {
    ComPtr<ICoreWebView2_3> web3;
    HRESULT hr = web->QueryInterface(IID_PPV_ARGS(&web3));
    if (SUCCEEDED(hr)) hr = web3->SetVirtualHostNameToFolderMapping(host.c_str(), folder.c_str(), COREWEBVIEW2_HOST_RESOURCE_ACCESS_KIND_DENY_CORS);
    return hr;
}

void ReleasePending(View* v) {
    if (--v->pending == 0 && !v->url.empty()) {
        const HRESULT hr = v->web->Navigate(v->url.c_str());
        if (FAILED(hr)) Report(v->id, L"navigate", hr);
    }
}

void ScriptDone(int id, bool counted, HRESULT hr, bool lockdown) {
    if (FAILED(hr)) Report(id, lockdown ? L"lockdown" : L"init-script", hr);
    View* v = Find(id);
    if (counted && v && !(lockdown && FAILED(hr))) ReleasePending(v);
}

HRESULT RegisterScript(View* v, const std::wstring& script, bool counted, bool lockdown) {
    const int id = v->id;
    const int seq = lockdown ? 0 : ++v->scriptSeq;
    if (counted) ++v->pending;
    return v->web->AddScriptToExecuteOnDocumentCreated(script.c_str(), Callback<ICoreWebView2AddScriptToExecuteOnDocumentCreatedCompletedHandler>([id, counted, lockdown, seq](HRESULT result, LPCWSTR scriptId) -> HRESULT {
        return Guard([&] {
            View* view = Find(id);
            if (!lockdown && view && SUCCEEDED(result)) {
                if (seq == view->scriptSeq) view->scriptId = scriptId;
                else {
                    const HRESULT removed = view->web->RemoveScriptToExecuteOnDocumentCreated(scriptId);
                    if (FAILED(removed)) Report(id, L"init-script", removed);
                }
            }
            ScriptDone(id, counted, result, lockdown);
            return S_OK;
        });
    }).Get());
}
#include "ObxLockDown.h"

void CloseController(IUnknown* any) {
    ComPtr<ICoreWebView2Controller> controller;
    if (any && SUCCEEDED(any->QueryInterface(IID_PPV_ARGS(&controller)))) controller->Close();
}

void Attach(View* v) {
    ApplyBounds(v);
    ComPtr<ICoreWebView2Controller2> controller2;
    if (SUCCEEDED(v->controller.As(&controller2))) controller2->put_DefaultBackgroundColor(COREWEBVIEW2_COLOR{0, 0, 0, 0});
    ApplyScale(v, GetDpiForWindow(gs->game));
    // Constraint: Attach holds a pending count until ReleasePending, so the first Navigate waits for every registration. A failed step keeps the count, so no page loads unfiltered.
    ++v->pending;
    HRESULT hr = v->controller->get_CoreWebView2(&v->web);
    if (FAILED(hr)) {
        Report(v->id, L"get_CoreWebView2", hr);
        return;
    }
    RecordBrowserPid(v->web.Get());
    ComPtr<ICoreWebView2Settings> settings;
    hr = v->web->get_Settings(&settings);
    if (SUCCEEDED(hr)) hr = settings->put_AreDefaultContextMenusEnabled(FALSE);
    if (SUCCEEDED(hr)) hr = settings->put_AreDevToolsEnabled((gs->flags & kFlagDevTools) != 0);
    if (SUCCEEDED(hr)) hr = settings->put_IsZoomControlEnabled(FALSE);
    if (SUCCEEDED(hr)) hr = settings->put_IsStatusBarEnabled(FALSE);
    if (SUCCEEDED(hr)) hr = settings->put_AreHostObjectsAllowed(FALSE);
    if (SUCCEEDED(hr)) hr = settings->put_AreDefaultScriptDialogsEnabled(FALSE);
    ComPtr<ICoreWebView2Settings3> settings3;
    if (SUCCEEDED(hr)) hr = settings.As(&settings3);
    if (SUCCEEDED(hr)) hr = settings3->put_AreBrowserAcceleratorKeysEnabled(FALSE);
    ComPtr<ICoreWebView2Settings4> settings4;
    if (SUCCEEDED(hr)) hr = settings.As(&settings4);
    if (SUCCEEDED(hr)) hr = settings4->put_IsGeneralAutofillEnabled(FALSE);
    if (SUCCEEDED(hr)) hr = settings4->put_IsPasswordAutosaveEnabled(FALSE);
    ComPtr<ICoreWebView2Settings6> settings6;
    if (SUCCEEDED(hr)) hr = settings.As(&settings6);
    if (SUCCEEDED(hr)) hr = settings6->put_IsSwipeNavigationEnabled(FALSE);
    if (FAILED(hr)) {
        Report(v->id, L"lockdown", hr);
        return;
    }
    if (FAILED(LockDown(v))) return;
    for (const auto& mapping : v->mappings) {
        const HRESULT mapHr = MapWebFolder(v->web.Get(), mapping.first, mapping.second);
        if (FAILED(mapHr)) {
            Report(v->id, L"map-folder", mapHr);
            return;
        }
    }
    if (!gs->profileCleared) {
        gs->profileCleared = true;
        ClearServiceWorkers(v);
    }
    const HRESULT killHr = RegisterScript(v, kNoServiceWorker, true, true);
    if (FAILED(killHr)) ScriptDone(v->id, true, killHr, true);
    if (!v->script.empty()) {
        const HRESULT scriptHr = RegisterScript(v, v->script, true, false);
        if (FAILED(scriptHr)) ScriptDone(v->id, true, scriptHr, false);
    }
    if (v->popup) {
        const HRESULT typingHr = RegisterScript(v, kTypingScript, true, true);
        if (FAILED(typingHr)) ScriptDone(v->id, true, typingHr, true);
    }
    EventRegistrationToken messageToken{};
    hr = v->web->add_WebMessageReceived(Callback<ICoreWebView2WebMessageReceivedEventHandler>([id = v->id](ICoreWebView2*, ICoreWebView2WebMessageReceivedEventArgs* args) -> HRESULT {
        return Guard([&] { return OnMessage(id, args); });
    }).Get(), &messageToken);
    if (FAILED(hr)) {
        Report(v->id, L"lockdown", hr);
        return;
    }
    if (v->comp) {
        EventRegistrationToken cursorToken{};
        v->comp->add_CursorChanged(Callback<ICoreWebView2CursorChangedEventHandler>([id = v->id](ICoreWebView2CompositionController*, IUnknown*) -> HRESULT {
            return Guard([&] { return OnCursor(id); });
        }).Get(), &cursorToken);
    }
    ApplyVisible(v);
    ReleasePending(v);
    EmitMode(v);
}

HRESULT OnWindowed(int id, HRESULT hr, ICoreWebView2Controller* controller);

HRESULT StartWindowed(View* v) {
    const int id = v->id;
    const HRESULT hr = gs->env->CreateCoreWebView2Controller(v->popup ? v->popup : gs->game, Callback<ICoreWebView2CreateCoreWebView2ControllerCompletedHandler>([id](HRESULT result, ICoreWebView2Controller* controller) -> HRESULT {
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
    if (gs->flags & kFlagComposition) {
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
    ShowPopup(&v, false);
    DetachVisual(&v);
    if (v.controller) v.controller->Close();
    v.web.Reset();
    v.controller.Reset();
    if (v.popup) {
        DestroyWindow(v.popup);
        v.popup = nullptr;
    }
}

void CloseAll() {
    for (auto& entry : gs->views) CloseView(*entry.second);
    gs->views.clear();
    gs->env.Reset();
}

HRESULT StartEnvironment() {
    const int epoch = gs->epoch;
    return CreateCoreWebView2EnvironmentWithOptions(nullptr, gs->folder.c_str(), nullptr, Callback<ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler>([epoch](HRESULT result, ICoreWebView2Environment* env) -> HRESULT {
        return Guard([&] { return OnEnvironment(epoch, result, env); });
    }).Get());
}

HRESULT Init(HWND game, const wchar_t* userData, int flags) {
    if (gs) return gs->stopping ? Fail(E_UNEXPECTED, L"previous shutdown did not finish") : S_OK;
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
    auto state = std::make_unique<State>();
    state->game = game;
    state->thread = GetCurrentThreadId();
    state->flags = flags;
    state->epoch = ++gEpoch;
    state->threaded = (flags & (kFlagComposition | kFlagWindowed)) == 0;
    state->folder = folder;
    gs = std::move(state);
    if (gs->threaded) {
        hr = StartShim();
        if (FAILED(hr) && SUCCEEDED(StopShim())) gs.reset();
        return hr;
    }
    if (!SetWindowSubclass(game, Subclass, kSubclassId, 0)) {
        const HRESULT sub = HRESULT_FROM_WIN32(GetLastError());
        gs.reset();
        return Fail(sub, L"SetWindowSubclass");
    }
    hr = StartEnvironment();
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
    if (gs->threaded) {
        const HRESULT stopped = StopShim();
        if (FAILED(stopped)) return stopped;
    } else {
        EndDrag();
        CloseAll();
    }
    gs->root.Reset();
    gs->target.Reset();
    gs->dcomp.Reset();
    gs->d3d.Reset();
    if (gs->cursor) DestroyCursor(gs->cursor);
    gs.reset();
    gKnown.clear();
    return S_OK;
}

int CreateView(int x, int y, int w, int h, const wchar_t* url) {
    if (!gs) return Fail(E_UNEXPECTED, L"Obx_Init has not run");
    if (w <= 0 || h <= 0 || !url) return Fail(E_INVALIDARG, L"Obx_Create arguments");
    const int id = gNextId++;
    gKnown.insert(id);
    const std::wstring target = url;
    Remote(L"Obx_Create", id, [=] {
        auto view = std::make_unique<View>();
        view->id = id;
        view->bounds = RECT{x, y, x + w, y + h};
        view->url = target;
        if (gs->threaded) {
            view->popup = CreatePopup(*view);
            if (!view->popup) return Report(id, L"popup", E_FAIL);
        }
        View* created = view.get();
        gs->views.emplace(id, std::move(view));
        if (gs->env) StartView(created);
        return S_OK;
    });
    return id;
}

HRESULT SetBounds(int id, int x, int y, int w, int h) {
    if (w <= 0 || h <= 0) return Fail(E_INVALIDARG, L"bounds size");
    return OnView(id, L"Obx_SetBounds", [=] {
        View* v = Find(id);
        if (!v) return Fail(E_INVALIDARG, L"unknown view");
        v->bounds = RECT{x, y, x + w, y + h};
        ApplyBounds(v);
        return S_OK;
    });
}

HRESULT SetVisible(int id, bool on) {
    return OnView(id, L"Obx_SetVisible", [=] {
        View* v = Find(id);
        if (!v) return Fail(E_INVALIDARG, L"unknown view");
        v->visible = on;
        if (!on) {
            if (gs->hoverId == id) gs->hoverId = 0;
            if (gs->dragId == id) EndDrag();
        }
        ApplyVisible(v, on);
        return S_OK;
    });
}

HRESULT Navigate(int id, const wchar_t* url) {
    if (!url) return Fail(E_INVALIDARG, L"url");
    const std::wstring target = url;
    return OnView(id, L"Obx_Navigate", [=] {
        View* v = Find(id);
        if (!v) return Fail(E_INVALIDARG, L"unknown view");
        v->url = target;
        return v->web && v->pending == 0 ? v->web->Navigate(target.c_str()) : S_OK;
    });
}

HRESULT MapFolder(int id, const wchar_t* host, const wchar_t* folder) {
    if (!host || !folder) return Fail(E_INVALIDARG, L"host or folder");
    const std::wstring name = host;
    const std::wstring path = folder;
    return OnView(id, L"Obx_MapFolder", [=] {
        View* v = Find(id);
        if (!v) return Fail(E_INVALIDARG, L"unknown view");
        if (!v->web) {
            v->mappings.emplace_back(name, path);
            return S_OK;
        }
        const HRESULT hr = MapWebFolder(v->web.Get(), name, path);
        if (SUCCEEDED(hr)) v->mappings.emplace_back(name, path);
        return hr;
    });
}

HRESULT SetShellHost(int id, const wchar_t* host) {
    if (!host) return Fail(E_INVALIDARG, L"host");
    const std::wstring name = host;
    return OnView(id, L"Obx_SetShellHost", [=] {
        View* v = Find(id);
        if (!v) return Fail(E_INVALIDARG, L"unknown view");
        v->shell = name;
        return S_OK;
    });
}

HRESULT PostJson(int id, const wchar_t* json) {
    if (!json) return Fail(E_INVALIDARG, L"json");
    const std::wstring text = json;
    return OnView(id, L"Obx_PostJson", [=] {
        View* v = Find(id);
        if (!v) return Fail(E_INVALIDARG, L"unknown view");
        if (!v->web) return Fail(E_PENDING, L"view not ready");
        return v->web->PostWebMessageAsJson(text.c_str());
    });
}

HRESULT AddInitScript(int id, const wchar_t* script) {
    if (!script) return Fail(E_INVALIDARG, L"script");
    const std::wstring text = script;
    return OnView(id, L"Obx_AddInitScript", [=] {
        View* v = Find(id);
        if (!v) return Fail(E_INVALIDARG, L"unknown view");
        v->script = text;
        if (!v->web) return S_OK;
        if (!v->scriptId.empty()) {
            const HRESULT removed = v->web->RemoveScriptToExecuteOnDocumentCreated(v->scriptId.c_str());
            if (FAILED(removed)) Report(v->id, L"init-script", removed);
            v->scriptId.clear();
        }
        return RegisterScript(v, v->script, false, false);
    });
}

HRESULT ExecuteScript(int id, const wchar_t* script) {
    if (!script) return Fail(E_INVALIDARG, L"script");
    const std::wstring text = script;
    return OnView(id, L"Obx_ExecuteScript", [=] {
        View* v = Find(id);
        if (!v) return Fail(E_INVALIDARG, L"unknown view");
        if (!v->web) return Fail(E_PENDING, L"view not ready");
        return v->web->ExecuteScript(text.c_str(), Callback<ICoreWebView2ExecuteScriptCompletedHandler>([](HRESULT, LPCWSTR) -> HRESULT {
            return S_OK;
        }).Get());
    });
}

HRESULT DestroyView(int id) {
    if (!Known(id)) return Fail(E_INVALIDARG, L"unknown view");
    gKnown.erase(id);
    return Remote(L"Obx_Destroy", id, [=] {
        View* v = Find(id);
        if (!v) return Fail(E_INVALIDARG, L"unknown view");
        if (gs->hoverId == id) gs->hoverId = 0;
        if (gs->dragId == id) EndDrag();
        CloseView(*v);
        gs->views.erase(id);
        return S_OK;
    });
}

HRESULT SetSuspended(bool on) {
    return Remote(L"Obx_SetSuspended", 0, [=] {
        gs->suspended = on;
        for (auto& entry : gs->views) {
            if (entry.second->popup) ApplyVisible(entry.second.get());
        }
        return S_OK;
    });
}

int PollMessage(wchar_t* buffer, int capacity, int* outId, int* outNative) {
    if (!buffer || capacity <= 0 || !outId || !outNative) return Fail(E_INVALIDARG, L"poll buffer");
    if (!gs) return 0;
    int length = 0;
    size_t dropped = 0;
    {
        std::lock_guard<std::mutex> hold(gLock);
        if (gs->messages.empty()) return 0;
        const QueuedMessage& front = gs->messages.front();
        length = static_cast<int>(front.json.size());
        if (length + 1 > capacity) return kBufferSmall;
        std::copy_n(front.json.c_str(), length + 1, buffer);
        *outId = front.id;
        *outNative = front.native ? 1 : 0;
        gs->messages.pop_front();
        if (gs->dropped && gs->messages.empty()) dropped = std::exchange(gs->dropped, 0);
    }
    if (dropped) Notify(0, L"messages-dropped", E_FAIL, kMaxQueue + 64, L",\"dropped\":" + std::to_wstring(dropped));
    return length;
}

int LastError(wchar_t* buffer, int capacity) {
    if (!buffer || capacity <= 0) return E_INVALIDARG;
    std::lock_guard<std::mutex> hold(gLock);
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

__declspec(dllexport) int __stdcall Obx_AllowFrameHost(int id, const wchar_t* host) {
    return Run([&] { return AllowFrameHost(id, host); });
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

__declspec(dllexport) int __stdcall Obx_SetSuspended(int suspended) {
    return Run([&] { return SetSuspended(suspended != 0); });
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
