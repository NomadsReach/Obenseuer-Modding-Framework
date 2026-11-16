import { app, BrowserWindow } from '../../../../src/index';
import { resolve, sep } from 'node:path';

const root = resolve(process.env.OBENSEUER_WEB!);
const child = `<!doctype html><script>
let parentReadable = false, storageReadable = false;
try { parentReadable = !!parent.document.body; } catch {}
try { localStorage.setItem('probe', '1'); storageReadable = true; } catch {}
parent.postMessage({parentReadable, storageReadable, bridge: typeof window.__bunmaska}, '*');
</script>`;
const server = Bun.serve({
  hostname: '127.0.0.1', port: 0,
  async fetch(request) {
    const url = new URL(request.url);
    const path = url.pathname;
    if (path === '/probe-child') return new Response(child, { headers: { 'content-type': 'text/html' } });
    const file = resolve(root, '.' + decodeURIComponent(path === '/' ? '/index.html' : path));
    if (!file.startsWith(root + sep)) return new Response('Forbidden', { status: 403 });
    if (path === '/') {
      const html = await Bun.file(file).text();
      const framePolicy = html.replace('frame-src ', "frame-src 'self' ");
      const served = url.searchParams.has('probe') ? framePolicy.replace("script-src 'self'", "script-src 'self' 'unsafe-eval'") : framePolicy;
      if (framePolicy === html || (url.searchParams.has('probe') && served === framePolicy)) return new Response('Expected CSP not found', { status: 500 });
      return new Response(served, { headers: { 'content-type': 'text/html' } });
    }
    return new Response(Bun.file(file));
  },
});

const finish = (message: string, code: number): never => {
  console.log(message);
  server.stop(true);
  process.exit(code);
};
setTimeout(() => finish('PROBE_FAIL timeout', 1), 45000);

app.whenReady().then(() => {
  const win = new BrowserWindow({ width: 1280, height: 720, show: false });
  let phase = 0;
  win.webContents.on('did-finish-load', async () => {
    try {
      if (phase++ === 0) {
        try {
          await win.webContents.executeJavaScript('1 + 1');
          console.log('CSP_EXEC allowed');
        } catch (error) {
          if (!String(error).includes('unsafe-eval')) throw error;
          console.log(`CSP_EXEC blocked: ${error}`);
        }
        win.loadURL(`http://127.0.0.1:${server.port}/?probe=1`);
        return;
      }
      const shell = await win.webContents.executeJavaScript(`JSON.stringify({title: document.title, apps: document.querySelectorAll('#desk [data-app]').length, registry: typeof window.Apps, modFrame: typeof window.ModFrame})`);
      console.log('OS_SHELL ' + shell);
      const state = JSON.parse(String(shell));
      if (state.apps < 5 || state.registry !== 'object' || state.modFrame !== 'object') throw new Error('OS shell did not initialize');
      await win.webContents.executeJavaScript(`(() => {
        const slot = document.createElement('div'); document.body.append(slot);
        window.addEventListener('message', e => { if (e.data && 'parentReadable' in e.data) window.result = { ...e.data, origin: e.origin, sourceMatches: e.source === slot.querySelector('iframe').contentWindow, sandbox: slot.querySelector('iframe').getAttribute('sandbox') }; });
        ModFrame.define({ id: 'probe', mod: 'probe', title: 'Probe', frame: '/probe-child' });
        Apps.mount('probe', slot, { live: false, send() {}, info() { return {}; }, open() {}, close() {} }, {}, false);
        return true;
      })()`);
      let sandbox: unknown = null;
      for (let i = 0; i < 20; i++) {
        sandbox = await win.webContents.executeJavaScript('JSON.stringify(window.result || null)');
        if (sandbox !== 'null') break;
        await new Promise(r => setTimeout(r, 500));
      }
      console.log('SANDBOX ' + sandbox);
      const result = JSON.parse(String(sandbox));
      if (!result || result.parentReadable || result.storageReadable || result.bridge !== 'undefined' || result.origin !== 'null' || !result.sourceMatches || result.sandbox !== 'allow-scripts') throw new Error('sandbox isolation probe failed');
      finish('PROBE_OK', 0);
    } catch (error) { finish(`PROBE_FAIL ${error}`, 1); }
  });
  win.loadURL(`http://127.0.0.1:${server.port}/`);
}).catch(error => finish(`PROBE_FAIL ${error}`, 1));
