const wchar_t* const kBadFrameHosts[] = {L".a.com", L"a.com.", L"a..com", L"-a.com", L"a-.com", L"127.0.0.1", L"1.2.3.4", L"A.com", L"0x7f.0x1", L".", L".."};
const wchar_t* const kGoodFrameHosts[] = {L"a-b.invalid", L"xn--80ak6aa92e.invalid", L"1.com", L"a.1a"};

template <class Allow>
bool FrameHostsValid(Allow allow, int view) {
    const std::wstring three = std::wstring(63, L'a') + L"." + std::wstring(63, L'a') + L"." + std::wstring(63, L'a') + L".";
    const std::wstring host253 = three + std::wstring(57, L'a') + L".com";
    const std::wstring host254 = three + std::wstring(58, L'a') + L".com";
    bool ok = true;
    for (const wchar_t* host : kBadFrameHosts) {
        if (allow(view, host) >= 0) ok = false;
    }
    if (allow(view, (std::wstring(64, L'a') + L".com").c_str()) >= 0) ok = false;
    if (allow(view, host254.c_str()) >= 0) ok = false;
    for (const wchar_t* host : kGoodFrameHosts) {
        if (allow(view, host) != 0) ok = false;
    }
    if (allow(view, (std::wstring(63, L'a') + L".com").c_str()) != 0) ok = false;
    if (allow(view, host253.c_str()) != 0) ok = false;
    return ok;
}
