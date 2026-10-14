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
    const POINT local{p.x - target->bounds.left, p.y - target->bounds.top};
    if (msg == WM_MOUSEMOVE && gs->hoverId == target->id && local.x == gs->moveAt.x && local.y == gs->moveAt.y) return true;
    if (gs->hoverId != target->id) {
        if (gs->hoverId) SendLeave(gs->hoverId);
        gs->hoverId = target->id;
    }
    UINT data = 0;
    if (wheel) data = static_cast<UINT>(GET_WHEEL_DELTA_WPARAM(wp));
    else if (IsXButton(msg)) data = HIWORD(wp);
    gs->moveAt = local;
    Timed(target->id, L"SendMouseInput", [&] { target->comp->SendMouseInput(static_cast<COREWEBVIEW2_MOUSE_EVENT_KIND>(msg), static_cast<COREWEBVIEW2_MOUSE_EVENT_VIRTUAL_KEYS>(LOWORD(wp)), data, local); });
    if (IsDown(msg)) {
        if (target->controller && !target->focused) Timed(target->id, L"MoveFocus", [&] { target->controller->MoveFocus(COREWEBVIEW2_MOVE_FOCUS_REASON_PROGRAMMATIC); });
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
