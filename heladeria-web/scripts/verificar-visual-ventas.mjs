// Smoke visual aislado. Todas las solicitudes /api se interceptan; nunca llama a la BD.
// node scripts/verificar-visual-ventas.mjs http://127.0.0.1:5186 <directorio-node_modules-con-playwright> [msedge|chrome]
import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { mkdir, readFile, writeFile } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const raiz = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const requireLocal = createRequire(join(raiz, "package.json"));
const requireBrowser = process.argv[3] ? createRequire(join(resolve(process.argv[3]), "../package.json")) : requireLocal;
const { chromium } = requireBrowser("playwright");
const ts = requireLocal("typescript");
const fixture = ts.transpileModule(await readFile(join(raiz, "src/test/datosFaseVisual5.ts"), "utf8"), { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
const datos = await import(`data:text/javascript;base64,${Buffer.from(fixture).toString("base64")}`);
const fixtureCaja = ts.transpileModule(await readFile(join(raiz, "src/test/datosFaseVisual6.ts"), "utf8"), { compilerOptions: { module: ts.ModuleKind.ESNext } }).outputText;
const datosCaja = await import(`data:text/javascript;base64,${Buffer.from(fixtureCaja).toString("base64")}`);
const base = process.argv[2] ?? "http://127.0.0.1:5186";
assert.match(base, /^http:\/\/(127\.0\.0\.1|localhost):\d+$/);
const salida = join(raiz, "test-results/visual-ventas");
await mkdir(salida, { recursive: true });
const permisos = ["VENTAS.CONSULTAR", "VENTAS.ADMINISTRAR", "VENTAS.VENDER", "VENTAS.ANULAR", "POS.OPERAR", "CAJA.CONSULTAR", "CAJA.OPERAR", "CAJA.COBRAR"];
const sucursal = { id: 4, nombre: "Sucursal principal", empresaId: 4, empresa: "Heladería", esPrincipal: true, zonaHoraria: "America/Guatemala" };
const clienteCredito = { id: 27, codigo: "CLI-27", nombre: "Cliente de crédito" };
const usuario = { id: 1, nombreUsuario: "operador-demo", correo: "demo@invalid.local", rolId: "DEMO", rol: "Operador", sucursalActiva: sucursal, sucursales: [sucursal] };
const navegador = await chromium.launch({ headless: true, channel: process.argv[4] || undefined });
const informe = [];
try {
  for (const ancho of [320, 390, 768, 1366, 1600]) {
    const contexto = await navegador.newContext({ viewport: { width: ancho, height: 900 }, timezoneId: "Asia/Tokyo", serviceWorkers: "block" });
    const pagina = await contexto.newPage();
    let venta = structuredClone(datos.ventaBorrador);
    const guardados = [];
    const errores = [];
    const peticionesInesperadas = [];
    pagina.on("pageerror", (error) => errores.push(error.message));
    pagina.on("console", (msg) => { if (msg.type() === "error") errores.push(msg.text()); });
    await contexto.route("**/*", async (ruta) => {
      const solicitud = ruta.request();
      const url = new URL(solicitud.url());
      const responder = (objeto, status = 200) => ruta.fulfill({ status, contentType: "application/json", body: JSON.stringify(objeto) });
      if (!url.pathname.startsWith("/api/")) {
        if (url.origin === base) return ruta.continue();
        peticionesInesperadas.push(solicitud.url()); return ruta.abort();
      }
      if (url.pathname === "/api/autenticacion/renovar") return responder({ exito: true, accessToken: "token-simulado-solo-en-memoria", expiraAccessTokenUtc: "2099-01-01T00:00:00Z", usuario, requiereCambioContrasena: false });
      if (url.pathname === "/api/autenticacion/sesion") return responder({ usuario, permisos, requiereCambioContrasena: false });
      if (url.pathname === "/api/ventas/referencias") return responder({ ...datos.referenciasVentas, clientes: [clienteCredito] });
      if (url.pathname === "/api/caja/referencias") return responder(datosCaja.referenciasCaja);
      if (url.pathname === "/api/caja/sesiones") return responder(datosCaja.paginaCaja([datosCaja.sesionCaja.encabezado]));
      if (url.pathname === "/api/ventas/estados") return responder(datos.referenciasVentas.estados);
      if (url.pathname === "/api/ventas/productos") return responder(datos.paginaVentas([datos.productoPos, { ...datos.productoPos, id: 3, sku: "HEL-VAI-003", nombre: "Helado Vainilla", disponibleBase: null }]));
      if (url.pathname === "/api/ventas/productos/1/presentaciones") return responder(datos.productoPos);
      if (url.pathname === "/api/ventas" && solicitud.method() === "GET") return responder(datos.paginaVentas([venta.encabezado]));
      if ((url.pathname === "/api/ventas" && solicitud.method() === "POST") || (url.pathname === "/api/ventas/2" && solicitud.method() === "PUT")) {
        const payload = solicitud.postDataJSON(); guardados.push(payload);
        venta = { ...venta, encabezado: { ...venta.encabezado, esCredito: payload.esCredito, fechaVencimientoCredito: payload.fechaVencimientoCredito, clienteId: payload.clienteId, cliente: payload.clienteId ? clienteCredito.nombre : null } };
        return responder(venta, solicitud.method() === "POST" ? 201 : 200);
      }
      if (url.pathname === "/api/ventas/2") return responder(venta);
      if (url.pathname === "/api/ventas/2/confirmar") { venta = structuredClone(datos.ventaConfirmada); return responder(venta); }
      if (url.pathname === "/api/ventas/2/completar") {
        const body = solicitud.postDataJSON(); assert.ok(body.cobro); assert.equal(body.cobro.pagos.length, 1);
        venta = { ...structuredClone(datos.ventaCompletada), encabezado: { ...datos.ventaCompletada.encabezado, claveOperacion: body.claveOperacion },
          cobro: { pagoId: 99, sesionCajaId: body.cobro.sesionCajaId, claveOperacion: body.claveOperacion, pagos: body.cobro.pagos } };
        return responder(venta);
      }
      peticionesInesperadas.push(`${solicitud.method()} ${url.pathname}`);
      return responder({ mensaje: "Petición no prevista en smoke aislado" }, 500);
    });
    const sinOverflow = async (pantalla) => {
      await pagina.evaluate(() => scrollTo(0, 0));
      const medidas = await pagina.evaluate(() => ({ viewport: innerWidth, pagina: document.documentElement.scrollWidth }));
      if (await pagina.getByRole("dialog").count()) {
        const modal = await pagina.getByRole("dialog").evaluate((e) => ({ ancho: e.clientWidth, contenido: e.scrollWidth, foco: e.contains(document.activeElement) }));
        assert.ok(modal.contenido <= modal.ancho, `${pantalla}: overflow modal a ${ancho}px`);
        assert.ok(modal.foco, `${pantalla}: foco fuera de modal`);
      }
      await pagina.screenshot({ path: join(salida, `${pantalla}-${ancho}.png`), fullPage: true });
      if (medidas.pagina > medidas.viewport) console.log(await pagina.evaluate(() => Array.from(document.querySelectorAll("main *")).filter((e) => e.getBoundingClientRect().right > innerWidth && getComputedStyle(e).position !== "absolute").slice(0, 12).map((e) => ({ elemento: e.tagName, clase: e.className, ancho: e.getBoundingClientRect().width }))));
      assert.ok(medidas.pagina <= medidas.viewport, `${pantalla} ${ancho}px: overflow ${JSON.stringify(medidas)}`);
      informe.push({ pantalla, ancho, ...medidas });
    };
    await pagina.goto(`${base}/pos`);
    await pagina.getByRole("article", { name: "Helado Chocolate" }).waitFor();
    await pagina.getByLabel("Presentación · Helado Chocolate").selectOption("2");
    await pagina.getByRole("button", { name: "Agregar Helado Chocolate Vaso" }).click();
    await pagina.getByRole("button", { name: "Agregar Helado Chocolate Vaso" }).click();
    await pagina.getByText("2 vaso = 500 ml").waitFor();
    await sinOverflow("pos");
    if (ancho < 1280) {
      await pagina.getByRole("link", { name: /Ver carrito/ }).click();
      await pagina.getByRole("button", { name: "Guardar borrador" }).scrollIntoViewIfNeeded();
    }
    await pagina.getByRole("button", { name: "Guardar borrador" }).click();
    await pagina.getByRole("table", { name: "Detalle de venta" }).waitFor();
    assert.equal(guardados.at(-1).esCredito, false);
    assert.equal(guardados.at(-1).fechaVencimientoCredito, null);
    await sinOverflow("detalle");
    await pagina.getByRole("button", { name: "Confirmar venta", exact: true }).click();
    await pagina.getByRole("dialog").waitFor();
    const dialogo = await pagina.getByRole("dialog").evaluate((e) => ({ ancho: e.clientWidth, contenido: e.scrollWidth }));
    assert.ok(dialogo.contenido <= dialogo.ancho, `Modal con overflow a ${ancho}px`);
    await sinOverflow("confirmacion");
    await pagina.getByRole("button", { name: "Cancelar", exact: true }).click();
    await pagina.getByRole("link", { name: "Volver a ventas" }).click();
    await pagina.getByRole("table", { name: "Listado de ventas" }).waitFor();
    await sinOverflow("listado");
    venta = structuredClone(datos.ventaConfirmada);
    await pagina.goto(`${base}/ventas/2`);
    await pagina.getByRole("button", { name: "Completar venta", exact: true }).focus(); await pagina.keyboard.press("Enter");
    await pagina.getByLabel("Forma de pago 1", { exact: true }).waitFor();
    await pagina.getByRole("button", { name: "Cerrar venta", exact: true }).focus(); await pagina.keyboard.press("Tab");
    assert.ok(await pagina.getByRole("dialog").locator("button").first().evaluate((e) => e === document.activeElement), "Tab debe permanecer dentro del diálogo");
    await pagina.keyboard.press("Shift+Tab");
    assert.ok(await pagina.getByRole("button", { name: "Cerrar venta", exact: true }).evaluate((e) => e === document.activeElement), "Shift+Tab debe regresar a la última acción");
    await pagina.getByLabel("Forma de pago 1", { exact: true }).selectOption("21");
    await pagina.getByLabel("Monto recibido 1", { exact: true }).fill(String(venta.encabezado.total + 20));
    await sinOverflow("cobro-integrado-efectivo");
    await pagina.getByLabel("Forma de pago 1", { exact: true }).selectOption("34");
    await pagina.getByLabel(/Referencia\/autorización/).fill("AUT-SMOKE");
    await sinOverflow("cobro-integrado-tarjeta");
    await pagina.getByRole("button", { name: "Cerrar venta", exact: true }).click();
    await pagina.getByText("COMPLETADA", { exact: true }).waitFor();
    assert.equal(await pagina.getByRole("button", { name: "Anular venta", exact: true }).count(), 0);
    await sinOverflow("completada-cobrada");
    venta = structuredClone(datos.ventaBorrador);
    // Flujo de crédito sobre los mismos componentes y API: no afecta la regresión de contado anterior.
    await pagina.goto(`${base}/ventas/nueva`);
    await pagina.getByRole("article", { name: "Helado Chocolate" }).waitFor();
    await pagina.getByLabel("Presentación · Helado Chocolate").selectOption("1");
    await pagina.getByRole("button", { name: "Agregar Helado Chocolate Litro" }).click();
    await pagina.getByRole("button", { name: "Crédito", exact: true }).click();
    await pagina.getByRole("button", { name: "Guardar borrador" }).click();
    await pagina.getByText("Seleccione un cliente para la venta a crédito.").waitFor();
    assert.equal(guardados.length, 1, "Crédito sin cliente no envía POST");
    await pagina.getByRole("button", { name: /^Cliente \*/ }).click();
    await pagina.getByRole("option", { name: /Cliente de crédito/ }).click();
    await pagina.getByRole("button", { name: "Guardar borrador" }).click();
    await pagina.getByText("Indique la fecha de vencimiento del crédito.").waitFor();
    assert.equal(guardados.length, 1, "Crédito sin fecha no envía POST");
    await pagina.getByLabel(/Fecha de vencimiento/).fill("2099-10-31");
    await sinOverflow("pos-credito");
    await pagina.getByRole("button", { name: "Guardar borrador" }).click();
    await pagina.getByRole("table", { name: "Detalle de venta" }).waitFor();
    assert.equal(guardados.at(-1).esCredito, true);
    assert.equal(guardados.at(-1).fechaVencimientoCredito, "2099-10-31");
    assert.equal(guardados.at(-1).clienteId, 27);
    await pagina.getByText("Crédito", { exact: true }).waitFor();
    await sinOverflow("detalle-credito");
    await pagina.getByRole("button", { name: "Editar borrador" }).click();
    await pagina.getByLabel("Cantidad Helado Chocolate Litro").waitFor();
    assert.equal(await pagina.getByRole("button", { name: "Crédito", exact: true }).getAttribute("aria-pressed"), "true");
    assert.equal(await pagina.getByLabel(/Fecha de vencimiento/).inputValue(), "2099-10-31");
    await pagina.getByRole("button", { name: "Contado", exact: true }).click();
    assert.equal(await pagina.getByLabel(/Fecha de vencimiento/).count(), 0);
    await pagina.getByRole("button", { name: "Guardar cambios del borrador" }).click();
    await pagina.getByRole("table", { name: "Detalle de venta" }).waitFor();
    assert.equal(guardados.at(-1).esCredito, false);
    assert.equal(guardados.at(-1).fechaVencimientoCredito, null);
    assert.deepEqual(errores, [], `Errores de consola a ${ancho}px`);
    assert.deepEqual(peticionesInesperadas, [], "Todas las llamadas API deben estar simuladas");
    await contexto.close();
  }
} finally { await navegador.close(); }
await writeFile(join(salida, "resultado.json"), JSON.stringify({ entorno: "API completamente simulada", errores: 0, verificaciones: informe }, null, 2));
console.log(JSON.stringify({ verificaciones: informe.length, anchos: [320, 390, 768, 1366, 1600], errores: 0, resultado: salida }));
