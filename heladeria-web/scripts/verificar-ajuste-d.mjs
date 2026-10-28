// Navegador real, API interceptada: nunca llama a la API/BD del usuario.
// node scripts/verificar-ajuste-d.mjs http://127.0.0.1:5193 <node_modules-con-playwright> [msedge]
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const local = createRequire(join(raiz, 'package.json'));
const externo = process.argv[3] ? createRequire(join(resolve(process.argv[3]), '../package.json')) : local;
const { chromium } = externo('playwright'), ts = local('typescript');
const js = ts.transpileModule(await readFile(join(raiz, 'src/test/datosFaseVisual2.ts'), 'utf8'), { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
const { productoPrueba, presentacionPrueba, paginaPrueba } = await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
const base = process.argv[2] ?? 'http://127.0.0.1:5193'; assert.match(base, /^http:\/\/(127\.0\.0\.1|localhost):\d+$/);
const salida = join(raiz, 'test-results/ajuste-d-visual'); await mkdir(salida, { recursive: true });
const sucursal = { id: 4, nombre: 'Sucursal principal', empresaId: 4, empresa: 'Heladería', esPrincipal: true, zonaHoraria: 'America/Guatemala' };
const usuario = { id: 1, nombreUsuario: 'admin-demo', correo: 'demo@invalid.local', rolId: 'ADMIN', rol: 'Administrador', sucursalActiva: sucursal, sucursales: [sucursal] };
const producto = { ...productoPrueba, id: 40, nombre: 'Vaca Loca', sku: 'VACA-LOCA', unidadBase: 'Unidad', abreviaturaUnidadBase: 'un', componentes: [] };
const browser = await chromium.launch({ headless: true, channel: process.argv[4] || undefined });
const resultados = [];
try {
  for (const ancho of [320, 390, 768, 1366, 1600]) {
    const context = await browser.newContext({ viewport: { width: ancho, height: 900 }, serviceWorkers: 'block' });
    const page = await context.newPage(); const errores = [], inesperadas = [], escrituras = [];
    page.on('pageerror', e => errores.push(e.message));
    await context.route('**/*', async route => {
      const req = route.request(), url = new URL(req.url()), path = url.pathname;
      const responder = data => route.fulfill({ contentType: 'application/json', body: JSON.stringify(data) });
      if (!path.startsWith('/api/')) { if (url.origin === base) return route.continue(); inesperadas.push(req.url()); return route.abort(); }
      if (path === '/api/autenticacion/renovar') return responder({ exito: true, accessToken: 'demo-en-memoria', expiraAccessTokenUtc: '2099-01-01T00:00:00Z', usuario, requiereCambioContrasena: false });
      if (path === '/api/autenticacion/sesion') return responder({ usuario, permisos: ['PRODUCTOS.CONSULTAR', 'PRODUCTOS.ADMINISTRAR'], requiereCambioContrasena: false });
      if (req.method() === 'PUT' && path === '/api/productos/40') { escrituras.push(req.postDataJSON()); return responder(producto); }
      if (req.method() !== 'GET') { inesperadas.push(`${req.method()} ${path}`); return route.abort(); }
      if (path === '/api/productos/referencias') return responder({ tiposSeguimiento: [{ id: 10, codigo: 'NINGUNO', nombre: 'Ninguno' }], metodosValuacion: [{ id: 11, codigo: 'PROMEDIO', nombre: 'Promedio' }] });
      if (path === '/api/productos') return responder(paginaPrueba([url.searchParams.has('soloComponentesVenta') ? productoPrueba : producto]));
      if (path === '/api/productos/40') return responder(producto);
      if (path === '/api/productos/17') return responder({ ...productoPrueba, esVendible: false });
      if (path === '/api/productos/17/presentaciones') return responder([{ ...presentacionPrueba, presentacion: 'Bola', unidadMedida: 'Bola', abreviaturaUnidadMedida: 'bola', factorConversion: 60, permiteVenta: false }]);
      if (['/api/productos/categorias','/api/productos/marcas','/api/productos/tipos','/api/productos/unidades-medida','/api/configuracion/estados'].includes(path)) return responder(paginaPrueba([{ id: 6, nombre: 'Activo' }, { id: 7, nombre: 'Helados' }, { id: 8, nombre: 'Inventariable' }, { id: 9, nombre: 'Unidad' }]));
      inesperadas.push(req.url()); return route.abort();
    });
    await page.goto(base + '/productos'); await page.getByRole('button', { name: 'Editar Vaca Loca', exact: true }).click();
    const dialogo = page.getByRole('dialog', { name: 'Editar producto' });
    await dialogo.getByRole('button', { name: 'Agregar componente' }).click();
    await dialogo.getByRole('button', { name: /Producto\/Insumo 1/ }).click();
    await dialogo.getByRole('option', { name: /Helado de vainilla/ }).click();
    await dialogo.getByRole('combobox', { name: /^Presentación 1/ }).selectOption('23');
    await dialogo.getByLabel('Cantidad 1', { exact: true }).fill('2');
    await dialogo.getByText('2 bola = 120 ml', { exact: true }).waitFor();
    assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), `${ancho}: overflow página`);
    assert.ok(await dialogo.evaluate(e => e.scrollWidth <= e.clientWidth + 1), `${ancho}: overflow modal`);
    await dialogo.getByLabel('Cantidad 1', { exact: true }).scrollIntoViewIfNeeded();
    await page.screenshot({ path: join(salida, `composicion-${ancho}.png`), fullPage: true });
    await dialogo.getByRole('button', { name: 'Guardar', exact: true }).click();
    await dialogo.waitFor({ state: 'hidden' });
    assert.equal(escrituras.length, 1); assert.deepEqual(escrituras[0].componentes, [{ productoId: 17, productoPresentacionId: 23, cantidad: 2 }]);
    assert.deepEqual(errores, []); assert.deepEqual(inesperadas, []);
    resultados.push({ ancho, overflow: false, equivalencia: '2 bola = 120 ml', payload: true }); await context.close();
  }
  await writeFile(join(salida, 'resultado.json'), JSON.stringify(resultados, null, 2));
  console.log(JSON.stringify({ escenarios: resultados.length, errores: 0, salida }));
} finally { await browser.close(); }
