bool UnderHost(const std::wstring& host, LPCWSTR uri) {
    const std::wstring prefix = L"https://" + host + L"/";
    return uri && _wcsnicmp(uri, prefix.c_str(), prefix.size()) == 0;
}

bool Allowed(const View& v, LPCWSTR uri) {
    for (const auto& mapping : v.mappings) {
        if (UnderHost(mapping.first, uri)) return true;
    }
    return false;
}

bool AllowedUri(int id, LPCWSTR uri) {
    const View* v = Find(id);
    return v && Allowed(*v, uri);
}

bool AllowedSource(int id, LPCWSTR uri) {
    const View* v = Find(id);
    return v && !v->shell.empty() && UnderHost(v->shell, uri);
}

bool FrameLabelOk(const std::wstring& label) {
    if (label.empty() || label.size() > 63 || label.front() == L'-' || label.back() == L'-') return false;
    return std::all_of(label.begin(), label.end(), [](wchar_t c) {
        return (c >= L'a' && c <= L'z') || (c >= L'0' && c <= L'9') || c == L'-';
    });
}

bool HexLabel(const std::wstring& label) {
    return label.compare(0, 2, L"0x") == 0 && std::all_of(label.begin() + 2, label.end(), [](wchar_t c) {
        return (c >= L'0' && c <= L'9') || (c >= L'a' && c <= L'f');
    });
}

bool FrameHostOk(const std::wstring& host) {
    if (host.empty() || host.size() > 253) return false;
    std::vector<std::wstring> labels;
    for (size_t start = 0;;) {
        const size_t dot = host.find(L'.', start);
        labels.push_back(host.substr(start, dot == std::wstring::npos ? dot : dot - start));
        if (dot == std::wstring::npos) break;
        start = dot + 1;
    }
    if (labels.size() < 2 || !std::all_of(labels.begin(), labels.end(), FrameLabelOk)) return false;
    const std::wstring& last = labels.back();
    if (HexLabel(last)) return false;
    return std::any_of(last.begin(), last.end(), [](wchar_t c) { return c < L'0' || c > L'9'; });
}

HRESULT AllowFrameHostNow(int id, const std::wstring& host) {
    View* v = Find(id);
    if (!v) return Fail(E_INVALIDARG, L"unknown view");
    v->frameHosts.push_back(host);
    return S_OK;
}

HRESULT AllowFrameHost(int id, const wchar_t* host) {
    if (!host || !FrameHostOk(host)) return Fail(E_INVALIDARG, L"frame host");
    const std::wstring name = host;
    return OnView(id, L"Obx_AllowFrameHost", [=] { return AllowFrameHostNow(id, name); });
}

bool FrameHostUri(const View& v, LPCWSTR uri) {
    for (const auto& host : v.frameHosts) {
        if (UnderHost(host, uri)) return true;
    }
    return false;
}

bool AllowedFrameUri(int id, LPCWSTR uri) {
    const View* v = Find(id);
    return v && FrameHostUri(*v, uri);
}

bool AllowedFrameNavigation(int id, LPCWSTR uri) {
    const View* v = Find(id);
    if (!v || (!v->shell.empty() && UnderHost(v->shell, uri))) return false;
    return Allowed(*v, uri) || FrameHostUri(*v, uri);
}

bool LocalResource(int id, LPCWSTR uri) {
    if (!uri) return false;
    if (AllowedUri(id, uri) || AllowedFrameUri(id, uri) || _wcsnicmp(uri, L"data:", 5) == 0 || _wcsicmp(uri, L"about:blank") == 0) return true;
    const View* v = Find(id);
    if (!v || _wcsnicmp(uri, L"blob:", 5) != 0) return false;
    for (const auto& mapping : v->mappings) {
        if (UnderHost(mapping.first, uri + 5)) return true;
    }
    return false;
}

std::wstring Escaped(const std::wstring& text) {
    std::wstring out;
    for (const wchar_t c : text) {
        if (c == L'"' || c == L'\\') out += L'\\';
        if (c < 0x20) {
            wchar_t esc[8];
            swprintf_s(esc, L"\\u%04x", static_cast<unsigned>(c));
            out += esc;
        } else {
            out += c;
        }
    }
    return out;
}

// Constraint: kTypingScript posts exactly these two strings.
const wchar_t* const kTypingIn = L"\"obx-typing-in\"";
const wchar_t* const kTypingOut = L"\"obx-typing-out\"";

bool TypingMessage(int id, LPCWSTR json) {
    const bool in = wcscmp(json, kTypingIn) == 0;
    if (!in && wcscmp(json, kTypingOut) != 0) return false;
    if (View* v = Find(id)) Typing(v, in);
    return true;
}

HRESULT OnMessage(int id, ICoreWebView2WebMessageReceivedEventArgs* args) {
    LPWSTR source = nullptr;
    LPWSTR json = nullptr;
    if (!SUCCEEDED(args->get_Source(&source)) || !AllowedSource(id, source)) {
        Blocked(id, L"message-source-blocked", L",\"source\":\"" + Escaped(std::wstring(source ? source : L"").substr(0, kMaxSourceChars)) + L"\"");
    } else if (SUCCEEDED(args->get_WebMessageAsJson(&json)) && json && *json && !TypingMessage(id, json)) {
        if (wcslen(json) <= kMaxMessage) Emit(id, json, false);
        else Blocked(id, L"message-too-large");
    }
    CoTaskMemFree(source);
    CoTaskMemFree(json);
    return S_OK;
}

HRESULT OnNavigation(int id, ICoreWebView2NavigationStartingEventArgs* args, bool frame) {
    LPWSTR uri = nullptr;
    const bool ok = SUCCEEDED(args->get_Uri(&uri)) && (frame ? AllowedFrameNavigation(id, uri) : AllowedUri(id, uri));
    if (frame) {
        View* v = Find(id);
        UINT64 nav = 0;
        if (v && uri && SUCCEEDED(args->get_NavigationId(&nav))) {
            if (v->frameUrls.size() >= kMaxFrameUrls && !v->frameUrls.count(nav)) v->frameUrls.erase(v->frameUrls.begin());
            v->frameUrls.emplace(nav, std::wstring(uri).substr(0, kMaxSourceChars));
        }
    }
    if (!ok) {
        args->put_Cancel(TRUE);
        Blocked(id, frame ? L"frame-navigation-blocked" : L"navigation-blocked");
    }
    CoTaskMemFree(uri);
    return S_OK;
}

HRESULT OnFrameDone(int id, ICoreWebView2NavigationCompletedEventArgs* args) {
    BOOL ok = FALSE;
    COREWEBVIEW2_WEB_ERROR_STATUS status{};
    UINT64 nav = 0;
    args->get_IsSuccess(&ok);
    args->get_WebErrorStatus(&status);
    args->get_NavigationId(&nav);
    std::wstring url;
    View* v = Find(id);
    if (v) {
        const auto found = v->frameUrls.find(nav);
        if (found != v->frameUrls.end()) {
            url = found->second;
            v->frameUrls.erase(found);
        }
    }
    if (v) {
        int& reports = ok ? v->frameOkReports : v->frameFailReports;
        if (reports++ < kMaxReports) Emit(id, L"{\"type\":\"obx.frame\",\"view\":" + std::to_wstring(id) + L",\"ok\":" + (ok ? L"true" : L"false") + L",\"status\":" + std::to_wstring(static_cast<int>(status)) + L",\"url\":\"" + Escaped(url.substr(0, kMaxSourceChars)) + L"\"}", true, kMaxQueue + 64);
    }
    return S_OK;
}

HRESULT OnFrameCreated(int id, ICoreWebView2FrameCreatedEventArgs* args) {
    ComPtr<ICoreWebView2Frame> frame;
    ComPtr<ICoreWebView2Frame2> frame2;
    EventRegistrationToken token{};
    if (SUCCEEDED(args->get_Frame(&frame)) && SUCCEEDED(frame.As(&frame2))) {
        frame2->add_WebMessageReceived(Callback<ICoreWebView2FrameWebMessageReceivedEventHandler>([id](ICoreWebView2Frame*, ICoreWebView2WebMessageReceivedEventArgs* message) -> HRESULT {
            return Guard([&] { return OnMessage(id, message); });
        }).Get(), &token);
    }
    return S_OK;
}

HRESULT OnResource(int id, ICoreWebView2WebResourceRequestedEventArgs* args) {
    ComPtr<ICoreWebView2WebResourceRequest> request;
    LPWSTR uri = nullptr;
    const bool local = SUCCEEDED(args->get_Request(&request)) && SUCCEEDED(request->get_Uri(&uri)) && LocalResource(id, uri);
    CoTaskMemFree(uri);
    if (local) return S_OK;
    ComPtr<ICoreWebView2WebResourceResponse> response;
    const bool answered = gs->env && SUCCEEDED(gs->env->CreateWebResourceResponse(nullptr, 403, L"Forbidden", L"", &response)) && SUCCEEDED(args->put_Response(response.Get()));
    if (!answered && request) request->put_Uri(L"https://obx.invalid/");
    Blocked(id, L"resource-blocked");
    return S_OK;
}

HRESULT OnKey(int id, ICoreWebView2AcceleratorKeyPressedEventArgs* args) {
    COREWEBVIEW2_KEY_EVENT_KIND kind = COREWEBVIEW2_KEY_EVENT_KIND_KEY_UP;
    UINT key = 0;
    COREWEBVIEW2_PHYSICAL_KEY_STATUS status{};
    args->get_KeyEventKind(&kind);
    args->get_VirtualKey(&key);
    args->get_PhysicalKeyStatus(&status);
    const bool down = kind == COREWEBVIEW2_KEY_EVENT_KIND_KEY_DOWN || kind == COREWEBVIEW2_KEY_EVENT_KIND_SYSTEM_KEY_DOWN;
    if (down && !status.WasKeyDown && (key == VK_ESCAPE || key == VK_F10)) {
        Emit(id, L"{\"type\":\"obx.key\",\"view\":" + std::to_wstring(id) + L",\"vk\":" + std::to_wstring(key) + L"}", true, kMaxQueue + 64);
    }
    return S_OK;
}

HRESULT AddResourceFilter(ICoreWebView2* web) {
    ComPtr<ICoreWebView2_22> web22;
    if (SUCCEEDED(web->QueryInterface(IID_PPV_ARGS(&web22)))) return web22->AddWebResourceRequestedFilterWithRequestSourceKinds(L"*", COREWEBVIEW2_WEB_RESOURCE_CONTEXT_ALL, COREWEBVIEW2_WEB_RESOURCE_REQUEST_SOURCE_KINDS_ALL);
    return web->AddWebResourceRequestedFilter(L"*", COREWEBVIEW2_WEB_RESOURCE_CONTEXT_ALL);
}

HRESULT LockDown(View* v) {
    ICoreWebView2* web = v->web.Get();
    const int id = v->id;
    EventRegistrationToken token{};
    HRESULT hr = web->add_NavigationStarting(Callback<ICoreWebView2NavigationStartingEventHandler>([id](ICoreWebView2*, ICoreWebView2NavigationStartingEventArgs* args) -> HRESULT {
        return Guard([&] { return OnNavigation(id, args, false); });
    }).Get(), &token);
    if (SUCCEEDED(hr)) hr = web->add_FrameNavigationStarting(Callback<ICoreWebView2NavigationStartingEventHandler>([id](ICoreWebView2*, ICoreWebView2NavigationStartingEventArgs* args) -> HRESULT {
        return Guard([&] { return OnNavigation(id, args, true); });
    }).Get(), &token);
    if (SUCCEEDED(hr)) hr = web->add_FrameNavigationCompleted(Callback<ICoreWebView2NavigationCompletedEventHandler>([id](ICoreWebView2*, ICoreWebView2NavigationCompletedEventArgs* args) -> HRESULT {
        return Guard([&] { return OnFrameDone(id, args); });
    }).Get(), &token);
    if (SUCCEEDED(hr)) hr = web->add_NewWindowRequested(Callback<ICoreWebView2NewWindowRequestedEventHandler>([id](ICoreWebView2*, ICoreWebView2NewWindowRequestedEventArgs* args) -> HRESULT {
        args->put_Handled(TRUE);
        Blocked(id, L"new-window-blocked");
        return S_OK;
    }).Get(), &token);
    if (SUCCEEDED(hr)) hr = web->add_PermissionRequested(Callback<ICoreWebView2PermissionRequestedEventHandler>([id](ICoreWebView2*, ICoreWebView2PermissionRequestedEventArgs* args) -> HRESULT {
        args->put_State(COREWEBVIEW2_PERMISSION_STATE_DENY);
        Blocked(id, L"permission-blocked");
        return S_OK;
    }).Get(), &token);
    if (FAILED(hr)) return Report(id, L"lockdown", hr);
    hr = AddResourceFilter(web);
    if (SUCCEEDED(hr)) hr = web->add_WebResourceRequested(Callback<ICoreWebView2WebResourceRequestedEventHandler>([id](ICoreWebView2*, ICoreWebView2WebResourceRequestedEventArgs* args) -> HRESULT {
        return Guard([&] { return OnResource(id, args); });
    }).Get(), &token);
    if (FAILED(hr)) return Report(id, L"resource-lockdown", hr);
    hr = v->controller->add_AcceleratorKeyPressed(Callback<ICoreWebView2AcceleratorKeyPressedEventHandler>([id](ICoreWebView2Controller*, ICoreWebView2AcceleratorKeyPressedEventArgs* args) -> HRESULT {
        return Guard([&] { return OnKey(id, args); });
    }).Get(), &token);
    if (SUCCEEDED(hr)) hr = v->controller->add_GotFocus(Callback<ICoreWebView2FocusChangedEventHandler>([id](ICoreWebView2Controller*, IUnknown*) -> HRESULT {
        if (View* f = Find(id)) f->focused = true;
        return S_OK;
    }).Get(), &token);
    if (SUCCEEDED(hr)) hr = v->controller->add_LostFocus(Callback<ICoreWebView2FocusChangedEventHandler>([id](ICoreWebView2Controller*, IUnknown*) -> HRESULT {
        if (View* f = Find(id)) f->focused = false;
        return S_OK;
    }).Get(), &token);
    if (SUCCEEDED(hr)) hr = web->add_ProcessFailed(Callback<ICoreWebView2ProcessFailedEventHandler>([id](ICoreWebView2*, ICoreWebView2ProcessFailedEventArgs* args) -> HRESULT {
        COREWEBVIEW2_PROCESS_FAILED_KIND kind = COREWEBVIEW2_PROCESS_FAILED_KIND_BROWSER_PROCESS_EXITED;
        args->get_ProcessFailedKind(&kind);
        Notify(id, L"process-failed", static_cast<HRESULT>(kind), kMaxQueue + 64);
        return S_OK;
    }).Get(), &token);
    ComPtr<ICoreWebView2_4> web4;
    if (SUCCEEDED(hr)) hr = v->web.As(&web4);
    if (SUCCEEDED(hr)) hr = web4->add_FrameCreated(Callback<ICoreWebView2FrameCreatedEventHandler>([id](ICoreWebView2*, ICoreWebView2FrameCreatedEventArgs* args) -> HRESULT {
        return Guard([&] { return OnFrameCreated(id, args); });
    }).Get(), &token);
    if (SUCCEEDED(hr)) hr = web4->add_DownloadStarting(Callback<ICoreWebView2DownloadStartingEventHandler>([id](ICoreWebView2*, ICoreWebView2DownloadStartingEventArgs* args) -> HRESULT {
        args->put_Cancel(TRUE);
        args->put_Handled(TRUE);
        Blocked(id, L"download-blocked");
        return S_OK;
    }).Get(), &token);
    ComPtr<ICoreWebView2_18> web18;
    if (SUCCEEDED(hr)) hr = v->web.As(&web18);
    if (SUCCEEDED(hr)) hr = web18->add_LaunchingExternalUriScheme(Callback<ICoreWebView2LaunchingExternalUriSchemeEventHandler>([id](ICoreWebView2*, ICoreWebView2LaunchingExternalUriSchemeEventArgs* args) -> HRESULT {
        args->put_Cancel(TRUE);
        Blocked(id, L"external-uri-blocked");
        return S_OK;
    }).Get(), &token);
    if (FAILED(hr)) return Report(id, L"lockdown", hr);
    return S_OK;
}

const wchar_t* const kNoServiceWorker = L"try { Object.defineProperty(Navigator.prototype, 'serviceWorker', { get: function () { return undefined; }, configurable: false }); } catch (e) {} try { delete window.SharedWorker; } catch (e) {} ['WebSocket', 'WebSocketStream', 'EventSource', 'WebTransport', 'RTCPeerConnection', 'webkitRTCPeerConnection', 'Worker'].forEach(function (n) { try { Object.defineProperty(window, n, { value: undefined, configurable: false, writable: false }); } catch (e) {} }); try { Object.defineProperty(Navigator.prototype, 'sendBeacon', { value: undefined, configurable: false, writable: false }); } catch (e) {}";

const wchar_t* const kTypingScript = L"(function () { if (window.top !== window) return; var editable = function (el) { return !!el && (el.isContentEditable || el.tagName === 'TEXTAREA' || (el.tagName === 'INPUT' && !/^(button|checkbox|color|file|hidden|image|radio|range|reset|submit)$/i.test(el.type))); }; var entered = function (e) { if (editable(e.target)) chrome.webview.postMessage('obx-typing-in'); }; var pressed = function (e) { chrome.webview.postMessage(editable(e.target) ? 'obx-typing-in' : 'obx-typing-out'); }; document.addEventListener('focusin', entered, true); document.addEventListener('mousedown', pressed, true); document.addEventListener('focusout', function () { setTimeout(function () { if (!editable(document.activeElement)) chrome.webview.postMessage('obx-typing-out'); }, 0); }, true); })();";

void ClearServiceWorkers(View* v) {
    ComPtr<ICoreWebView2_13> web13;
    ComPtr<ICoreWebView2Profile> profile;
    ComPtr<ICoreWebView2Profile2> profile2;
    HRESULT hr = v->web.As(&web13);
    if (SUCCEEDED(hr)) hr = web13->get_Profile(&profile);
    if (SUCCEEDED(hr)) hr = profile.As(&profile2);
    if (FAILED(hr)) {
        Report(v->id, L"clear-browsing-data", hr);
        return;
    }
    const int id = v->id;
    ++v->pending;
    hr = profile2->ClearBrowsingData(COREWEBVIEW2_BROWSING_DATA_KINDS_SERVICE_WORKERS | COREWEBVIEW2_BROWSING_DATA_KINDS_CACHE_STORAGE, Callback<ICoreWebView2ClearBrowsingDataCompletedHandler>([id](HRESULT result) -> HRESULT {
        return Guard([&] {
            if (FAILED(result)) Report(id, L"clear-browsing-data", result);
            View* view = Find(id);
            if (view) ReleasePending(view);
            return S_OK;
        });
    }).Get());
    if (FAILED(hr)) {
        Report(id, L"clear-browsing-data", hr);
        ReleasePending(v);
    }
}
