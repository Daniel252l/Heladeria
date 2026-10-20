// Prueba aislada de modalidad: TODAS las API son simuladas, sin escrituras reales.
// node scripts/verificar-visual-compras.mjs http://127.0.0.1:5187 <node_modules-con-playwright> [msedge|chrome]
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
const js = ts.transpileModule(await readFile(join(raiz, 'src/test/datosFaseVisual3.ts'), 'utf8'), { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
const datos = await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
const base = process.argv[2] ?? 'http://127.0.0.1:5187';
assert.match(base, /^http:\/\/(127\.0\.0\.1|localhost):\d+$/);
const salida = join(raiz, 'test-results/visual-compras');
await mkdir(salida, { recursive: true });
const sucursal = { id: 4, nombre: 'Sucursal de prueba', empresaId: 4, empresa: 'Heladería', esPrincipal: true, zonaHoraria: 'America/Guatemala' };
const usuario = { id: 1, nombreUsuario: 'demo', correo: 'demo@invalid.local', rolId: 'ADMIN', rol: 'Administrador', sucursalActiva: sucursal, sucursales: [sucursal] };
const permisos = ['COMPRAS.CONSULTAR', 'COMPRAS.ADMINISTRAR', 'COMPRAS.RECIBIR', 'PROVEEDORES.CONSULTAR', 'PRODUCTOS.CONSULTAR'];
const navegador = await chromium.launch({ headless: true, channel: process.argv[4] || undefined });
const verificaciones = [];
try {
  for (const ancho of [320, 390, 768, 1366]) {
    const contexto = await navegador.newContext({ viewport: { width: ancho, height: 900 }, serviceWorkers: 'block' });
    const pagina = await contexto.newPage();
    let compra = datos.compraDetalle('BORRADOR', 10);
    const guardados = [], errores = [];
    pagina.on('pageerror', (error) => errores.push(error.message));
    pagina.on('console', (mensaje) => { if (mensaje.type() === 'error') errores.push(mensaje.text()); });
    await contexto.route('**/*', async (ruta) => {
      const req = ruta.request(), u = new URL(req.url()), path = u.pathname;
      const responder = (objeto) => ruta.fulfill({ contentType: 'application/json', body: JSON.stringify(objeto) });
      if (!path.startsWith('/api/')) {
        if (u.origin === base) return ruta.continue();
        errores.push(`Red externa bloqueada: ${path}`); return ruta.abort();
      }
      if (path === '/api/autenticacion/renovar') return responder({ exito: true, accessToken: 'token-simulado', expiraAccessTokenUtc: '2099-01-01T00:00:00Z', usuario, requiereCambioContrasena: false });
      if (path === '/api/autenticacion/sesion') return responder({ usuario, permisos, requiereCambioContrasena: false });
      if (path === '/api/compras/referencias') return responder(datos.referenciasCompras);
      if (path === '/api/compras/proveedores') return responder(datos.pagina([datos.proveedorPrueba]));
      if (path === '/api/productos') return responder(datos.pagina([datos.productoCompra]));
      if (path === '/api/compras/productos/17/presentaciones') return responder(datos.presentacionesCompra);
      if ((path === '/api/compras' && req.method() === 'POST') || (path === '/api/compras/51' && req.method() === 'PUT')) {
        const body = req.postDataJSON(); guardados.push(body);
        assert.deepEqual(Object.keys(body).sort(), ['sucursalId', 'proveedorId', 'fechaEmision', 'fechaEntregaEsperada', 'monedaId', 'tipoCambio', 'esCredito', 'detalles'].sort());
        compra.encabezado.esCredito = body.esCredito ?? (req.method() === 'POST' ? false : compra.encabezado.esCredito);
        return responder(compra);
      }
      if (path === '/api/compras/51') return responder(compra);
      errores.push(`API no prevista: ${req.method()} ${path}`); return ruta.abort();
    });
    const capturar = async (pantalla) => {
      await pagina.evaluate(() => scrollTo(0, 0));
      const medidas = await pagina.evaluate(() => ({ viewport: innerWidth, contenido: document.documentElement.scrollWidth }));
      assert.ok(medidas.contenido <= medidas.viewport, `${pantalla}: desbordamiento a ${ancho}px`);
      await pagina.screenshot({ path: join(salida, `${pantalla}-${ancho}.png`), fullPage: true });
      verificaciones.push({ pantalla, ancho });
    };
    const llenarNueva = async () => {
      await pagina.getByRole('button', { name: /^Proveedor \*/ }).click();
      await pagina.getByRole('option', { name: /PROV-007.*Lácteos del Valle/ }).click();
      await pagina.getByRole('button', { name: /^Producto \*/ }).click();
      await pagina.getByRole('option', { name: 'Base de vainilla' }).click();
      await pagina.getByRole('button', { name: /^Presentación \*/ }).click();
      await pagina.getByRole('option', { name: /Litro.*1,000/ }).click();
      await pagina.getByLabel('Cantidad *', { exact: true }).fill('4');
      await pagina.getByLabel('Precio unitario *', { exact: true }).fill('25');
    };
    const guardar = async () => {
      await pagina.getByRole('button', { name: 'Guardar borrador' }).click();
      await pagina.getByRole('heading', { name: 'Orden OC-00000051' }).waitFor();
    };
    for (const credito of [false, true]) {
      await pagina.goto(`${base}/compras/nueva`);
      await pagina.getByRole('heading', { name: 'Nueva orden de compra' }).waitFor();
      assert.equal(await pagina.getByLabel('Modalidad', { exact: true }).inputValue(), 'CONTADO');
      if (credito) await pagina.getByLabel('Modalidad', { exact: true }).selectOption('CREDITO');
      await llenarNueva(); await capturar(credito ? 'nueva-credito' : 'nueva-contado');
      await guardar(); assert.equal(guardados.at(-1).esCredito, credito);
      await pagina.getByText(credito ? 'Crédito' : 'Contado', { exact: true }).waitFor();
      await capturar(credito ? 'detalle-credito' : 'detalle-contado');
    }
    await pagina.getByRole('button', { name: 'Editar', exact: true }).click();
    await pagina.getByRole('heading', { name: 'Editar orden de compra' }).waitFor();
    assert.equal(await pagina.getByLabel('Modalidad', { exact: true }).inputValue(), 'CREDITO');
    await guardar(); assert.equal(guardados.at(-1).esCredito, true);
    await pagina.getByRole('button', { name: 'Editar', exact: true }).click();
    await pagina.getByRole('heading', { name: 'Editar orden de compra' }).waitFor();
    await pagina.getByLabel('Modalidad', { exact: true }).selectOption('CONTADO');
    await guardar(); assert.equal(guardados.at(-1).esCredito, false);
    compra.encabezado.esCredito = null;
    await pagina.goto(`${base}/compras/51/editar`);
    await pagina.getByRole('heading', { name: 'Editar orden de compra' }).waitFor();
    assert.equal(await pagina.getByLabel('Modalidad', { exact: true }).inputValue(), '');
    await capturar('edicion-historica');
    await guardar(); assert.equal(guardados.at(-1).esCredito, null);
    await pagina.getByText('Sin definir', { exact: true }).waitFor();
    await capturar('detalle-historico');
    assert.deepEqual(errores, []);
    await contexto.close();
  }
} finally { await navegador.close(); }
await writeFile(join(salida, 'resultado.json'), JSON.stringify({ entorno: 'API completamente simulada, sin BD real', errores: 0, verificaciones }, null, 2));
console.log(JSON.stringify({ verificaciones: verificaciones.length, errores: 0, salida }));
