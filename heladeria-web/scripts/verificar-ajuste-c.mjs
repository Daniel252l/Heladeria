// UI real + APIs completamente interceptadas. Ningún acceso a la BD o API del usuario.
// node scripts/verificar-ajuste-c.mjs http://127.0.0.1:5193 <node_modules-con-playwright> [msedge|chrome]
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const local = createRequire(join(raiz, 'package.json'));
const externo = process.argv[3] ? createRequire(join(resolve(process.argv[3]), '../package.json')) : local;
const { chromium } = externo('playwright'), ts = local('typescript');
const js = ts.transpileModule(await readFile(join(raiz, 'src/test/datosAjusteC.ts'), 'utf8'), { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
const { ticketDemo, diarioDemo } = await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
const base = process.argv[2] ?? 'http://127.0.0.1:5193';
assert.match(base, /^http:\/\/(127\.0\.0\.1|localhost):\d+$/);
const salida = join(raiz, 'test-results/ajuste-c-visual'); await mkdir(salida, { recursive: true });
const sucursal = { id: 4, nombre: 'Sucursal principal', empresaId: 4, empresa: 'Heladería', esPrincipal: true, zonaHoraria: 'America/Guatemala' };
const usuario = { id: 1, nombreUsuario: 'admin-demo', correo: 'demo@invalid.local', rolId: 'ADMIN', rol: 'Administrador', sucursalActiva: sucursal, sucursales: [sucursal] };
const browser = await chromium.launch({ headless: true, channel: process.argv[4] || undefined });
const resultados = [];
try {
  for (const ancho of [320, 390, 768, 1366, 1600]) {
    const context = await browser.newContext({ viewport: { width: ancho, height: 900 }, timezoneId: 'Asia/Tokyo', serviceWorkers: 'block' });
    const page = await context.newPage();
    const errores = [], inesperadas = [], consultas = []; let modo = 'contado';
    page.on('pageerror', e => errores.push(e.message));
    page.on('console', m => { if (m.type() === 'error') errores.push(m.text()); });
    await page.addInitScript(() => { window.impresiones = 0; window.print = () => { window.impresiones++; }; });
    await context.route('**/*', async route => {
      const req = route.request(), url = new URL(req.url()), path = url.pathname;
      const responder = data => route.fulfill({ contentType: 'application/json', body: JSON.stringify(data) });
      if (!path.startsWith('/api/')) {
        if (url.origin === base) return route.continue();
        inesperadas.push(req.url()); return route.abort();
      }
      if (path === '/api/autenticacion/renovar') return responder({ exito: true, accessToken: 'demo-en-memoria', expiraAccessTokenUtc: '2099-01-01T00:00:00Z', usuario, requiereCambioContrasena: false });
      if (path === '/api/autenticacion/sesion') return responder({ usuario, permisos: ['VENTAS.CONSULTAR', 'REPORTES.CONSULTAR'], requiereCambioContrasena: false });
      if (req.method() !== 'GET') { inesperadas.push(`${req.method()} ${path}`); return route.abort(); }
      consultas.push(url);
      if (path === '/api/ventas/2') {
        const ticket = structuredClone(ticketDemo);
        if (modo === 'credito') { Object.assign(ticket.encabezado, { esCredito: true, cliente: 'Cafetería del Parque' }); Object.assign(ticket.comprobante, { codigoCliente: 'CL-01', pagos: [], saldoInicialCredito: 50, cuentaPorCobrarId: 1 }); }
        if (modo === 'anulada') Object.assign(ticket.encabezado, { estado: 'ANULADA', fechaAnulacion: '2026-10-07T17:00:00Z', motivoAnulacion: 'Anulación de histórico sin cobro' });
        return responder(ticket);
      }
      if (path === '/api/reportes/diario') return responder(diarioDemo);
      inesperadas.push(req.url()); return route.abort();
    });
    for (modo of ['contado', 'credito', 'anulada', 'diario']) {
      await page.emulateMedia({ media: 'screen' });
      await page.goto(base + (modo === 'diario' ? '/reportes/diario' : '/ventas/2/comprobante'));
      await page.getByRole(modo === 'diario' ? 'table' : 'article', { name: modo === 'diario' ? 'Ventas del día' : 'Comprobante de venta', exact: true }).waitFor();
      assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), `${modo} ${ancho}: overflow de página`);
      if (modo !== 'diario') {
        assert.equal(await page.getByRole('navigation', { name: 'Navegación principal' }).count(), 0);
        assert.ok((await page.locator('.comprobante-ticket').innerText()).includes('10:00'), 'Usa hora Guatemala, no Tokyo');
        if (modo === 'anulada') assert.equal(await page.locator('.comprobante-anulada').innerText(), 'ANULADA');
      } else {
        assert.equal(await page.getByLabel('Fecha', { exact: true }).inputValue(), '2026-10-07');
        const antes = consultas.length;
        await page.getByLabel('Modalidad', { exact: true }).selectOption('credito');
        assert.equal(consultas.length, antes);
        await Promise.all([page.waitForResponse(r => r.url().includes('/api/reportes/diario?') && r.url().includes('esCredito=true')), page.getByRole('button', { name: 'Aplicar', exact: true }).click()]);
        assert.equal(consultas.at(-1).searchParams.get('esCredito'), 'true');
        await page.getByRole('table', { name: 'Ventas del día', exact: true }).waitFor();
        await Promise.all([page.waitForResponse(r => r.url().includes('/api/reportes/diario?') && !r.url().includes('esCredito=')), page.getByRole('button', { name: 'Limpiar', exact: true }).click()]);
        await page.getByRole('table', { name: 'Ventas del día', exact: true }).waitFor();
        assert.equal(consultas.at(-1).searchParams.has('esCredito'), false);
        if (ancho <= 768) assert.ok(await page.locator('.reporte-tabla').first().evaluate(e => e.scrollWidth > e.clientWidth), 'scroll interno');
      }
      assert.equal(await page.evaluate(() => window.impresiones), 0);
      await page.getByRole('button', { name: modo === 'diario' ? 'Imprimir reporte' : 'Imprimir comprobante', exact: true }).click();
      assert.equal(await page.evaluate(() => window.impresiones), 1);
      await page.screenshot({ path: join(salida, `${modo}-${ancho}.png`), fullPage: true });
      await page.emulateMedia({ media: 'print' });
      assert.equal(await page.getByRole('button', { name: modo === 'diario' ? 'Imprimir reporte' : 'Imprimir comprobante', exact: true }).isVisible(), false);
      if (modo === 'diario') {
        assert.equal(await page.getByRole('navigation', { name: 'Navegación principal' }).isVisible(), false);
        assert.equal(await page.getByRole('table', { name: 'Ventas del día', exact: true }).isVisible(), true);
        assert.equal(await page.locator('.diario-vista').evaluate(e => getComputedStyle(e).page), 'diario');
      }
      if (ancho === 1366) {
        await page.pdf({ path: join(salida, `${modo}.pdf`), preferCSSPageSize: true, printBackground: true, ...(modo === 'diario' ? {} : { width: '80mm', height: '297mm' }) });
        await page.screenshot({ path: join(salida, `${modo}-impresion.png`), fullPage: true });
      }
      resultados.push({ modo, ancho, overflow: false, impresion: true });
    }
    assert.deepEqual(errores, []); assert.deepEqual(inesperadas, []);
    await context.close();
  }
  await writeFile(join(salida, 'resultado.json'), JSON.stringify(resultados, null, 2));
  console.log(JSON.stringify({ escenarios: resultados.length, errores: 0, anchos: [320, 390, 768, 1366, 1600], salida }));
} finally { await browser.close(); }
