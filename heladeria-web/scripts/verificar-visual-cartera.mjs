// Smoke aislado: TODAS las API se simulan, también autenticación. Ningún dato real.
// node scripts/verificar-visual-cartera.mjs http://127.0.0.1:5189 <node_modules-con-playwright> [msedge|chrome]
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
async function fixture(nombre, reemplazos = {}) {
  let js = ts.transpileModule(await readFile(join(raiz, `src/test/${nombre}.ts`), 'utf8'), { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
  for (const [origen, destino] of Object.entries(reemplazos)) js = js.replaceAll(`'${origen}'`, JSON.stringify(destino));
  return `data:text/javascript;base64,${Buffer.from(js).toString('base64')}`;
}
const comprasUrl = await fixture('datosFaseVisual3');
const datos = await import(await fixture('datosFaseVisual7', { './datosFaseVisual3': comprasUrl }));
const cajaDatos = await import(await fixture('datosFaseVisual6'));
const base = process.argv[2] ?? 'http://127.0.0.1:5189';
assert.match(base, /^http:\/\/(127\.0\.0\.1|localhost):\d+$/);
const salida = join(raiz, 'test-results/visual-cartera');
await mkdir(salida, { recursive: true });
const permisos = ['CXC.CONSULTAR', 'CXC.COBRAR', 'CXP.CONSULTAR', 'CXP.ADMINISTRAR', 'CXP.PAGAR', 'CAJA.CONSULTAR', 'CAJA.OPERAR', 'CAJA.COBRAR', 'COMPRAS.CONSULTAR', 'PROVEEDORES.CONSULTAR', 'VENTAS.CONSULTAR'];
const sucursal = { id: 4, nombre: 'Sucursal principal', empresaId: 4, empresa: 'Heladería', esPrincipal: true, zonaHoraria: 'America/Guatemala' };
const usuario = { id: 1, nombreUsuario: 'operador-demo', correo: 'demo@invalid.local', rolId: 'DEMO', rol: 'Operador', sucursalActiva: sucursal, sucursales: [sucursal] };
const navegador = await chromium.launch({ headless: true, channel: process.argv[4] || undefined });
const informe = [];
try {
  for (const ancho of [320, 390, 768, 1366]) {
    const contexto = await navegador.newContext({ viewport: { width: ancho, height: 900 }, timezoneId: 'Asia/Tokyo', serviceWorkers: 'block' });
    const pagina = await contexto.newPage();
    let cobrar = structuredClone(datos.detalleCartera);
    let pagar = { encabezado: { ...datos.cuentaCartera, tercero: 'Lácteos del Valle', numeroDocumentoOrigen: 'PROV F-001', tipoDocumentoOrigen: 'FACTURA_PROVEEDOR', compraId: 25 }, historial: [] };
    const caja = structuredClone(cajaDatos.sesionCaja);
    const errores = [], inesperadas = [];
    pagina.on('pageerror', (e) => errores.push(e.message));
    pagina.on('console', (msg) => { if (msg.type() === 'error') errores.push(msg.text()); });
    await contexto.route('**/*', async (ruta) => {
      const req = ruta.request(), url = new URL(req.url()), path = url.pathname;
      const responder = (obj, status = 200) => ruta.fulfill({ status, contentType: 'application/json', body: JSON.stringify(obj) });
      if (!path.startsWith('/api/')) {
        if (url.origin === base) return ruta.continue();
        inesperadas.push(req.url()); return ruta.abort();
      }
      const body = req.method() === 'POST' ? req.postDataJSON() : null;
      if (path === '/api/autenticacion/renovar') return responder({ exito: true, accessToken: 'token-simulado-en-memoria', expiraAccessTokenUtc: '2099-01-01T00:00:00Z', usuario, requiereCambioContrasena: false });
      if (path === '/api/autenticacion/sesion') return responder({ usuario, permisos, requiereCambioContrasena: false });
      if (path === '/api/clientes') return responder(cajaDatos.paginaCaja([{ id: 27, nombreCliente: 'Cafetería del Parque' }]));
      if (path === '/api/compras/proveedores') return responder(cajaDatos.paginaCaja([{ id: 27, nombreProveedor: 'Lácteos del Valle' }]));
      if (path === '/api/caja/sesiones') return responder(cajaDatos.paginaCaja([caja.encabezado]));
      if (path === '/api/caja/sesiones/7') return responder(caja);
      if (path === '/api/compras/referencias') return responder({ monedas: [{ id: 6, codigoISO: 'GTQ' }], estados: [{ id: 93, flujo: 'ORDENES_COMPRA', codigo: 'RECIBIDA' }], series: [] });
      if (path === '/api/compras') return responder(cajaDatos.paginaCaja([datos.compraCredito.encabezado]));
      if (path === '/api/compras/51') return responder(datos.compraCredito);
      const cuenta = path.includes('por-pagar') ? pagar : cobrar;
      if (path.endsWith('/referencias')) return responder({ ...cajaDatos.referenciasCaja, formasPago: path.includes('por-pagar') ? cajaDatos.referenciasCaja.formasPago.filter((f) => f.esEfectivo) : cajaDatos.referenciasCaja.formasPago });
      if (path.endsWith('/resumen')) return responder(cuenta.encabezado.saldoPendiente ? [{ ...datos.resumenCartera, totalPendiente: cuenta.encabezado.saldoPendiente, totalVigente: cuenta.encabezado.saldoPendiente }] : []);
      if (path.endsWith('/abonos') || path.endsWith('/pagos')) {
        const nueva = datos.aplicarPagoDemo(cuenta, body.monto, body.claveOperacion, body.formaPagoId === 34 ? 'Tarjeta' : 'Efectivo');
        if (path.endsWith('/abonos')) cobrar = nueva; else pagar = nueva;
        caja.encabezado.versionFila = 'CAJA-V2';
        return responder({ cuenta: nueva, pagoId: 70, reintento: false });
      }
      if (path.endsWith('/desde-compra')) {
        pagar = { encabezado: { ...datos.cuentaCartera, id: 5, compraId: 51, tercero: 'Lácteos del Valle', numeroDocumentoOrigen: body.numeroDocumento, tipoDocumentoOrigen: 'FACTURA_PROVEEDOR' }, historial: [] };
        return responder({ facturaId: 18, cuenta: pagar, reintento: false });
      }
      if (/\/cuentas-por-(cobrar|pagar)\/\d+$/.test(path)) return responder(cuenta);
      if (/\/cuentas-por-(cobrar|pagar)$/.test(path)) return responder(cajaDatos.paginaCaja([cuenta.encabezado]));
      inesperadas.push(`${req.method()} ${path}`); return responder({ mensaje: 'API no prevista en prueba aislada' }, 500);
    });
    const capturar = async (pantalla) => {
      await pagina.evaluate(() => scrollTo(0, 0));
      const medidas = await pagina.evaluate(() => ({ viewport: innerWidth, pagina: document.documentElement.scrollWidth }));
      const modal = pagina.getByRole('dialog');
      if (await modal.count()) {
        const m = await modal.evaluate((e) => ({ ancho: e.clientWidth, contenido: e.scrollWidth, foco: e.contains(document.activeElement) }));
        assert.ok(m.contenido <= m.ancho, `${pantalla}: overflow modal ${JSON.stringify(m)}`);
        assert.ok(m.foco, `${pantalla}: foco fuera de modal`);
      }
      await pagina.screenshot({ path: join(salida, `${pantalla}-${ancho}.png`), fullPage: true });
      assert.ok(medidas.pagina <= medidas.viewport, `${pantalla} ${ancho}px overflow ${JSON.stringify(medidas)}`);
      informe.push({ pantalla, ancho, ...medidas });
    };
    for (const tipo of ['cobrar', 'pagar']) {
      await pagina.goto(`${base}/cuentas-por-${tipo}`);
      await pagina.getByRole('table', { name: `Cuentas por ${tipo}` }).waitFor(); await capturar(`lista-${tipo}`);
      await pagina.getByRole('link', { name: /^Ver detalle/ }).click();
      await pagina.getByText('Sin pagos registrados').waitFor(); await capturar(`detalle-${tipo}`);
      await pagina.getByRole('button', { name: tipo === 'cobrar' ? 'Registrar abono' : 'Registrar pago', exact: true }).click();
      await pagina.getByLabel('Sesión de caja').selectOption('7'); await pagina.getByLabel('Forma de pago').selectOption(tipo === 'cobrar' ? '34' : '21');
      await pagina.getByLabel('Monto', { exact: true }).fill('3'); await pagina.getByRole('button', { name: 'Revisar operación' }).waitFor();
      if (tipo === 'pagar') assert.equal(await pagina.getByRole('option', { name: 'Tarjeta' }).count(), 0);
      await capturar(`formulario-${tipo}`);
      await pagina.getByRole('button', { name: 'Revisar operación' }).click(); await capturar(`confirmacion-${tipo}`);
      await pagina.getByRole('button', { name: 'Confirmar operación' }).click();
      await pagina.getByRole('table', { name: 'Historial financiero' }).waitFor(); await capturar(`historial-${tipo}`);
      assert.ok((await pagina.getByRole('table', { name: 'Historial financiero' }).innerText()).includes('2:31'), 'Timestamp debe usar Guatemala, no Tokyo');
    }
    await pagina.goto(`${base}/cuentas-por-pagar/registrar-factura`);
    await pagina.getByRole('button', { name: /Compra recibida a crédito/ }).click();
    await pagina.getByRole('option', { name: /OC-00000051/ }).click();
    await pagina.getByLabel('Número de factura').fill('FACT-17'); await pagina.getByLabel('Fecha documento').fill('2026-10-10'); await pagina.getByLabel('Fecha vencimiento').fill('2026-10-30');
    await pagina.getByText(/Recepción completa/).waitFor(); await capturar('factura');
    await pagina.getByRole('button', { name: 'Revisar factura' }).click(); await pagina.getByRole('button', { name: 'Confirmar factura' }).click();
    await pagina.getByText('Factura registrada. Cuenta por pagar creada.').waitFor(); await capturar('factura-registrada');
    assert.deepEqual(errores, [], `Consola ${ancho}px`); assert.deepEqual(inesperadas, [], 'Ninguna API debe llegar a red');
    await contexto.close();
  }
} finally { await navegador.close(); }
await writeFile(join(salida, 'resultado.json'), JSON.stringify({ entorno: 'API simulada; navegador Tokyo; sucursal Guatemala', errores: 0, verificaciones: informe }, null, 2));
console.log(JSON.stringify({ verificaciones: informe.length, anchos: [320, 390, 768, 1366], errores: 0, resultado: salida }));
