// Navegador real con transporte completamente interceptado; no conecta a la API/BD del propietario.
// node scripts/verificar-ajuste-e.mjs http://127.0.0.1:5194 <node_modules-con-playwright> [msedge]
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const local = createRequire(join(raiz, 'package.json'));
const externo = process.argv[3] ? createRequire(join(resolve(process.argv[3]), '../package.json')) : local;
const { chromium } = externo('playwright'), ts = local('typescript');
async function fixture(nombre) {
  const js = ts.transpileModule(await readFile(join(raiz, 'src/test', nombre), 'utf8'), { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
  return import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
}
const datos = await fixture('datosFaseVisual5.ts'), caja = await fixture('datosFaseVisual6.ts');
const base = process.argv[2] ?? 'http://127.0.0.1:5194'; assert.match(base, /^http:\/\/(127\.0\.0\.1|localhost):\d+$/);
const salida = join(raiz, 'test-results/ajuste-e-visual'); await mkdir(salida, { recursive: true });
const permisos = ['VENTAS.CONSULTAR', 'VENTAS.ADMINISTRAR', 'VENTAS.VENDER', 'POS.OPERAR', 'CAJA.CONSULTAR', 'CAJA.OPERAR', 'CAJA.COBRAR'];
const sucursal = { id: 4, nombre: 'Sucursal Central', empresaId: 4, empresa: 'Heladería', esPrincipal: true, zonaHoraria: 'America/Guatemala' };
const usuario = { id: 25, nombreUsuario: 'ventas', correo: 'ventas@fixture.invalid', rolId: 'VENDEDOR', rol: 'Vendedor', sucursalActiva: sucursal, sucursales: [sucursal] };
const cliente = { id: 27, codigo: 'CLI-27', nombre: 'Cliente de crédito' };
const unidad = { ...datos.productoPos.presentaciones[0], nombre: 'Unidad', abreviatura: 'un', factorConversion: 1, precio: 28 };
const productos = [datos.productoPos,
  ...['Helado Vainilla', 'Helado Fresa', 'Helado de Pistacho', 'Helado Café'].map((nombre, i) => ({ ...datos.productoPos, id: i + 3, nombre })),
  { ...datos.productoPos, id: 9, nombre: 'Agua natural', categoriaId: 30, categoria: 'Bebidas', presentaciones: [{ ...unidad, precio: 6 }] },
  { ...datos.productoPos, id: 10, nombre: 'Vaca Loca', manejaExistencia: false, disponibleBase: null, categoriaId: 30, categoria: 'Especialidades', presentaciones: [unidad] },
  { ...datos.productoPos, id: 11, nombre: 'Helado agotado', disponibleBase: 0 }];
const navegador = await chromium.launch({ headless: true, channel: process.argv[4] || undefined });
const informe = [];
try {
  for (const ancho of [320, 390, 768, 1366, 1600]) {
    const context = await navegador.newContext({ viewport: { width: ancho, height: 900 }, serviceWorkers: 'block' });
    const page = await context.newPage(); const errores = [], inesperadas = [], escrituras = [];
    let vacio = true, venta = structuredClone(datos.ventaBorrador);
    page.on('pageerror', e => errores.push(e.message));
    page.on('console', e => { if (e.type() === 'error') errores.push(e.text()); });
    await context.route('**/*', async route => {
      const req = route.request(), url = new URL(req.url()), path = url.pathname;
      const responder = (data, status = 200) => route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(data) });
      if (!path.startsWith('/api/')) { if (url.origin === base) return route.continue(); inesperadas.push(req.url()); return route.abort(); }
      if (path === '/api/autenticacion/renovar') return responder({ exito: true, accessToken: 'fixture-en-memoria', expiraAccessTokenUtc: '2099-01-01T00:00:00Z', usuario, requiereCambioContrasena: false });
      if (path === '/api/autenticacion/sesion') return responder({ usuario, permisos, requiereCambioContrasena: false });
      if (path === '/api/ventas/referencias') return responder({ ...datos.referenciasVentas, clientes: [cliente] });
      if (path === '/api/caja/referencias') return responder(caja.referenciasCaja);
      if (path === '/api/caja/sesiones') return responder(caja.paginaCaja([caja.sesionCaja.encabezado]));
      if (path === '/api/ventas/productos') return responder(datos.paginaVentas(vacio ? [] : productos.filter(p =>
        (!url.searchParams.get('categoriaId') || String(p.categoriaId) === url.searchParams.get('categoriaId')) &&
        (!url.searchParams.get('busqueda') || p.nombre.toLowerCase().includes(url.searchParams.get('busqueda').toLowerCase())))));
      if (path === '/api/ventas' && req.method() === 'POST') {
        const body = req.postDataJSON(); escrituras.push({ path, body });
        venta = { ...venta, encabezado: { ...venta.encabezado, ...body, total: 28, subtotal: 28, cliente: body.clienteId ? cliente.nombre : null } };
        return responder(venta, 201);
      }
      if (path === '/api/ventas/2/confirmar') { escrituras.push({ path }); venta.encabezado.estado = 'CONFIRMADA'; venta.encabezado.versionFila = 'V2'; return responder(venta); }
      if (path === '/api/ventas/2/completar') { const body = req.postDataJSON(); escrituras.push({ path, body }); venta.encabezado.estado = 'COMPLETADA'; venta.encabezado.claveOperacion = body.claveOperacion; return responder(venta); }
      if (path === '/api/ventas/2') return responder(venta);
      inesperadas.push(`${req.method()} ${path}`); return responder({ mensaje: 'Ruta fuera del fixture' }, 500);
    });
    const capturar = async estado => {
      await page.evaluate(() => scrollTo(0, 0));
      const medidas = await page.evaluate(() => ({ viewport: innerWidth, documento: document.documentElement.scrollWidth }));
      assert.ok(medidas.documento <= medidas.viewport, `${estado}/${ancho}: overflow ${JSON.stringify(medidas)}`);
      if (ancho >= 1280) assert.equal(await page.locator('.pos-carrito').evaluate(e => getComputedStyle(e).position), 'sticky');
      if (await page.getByRole('dialog').count()) {
        const m = await page.getByRole('dialog').evaluate(e => ({ ancho: e.clientWidth, scroll: e.scrollWidth, foco: e.contains(document.activeElement) }));
        assert.ok(m.scroll <= m.ancho, `Overflow modal ${ancho}`); assert.ok(m.foco, `Foco modal ${ancho}`);
      }
      if (ancho < 1280 && /carrito|compuesto|contado|credito/.test(estado) && !(await page.getByRole('dialog').count()))
        await page.locator('#carrito-pos').evaluate(e => scrollTo(0, e.getBoundingClientRect().top + scrollY - 96));
      await page.screenshot({ path: join(salida, `${estado}-${ancho}.png`), fullPage: ancho < 1280 && !/carrito|compuesto|contado|credito|cierre|finalizada/.test(estado), timeout: 60000, animations: 'disabled' });
      informe.push({ estado, ancho, ...medidas });
    };
    await page.goto(`${base}/dashboard`); await page.getByText('No se encontraron productos', { exact: true }).waitFor();
    assert.equal(new URL(page.url()).pathname, '/pos');
    await capturar('01-pos-vacio');
    vacio = false; await page.reload(); await page.getByRole('article', { name: 'Helado Chocolate', exact: true }).waitFor();
    await capturar('02-catalogo-lleno');
    await page.getByRole('button', { name: 'Bebidas', exact: true }).click(); await page.getByRole('article', { name: 'Agua natural' }).waitFor();
    assert.equal(await page.getByRole('article').count(), 2); await capturar('03-categoria');
    await page.getByRole('button', { name: 'Todos', exact: true }).click();
    await page.getByPlaceholder('Buscar producto').fill('Pistacho'); await page.getByRole('article', { name: 'Helado de Pistacho' }).waitFor();
    await page.waitForFunction(() => document.querySelectorAll('article').length === 1); await capturar('04-busqueda');
    await page.getByPlaceholder('Buscar producto').fill(''); await page.getByRole('article', { name: 'Helado Chocolate', exact: true }).waitFor();
    await page.getByLabel('Presentación · Helado Chocolate').selectOption('1');
    await page.getByRole('button', { name: 'Agregar Helado Chocolate Litro', exact: true }).click(); await capturar('05-carrito-uno');
    await page.getByRole('button', { name: 'Incrementar Helado Chocolate Litro', exact: true }).click();
    await page.getByRole('button', { name: 'Agregar Agua natural Unidad', exact: true }).click(); await capturar('06-carrito-varios');
    await page.getByRole('button', { name: 'Agregar Vaca Loca Unidad', exact: true }).click(); await capturar('07-compuesto');
    assert.equal(await page.locator('#carrito-pos li').count(), 3);
    await page.getByRole('button', { name: 'Contado', exact: true }).focus(); await page.keyboard.press('Enter'); await capturar('08-contado');
    await page.getByRole('button', { name: 'Crédito', exact: true }).click();
    await page.getByRole('button', { name: /^Cliente \*/ }).click(); await page.getByRole('option', { name: /Cliente de crédito/ }).click();
    await page.getByLabel(/Fecha de vencimiento/).fill('2099-12-31'); await capturar('09-credito');
    const agotado = page.getByRole('article', { name: 'Helado agotado', exact: true });
    assert.equal(await agotado.getByRole('button', { name: /Agregar/ }).isDisabled(), true); await capturar('10-agotado');
    // Cierre real del frontend con backend simulado: mantiene la secuencia y jamás usa datos del propietario.
    await page.getByRole('button', { name: 'Finalizar venta', exact: true }).click(); await page.getByRole('dialog').waitFor(); await capturar('11-cierre');
    await page.getByRole('button', { name: 'Confirmar y finalizar', exact: true }).focus(); await page.keyboard.press('Tab');
    assert.ok(await page.getByRole('dialog').locator('button').first().evaluate(e => e === document.activeElement));
    await page.getByRole('button', { name: 'Confirmar y finalizar', exact: true }).click(); await page.getByRole('heading', { name: 'Venta finalizada' }).waitFor();
    await capturar('12-finalizada');
    assert.deepEqual(escrituras.map(x => x.path), ['/api/ventas', '/api/ventas/2/confirmar', '/api/ventas/2/completar']);
    assert.equal(escrituras[0].body.esCredito, true); assert.equal(escrituras[2].body.cobro, undefined);
    await page.getByRole('button', { name: 'Nueva venta', exact: true }).click(); await page.getByText('Su carrito está vacío', { exact: true }).waitFor();
    assert.deepEqual(errores, []); assert.deepEqual(inesperadas, []);
    await context.close();
  }
} finally { await navegador.close(); }
await writeFile(join(salida, 'resultado.json'), JSON.stringify({ entorno: 'API simulada, sin datos reales', errores: 0, verificaciones: informe }, null, 2));
console.log(JSON.stringify({ verificaciones: informe.length, errores: 0, salida }));
