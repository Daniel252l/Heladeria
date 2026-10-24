// Smoke aislado: intercepta TODAS las API (incluida autenticación). No usa datos ni sesiones reales.
// node scripts/verificar-visual-reportes.mjs http://127.0.0.1:5190 <node_modules-con-playwright> [msedge|chrome]
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const local = createRequire(join(raiz, 'package.json'));
const externo = process.argv[3] ? createRequire(join(resolve(process.argv[3]), '../package.json')) : local;
const { chromium } = externo('playwright');
const ts = local('typescript');
const js = ts.transpileModule(await readFile(join(raiz, 'src/test/datosFaseVisual8.ts'), 'utf8'), { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
const { datosReportes } = await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
const base = process.argv[2] ?? 'http://127.0.0.1:5190';
assert.match(base, /^http:\/\/(127\.0\.0\.1|localhost):\d+$/);
const salida = join(raiz, 'test-results/visual-reportes');
await mkdir(salida, { recursive: true });
const sucursal = { id: 4, nombre: 'Sucursal principal', empresaId: 4, empresa: 'Heladería', esPrincipal: true, zonaHoraria: 'America/Guatemala' };
const usuario = { id: 1, nombreUsuario: 'admin-demo', correo: 'demo@invalid.local', rolId: 'ADMIN', rol: 'Administrador', sucursalActiva: sucursal, sucursales: [sucursal] };
const permisos = ['REPORTES.CONSULTAR', 'AUDITORIA.CONSULTAR', 'VENTAS.CONSULTAR', 'PRODUCTOS.CONSULTAR', 'PROVEEDORES.CONSULTAR', 'CAJA.CONSULTAR'];
const tablas = { ventas: 'Ventas', 'productos-vendidos': 'Productos vendidos', inventario: 'Inventario actual', kardex: 'Kardex', compras: 'Compras', caja: 'Sesiones de caja', 'cuentas-por-cobrar': 'Cuentas por cobrar', 'cuentas-por-pagar': 'Cuentas por pagar', auditoria: 'Registros de auditoría' };
const rutaVista = (tipo) => tipo === 'auditoria' ? '/auditoria' : `/reportes/${tipo}`;
const rutaApi = (tipo) => `/api${rutaVista(tipo)}`;
const navegador = await chromium.launch({ headless: true, channel: process.argv[4] || undefined });
const verificaciones = [];
const verificacionesSidebar = [];
try {
  for (const ancho of [320, 390, 768, 1366]) {
    const contexto = await navegador.newContext({ viewport: { width: ancho, height: 900 }, timezoneId: 'Asia/Tokyo', serviceWorkers: 'block' });
    const pagina = await contexto.newPage();
    const errores = [], inesperadas = [], consultas = [];
    let consulta = false;
    pagina.on('pageerror', (e) => errores.push(e.message));
    pagina.on('console', (m) => { if (m.type() === 'error') errores.push(m.text()); });
    await contexto.route('**/*', async (route) => {
      const req = route.request(), url = new URL(req.url()), path = url.pathname;
      const responder = (datos, status = 200) => route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(datos) });
      if (!path.startsWith('/api/')) {
        if (url.origin === base) return route.continue();
        inesperadas.push(req.url()); return route.abort();
      }
      if (path === '/api/autenticacion/renovar') return responder({ exito: true, accessToken: 'token-simulado-en-memoria', expiraAccessTokenUtc: '2099-01-01T00:00:00Z', usuario, requiereCambioContrasena: false });
      if (path === '/api/autenticacion/sesion') return responder({ usuario: consulta ? { ...usuario, rolId: 'CONSULTA', rol: 'Consulta' } : usuario, permisos: consulta ? [] : permisos, requiereCambioContrasena: false });
      if (req.method() !== 'GET') { inesperadas.push(`${req.method()} ${path}`); return responder({}, 500); }
      const catalogos = { '/api/productos': { id: 1, codigo: 'HEL-CHO', nombre: 'Helado Chocolate' },
        '/api/clientes': { id: 7, nombreCliente: 'Cafetería del Parque' }, '/api/compras/proveedores': { id: 8, nombreProveedor: 'Lácteos del Valle' }, '/api/caja/cajas': { id: 9, nombre: 'Caja principal' } };
      if (catalogos[path]) return responder({ elementos: [catalogos[path]], totalPaginas: 1, totalRegistros: 1, pagina: 1, tamanioPagina: 30 });
      const tipo = Object.keys(datosReportes).find((t) => rutaApi(t) === path);
      if (tipo) {
        consultas.push(url);
        const datos = structuredClone(datosReportes[tipo]);
        const paginado = tipo === 'ventas' ? datos.ventas.datos : datos.datos ?? datos;
        paginado.pagina = Number(url.searchParams.get('pagina') ?? 1);
        paginado.tamanioPagina = Number(url.searchParams.get('tamanioPagina') ?? 20);
        paginado.totalRegistros = 40; paginado.totalPaginas = 2;
        if (tipo === 'auditoria') Object.assign(datos.elementos[0], { valorAnterior: 'SECRETO_NO_RENDERIZAR', comentario: 'SECRETO_NO_RENDERIZAR' });
        return responder(datos);
      }
      inesperadas.push(req.url()); return responder({}, 500);
    });
    for (const tipo of Object.keys(datosReportes)) {
      await pagina.goto(`${base}${rutaVista(tipo)}`);
      await pagina.getByRole('table', { name: tablas[tipo], exact: true }).waitFor();
      const navInicial = pagina.getByRole('navigation', { name: 'Navegación principal' });
      const gruposIniciales = await navInicial.getByRole('button').evaluateAll((botones) => botones.map((b) => b.getAttribute('aria-expanded')));
      assert.ok(gruposIniciales.length > 0);
      assert.ok(gruposIniciales.every((estado) => estado === 'false'), `${tipo}: grupos cerrados incluso con ruta activa`);
      assert.deepEqual(await navInicial.getByRole('link').evaluateAll((enlaces) => enlaces.map((a) => a.getAttribute('href'))), ['/dashboard']);
      assert.equal(consultas.filter((u) => u.pathname === rutaApi(tipo)).length, 1, `${tipo}: carga inicial duplicada`);
      const anchoPagina = await pagina.evaluate(() => document.documentElement.scrollWidth);
      assert.ok(anchoPagina <= ancho, `${tipo} ${ancho}px: overflow de página ${anchoPagina}`);
      const estructura = await pagina.getByRole('table', { name: tablas[tipo], exact: true }).evaluate((tabla) => ({
        display: getComputedStyle(tabla).display, encabezado: getComputedStyle(tabla.tHead).display,
        scroll: tabla.parentElement.scrollWidth > tabla.parentElement.clientWidth,
      }));
      assert.equal(estructura.display, 'table');
      assert.equal(estructura.encabezado, 'table-header-group');
      if (ancho <= 768) assert.ok(estructura.scroll, 'Tabla debe tener scroll horizontal interno');
      const region = pagina.getByRole('region', { name: `Desplazar tabla: ${tablas[tipo]}`, exact: true });
      if (ancho <= 768) {
        await region.focus();
        await region.press('ArrowRight');
        await pagina.waitForFunction((etiqueta) => Array.from(document.querySelectorAll('[role="region"]')).find((e) => e.getAttribute('aria-label') === etiqueta)?.scrollLeft > 0, `Desplazar tabla: ${tablas[tipo]}`);
        // Comprobar el desplazamiento por teclado del contenedor, no del documento.
        assert.ok(await region.evaluate((e) => e.scrollLeft > 0));
        await region.evaluate((e) => { e.scrollLeft = 0; });
      }
      await pagina.evaluate(() => scrollTo(0, 0));
      await pagina.screenshot({ path: join(salida, `${tipo}-${ancho}.png`), fullPage: true });
      assert.ok(!(await pagina.locator('body').innerText()).includes('SECRETO_NO_RENDERIZAR'));
      if (tipo === 'ventas') {
        const antes = consultas.length;
        await pagina.getByLabel('Estado', { exact: true }).selectOption('CONFIRMADA');
        await pagina.getByLabel('Fecha desde', { exact: true }).fill('2026-10-01');
        await pagina.getByLabel('Fecha hasta', { exact: true }).fill('2026-10-19');
        await pagina.getByLabel('Búsqueda', { exact: true }).fill('chocolate');
        assert.equal(consultas.length, antes);
        await pagina.getByRole('button', { name: 'Aplicar filtros', exact: true }).click();
        await pagina.getByRole('table', { name: 'Ventas', exact: true }).waitFor();
        assert.equal(consultas.at(-1).searchParams.get('estado'), 'CONFIRMADA');
        assert.equal(consultas.at(-1).searchParams.get('fechaDesde'), '2026-10-01');
        assert.equal(consultas.at(-1).searchParams.get('busqueda'), 'chocolate');
        await pagina.getByRole('button', { name: 'Página siguiente' }).click();
        await pagina.getByRole('table', { name: 'Ventas', exact: true }).waitFor();
        assert.equal(consultas.at(-1).searchParams.get('pagina'), '2');
        assert.equal(consultas.at(-1).searchParams.get('estado'), 'CONFIRMADA');
        await pagina.getByRole('button', { name: 'Limpiar', exact: true }).click();
        await pagina.getByRole('table', { name: 'Ventas', exact: true }).waitFor();
        assert.equal(consultas.at(-1).searchParams.has('estado'), false);
        const fechaEsperada = await pagina.evaluate(() => new Intl.DateTimeFormat('es-GT', { timeZone: 'America/Guatemala', dateStyle: 'medium', timeStyle: 'short' }).format(new Date('2026-10-19T02:31:00Z')));
        assert.ok((await pagina.getByRole('table', { name: 'Ventas', exact: true }).innerText()).includes(fechaEsperada), 'Usar Guatemala, no Tokyo');
      }
      verificaciones.push({ tipo, ancho, anchoPagina, ...estructura });
    }
    // Abrir explícitamente el grupo, igual que el usuario; no alterar estado React/DOM.
    const navegacion = pagina.getByRole('navigation', { name: 'Navegación principal' });
    const grupoReportes = navegacion.getByRole('button', { name: 'Reportes', exact: true });
    const esperarTransicionesSidebar = () => pagina.getByTestId('menu-lateral').evaluate(async (e) => {
      e.getBoundingClientRect(); // Resolver estilos antes de esperar las transiciones reales.
      await Promise.all(e.getAnimations({ subtree: true }).map((animacion) => animacion.finished.catch(() => {})));
    });
    if (ancho >= 1024) {
      await grupoReportes.focus();
      await grupoReportes.press('Enter');
      assert.equal(await grupoReportes.getAttribute('aria-expanded'), 'true', 'También funciona en modo compacto');
      await grupoReportes.press('Space');
      assert.equal(await grupoReportes.getAttribute('aria-expanded'), 'false');
    }
    if (ancho < 1024) await pagina.getByRole('button', { name: 'Abrir navegación' }).click();
    else await pagina.getByRole('button', { name: 'Expandir menú lateral' }).click();
    assert.ok((await navegacion.getByRole('button').evaluateAll((botones) => botones.map((b) => b.getAttribute('aria-expanded')))).every((estado) => estado === 'false'));
    await esperarTransicionesSidebar();
    await pagina.screenshot({ path: join(salida, `sidebar-cerrado-${ancho}.png`), fullPage: true });
    await grupoReportes.focus();
    await grupoReportes.press('Enter');
    assert.equal(await grupoReportes.getAttribute('aria-expanded'), 'true');
    assert.ok(await grupoReportes.evaluate((b) => b === document.activeElement && b.matches(':focus-visible') && getComputedStyle(b).boxShadow !== 'none'), 'Foco de teclado visible');
    await grupoReportes.press('Space');
    assert.equal(await grupoReportes.getAttribute('aria-expanded'), 'false');
    assert.equal(await navegacion.getByRole('link', { name: 'Reporte de inventario', exact: true }).count(), 0);
    await grupoReportes.click();
    assert.equal(await grupoReportes.getAttribute('aria-expanded'), 'true');
    await grupoReportes.click();
    assert.equal(await grupoReportes.getAttribute('aria-expanded'), 'false');
    await grupoReportes.press('Enter');
    assert.ok((await navegacion.getByRole('button').evaluateAll((botones) => botones.filter((b) => b.getAttribute('aria-label') !== 'Reportes').map((b) => b.getAttribute('aria-expanded')))).every((estado) => estado === 'false'));
    await esperarTransicionesSidebar();
    const medidasSidebar = await pagina.getByTestId('menu-lateral').evaluate((e) => ({ ancho: e.getBoundingClientRect().width, anchoPagina: document.documentElement.scrollWidth, anchoNav: e.querySelector('nav').clientWidth, scrollNav: e.querySelector('nav').scrollWidth }));
    assert.ok(medidasSidebar.ancho <= ancho && medidasSidebar.anchoPagina <= ancho && medidasSidebar.scrollNav <= medidasSidebar.anchoNav, 'Sin overflow al abrir grupos');
    await pagina.screenshot({ path: join(salida, `sidebar-abierto-${ancho}.png`), fullPage: true });
    await pagina.getByRole('link', { name: 'Reporte de inventario', exact: true }).click();
    await pagina.getByRole('table', { name: 'Inventario actual' }).waitFor();
    if (ancho < 1024) assert.equal(await pagina.getByRole('button', { name: 'Cerrar menú', exact: true }).count(), 0);
    verificacionesSidebar.push({ anchoViewport: ancho, ...medidasSidebar, teclado: true, aperturaManual: true, navegacion: true });
    consulta = true;
    const antes = consultas.length;
    for (const tipo of ['ventas', 'auditoria']) {
      await pagina.goto(`${base}${rutaVista(tipo)}`);
      await pagina.getByText('Acceso no autorizado', { exact: true }).waitFor();
      assert.equal(await pagina.locator('a[href^="/reportes/"], a[href="/auditoria"]').count(), 0);
    }
    assert.equal(consultas.length, antes, 'CONSULTA no debe consultar reportes');
    assert.deepEqual(errores, []); assert.deepEqual(inesperadas, []);
    await contexto.close();
  }
} finally { await navegador.close(); }
await writeFile(join(salida, 'resultado.json'), JSON.stringify({ entorno: 'API simulada; navegador Tokyo; sucursal Guatemala', errores: 0, verificaciones, verificacionesSidebar }, null, 2));
console.log(JSON.stringify({ vistas: 9, capturas: verificaciones.length + verificacionesSidebar.length * 2, sidebar: verificacionesSidebar.length, anchos: [320, 390, 768, 1366], errores: 0, resultado: salida }));
