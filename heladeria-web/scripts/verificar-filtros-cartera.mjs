// Prueba de navegador aislada: intercepta TODAS las API, nunca usa la BD real.
// node scripts/verificar-filtros-cartera.mjs http://localhost:5173 <node_modules-con-playwright> [msedge|chrome]
import assert from 'node:assert/strict';
import { createRequire } from 'node:module';
import { resolve, join } from 'node:path';

const require = createRequire(join(resolve(process.argv[3]), '../package.json'));
const { chromium } = require('playwright');
const base = process.argv[2];
assert.match(base, /^http:\/\/(localhost|127\.0\.0\.1):\d+$/);
const sucursal = { id: 4, nombre: 'Sucursal simulada', empresaId: 4, empresa: 'Prueba', esPrincipal: true, zonaHoraria: 'America/Guatemala' };
const usuario = { id: 1, nombreUsuario: 'demo', correo: 'demo@invalid.local', rolId: 'ADMIN', rol: 'Administrador', sucursalActiva: sucursal, sucursales: [sucursal] };
const permisos = ['CXC.CONSULTAR', 'CXP.CONSULTAR', 'VENTAS.CONSULTAR', 'PROVEEDORES.CONSULTAR'];
const navegador = await chromium.launch({ headless: true, channel: process.argv[4] || undefined });
const solicitudes = [], errores = [];
let verificaciones = 0;
try {
  const contexto = await navegador.newContext({ serviceWorkers: 'block' });
  await contexto.route('**/*', async (ruta) => {
    const url = new URL(ruta.request().url());
    if (!url.pathname.startsWith('/api/')) {
      if (url.origin === base) return ruta.continue();
      errores.push(`Red externa bloqueada: ${url.pathname}`); return ruta.abort();
    }
    solicitudes.push(url);
    const responder = (datos) => ruta.fulfill({ contentType: 'application/json', body: JSON.stringify(datos) });
    if (url.pathname === '/api/autenticacion/renovar') return responder({ exito: true, accessToken: 'simulado', expiraAccessTokenUtc: '2099-01-01T00:00:00Z', usuario, requiereCambioContrasena: false });
    if (url.pathname === '/api/autenticacion/sesion') return responder({ usuario, permisos, requiereCambioContrasena: false });
    if (!/^\/api\/(cuentas-por-(cobrar|pagar)(\/resumen)?|clientes|compras\/proveedores)$/.test(url.pathname) || ruta.request().method() !== 'GET') {
      errores.push(`API no prevista: ${ruta.request().method()} ${url.pathname}`); return ruta.abort();
    }
    if (url.pathname.endsWith('/resumen')) return responder([]);
    const elementos = url.pathname === '/api/clientes' ? [{ id: 27, nombreCliente: 'Cliente simulado' }]
      : url.pathname === '/api/compras/proveedores' ? [{ id: 37, nombreProveedor: 'Proveedor simulado' }] : [];
    return responder({ elementos, pagina: Number(url.searchParams.get('pagina') || 1), tamanioPagina: Number(url.searchParams.get('tamanioPagina') || 10), totalRegistros: 30, totalPaginas: 3 });
  });
  const pagina = await contexto.newPage();
  pagina.on('pageerror', (error) => errores.push(error.message));
  for (const tipo of ['cobrar', 'pagar']) {
    const ruta = `/api/cuentas-por-${tipo}`;
    const baseParams = { sucursalId: '4', pagina: '1', tamanioPagina: '10' };
    const comprobar = (path, esperados) => {
      const ultima = solicitudes.filter((u) => u.pathname === path).at(-1);
      assert.deepEqual(Object.fromEntries(ultima.searchParams), esperados, path);
      verificaciones++;
    };
    const esperarPeticion = (path, criterio) => pagina.waitForResponse((r) => {
      const u = new URL(r.url()); return u.pathname === path && criterio(u.searchParams);
    }, { timeout: 5000 });
    const aplicar = async (filtros) => {
      const coincidencia = (p) => Object.entries(filtros).every(([k, v]) => p.get(k) === v);
      const respuestas = [esperarPeticion(ruta, coincidencia), esperarPeticion(`${ruta}/resumen`, coincidencia)];
      await pagina.getByRole('button', { name: 'Buscar cuentas', exact: true }).click();
      await Promise.all(respuestas);
      comprobar(ruta, { ...baseParams, ...filtros });
      comprobar(`${ruta}/resumen`, { sucursalId: '4', ...filtros });
    };
    await pagina.goto(`${base}/cuentas-por-${tipo}`);
    await pagina.getByText('No se encontraron cuentas').waitFor();
    comprobar(ruta, baseParams);
    comprobar(`${ruta}/resumen`, { sucursalId: '4' });
    // Teclado nativo, además de selectOption: reproduce la selección sin sintetizar onChange de React.
    const estado = pagina.getByRole('combobox', { name: /^Estado/ });
    await estado.focus(); await estado.press('End'); await estado.press('ArrowUp');
    assert.equal(await estado.inputValue(), 'PAGADA');
    await aplicar({ estado: 'PAGADA' });
    await pagina.getByRole('combobox', { name: /^Vencimiento/ }).selectOption('VIGENTE');
    await pagina.getByLabel('Búsqueda', { exact: true }).fill('  Café & factura #2  ');
    await pagina.getByLabel('Fecha desde', { exact: true }).fill('2026-10-01');
    await pagina.getByLabel('Fecha hasta', { exact: true }).fill('2026-10-14');
    await pagina.getByRole('button', { name: tipo === 'cobrar' ? /^Cliente Todos/ : /^Proveedor Todos/ }).click();
    await pagina.getByRole('option', { name: tipo === 'cobrar' ? 'Cliente simulado' : 'Proveedor simulado' }).click();
    const filtros = { estado: 'PAGADA', vencimiento: 'VIGENTE', busqueda: 'Café & factura #2', fechaDesde: '2026-10-01', fechaHasta: '2026-10-14', terceroId: tipo === 'cobrar' ? '27' : '37' };
    await aplicar(filtros);
    const resumenes = solicitudes.filter((u) => u.pathname === `${ruta}/resumen`).length;
    await estado.selectOption('PARCIAL');
    let respuesta = esperarPeticion(ruta, (p) => p.get('pagina') === '2');
    await pagina.getByRole('button', { name: 'Página siguiente' }).click(); await respuesta;
    await pagina.getByLabel('Página 2', { exact: true }).waitFor();
    comprobar(ruta, { ...baseParams, ...filtros, pagina: '2' });
    respuesta = esperarPeticion(ruta, (p) => p.get('tamanioPagina') === '20');
    await pagina.getByLabel('Registros por página', { exact: true }).selectOption('20'); await respuesta;
    await pagina.getByLabel('Página 1', { exact: true }).waitFor();
    comprobar(ruta, { ...baseParams, ...filtros, tamanioPagina: '20' });
    assert.equal(solicitudes.filter((u) => u.pathname === `${ruta}/resumen`).length, resumenes);
    const limpias = [esperarPeticion(ruta, (p) => !p.has('estado')), esperarPeticion(`${ruta}/resumen`, (p) => !p.has('estado'))];
    await pagina.getByRole('button', { name: 'Limpiar filtros' }).click(); await Promise.all(limpias);
    comprobar(ruta, { ...baseParams, tamanioPagina: '20' });
    comprobar(`${ruta}/resumen`, { sucursalId: '4' });
  }
  assert.deepEqual(errores, []);
  console.log(JSON.stringify({ verificaciones, errores, entorno: 'Chromium, todas las API simuladas; sin acceso a BD real' }));
} finally { await navegador.close(); }
