constexpr LONGLONG kSlowMs = 100;

template <class F>
void Timed(int id, const wchar_t* where, F f) {
    LARGE_INTEGER freq{}, begin{}, end{};
    QueryPerformanceFrequency(&freq);
    QueryPerformanceCounter(&begin);
    f();
    QueryPerformanceCounter(&end);
    const LONGLONG ms = (end.QuadPart - begin.QuadPart) * 1000 / freq.QuadPart;
    if (ms > kSlowMs) Status(id, L"slow", (std::wstring(where) + L" " + std::to_wstring(ms) + L" ms").c_str());
}
