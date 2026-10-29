import { act, fireEvent, screen, waitFor, within } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { renderSeguridad } from "../../test/renderSeguridad";
import { paginaVentas, productoPos, referenciasVentas, ventaAnulada, ventaBorrador, ventaCompletada, ventaConfirmada } from "../../test/datosFaseVisual5";
import type { ProductoPos, VentaConsulta } from "../../types/ventas";
import { PaginaVentas } from "./PaginaVentas";
import { PaginaDetalleVenta } from "./PaginaDetalleVenta";
import { PaginaPos } from "./PaginaPos";
import { configurarZonaHoraria } from "../../utils/fechas";
import { referenciasCaja, sesionCaja, paginaCaja } from "../../test/datosFaseVisual6";

afterEach(() => configurarZonaHoraria());

vi.mock("../../api/clienteHttp", async (original) => ({ ...(await original<typeof import("../../api/clienteHttp")>()), solicitar: vi.fn() }));
const api = vi.mocked(solicitar);
const permisos = [PERMISOS.ventasConsultar, PERMISOS.ventasAdministrar, PERMISOS.ventasVender, PERMISOS.ventasAnular, PERMISOS.posOperar, PERMISOS.cajaConsultar, PERMISOS.cajaOperar, PERMISOS.cajaCobrar];
let actual: VentaConsulta;
let producto: ProductoPos;
let refs: typeof referenciasVentas;
const escrituras = () => api.mock.calls.filter(([, o]) => o?.method);
const cuerpos = () => escrituras().map(([, o]) => JSON.parse(String(o?.body)));
function navegar(ruta = "/ventas/2", autorizados = permisos) {
  return renderSeguridad(<Routes>
    <Route path="/ventas" element={<PaginaVentas />} />
    <Route path="/ventas/nueva" element={<PaginaPos nueva />} />
    <Route path="/ventas/:id/editar" element={<PaginaPos />} />
    <Route path="/ventas/:id" element={<PaginaDetalleVenta />} />
    <Route path="/pos" element={<PaginaPos />} />
  </Routes>, autorizados, ruta);
}
async function agregar(presentacion = "1") {
  const tarjeta = await screen.findByRole("article", { name: "Helado Chocolate" });
  fireEvent.change(within(tarjeta).getByLabelText("Presentación · Helado Chocolate"), { target: { value: presentacion } });
  fireEvent.click(within(tarjeta).getByRole("button", { name: /Agregar/ }));
}
async function abrirAccion(nombre: string, ejecutar: string) {
  fireEvent.click(await screen.findByRole("button", { name: nombre }));
  if (nombre === "Completar venta" && !actual.encabezado.esCredito)
    fireEvent.change(await screen.findByLabelText("Forma de pago 1"), { target: { value: String(referenciasCaja.formasPago[0].id) } });
  fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: ejecutar }));
}
beforeEach(() => {
  sessionStorage.clear(); api.mockReset(); actual = structuredClone(ventaBorrador); producto = structuredClone(productoPos); refs = structuredClone(referenciasVentas);
  api.mockImplementation(async (ruta, opciones) => {
    if (ruta === "/api/caja/referencias") return referenciasCaja as never;
    if (ruta.startsWith("/api/caja/sesiones?")) return paginaCaja([sesionCaja.encabezado]) as never;
    if (ruta === "/api/ventas/estados") return refs.estados as never;
    if (ruta === "/api/ventas/referencias") return refs as never;
    if (ruta.startsWith("/api/ventas/productos?")) return paginaVentas([producto]) as never;
    if (ruta.endsWith("/presentaciones")) return producto as never;
    if (ruta.startsWith("/api/ventas?")) return paginaVentas([ventaBorrador, ventaConfirmada, ventaCompletada, ventaAnulada].map((v, i) => ({ ...v.encabezado, id: i + 2, numeroVenta: `VT-${i + 2}` })), 1, 2) as never;
    if (ruta.endsWith("/confirmar")) { actual = structuredClone(ventaConfirmada); return actual as never; }
    if (ruta.endsWith("/completar")) {
      const body = JSON.parse(String(opciones?.body));
      actual = { ...structuredClone(ventaCompletada), encabezado: { ...ventaCompletada.encabezado, claveOperacion: body.claveOperacion },
        cobro: body.cobro ? { pagoId: 99, sesionCajaId: body.cobro.sesionCajaId, claveOperacion: body.claveOperacion, pagos: body.cobro.pagos } : null };
      return actual as never;
    }
    if (ruta.endsWith("/anular")) { actual = structuredClone(ventaAnulada); return actual as never; }
    if (opciones?.method) { actual = { ...actual, encabezado: { ...actual.encabezado, versionFila: "nueva-version" } }; return actual as never; }
    if (ruta === "/api/ventas/2") return actual as never;
    throw new Error(`Ruta no prevista en la prueba: ${ruta}`);
  });
});

describe("listado y detalle de ventas", () => {
  it("renderiza 20:42 UTC como 2:42 p. m. en Guatemala en el listado real", async () => {
    configurarZonaHoraria("America/Guatemala");
    const previo = api.getMockImplementation()!;
    api.mockImplementation(async (ruta, opciones) => ruta.startsWith("/api/ventas?")
      ? paginaVentas([{ ...ventaBorrador.encabezado, fechaVenta: "2026-10-08T20:42:15.376Z" }]) as never
      : previo(ruta, opciones));
    navegar("/ventas");
    const tabla = await screen.findByRole("table", { name: "Listado de ventas" });
    expect(within(tabla).getByText(/2:42/).textContent).not.toContain("8:42");
  });
  it("muestra estados, moneda, usuario, datos nulos y acciones solo de borrador", async () => {
    navegar("/ventas"); const tabla = await screen.findByRole("table", { name: "Listado de ventas" });
    for (const estado of ["BORRADOR", "CONFIRMADA", "COMPLETADA", "ANULADA"]) expect(within(tabla).getByText(estado)).toBeDefined();
    expect(within(tabla).getAllByRole("link", { name: "Editar" })).toHaveLength(1);
    expect(within(tabla).getAllByText("Sin cliente identificado")).toHaveLength(4);
    expect(within(tabla).getAllByText("admin")).toHaveLength(4);
    expect(tabla.querySelectorAll('td[data-etiqueta="Acciones"]')).toHaveLength(4);
    expect(screen.getByRole("link", { name: /Abrir POS/ }).getAttribute("href")).toBe("/pos");
  });
  it("filtra por número/cliente, estado real y fechas; limpia y pagina", async () => {
    navegar("/ventas"); await screen.findByRole("table");
    fireEvent.change(screen.getByLabelText("Número o cliente"), { target: { value: "VT-2" } });
    fireEvent.change(screen.getByLabelText("Estado"), { target: { value: "13" } });
    fireEvent.change(screen.getByLabelText("Fecha desde"), { target: { value: "2026-10-01" } });
    fireEvent.change(screen.getByLabelText("Fecha hasta"), { target: { value: "2026-10-07" } });
    fireEvent.click(screen.getByRole("button", { name: "Buscar ventas" }));
    await waitFor(() => expect(api.mock.calls.some(([r]) => r.includes("busqueda=VT-2&estadoId=13&fechaDesde=2026-10-01&fechaHasta=2026-10-07T23%3A59%3A59.999"))).toBe(true));
    fireEvent.click(await screen.findByRole("button", { name: "Página siguiente" }));
    await waitFor(() => expect(api.mock.calls.some(([r]) => r.includes("pagina=2"))).toBe(true));
    fireEvent.click(screen.getByRole("button", { name: "Limpiar filtros" }));
    expect((screen.getByLabelText("Número o cliente") as HTMLInputElement).value).toBe("");
  });
  it("impide rango de fechas invertido sin solicitar listado nuevo", async () => {
    navegar("/ventas"); await screen.findByRole("table"); const antes = api.mock.calls.length;
    fireEvent.change(screen.getByLabelText("Fecha desde"), { target: { value: "2026-10-07" } });
    fireEvent.change(screen.getByLabelText("Fecha hasta"), { target: { value: "2026-10-01" } });
    fireEvent.click(screen.getByRole("button", { name: "Buscar ventas" }));
    expect(screen.getByRole("alert").textContent).toContain("posterior"); expect(api).toHaveBeenCalledTimes(antes);
  });
  it("solo CONSULTAR obtiene filtros sin llamar referencias POS ni ofrecer creación", async () => {
    navegar("/ventas", [PERMISOS.ventasConsultar]); await screen.findByRole("table");
    expect(api.mock.calls.map(([r]) => r)).toContain("/api/ventas/estados");
    expect(api.mock.calls.some(([r]) => r === "/api/ventas/referencias")).toBe(false);
    expect(screen.queryByRole("link", { name: /Nueva venta|Abrir POS|Editar/ })).toBeNull();
  });
  it("detalle muestra multimedida, costos con seis decimales máximos y trazabilidad", async () => {
    actual = structuredClone(ventaCompletada); navegar();
    const tabla = await screen.findByRole("table", { name: "Detalle de venta" });
    expect(within(tabla).getByText("20 L")).toBeDefined(); expect(within(tabla).getByText("20,000 ml")).toBeDefined();
    expect(within(tabla).getByText("Base: GTQ 0.0245 / ml")).toBeDefined();
    expect(screen.getByText("VENTA-SALIDA")).toBeDefined(); expect(screen.getByText(ventaCompletada.encabezado.claveOperacion!)).toBeDefined();
  });
  it.each([ventaBorrador, ventaConfirmada, ventaCompletada, ventaAnulada])("solo CONSULTAR no ofrece operaciones en $encabezado.estado", async (v) => {
    actual = structuredClone(v); navegar("/ventas/2", [PERMISOS.ventasConsultar]); await screen.findByText(v.encabezado.estado);
    expect(screen.queryByRole("button", { name: /Confirmar venta|Completar venta|Anular venta/ })).toBeNull();
    expect(screen.queryByRole("link", { name: "Editar borrador" })).toBeNull();
  });
  it.each([[PERMISOS.ventasVender], [PERMISOS.posOperar]])("completar exige ambos permisos, no solo %s", async (extra) => {
    actual = structuredClone(ventaConfirmada); navegar("/ventas/2", [PERMISOS.ventasConsultar, extra]); await screen.findByText("CONFIRMADA");
    expect(screen.queryByRole("button", { name: "Completar venta" })).toBeNull();
  });
  it("CONFIRMADA solo ofrece completar y anular; ANULADA solo consulta", async () => {
    actual = structuredClone(ventaConfirmada); const vista = navegar(); await screen.findByText("CONFIRMADA");
    expect(screen.getByRole("button", { name: "Completar venta" })).toBeDefined(); expect(screen.queryByRole("button", { name: "Confirmar venta" })).toBeNull();
    expect(screen.queryByRole("link", { name: "Editar borrador" })).toBeNull(); vista.unmount(); actual = structuredClone(ventaAnulada); navegar();
    await screen.findByText("ANULADA"); expect(screen.queryByRole("button", { name: "Anular venta" })).toBeNull();
  });
  it.each([403, 404])("lectura HTTP %s presenta experiencia consistente", async (estado) => {
    api.mockRejectedValue(new ErrorApi("No disponible", estado)); navegar(); await screen.findByText(`Error ${estado}`); expect(escrituras()).toHaveLength(0);
  });
});

describe("catálogo y carrito POS", () => {
  it("carga catálogo operativo con almacenes reales sin pedir productos administrativos", async () => {
    navegar("/pos"); await screen.findByRole("article");
    expect(api.mock.calls.some(([r]) => r.includes("bodegaId=8&ubicacionId=16"))).toBe(true);
    expect(api.mock.calls.some(([r]) => r.startsWith("/api/productos"))).toBe(false);
    expect(screen.getByText("18,500 ml disponibles")).toBeDefined();
  });
  it("busca con debounce y filtra categoría sin solicitudes por cada tecla", async () => {
    navegar("/pos"); await screen.findByRole("article");
    fireEvent.change(screen.getByLabelText("Buscar productos"), { target: { value: "H" } });
    fireEvent.change(screen.getByLabelText("Buscar productos"), { target: { value: "Helado" } });
    expect(api.mock.calls.some(([r]) => r.includes("busqueda="))).toBe(false);
    await waitFor(() => expect(api.mock.calls.some(([r]) => r.includes("busqueda=Helado"))).toBe(true));
    expect(api.mock.calls.filter(([r]) => r.includes("busqueda=H&"))).toHaveLength(0);
    fireEvent.click(within(screen.getByRole("group", { name: "Categorías" })).getByRole("button", { name: refs.categorias.find((c) => c.id === 20)!.nombre }));
    await waitFor(() => expect(api.mock.calls.some(([r]) => r.includes("categoriaId=20"))).toBe(true));
  });
  it("disponibilidad null no se interpreta como cero", async () => {
    producto.disponibleBase = null; navegar("/pos"); await screen.findByText("Disponibilidad no calculada"); expect(screen.queryByText("0 ml disponibles")).toBeNull();
  });
  it("presentación de precio inválido informa y deshabilita agregar", async () => {
    producto.presentaciones[1].precioValido = false; producto.presentaciones[1].precio = null;
    navegar("/pos"); const tarjeta = await screen.findByRole("article"); fireEvent.change(within(tarjeta).getByLabelText("Presentación · Helado Chocolate"), { target: { value: "1" } });
    expect(within(tarjeta).getByText("Precio no configurado")).toBeDefined(); expect((within(tarjeta).getByRole("button", { name: /Agregar/ }) as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByText("Su carrito está vacío")).toBeDefined();
  });
  it("Vaso conserva factor 250 y dos vasos muestran 500 ml", async () => {
    navegar("/pos"); await agregar("2"); await agregar("2");
    const carrito = screen.getByRole("complementary", { name: "Carrito de venta" }); expect(within(carrito).getByText("2 vaso = 500 ml")).toBeDefined();
    expect(within(carrito).getAllByRole("listitem")).toHaveLength(1); expect(within(carrito).getByText("GTQ 8.00 / vaso")).toBeDefined();
  });
  it("separa Litro de Vaso y permite incrementar, disminuir, escribir y eliminar", async () => {
    navegar("/pos"); await agregar(); await agregar("2"); const carrito = screen.getByRole("complementary");
    expect(within(carrito).getAllByRole("listitem")).toHaveLength(2);
    fireEvent.click(screen.getByRole("button", { name: "Incrementar Helado Chocolate Litro" }));
    expect((screen.getByLabelText("Cantidad Helado Chocolate Litro") as HTMLInputElement).value).toBe("2");
    fireEvent.click(screen.getByRole("button", { name: "Disminuir Helado Chocolate Litro" }));
    fireEvent.change(screen.getByLabelText("Cantidad Helado Chocolate Litro"), { target: { value: "4" } }); expect(screen.getByText("4 L = 4,000 ml")).toBeDefined();
    fireEvent.click(screen.getByRole("button", { name: "Eliminar Helado Chocolate Vaso" })); expect(within(carrito).getAllByRole("listitem")).toHaveLength(1);
  });
  it("permite descuento simple solo cuando el producto lo habilita", async () => {
    navegar("/pos"); await agregar(); fireEvent.change(screen.getByLabelText("Descuento % · Helado Chocolate Litro"), { target: { value: "10" } });
    expect(within(screen.getByRole("region", { name: "Totales estimados" })).getByText("GTQ 22.50")).toBeDefined();
  });
  it("producto sin descuentos no expone control editable", async () => {
    producto.permiteDescuento = false; navegar("/pos"); await agregar(); expect(screen.queryByLabelText(/Descuento %/)).toBeNull();
    expect(screen.getByText("Este producto no permite descuentos.")).toBeDefined();
  });
  it("crea sin cliente, sin IDs fijos ni importes calculados, y usa totales del servidor", async () => {
    navegar("/ventas/nueva"); await agregar(); fireEvent.click(screen.getByRole("button", { name: "Guardar borrador" }));
    await screen.findByText("BORRADOR"); expect(cuerpos()[0]).toEqual({ sucursalId: 4, bodegaId: 8, ubicacionId: 16, clienteId: null, monedaId: 6, esCredito: false, fechaVencimientoCredito: null, detalles: [{ productoId: 1, productoPresentacionId: 1, cantidad: 1, porcentajeDescuento: 0 }] });
    expect(within(screen.getByRole("region", { name: "Totales de la venta" })).getAllByText("GTQ 500.00").length).toBeGreaterThan(0);
    expect(screen.queryByText("Vista previa · no definitiva")).toBeNull();
  });
  it("unidad base envía presentación null, no un ID inventado", async () => {
    navegar("/pos"); await agregar("base"); fireEvent.click(screen.getByRole("button", { name: "Guardar borrador" })); await screen.findByText("BORRADOR");
    expect(cuerpos()[0].detalles[0].productoPresentacionId).toBeNull();
  });
  it("sin ADMINISTRAR puede consultar catálogo, pero no guardar", async () => {
    navegar("/pos", [PERMISOS.posOperar]); await agregar(); expect(screen.queryByRole("button", { name: "Guardar borrador" })).toBeNull(); expect(escrituras()).toHaveLength(0);
  });
  it("no selecciona IDs de bodega arbitrarios y cambia ubicación con su padre", async () => {
    refs.almacenes.push({ bodegaId: 9, bodega: "Bodega auxiliar", ubicacionId: 17, ubicacion: "Frío auxiliar" }); navegar("/pos"); await screen.findByRole("article");
    expect((screen.getByLabelText("Bodega") as HTMLSelectElement).value).toBe("");
    fireEvent.change(screen.getByLabelText("Bodega"), { target: { value: "9" } });
    expect((screen.getByLabelText("Ubicación") as HTMLSelectElement).value).toBe("17");
    expect(screen.queryByRole("option", { name: "Congelador" })).toBeNull();
  });
  it("edita solo borrador usando versión vigente", async () => {
    navegar("/ventas/2/editar"); await screen.findByLabelText("Cantidad Helado Chocolate Litro");
    fireEvent.change(screen.getByLabelText("Cantidad Helado Chocolate Litro"), { target: { value: "2" } });
    fireEvent.click(screen.getByRole("button", { name: "Guardar cambios del borrador" })); await screen.findByText("BORRADOR");
    expect(escrituras()[0][0]).toBe("/api/ventas/2"); expect(escrituras()[0][1]?.method).toBe("PUT"); expect(cuerpos()[0]).toMatchObject({ versionFila: "AQ==", detalles: [{ cantidad: 2 }] });
    await abrirAccion("Confirmar venta", "Confirmar"); await screen.findByText("CONFIRMADA"); expect(cuerpos()[1].versionFila).toBe("nueva-version");
  });
  it.each([ventaConfirmada, ventaCompletada, ventaAnulada])("no permite edición por URL de $encabezado.estado", async (v) => {
    actual = structuredClone(v); navegar("/ventas/2/editar"); await screen.findByText(/no admite edición/); expect(screen.queryByRole("button", { name: /Guardar/ })).toBeNull();
  });
  it("400 conserva carrito y muestra mensaje exacto sin modificar cantidad", async () => {
    const previo = api.getMockImplementation()!; api.mockImplementation(async (r, o) => { if (o?.method) throw new ErrorApi("El producto no permite descuentos.", 400, { codigo: "VALIDACION_VENTAS" }); return previo(r, o); });
    navegar("/pos"); await agregar(); fireEvent.click(screen.getByRole("button", { name: "Guardar borrador" })); await screen.findByText("El producto no permite descuentos.");
    expect((screen.getByLabelText("Cantidad Helado Chocolate Litro") as HTMLInputElement).value).toBe("1");
  });
});

describe("operaciones, concurrencia e idempotencia visual", () => {
  it("confirma con versión actual, luego completa usando la nueva versión y UUID", async () => {
    navegar(); await abrirAccion("Confirmar venta", "Confirmar"); await screen.findByText("CONFIRMADA");
    expect(cuerpos()[0]).toEqual({ versionFila: "AQ==" }); expect(screen.getByText("Sin movimientos")).toBeDefined();
    await abrirAccion("Completar venta", "Cerrar venta"); await screen.findByText("COMPLETADA");
    expect(cuerpos()[1]).toMatchObject({ versionFila: "Ag==" }); expect(cuerpos()[1].claveOperacion).toMatch(/^[\da-f-]{36}$/i);
  });
  it("stock insuficiente mantiene venta confirmada, cantidades, sin movimientos y misma UUID al reintentar", async () => {
    actual = structuredClone(ventaConfirmada); const previo = api.getMockImplementation()!; let intento = 0;
    api.mockImplementation(async (r, o) => { if (r.endsWith("/completar") && intento++ === 0) throw new ErrorApi("Existencia insuficiente para producto 1. Ninguna línea fue descontada.", 409, { codigo: "EXISTENCIA_INSUFICIENTE" }); return previo(r, o); });
    navegar(); await abrirAccion("Completar venta", "Cerrar venta"); await screen.findByText("Existencia insuficiente para Helado Chocolate. Ninguna línea fue descontada.");
    expect(screen.getByText("CONFIRMADA")).toBeDefined(); expect(screen.getByText("20 L")).toBeDefined(); expect(screen.getByText("Sin movimientos")).toBeDefined();
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Cerrar venta" })); await screen.findByText("COMPLETADA");
    expect(cuerpos()[0].claveOperacion).toBe(cuerpos()[1].claveOperacion);
    const rutas = api.mock.calls.map(([r]) => r); expect(rutas.slice(rutas.indexOf("/api/ventas/2/completar") + 1, -1)).toContain("/api/ventas/2");
  });
  it("muestra el componente sin stock aunque no sea una línea comercial de la venta", async () => {
    actual = structuredClone(ventaConfirmada); const previo = api.getMockImplementation()!;
    api.mockImplementation(async (r, o) => { if (r.endsWith("/completar")) throw new ErrorApi("Inventario insuficiente para completar la venta: SERVILLETA (producto 99). Ninguna línea fue descontada.", 409, { codigo: "EXISTENCIA_INSUFICIENTE" }); return previo(r, o); });
    navegar(); await abrirAccion("Completar venta", "Cerrar venta");
    await screen.findByText(/Inventario insuficiente para completar la venta: SERVILLETA/);
    expect(screen.getByText("CONFIRMADA")).toBeDefined(); expect(screen.getByText("Sin movimientos")).toBeDefined();
  });
  it("bloquea doble clic mientras el cierre está pendiente", async () => {
    actual = structuredClone(ventaConfirmada); let resolver!: (v: VentaConsulta) => void;
    const pendiente = new Promise<VentaConsulta>((resolve) => { resolver = resolve; }); const previo = api.getMockImplementation()!;
    api.mockImplementation(async (r, o) => r.endsWith("/completar") ? pendiente as never : previo(r, o));
    navegar(); await abrirAccion("Completar venta", "Cerrar venta"); const boton = within(screen.getByRole("dialog")).getByRole("button", { name: "Cerrar venta" });
    fireEvent.click(boton); fireEvent.click(boton); expect(escrituras()).toHaveLength(1); expect((boton as HTMLButtonElement).disabled).toBe(true);
    await act(async () => resolver(ventaCompletada)); await screen.findByText("COMPLETADA");
  });
  it("respuesta perdida consulta estado y muestra cierre existente sin otro POST", async () => {
    actual = structuredClone(ventaConfirmada); const previo = api.getMockImplementation()!;
    api.mockImplementation(async (r, o) => { if (r.endsWith("/completar")) { await previo(r, o); throw new TypeError("Failed to fetch"); } return previo(r, o); });
    navegar(); await abrirAccion("Completar venta", "Cerrar venta"); await screen.findByText("COMPLETADA"); expect(escrituras()).toHaveLength(1);
    expect(api.mock.calls.filter(([r, o]) => r === "/api/ventas/2" && !o?.method)).toHaveLength(2);
  });
  it("timeout pendiente conserva UUID incluso tras desmontar y volver a abrir", async () => {
    actual = structuredClone(ventaConfirmada); const previo = api.getMockImplementation()!; let intento = 0;
    api.mockImplementation(async (r, o) => { if (r.endsWith("/completar") && intento++ === 0) throw new TypeError("timeout"); return previo(r, o); });
    const vista = navegar(); await abrirAccion("Completar venta", "Cerrar venta"); await screen.findByText(/No se pudo confirmar el resultado/); const uuid = cuerpos()[0].claveOperacion;
    vista.unmount(); navegar(); await abrirAccion("Completar venta", "Cerrar venta"); await screen.findByText("COMPLETADA"); expect(cuerpos()[1].claveOperacion).toBe(uuid);
  });
  it("reconciliación fallida nunca envía un reintento ciego", async () => {
    actual = structuredClone(ventaConfirmada); const previo = api.getMockImplementation()!; let cierreEnviado = false;
    api.mockImplementation(async (r, o) => { if (r.endsWith("/completar")) { cierreEnviado = true; throw new TypeError("timeout"); } if (r === "/api/ventas/2" && cierreEnviado) throw new TypeError("Sin conexión"); return previo(r, o); });
    navegar(); await abrirAccion("Completar venta", "Cerrar venta"); await screen.findByText(/No se pudo confirmar el resultado/);
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Cerrar venta" })); await screen.findByText("Sin conexión"); expect(escrituras()).toHaveLength(1);
  });
  it("anula completada con motivo, advertencia y movimientos original/reverso", async () => {
    actual = structuredClone(ventaCompletada); navegar(); fireEvent.click(await screen.findByRole("button", { name: "Anular venta" }));
    expect(screen.getByText("Esta anulación generará una reversión de inventario.")).toBeDefined(); fireEvent.change(screen.getByLabelText("Motivo de anulación"), { target: { value: "Cliente desistió" } });
    fireEvent.click(screen.getByRole("button", { name: "Anular definitivamente" })); await screen.findByText("ANULADA");
    expect(cuerpos()[0]).toEqual({ versionFila: "Aw==", motivo: "Cliente desistió" }); expect(screen.getByText("VENTA-SALIDA")).toBeDefined(); expect(screen.getByText("VENTA-ANULACION-ENTRADA")).toBeDefined();
    expect(screen.queryByRole("button", { name: "Anular venta" })).toBeNull();
  });
  it("doble anulación se muestra controlada y recarga ANULADA sin otra escritura", async () => {
    actual = structuredClone(ventaCompletada); const previo = api.getMockImplementation()!;
    api.mockImplementation(async (r, o) => { if (r.endsWith("/anular")) { actual = structuredClone(ventaAnulada); throw new ErrorApi("La venta ya fue anulada.", 409, { codigo: "CONFLICTO_VENTAS" }); } return previo(r, o); });
    navegar(); fireEvent.click(await screen.findByRole("button", { name: "Anular venta" })); fireEvent.change(screen.getByLabelText("Motivo de anulación"), { target: { value: "Otro motivo" } }); fireEvent.click(screen.getByRole("button", { name: "Anular definitivamente" }));
    await screen.findByText("La venta ya fue anulada."); fireEvent.click(screen.getByRole("button", { name: "Recargar información" })); await screen.findByText("ANULADA"); expect(escrituras()).toHaveLength(1);
  });
  it("RowVersion obsoleta exige recargar y no vuelve a enviar hasta hacerlo", async () => {
    actual = structuredClone(ventaConfirmada); const previo = api.getMockImplementation()!;
    api.mockImplementation(async (r, o) => { if (r.endsWith("/completar")) throw new ErrorApi("La venta fue modificada. Recargue su versión actual.", 409, { codigo: "CONFLICTO_VENTAS" }); return previo(r, o); });
    navegar(); await abrirAccion("Completar venta", "Cerrar venta"); await screen.findByText("La venta cambió desde que la abriste. Recarga la información.");
    expect((screen.getByRole("button", { name: "Cerrar venta" }) as HTMLButtonElement).disabled).toBe(true);
    actual = { ...actual, encabezado: { ...actual.encabezado, versionFila: "actualizada" } };
    fireEvent.click(screen.getByRole("button", { name: "Recargar información" })); await waitFor(() => expect(screen.queryByRole("dialog")).toBeNull());
    await abrirAccion("Completar venta", "Cerrar venta"); await waitFor(() => expect(escrituras()).toHaveLength(2)); expect(cuerpos()[1].versionFila).toBe("actualizada"); expect(cuerpos()[1].claveOperacion).toBe(cuerpos()[0].claveOperacion);
  });
  it("el modal es operable por teclado y restaura foco al cancelar", async () => {
    navegar(); const boton = await screen.findByRole("button", { name: "Confirmar venta" }); boton.focus(); fireEvent.click(boton);
    const confirmar = within(screen.getByRole("dialog")).getByRole("button", { name: "Confirmar" }); confirmar.focus(); fireEvent.keyDown(document, { key: "Tab" });
    expect(document.activeElement?.getAttribute("aria-label")).toBe("Cerrar ventana"); fireEvent.click(screen.getByRole("button", { name: "Cancelar" })); expect(document.activeElement).toBe(boton);
  });
  it.each([403, 404])("escritura HTTP %s presenta error controlado", async (estado) => {
    const previo = api.getMockImplementation()!; api.mockImplementation(async (r, o) => { if (o?.method) throw new ErrorApi("No disponible", estado); return previo(r, o); });
    navegar(); await abrirAccion("Confirmar venta", "Confirmar"); await screen.findByText(`Error ${estado}`); expect(escrituras()).toHaveLength(1);
  });
});
