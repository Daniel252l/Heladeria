// Smoke aislado: intercepta TODAS las API; no conecta backend ni crea datos reales.
// node scripts/verificar-visual-caja.mjs http://127.0.0.1:5187 <node_modules-con-playwright> [msedge|chrome]
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const local = createRequire(join(raiz, 'package.json'));
const browser = process.argv[3] ? createRequire(join(resolve(process.argv[3]), '../package.json')) : local;
const { chromium } = browser('playwright');
const ts = local('typescript');
const fixture = ts.transpileModule(await readFile(join(raiz, 'src/test/datosFaseVisual6.ts'), 'utf8'), { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
const datos = await import(`data:text/javascript;base64,${Buffer.from(fixture).toString('base64')}`);
const base = process.argv[2] ?? 'http://127.0.0.1:5187';
assert.match(base, /^http:\/\/(127\.0\.0\.1|localhost):\d+$/);
const salida = join(raiz, 'test-results/visual-caja');
await mkdir(salida, { recursive: true });
const permisos = ['CAJA.CONSULTAR', 'CAJA.ABRIR', 'CAJA.OPERAR', 'CAJA.COBRAR', 'CAJA.CERRAR', 'CAJA.MOVIMIENTO_MANUAL'];
const sucursal = { id: 4, nombre: 'Sucursal principal', empresaId: 4, empresa: 'Heladería', esPrincipal: true, zonaHoraria: 'America/Guatemala' };
const usuario = { id: 1, nombreUsuario: 'operador-demo', correo: 'demo@invalid.local', rolId: 'DEMO', rol: 'Operador', sucursalActiva: sucursal, sucursales: [sucursal] };
const navegador = await chromium.launch({ headless: true, channel: process.argv[4] || undefined });
const informe = [];
try {
  for (const ancho of [320, 390, 768, 1366, 1600]) {
    // Zona deliberadamente diferente: el render debe usar la sucursal, no el navegador.
    const contexto = await navegador.newContext({ viewport: { width: ancho, height: 900 }, timezoneId: 'Asia/Tokyo', serviceWorkers: 'block' });
    const pagina = await contexto.newPage();
    let sesion = structuredClone(datos.sesionCaja);
    const errores = [], inesperadas = [];
    pagina.on('pageerror', (error) => errores.push(error.message));
    pagina.on('console', (msg) => { if (msg.type() === 'error') errores.push(msg.text()); });
    await contexto.route('**/*', async (ruta) => {
      const req = ruta.request(), url = new URL(req.url());
      const responder = (obj, status = 200) => ruta.fulfill({ status, contentType: 'application/json', body: JSON.stringify(obj) });
      if (!url.pathname.startsWith('/api/')) {
        if (url.origin === base) return ruta.continue();
        inesperadas.push(req.url()); return ruta.abort();
      }
      const body = req.method() === 'POST' ? req.postDataJSON() : null;
      if (url.pathname === '/api/autenticacion/renovar') return responder({ exito: true, accessToken: 'token-simulado-en-memoria', expiraAccessTokenUtc: '2099-01-01T00:00:00Z', usuario, requiereCambioContrasena: false });
      if (url.pathname === '/api/autenticacion/sesion') return responder({ usuario, permisos, requiereCambioContrasena: false });
      if (url.pathname === '/api/caja/cajas') return responder(body ? datos.cajaDemo : datos.paginaCaja([datos.cajaDemo]));
      if (url.pathname === '/api/caja/referencias') return responder({ ...datos.referenciasCaja, saldoUltimoCierre: url.searchParams.has('cajaId') ? { sesionId: 6, cajaId: 9, numeroCierre: 'CC-ANTERIOR', fechaCierre: '2026-10-22T21:00:00Z', efectivoContado: 98 } : null });
      if (url.pathname === '/api/caja/ventas-pendientes') return responder(datos.paginaCaja(sesion.cobros.length ? [] : [datos.pendienteCaja]));
      if (url.pathname === '/api/caja/sesiones') return responder(datos.paginaCaja([sesion.encabezado]));
      if (url.pathname === '/api/caja/sesiones/7' || url.pathname === '/api/caja/sesiones/abrir') return responder(sesion);
      if (url.pathname.endsWith('/cobrar-venta')) { const r = datos.respuestaCobro(body.pagos, body.claveOperacion); sesion = r.sesion; return responder(r); }
      if (url.pathname.endsWith('/arqueo')) {
        sesion = { ...sesion, encabezado: { ...sesion.encabezado, versionFila: 'ARQUEO-V2' }, arqueos: [{ ...datos.arqueoCaja, esperado: sesion.efectivoEsperado, contado: body.efectivoContado, diferencia: body.efectivoContado - sesion.efectivoEsperado }] };
        return responder(sesion);
      }
      if (url.pathname.endsWith('/cerrar')) {
        const arqueo = sesion.arqueos[0];
        sesion = { ...sesion, usuarioCierre: 'admin', encabezado: { ...sesion.encabezado, ...datos.sesionCerrada.encabezado, efectivoEsperadoCierre: arqueo.esperado, efectivoContadoCierre: arqueo.contado, diferenciaCierre: arqueo.diferencia, totalIngresosCierre: 16 } };
        return responder(sesion);
      }
      inesperadas.push(`${req.method()} ${url.pathname}`); return responder({ mensaje: 'API no prevista en prueba aislada' }, 500);
    });
    const capturar = async (pantalla) => {
      await pagina.evaluate(() => scrollTo(0, 0));
      const medidas = await pagina.evaluate(() => ({ viewport: innerWidth, pagina: document.documentElement.scrollWidth }));
      const modal = pagina.getByRole('dialog');
      if (await modal.count()) {
        const m = await modal.evaluate((e) => ({ ancho: e.clientWidth, contenido: e.scrollWidth, foco: e.contains(document.activeElement) }));
        assert.ok(m.contenido <= m.ancho, `${pantalla}: overflow modal a ${ancho}px ${JSON.stringify(m)}`);
        assert.ok(m.foco, `${pantalla}: foco fuera de modal`);
      }
      await pagina.screenshot({ path: join(salida, `${pantalla}-${ancho}.png`), fullPage: true });
      assert.ok(medidas.pagina <= medidas.viewport, `${pantalla} ${ancho}px overflow ${JSON.stringify(medidas)}`);
      informe.push({ pantalla, ancho, ...medidas });
    };
    await pagina.goto(`${base}/caja`); await pagina.getByRole('region', { name: 'Resumen financiero' }).waitFor(); await capturar('resumen');
    await pagina.goto(`${base}/caja/cajas`); await pagina.getByRole('table', { name: 'Listado de cajas' }).waitFor(); await capturar('cajas');
    await pagina.getByRole('button', { name: 'Registrar caja' }).click(); await capturar('registrar-caja'); await pagina.keyboard.press('Escape');
    await pagina.goto(`${base}/caja/abrir?cajaId=9`); await pagina.getByLabel('Saldo inicial').waitFor();
    assert.equal(await pagina.getByLabel('Saldo inicial').inputValue(), '98.00'); await capturar('apertura');
    await pagina.goto(`${base}/caja/sesiones`); await pagina.getByRole('table', { name: 'Listado de sesiones' }).waitFor(); await capturar('sesiones');
    await pagina.goto(`${base}/caja/sesiones/7`); await pagina.getByRole('table', { name: 'Movimientos de caja' }).waitFor();
    if (ancho < 1024) await pagina.getByRole('button', { name: 'Abrir navegación', exact: true }).click();
    await pagina.getByRole('button', { name: 'Caja', exact: true }).click();
    assert.equal(await pagina.locator('nav[aria-label="Navegación principal"] a[aria-current="page"]').count(), 1, 'Solo una sección de Caja debe estar activa');
    if (ancho < 1024) await pagina.getByRole('button', { name: 'Cerrar navegación', exact: true }).click();
    assert.ok((await pagina.locator('main').innerText()).includes('2:16'), 'Apertura debe renderizar Guatemala'); await capturar('detalle');
    assert.equal(await pagina.getByRole('button', { name: 'Cobrar venta', exact: true }).count(), 0);
    await pagina.getByText('Cobros anteriores pendientes', { exact: true }).click();
    await pagina.getByRole('button', { name: 'Cobrar venta anterior', exact: true }).click(); await pagina.getByRole('button', { name: 'Cobrar VT-0000000012' }).waitFor(); await capturar('pendientes');
    await pagina.getByRole('button', { name: 'Cobrar VT-0000000012' }).click(); await pagina.getByLabel('Monto recibido 1').fill('20'); await capturar('efectivo');
    await pagina.getByLabel('Forma de pago 1', { exact: true }).focus();
    await pagina.getByLabel('Forma de pago 1', { exact: true }).selectOption('34'); await capturar('tarjeta');
    await pagina.getByLabel('Forma de pago 1', { exact: true }).selectOption('21');
    await pagina.getByLabel('Monto aplicado 1').fill('10'); await pagina.getByLabel('Monto recibido 1').fill('10');
    await pagina.getByRole('button', { name: 'Agregar forma de pago' }).click(); await pagina.getByLabel('Monto aplicado 2').fill('6'); await capturar('mixto');
    await pagina.getByRole('button', { name: 'Revisar cobro' }).click(); await capturar('confirmar-cobro');
    await pagina.getByRole('button', { name: 'Confirmar cobro' }).click(); await pagina.getByText(/Cobro registrado para/).waitFor();
    assert.ok((await pagina.getByRole('region', { name: 'Resumen financiero' }).innerText()).includes('GTQ 60.00'));
    assert.ok((await pagina.locator('main').innerText()).includes('2:42'), 'Cobro y movimiento deben renderizar Guatemala'); await capturar('cobrado');
    await pagina.getByRole('button', { name: 'Registrar ingreso', exact: true }).click(); await pagina.getByLabel('Monto', { exact: true }).fill('5'); await pagina.getByLabel('Motivo').fill('Fondo operativo'); await capturar('manual'); await pagina.keyboard.press('Escape');
    await pagina.getByRole('button', { name: 'Registrar egreso', exact: true }).click();
    assert.equal(await pagina.getByLabel('Tipo de movimiento').inputValue(), 'EGRESO'); await capturar('egreso'); await pagina.keyboard.press('Escape');
    await pagina.getByRole('button', { name: 'Registrar arqueo', exact: true }).click(); await pagina.getByLabel('Efectivo contado').fill('58'); await capturar('arqueo');
    await pagina.getByRole('button', { name: 'Guardar arqueo' }).click(); await pagina.getByText('Vigente', { exact: true }).last().waitFor();
    assert.ok((await pagina.locator('main').innerText()).includes('2:30'), 'Arqueo debe renderizar Guatemala');
    await pagina.getByRole('button', { name: 'Cerrar sesión de caja', exact: true }).click(); await capturar('confirmar-cierre');
    await pagina.getByRole('button', { name: 'Confirmar cierre' }).click(); await pagina.getByRole('heading', { name: 'Cierre confirmado' }).waitFor();
    assert.equal(await pagina.getByRole('button', { name: 'Cobrar venta', exact: true }).count(), 0);
    assert.ok((await pagina.locator('main').innerText()).includes('2:37'), 'Cierre debe renderizar Guatemala'); await capturar('cerrada');
    assert.deepEqual(errores, [], `Errores de consola a ${ancho}px`); assert.deepEqual(inesperadas, [], 'Ninguna API debe llegar a red');
    await contexto.close();
  }
} finally { await navegador.close(); }
await writeFile(join(salida, 'resultado.json'), JSON.stringify({ entorno: 'API totalmente simulada, navegador Asia/Tokyo y sucursal America/Guatemala', errores: 0, verificaciones: informe }, null, 2));
console.log(JSON.stringify({ verificaciones: informe.length, anchos: [320, 390, 768, 1366, 1600], errores: 0, resultado: salida }));
