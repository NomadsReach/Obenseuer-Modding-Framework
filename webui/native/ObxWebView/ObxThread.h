constexpr UINT kTaskMessage = WM_APP + 1;
constexpr UINT_PTR kFollowTimer = 1;
constexpr UINT_PTR kHandBackTimer = 2;
constexpr UINT_PTR kHoldSubclassId = 2;
std::atomic<DWORD> gBrowserPid{0};
constexpr UINT kFollowMs = 100;
constexpr UINT kHandBackMs = 250;
constexpr int kFlagFocusDiag = 8;
constexpr UINT kDiagNearMs = 100;
constexpr UINT kDiagFarMs = 500;
constexpr DWORD kStartMs = 5000;
constexpr DWORD kStopMs = 3000;
const wchar_t* const kPopupClass = L"ObxWebViewPopup";

UINT ForwardActivateMessage() {
    static const UINT message = RegisterWindowMessageW(L"ObxForwardActivate");
    return message;
}

UINT ForwardActivateAppMessage() {
    static const UINT message = RegisterWindowMessageW(L"ObxForwardActivateApp");
    return message;
}

void CloseView(View& v);
void CloseAll();
HRESULT StartEnvironment();
void Foreground(HWND target);
bool OwnedByGame(HWND w);
bool GainedByOurs(LPARAM thread);

View* PopupView(HWND popup) {
    for (auto& entry : gs->views) {
        if (entry.second->popup == popup) return entry.second.get();
    }
    return nullptr;
}

std::wstring gDiagWhere;
ULONGLONG gDiagNearAt = 0;
ULONGLONG gDiagFarAt = 0;

std::wstring Hex(HWND w) {
    wchar_t text[24];
    swprintf_s(text, L"0x%llx", static_cast<unsigned long long>(reinterpret_cast<UINT_PTR>(w)));
    return text;
}

std::wstring Describe(HWND w) {
    if (!w) return L"none";
    wchar_t cls[256] = L"";
    GetClassNameW(w, cls, 256);
    DWORD pid = 0;
    GetWindowThreadProcessId(w, &pid);
    return Hex(w) + L" class " + cls + L" pid " + std::to_wstring(pid) + (pid == GetCurrentProcessId() ? L" (ours)" : L" (other)") + L" owner " + Hex(GetWindow(w, GW_OWNER)) + L" rootOwner " + Hex(GetAncestor(w, GA_ROOTOWNER));
}

void DiagLine(const std::wstring& where) {
    Emit(0, L"{\"type\":\"obx.diag\",\"text\":\"" + where + L": front " + Describe(GetForegroundWindow()) + L"\"}", true, kMaxQueue + 64);
}

void DiagLater() {
    const ULONGLONG now = GetTickCount64();
    if (gDiagNearAt && now >= gDiagNearAt) {
        gDiagNearAt = 0;
        DiagLine(gDiagWhere + L", " + std::to_wstring(kDiagNearMs) + L" ms later");
    }
    if (gDiagFarAt && now >= gDiagFarAt) {
        gDiagFarAt = 0;
        DiagLine(gDiagWhere + L", " + std::to_wstring(kDiagFarMs) + L" ms later");
    }
}

void FocusDiag(const wchar_t* where) {
    if (!gs || !gs->threaded || (gs->flags & kFlagFocusDiag) == 0) return;
    gDiagWhere = where;
    DiagLine(gDiagWhere);
    gDiagNearAt = GetTickCount64() + kDiagNearMs;
    gDiagFarAt = GetTickCount64() + kDiagFarMs;
}

HWND OtherWindow(LPARAM lp) {
    return lp ? reinterpret_cast<HWND>(lp) : GetForegroundWindow();
}

LRESULT CALLBACK PopupProc(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp) {
    if (msg == WM_CLOSE) {
        PostMessageW(gs->game, WM_CLOSE, 0, 0);
        return 0;
    }
    if (msg == WM_MOUSEACTIVATE) return MA_NOACTIVATE;
    if (msg == WM_ACTIVATEAPP && !GainedByOurs(lp)) PostMessageW(gs->game, ForwardActivateAppMessage(), wp, lp);
    if (msg == WM_ACTIVATE) {
        const HWND other = LOWORD(wp) == WA_INACTIVE ? OtherWindow(lp) : reinterpret_cast<HWND>(lp);
        if (!OwnedByGame(other) && !GainedByOurs(GetWindowThreadProcessId(other, nullptr))) PostMessageW(gs->game, ForwardActivateMessage(), wp, lp);
    }
    if (msg == WM_DESTROY) FocusDiag(L"after controller close, popup destroy");
    View* v = msg == WM_ACTIVATE || msg == WM_TIMER ? PopupView(hwnd) : nullptr;
    if (v && msg == WM_ACTIVATE) {
        if (LOWORD(wp) == WA_INACTIVE) {
            v->typing = false;
            KillTimer(hwnd, kHandBackTimer);
        } else if (!v->typing) {
            SetTimer(hwnd, kHandBackTimer, kHandBackMs, nullptr);
        }
    }
    if (msg == WM_TIMER && wp == kHandBackTimer) {
        KillTimer(hwnd, kHandBackTimer);
        if (v && !v->typing && GetForegroundWindow() == hwnd) {
            FocusDiag(L"hand-back timer, before");
            Foreground(gs->game);
            FocusDiag(L"hand-back timer, after");
        }
        return 0;
    }
    return DefWindowProcW(hwnd, msg, wp, lp);
}

HINSTANCE ModuleHandle() {
    HMODULE module = nullptr;
    GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, reinterpret_cast<LPCWSTR>(&kPopupClass), &module);
    return module;
}

bool EnsurePopupClass() {
    WNDCLASSEXW wc{};
    wc.cbSize = sizeof(wc);
    wc.lpfnWndProc = PopupProc;
    wc.hInstance = ModuleHandle();
    wc.lpszClassName = kPopupClass;
    wc.hbrBackground = static_cast<HBRUSH>(GetStockObject(BLACK_BRUSH));
    return RegisterClassExW(&wc) != 0 || GetLastError() == ERROR_CLASS_ALREADY_EXISTS;
}

HWND CreatePopup(const View& v) {
    if (!EnsurePopupClass()) return nullptr;
    return CreateWindowExW(WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE, kPopupClass, L"", WS_POPUP, 0, 0, v.bounds.right - v.bounds.left, v.bounds.bottom - v.bounds.top, gs->game, nullptr, ModuleHandle(), nullptr);
}

void PlacePopup(View* v) {
    if (!v->popup) return;
    POINT origin{};
    ClientToScreen(gs->game, &origin);
    const int width = v->bounds.right - v->bounds.left;
    const int height = v->bounds.bottom - v->bounds.top;
    SetWindowPos(v->popup, nullptr, origin.x + v->bounds.left, origin.y + v->bounds.top, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
    if (v->controller) {
        v->controller->put_Bounds(RECT{0, 0, width, height});
        v->controller->NotifyParentWindowPositionChanged();
    }
}

void FollowGame() {
    RECT client{};
    if (!GetClientRect(gs->game, &client) || client.right <= 0 || client.bottom <= 0) return;
    POINT origin{};
    ClientToScreen(gs->game, &origin);
    const RECT screen{origin.x, origin.y, origin.x + client.right, origin.y + client.bottom};
    if (EqualRect(&screen, &gs->placed)) return;
    gs->placed = screen;
    for (auto& entry : gs->views) PlacePopup(entry.second.get());
}

void CALLBACK FollowTimer(HWND, UINT, UINT_PTR, DWORD) {
    try {
        FollowGame();
        DiagLater();
    } catch (...) {
    }
}

bool OwnedByGame(HWND w) {
    if (!w || !gs) return false;
    DWORD pid = 0;
    GetWindowThreadProcessId(w, &pid);
    return pid == GetCurrentProcessId() || GetAncestor(w, GA_ROOTOWNER) == gs->game;
}

bool BelongsToGame(HWND w) {
    if (!w) return false;
    return OwnedByGame(w) || PopupView(GetAncestor(w, GA_ROOT)) != nullptr;
}

bool GainedByOurs(LPARAM thread) {
    HANDLE t = OpenThread(THREAD_QUERY_LIMITED_INFORMATION, FALSE, static_cast<DWORD>(thread));
    const DWORD pid = t ? GetProcessIdOfThread(t) : 0;
    if (t) CloseHandle(t);
    return pid && (pid == GetCurrentProcessId() || pid == gBrowserPid);
}

LRESULT CALLBACK HoldActive(HWND hwnd, UINT msg, WPARAM wp, LPARAM lp, UINT_PTR, DWORD_PTR) {
    if (msg == ForwardActivateMessage() || msg == ForwardActivateAppMessage()) {
        const bool away = msg == ForwardActivateMessage() ? LOWORD(wp) == WA_INACTIVE : wp == FALSE;
        const HWND front = GetForegroundWindow();
        if (away && (OwnedByGame(front) || GainedByOurs(GetWindowThreadProcessId(front, nullptr)))) return 0;
        return DefSubclassProc(hwnd, msg == ForwardActivateMessage() ? WM_ACTIVATE : WM_ACTIVATEAPP, wp, lp);
    }
    if (msg == WM_ACTIVATEAPP) return wp == FALSE && GainedByOurs(lp) ? 0 : DefSubclassProc(hwnd, msg, wp, lp);
    if ((msg == WM_ACTIVATE && LOWORD(wp) == WA_INACTIVE) || (msg == WM_NCACTIVATE && wp == FALSE)) {
        const HWND to = OtherWindow(lp);
        // Constraint: WM_NCACTIVATE must return TRUE here, because FALSE blocks the deactivation and the popup cannot activate.
        if (OwnedByGame(to) && to != gs->game) return msg == WM_NCACTIVATE;
    }
    return DefSubclassProc(hwnd, msg, wp, lp);
}

bool PopupActive(HWND popup) {
    const HWND front = GetForegroundWindow();
    return front && (GetAncestor(front, GA_ROOT) == popup || GetWindow(front, GW_OWNER) == popup);
}

void Foreground(HWND target) {
    const HWND before = GetForegroundWindow();
    if (!BelongsToGame(before)) return;
    SetForegroundWindow(target);
    if (GetForegroundWindow() == target) return;
    const DWORD front = GetWindowThreadProcessId(before, nullptr);
    const DWORD self = GetCurrentThreadId();
    if (front == 0 || front == self || !AttachThreadInput(front, self, TRUE)) return;
    SetForegroundWindow(target);
    SetFocus(target);
    AttachThreadInput(front, self, FALSE);
}

void RecordBrowserPid(ICoreWebView2* web) {
    UINT32 pid = 0;
    if (!gBrowserPid && web && SUCCEEDED(web->get_BrowserProcessId(&pid))) gBrowserPid = pid;
}

void ShowPopup(View* v, bool on, bool raise = false) {
    if (!v->popup) return;
    if (on) {
        RecordBrowserPid(v->web.Get());
        ShowWindow(v->popup, SW_SHOWNOACTIVATE);
        if (raise && BelongsToGame(GetForegroundWindow())) SetWindowPos(v->popup, HWND_TOP, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        return;
    }
    FocusDiag(L"before hide");
    if (PopupActive(v->popup)) Foreground(gs->game);
    FocusDiag(L"after hand-back");
    ShowWindow(v->popup, SW_HIDE);
    FocusDiag(L"after hide");
}

void Typing(View* v, bool on) {
    if (!v->popup || !v->controller) return;
    const HWND front = GetForegroundWindow();
    if (!on) {
        v->typing = false;
        if (front == v->popup) {
            FocusDiag(L"typing out, before");
            Foreground(gs->game);
            FocusDiag(L"typing out, after");
        }
        return;
    }
    if (!v->visible || gs->suspended || (front != gs->game && GetWindow(front, GW_OWNER) != gs->game)) return;
    Foreground(v->popup);
    v->typing = GetForegroundWindow() == v->popup;
    if (v->typing && (front != v->popup || !v->focused)) Timed(v->id, L"MoveFocus", [&] { v->controller->MoveFocus(COREWEBVIEW2_MOVE_FOCUS_REASON_PROGRAMMATIC); });
}

void EmitMode(View* v) {
    wchar_t handle[32] = L"none";
    const wchar_t* focus = L"none";
    if (v->popup) {
        swprintf_s(handle, L"0x%llx", static_cast<unsigned long long>(reinterpret_cast<UINT_PTR>(v->popup)));
        focus = GetForegroundWindow() == v->popup ? L"popup" : L"game";
    }
    const wchar_t* mode = v->comp ? L"composition" : v->popup ? L"thread" : L"windowed";
    Emit(v->id, L"{\"type\":\"obx.status\",\"view\":" + std::to_wstring(v->id) + L",\"mode\":\"" + mode + L"\",\"popup\":\"" + handle + L"\",\"focus\":\"" + focus + L"\"}", true, kMaxQueue + 64);
}

void Post(std::function<void()> task) {
    {
        std::lock_guard<std::mutex> hold(gLock);
        gTasks.push_back(std::move(task));
    }
    PostThreadMessageW(static_cast<DWORD>(gs->shimId), kTaskMessage, 0, 0);
}

template <class F>
HRESULT Remote(const wchar_t* name, int id, F f) {
    if (!gs) return Fail(E_UNEXPECTED, L"Obx_Init has not run");
    if (!gs->threaded) return f();
    Post([=] {
        const HRESULT hr = f();
        if (FAILED(hr) && hr != E_PENDING && Find(id)) Report(id, name, hr);
    });
    return S_OK;
}

template <class F>
HRESULT OnView(int id, const wchar_t* name, F f) {
    return Known(id) ? Remote(name, id, f) : Fail(E_INVALIDARG, L"unknown view");
}

void RunTasks() {
    for (;;) {
        std::function<void()> task;
        {
            std::lock_guard<std::mutex> hold(gLock);
            if (gTasks.empty()) return;
            task = std::move(gTasks.front());
            gTasks.pop_front();
        }
        try {
            task();
        } catch (...) {
            Fail(E_FAIL, L"unexpected exception on the shim thread");
        }
    }
}

bool WaitPumping(HANDLE handle, DWORD ms) {
    const ULONGLONG until = GetTickCount64() + ms;
    for (;;) {
        const ULONGLONG now = GetTickCount64();
        if (now >= until) return false;
        const DWORD woke = MsgWaitForMultipleObjects(1, &handle, FALSE, static_cast<DWORD>(until - now), QS_SENDMESSAGE);
        if (woke == WAIT_OBJECT_0) return true;
        if (woke != WAIT_OBJECT_0 + 1) return false;
        MSG msg{};
        PeekMessageW(&msg, nullptr, 0, 0, PM_NOREMOVE);
    }
}

void SetReady() {
    if (gs->ready) SetEvent(gs->ready);
}

unsigned __stdcall ShimMain(void*) {
    SetThreadDpiAwarenessContext(GetWindowDpiAwarenessContext(gs->game));
    const HRESULT com = CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED);
    MSG msg{};
    PeekMessageW(&msg, nullptr, 0, 0, PM_NOREMOVE);
    SetTimer(nullptr, kFollowTimer, kFollowMs, FollowTimer);
    SetReady();
    const HRESULT started = SUCCEEDED(com) ? Guard([] { return StartEnvironment(); }) : com;
    if (FAILED(started)) Report(0, L"environment", started);
    while (GetMessageW(&msg, nullptr, 0, 0) > 0) {
        if (msg.message == kTaskMessage) {
            RunTasks();
        } else {
            TranslateMessage(&msg);
            DispatchMessageW(&msg);
        }
    }
    if (SUCCEEDED(com)) CoUninitialize();
    return 0;
}

HRESULT StartShim() {
    gs->ready = CreateEventW(nullptr, TRUE, FALSE, nullptr);
    if (!gs->ready) return Fail(HRESULT_FROM_WIN32(GetLastError()), L"CreateEvent");
    if (!SetWindowSubclass(gs->game, HoldActive, kHoldSubclassId, 0)) return Fail(HRESULT_FROM_WIN32(GetLastError()), L"SetWindowSubclass");
    gs->shim = reinterpret_cast<HANDLE>(_beginthreadex(nullptr, 0, ShimMain, nullptr, 0, &gs->shimId));
    if (!gs->shim) {
        RemoveWindowSubclass(gs->game, HoldActive, kHoldSubclassId);
        return Fail(E_FAIL, L"_beginthreadex");
    }
    if (!WaitPumping(gs->ready, kStartMs)) {
        RemoveWindowSubclass(gs->game, HoldActive, kHoldSubclassId);
        return Fail(HRESULT_FROM_WIN32(ERROR_TIMEOUT), L"shim thread did not start");
    }
    return S_OK;
}

HRESULT StopShim() {
    gs->stopping = true;
    RemoveWindowSubclass(gs->game, HoldActive, kHoldSubclassId);
    if (gs->shim) {
        Post([] {
            CloseAll();
            PostQuitMessage(0);
        });
        if (!WaitPumping(gs->shim, kStopMs)) return Fail(HRESULT_FROM_WIN32(ERROR_TIMEOUT), L"shim thread did not stop");
        CloseHandle(gs->shim);
        gs->shim = nullptr;
    }
    gBrowserPid = 0;
    if (gs->ready) CloseHandle(gs->ready);
    gs->ready = nullptr;
    return S_OK;
}
