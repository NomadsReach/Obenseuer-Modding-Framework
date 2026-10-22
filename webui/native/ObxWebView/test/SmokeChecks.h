HWND gPopup = nullptr;
int gStep = 0;
ULONGLONG gStepAt = 0;
bool gFocusReady = false;
bool gTyped = false;
bool gEscKey = false;
int gFocusNo = 0;
ULONGLONG gFocusAt = 0;
ULONGLONG gShownAt = 0;
int gFocusFails = 0;
int gPressed = 0;
int gPressWant = 0;
bool gButtonPressed = false;
int gDeactivations = 0;
int gReactivations = 0;
int gStaleActivations = 0;
int gMark = 0;
int gStaleMark = 0;
int gOurDeactivations = 0;
int gOursMark = 0;
bool gFieldSeen = false;
int gFieldX = 450;
int gFieldY = 216;
bool gBlankDbl = false;
ULONGLONG gPickAt = 0;
ULONGLONG gDblAt = 0;
ULONGLONG gNavClick = 0;
ULONGLONG gNavLoadAt = 0;

bool TypedX(const std::wstring& json) {
    const size_t at = json.find(L"\"value\":\"");
    return at != std::wstring::npos && json.find_first_of(L"xX", at + 9) < json.find(L'"', at + 9);
}

void FocusNote(const std::wstring& json, bool native) {
    if (!native && Has(json, L"\"type\":\"focus-ready\"")) gFocusReady = true;
    if (!native && Has(json, L"\"type\":\"pressed\"")) ++gPressed;
    if (!native && Has(json, L"\"type\":\"dbl\"") && Has(json, L"\"target\":\"blank\"")) gBlankDbl = true;
    if (!native && Has(json, L"\"type\":\"nav-loaded\"")) gNavLoadAt = GetTickCount64();
    if (!native && Has(json, L"\"type\":\"rect\"")) {
        gFieldX = static_cast<int>(NumberAfter(json, L"\"x\":"));
        gFieldY = static_cast<int>(NumberAfter(json, L"\"y\":"));
        gFieldSeen = true;
    }
    if (!native && Has(json, L"\"target\":\"btn\"")) gButtonPressed = true;
    if (!native && Has(json, L"\"type\":\"typed\"") && TypedX(json)) gTyped = true;
    if (native && Has(json, L"\"type\":\"obx.key\"") && Has(json, L"\"vk\":27")) gEscKey = true;
}

void FocusVerdict(const std::wstring& label, bool ok) {
    Say(L"FOCUS " + label + (ok ? L": PASS" : L": FAIL"));
    if (!ok) ++gFocusFails;
}

bool ClickPage(int x, int y) {
    RECT rect{};
    GetWindowRect(gPopup, &rect);
    const double scale = GetDpiForWindow(gWnd) / 96.0;
    POINT p{rect.left + static_cast<LONG>(x * scale), rect.top + static_cast<LONG>(y * scale)};
    SetCursorPos(p.x, p.y);
    if (GetAncestor(WindowFromPoint(p), GA_ROOT) != gPopup) return false;
    INPUT click[3]{};
    for (INPUT& input : click) input.type = INPUT_MOUSE;
    click[0].mi.dx = MulDiv(p.x - GetSystemMetrics(SM_XVIRTUALSCREEN), 65535, GetSystemMetrics(SM_CXVIRTUALSCREEN) - 1);
    click[0].mi.dy = MulDiv(p.y - GetSystemMetrics(SM_YVIRTUALSCREEN), 65535, GetSystemMetrics(SM_CYVIRTUALSCREEN) - 1);
    click[0].mi.dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK;
    click[1].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
    click[2].mi.dwFlags = MOUSEEVENTF_LEFTUP;
    gPressWant = gPressed + 1;
    SendInput(3, click, sizeof(INPUT));
    return true;
}

void TypeKey(WORD vk) {
    INPUT key[2]{};
    key[0].type = INPUT_KEYBOARD;
    key[0].ki.wVk = vk;
    key[1].type = INPUT_KEYBOARD;
    key[1].ki.wVk = vk;
    key[1].ki.dwFlags = KEYEVENTF_KEYUP;
    SendInput(2, key, sizeof(INPUT));
}

std::wstring Who(HWND w) {
    if (!w) return L"none";
    wchar_t cls[256] = L"";
    GetClassNameW(w, cls, 256);
    DWORD pid = 0;
    GetWindowThreadProcessId(w, &pid);
    return std::wstring(cls) + (pid == GetCurrentProcessId() ? L" (ours)" : L" (other)");
}

void NoteActivation(UINT msg, WPARAM wp, LPARAM lp) {
    const wchar_t* name = msg == WM_ACTIVATE ? L"WM_ACTIVATE" : msg == WM_ACTIVATEAPP ? L"WM_ACTIVATEAPP" : msg == WM_NCACTIVATE ? L"WM_NCACTIVATE" : msg == WM_KILLFOCUS ? L"WM_KILLFOCUS" : msg == WM_SETFOCUS ? L"WM_SETFOCUS" : nullptr;
    if (!name) return;
    const bool away = msg == WM_ACTIVATE ? LOWORD(wp) == WA_INACTIVE : msg == WM_ACTIVATEAPP && wp == FALSE;
    const bool back = msg == WM_ACTIVATE ? LOWORD(wp) != WA_INACTIVE : msg == WM_ACTIVATEAPP && wp == TRUE;
    const HWND front = GetForegroundWindow();
    if (away) ++gDeactivations;
    if (back && front == gPopup) ++gReactivations;
    if (back && front != gWnd && front != gPopup) ++gStaleActivations;
    DWORD frontPid = 0;
    GetWindowThreadProcessId(front, &frontPid);
    if (away && frontPid == GetCurrentProcessId()) ++gOurDeactivations;
    const std::wstring other = msg == WM_ACTIVATE ? L" other=" + Who(reinterpret_cast<HWND>(lp)) : std::wstring();
    Say(std::wstring(L"ACTIVATION ") + name + L" wp=" + Num(static_cast<long long>(wp)) + L" lp=" + Num(static_cast<long long>(lp)) + other + L" front=" + Who(front) + (away ? L" (would minimize the game)" : L""));
}

void ActivateTaskbar() {
    const HWND tray = FindWindowW(L"Shell_TrayWnd", nullptr);
    if (tray) SetForegroundWindow(tray);
}

bool Settled(int no) {
    const HWND front = GetForegroundWindow();
    if (no == 5) return gTyped;
    if (no == 6) return gEscKey;
    if (no == 10) return gFieldSeen && GetTickCount64() >= gShownAt + 500;
    if (no == 13) return gDeactivations > gMark;
    if (no == 14) return gReactivations > gMark;
    if (no == 18) return GetTickCount64() >= gPickAt + 800;
    if (no == 19) return gBlankDbl;
    if (no == 20) return front == gWnd && GetTickCount64() >= gDblAt + 900;
    if (no == 22) return (gNavLoadAt != 0 && front == gWnd) || GetTickCount64() >= gNavClick + 1500;
    if (no == 23) return front == gWnd && GetTickCount64() >= gNavLoadAt + 900;
    if (no == 24 || no == 27) return true;
    if (no == 26) return gDeactivations > gMark;
    if (gPressed < gPressWant) return false;
    if (no == 4 || no == 8 || no == 11 || no == 12 || no == 15) return front == gPopup;
    return no == 0 || front == gWnd;
}

void FocusStep() {
    if (!gFocusReady || !gPopup || gStep <= 2 || gFocusNo > 27) return;
    if (!Settled(gFocusNo) && GetTickCount64() < gFocusAt) return;
    const int no = gFocusNo++;
    gDeadline = GetTickCount64() + 5000;
    const HWND front = GetForegroundWindow();
    const bool game = front == gWnd;
    const bool popup = front == gPopup;
    gFocusAt = GetTickCount64() + 3000;
    switch (no) {
    case 0:
        gDeactivations = 0;
        gOurDeactivations = 0;
        TakeForeground(gWnd);
        break;
    case 1:
        FocusVerdict(L"SHOW keeps the game active", game);
        FocusVerdict(L"BLANK CLICK reaches the popup", ClickPage(800, 540));
        break;
    case 2:
        FocusVerdict(L"BLANK CLICK keeps the game active", game);
        ClickPage(380, 340);
        break;
    case 3:
        FocusVerdict(L"BUTTON CLICK reaches the page", gButtonPressed);
        FocusVerdict(L"BUTTON CLICK releases activation to the game", game);
        FocusVerdict(L"INPUT CLICK reaches the popup", ClickPage(450, 216));
        break;
    case 4:
        FocusVerdict(L"INPUT FOCUS activates the popup", popup);
        if (popup) TypeKey('X');
        break;
    case 5:
        FocusVerdict(L"TYPING reaches the page input", gTyped);
        if (popup) TypeKey(VK_ESCAPE);
        break;
    case 6:
        FocusVerdict(L"ESC reaches the host as obx.key", gEscKey);
        FocusVerdict(L"BLANK CLICK reaches the popup again", ClickPage(800, 540));
        break;
    case 7:
        FocusVerdict(L"BLANK CLICK releases activation to the game", game);
        FocusVerdict(L"INPUT CLICK reaches the popup again", ClickPage(450, 216));
        break;
    case 8:
        FocusVerdict(L"INPUT FOCUS again after the game was active", popup);
        gApi.visible(gView, 0);
        break;
    case 9:
        FocusVerdict(L"HIDE while the popup is active gives focus to the game", game);
        gApi.visible(gView, 1);
        gShownAt = GetTickCount64();
        gFieldSeen = false;
        gApi.post(gView, L"{\"type\":\"rect\"}");
        break;
    case 10:
        ClickPage(gFieldX, gFieldY);
        break;
    case 11:
        FocusVerdict(L"INPUT CLICK after the hide reaches the popup", popup);
        FocusVerdict(L"GAME NOT DEACTIVATED BY OUR WINDOWS", gOurDeactivations == 0);
        break;
    case 12:
        gMark = gDeactivations;
        gOursMark = gOurDeactivations;
        ActivateTaskbar();
        break;
    case 13:
        FocusVerdict(L"ALT-TAB FROM THE POPUP DEACTIVATES THE GAME", gDeactivations > gMark);
        gMark = gReactivations;
        TakeForeground(gPopup);
        break;
    case 14:
        FocusVerdict(L"POPUP REGAINS FOCUS SENDS THE GAME ITS ACTIVATION", gReactivations > gMark);
        TakeForeground(gWnd);
        ClickPage(gFieldX, gFieldY);
        break;
    case 15:
        FocusVerdict(L"INPUT CLICK after the popup regains focus reaches the popup", popup);
        FocusVerdict(L"GAME NOT DEACTIVATED BY THE CLICK AFTER THE POPUP REGAINS FOCUS", gOurDeactivations == gOursMark);
        ActivateTaskbar();
        gMark = gDeactivations;
        Sleep(150);
        TakeForeground(gWnd);
        Sleep(300);
        break;
    case 16:
        FocusVerdict(L"STALE FORWARDED DEACTIVATION IS DROPPED, GAME STAYS ACTIVE", gDeactivations == gMark && game);
        break;
    case 17:
        ClickPage(380, 416);
        gPickAt = GetTickCount64();
        break;
    case 18:
        FocusVerdict(L"SELECT CLICK keeps the popup active", popup);
        TakeForeground(gWnd);
        Sleep(300);
        gBlankDbl = false;
        ClickPage(800, 540);
        Sleep(120);
        ClickPage(800, 540);
        gDblAt = GetTickCount64();
        break;
    case 19:
        FocusVerdict(L"BLANK DOUBLE CLICK reaches the page as dbl", gBlankDbl);
        break;
    case 20:
        FocusVerdict(L"BLANK DOUBLE CLICK releases activation to the game within 1.5 s", game && GetTickCount64() <= gDblAt + 1500);
        break;
    case 21:
        gNavLoadAt = 0;
        gNavClick = GetTickCount64();
        ClickPage(700, 216);
        break;
    case 22:
        FocusVerdict(L"LINK CLICK that navigates releases activation to the game", gNavLoadAt != 0 && game && GetTickCount64() <= gNavClick + 1500);
        break;
    case 23:
        ActivateTaskbar();
        Sleep(150);
        TakeForeground(gWnd);
        Sleep(150);
        ActivateTaskbar();
        gStaleMark = gStaleActivations;
        TakeForeground(gPopup);
        ActivateTaskbar();
        Sleep(300);
        break;
    case 24:
        FocusVerdict(L"STALE FORWARDED ACTIVATION IS DROPPED", gStaleActivations == gStaleMark);
        FocusVerdict(L"GAME STAYS INACTIVE AFTER THE STALE ACTIVATION", !game);
        TakeForeground(gWnd);
        gApi.destroy(gView);
        break;
    case 25:
        FocusVerdict(L"CLOSE while the popup is active gives focus to the game", game);
        gMark = gDeactivations;
        ActivateTaskbar();
        break;
    case 26:
        FocusVerdict(L"ALT-TAB WITH THE GAME ACTIVE REACHES THE GAME", gDeactivations > gMark);
        break;
    default:
        Say(gFocusFails == 0 ? L"FOCUS RESULT PASS" : L"FOCUS RESULT FAIL");
        gExit = gFocusFails == 0 ? 0 : 1;
        gRunning = false;
        break;
    }
}

void SmokeNote(const std::wstring& json, bool native) {
    FocusNote(json, native);
    const std::wstring key = L"\"popup\":\"0x";
    const size_t at = json.find(key);
    if (!native || at == std::wstring::npos) return;
    gPopup = reinterpret_cast<HWND>(static_cast<UINT_PTR>(wcstoull(json.c_str() + at + key.size(), nullptr, 16)));
    if (gStepAt == 0) gStepAt = GetTickCount64() + 1000;
}

void SmokeStep() {
    FocusStep();
    if (!gPopup || gStep > 2 || GetTickCount64() < gStepAt) return;
    ++gStep;
    if (gStep == 1) {
        gApi.setSuspended(1);
    } else if (gStep == 2) {
        Say(std::wstring(L"SUSPEND: ") + (IsWindowVisible(gPopup) ? L"FAIL popup still visible" : L"PASS popup hidden"));
        gApi.setSuspended(0);
    } else {
        Say(std::wstring(L"RESUME: ") + (IsWindowVisible(gPopup) ? L"PASS popup visible" : L"FAIL popup still hidden"));
    }
    gStepAt = GetTickCount64() + 400;
}

int StopTimed() {
    Say(L"HIDE hr=" + Num(gApi.visible(gView, 0)));
    gApi.destroy(gView);
    const ULONGLONG begin = GetTickCount64();
    const int hr = gApi.shutdown();
    const ULONGLONG took = GetTickCount64() - begin;
    Say(L"SHUTDOWN hr=" + Num(hr) + L" ms=" + Num(static_cast<long long>(took)) + (took < 3000ULL ? L" joined" : L" TIMEOUT"));
    return gExit;
}
