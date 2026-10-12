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

bool LocalResource(int id, LPCWSTR uri) {
    if (!uri) return false;
    if (AllowedUri(id, uri) || _wcsnicmp(uri, L"data:", 5) == 0 || _wcsicmp(uri, L"about:blank") == 0) return true;
    const View* v = Find(id);
    if (!v || _wcsnicmp(uri, L"blob:", 5) != 0) return false;
    for (const auto& mapping : v->mappings) {
        if (UnderHost(mapping.first, uri + 5)) return true;
    }
    return false;
}

HRESULT OnMessage(int id, ICoreWebView2WebMessageReceivedEventArgs* args) {
    LPWSTR source = nullptr;
    LPWSTR json = nullptr;
    if (!SUCCEEDED(args->get_Source(&source)) || !AllowedSource(id, source)) {
        Blocked(id, L"message-source-blocked");
    } else if (SUCCEEDED(args->get_WebMessageAsJson(&json)) && json && *json) {
        if (wcslen(json) <= kMaxMessage) Emit(id, json, false);
        else Blocked(id, L"message-too-large");
    }
    CoTaskMemFree(source);
    CoTaskMemFree(json);
    return S_OK;
}

HRESULT OnNavigation(int id, ICoreWebView2NavigationStartingEventArgs* args) {
    LPWSTR uri = nullptr;
    if (!SUCCEEDED(args->get_Uri(&uri)) || !AllowedUri(id, uri)) {
        args->put_Cancel(TRUE);
        Blocked(id, L"navigation-blocked");
    }
    CoTaskMemFree(uri);
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
    web->add_NavigationStarting(Callback<ICoreWebView2NavigationStartingEventHandler>([id](ICoreWebView2*, ICoreWebView2NavigationStartingEventArgs* args) -> HRESULT {
        return Guard([&] { return OnNavigation(id, args); });
    }).Get(), &token);
    web->add_FrameNavigationStarting(Callback<ICoreWebView2NavigationStartingEventHandler>([id](ICoreWebView2*, ICoreWebView2NavigationStartingEventArgs* args) -> HRESULT {
        return Guard([&] { return OnNavigation(id, args); });
    }).Get(), &token);
    web->add_NewWindowRequested(Callback<ICoreWebView2NewWindowRequestedEventHandler>([id](ICoreWebView2*, ICoreWebView2NewWindowRequestedEventArgs* args) -> HRESULT {
        args->put_Handled(TRUE);
        Blocked(id, L"new-window-blocked");
        return S_OK;
    }).Get(), &token);
    web->add_PermissionRequested(Callback<ICoreWebView2PermissionRequestedEventHandler>([id](ICoreWebView2*, ICoreWebView2PermissionRequestedEventArgs* args) -> HRESULT {
        args->put_State(COREWEBVIEW2_PERMISSION_STATE_DENY);
        Blocked(id, L"permission-blocked");
        return S_OK;
    }).Get(), &token);
    HRESULT hr = AddResourceFilter(web);
    if (SUCCEEDED(hr)) hr = web->add_WebResourceRequested(Callback<ICoreWebView2WebResourceRequestedEventHandler>([id](ICoreWebView2*, ICoreWebView2WebResourceRequestedEventArgs* args) -> HRESULT {
        return Guard([&] { return OnResource(id, args); });
    }).Get(), &token);
    if (FAILED(hr)) return Report(id, L"resource-lockdown", hr);
    v->controller->add_AcceleratorKeyPressed(Callback<ICoreWebView2AcceleratorKeyPressedEventHandler>([id](ICoreWebView2Controller*, ICoreWebView2AcceleratorKeyPressedEventArgs* args) -> HRESULT {
        return Guard([&] { return OnKey(id, args); });
    }).Get(), &token);
    web->add_ProcessFailed(Callback<ICoreWebView2ProcessFailedEventHandler>([id](ICoreWebView2*, ICoreWebView2ProcessFailedEventArgs* args) -> HRESULT {
        COREWEBVIEW2_PROCESS_FAILED_KIND kind = COREWEBVIEW2_PROCESS_FAILED_KIND_BROWSER_PROCESS_EXITED;
        args->get_ProcessFailedKind(&kind);
        Notify(id, L"process-failed", static_cast<HRESULT>(kind), kMaxQueue + 64);
        return S_OK;
    }).Get(), &token);
    ComPtr<ICoreWebView2_4> web4;
    if (SUCCEEDED(v->web.As(&web4))) {
        web4->add_DownloadStarting(Callback<ICoreWebView2DownloadStartingEventHandler>([id](ICoreWebView2*, ICoreWebView2DownloadStartingEventArgs* args) -> HRESULT {
            args->put_Cancel(TRUE);
            args->put_Handled(TRUE);
            Blocked(id, L"download-blocked");
            return S_OK;
        }).Get(), &token);
    } else {
        Report(id, L"download-lockdown-unavailable", E_NOINTERFACE);
    }
    ComPtr<ICoreWebView2_18> web18;
    if (SUCCEEDED(v->web.As(&web18))) {
        web18->add_LaunchingExternalUriScheme(Callback<ICoreWebView2LaunchingExternalUriSchemeEventHandler>([id](ICoreWebView2*, ICoreWebView2LaunchingExternalUriSchemeEventArgs* args) -> HRESULT {
            args->put_Cancel(TRUE);
            Blocked(id, L"external-uri-blocked");
            return S_OK;
        }).Get(), &token);
    }
    return S_OK;
}

const wchar_t* const kNoServiceWorker = L"try { Object.defineProperty(Navigator.prototype, 'serviceWorker', { get: function () { return undefined; }, configurable: false }); } catch (e) {} try { delete window.SharedWorker; } catch (e) {}";

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
