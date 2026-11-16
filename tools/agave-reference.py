import sys
import time

from PIL import Image, ImageChops, ImageStat

if len(sys.argv) > 1:
    actual = Image.open("agave.ppm").convert("RGB")
    reference = Image.open("reference.png").convert("RGB")
    difference = ImageChops.difference(actual, reference)
    actual.save("agave.png")
    difference.save("difference.png")
    mean = ImageStat.Stat(difference).mean
    print("Mean absolute difference per channel:", mean)
    if max(mean) > 1:
        raise SystemExit("Visual difference exceeds 1/255 mean intensity")
else:
    from playwright.sync_api import sync_playwright

    with sync_playwright() as browser_api:
        browser = browser_api.chromium.launch()
        page = browser.new_page(viewport={"width": 1280, "height": 720}, device_scale_factor=1)
        for _ in range(30):
            try:
                page.goto("http://127.0.0.1:18765/index.html", wait_until="load")
                break
            except Exception:
                time.sleep(1)
        else:
            raise RuntimeError("OS page did not load")
        page.wait_for_timeout(4000)
        print("REFERENCE", page.title(), page.locator("#desk [data-app]").count())
        page.screenshot(path="reference.png")
        browser.close()
