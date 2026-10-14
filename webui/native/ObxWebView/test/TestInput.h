inline void TakeForeground(HWND wnd) {
    const HWND fg = GetForegroundWindow();
    if (fg != wnd) {
        const DWORD thread = GetWindowThreadProcessId(fg, nullptr);
        AttachThreadInput(GetCurrentThreadId(), thread, TRUE);
        SetForegroundWindow(wnd);
        AttachThreadInput(GetCurrentThreadId(), thread, FALSE);
    }
}

inline bool ClickTargetOk(HWND wnd) {
    POINT cursor{};
    if (!GetCursorPos(&cursor) || GetForegroundWindow() != wnd) return false;
    return GetAncestor(WindowFromPoint(cursor), GA_ROOT) == wnd;
}

inline void MoveCursorInWindow(HWND wnd, int x, int y) {
    POINT p{x, y};
    ClientToScreen(wnd, &p);
    SetCursorPos(p.x, p.y);
}
