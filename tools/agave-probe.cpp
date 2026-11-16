#include <chrono>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <thread>
#include <vector>
#include "macross.h"

static bool shell_ok = false;
static void on_alert(MaCrossView*, const char* text) {
    std::printf("JS_ALERT %s\n", text ? text : "");
    if (text && std::strstr(text, "SHELL_READY:PASS")) shell_ok = true;
}
static void on_title(MaCrossView*, const char* text) { std::printf("PAGE_TITLE %s\n", text ? text : ""); }
static void on_invalidate(MaCrossView*, const MC_RECT*) {}
static void on_update(MaCrossView*) {}

static void pump(MaCrossView* view, int count) {
    for (int i = 0; i < count; ++i) {
        macross_event_dispatch();
        macross_view_update(view, nullptr);
        std::this_thread::sleep_for(std::chrono::milliseconds(20));
    }
}

int main(int argc, char** argv) {
    if (argc != 3) return 2;
    const int width = 1280, height = 720, pitch = width * 4;
    if (macross_initialize(PIXEL_FORMAT_BGRA32, width, height) != MC_STATUS_SUCCESS) return 3;
    MC_CALLBACK_INFO callbacks = {};
    callbacks.cb_alert_box = on_alert;
    callbacks.cb_set_title = on_title;
    callbacks.cb_invalidate_rect = on_invalidate;
    callbacks.cb_update_view_now = on_update;
    if (macross_set_callback(&callbacks) != MC_STATUS_SUCCESS) return 6;
    std::vector<unsigned char> pixels(pitch * height, 255);
    MaCrossView* view = macross_view_create(pixels.data(), width, height, pitch, nullptr);
    if (!view) return 4;
    pump(view, 15);
    if (macross_view_open_url(view, argv[1]) != MC_STATUS_SUCCESS) return 7;
    pump(view, 200);
    const char* script = "alert('SHELL_READY:' + (document.querySelectorAll('#desk [data-app]').length >= 5 && typeof window.Apps === 'object' && typeof window.ModFrame === 'object' ? 'PASS' : 'FAIL') + ' '+JSON.stringify({title:document.title,apps:document.querySelectorAll('#desk [data-app]').length,registry:typeof window.Apps,modFrame:typeof window.ModFrame,postMessage:typeof window.postMessage}))";
    std::printf("HOST_EVAL %d\n", macross_view_eval_script(view, script));
    pump(view, 50);
    FILE* out = std::fopen(argv[2], "wb");
    if (!out) return 5;
    std::fprintf(out, "P6\n%d %d\n255\n", width, height);
    for (int i = 0; i < width * height; ++i) {
        std::fputc(pixels[i * 4 + 2], out);
        std::fputc(pixels[i * 4 + 1], out);
        std::fputc(pixels[i * 4], out);
    }
    std::fclose(out);
    macross_view_destroy(view);
    macross_shutdown();
    return shell_ok ? 0 : 1;
}
