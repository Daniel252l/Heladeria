import { act, fireEvent, screen, waitFor, within } from "@testing-library/react";
import { useState } from "react";
import { Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { renderSeguridad } from "../../test/renderSeguridad";
import { ordenConfirmada, ordenFinalizada, ordenProduccion, paginaProduccion as pagina, presentacionesProduccion, recetaProduccion, referenciasProduccion, referenciasRecetas } from "../../test/datosFaseVisual4";
import type { OrdenProduccionConsulta } from "../../types/produccion";
import { PaginaProduccion } from "./PaginaProduccion";
import { PaginaFormularioProduccion } from "./PaginaFormularioProduccion";
import { PaginaDetalleProduccion } from "./PaginaDetalleProduccion";
import { PaginaFinalizarProduccion } from "./PaginaFinalizarProduccion";
import { useRecursoProduccion } from "./useDatosProduccion";

vi.mock("../../api/clienteHttp", async (original) => ({ ...(await original<typeof import("../../api/clienteHttp")>()), solicitar: vi.fn() }));
const api = vi.mocked(solicitar);
const permisos = [PERMISOS.produccionConsultar, PERMISOS.produccionAdministrar, PERMISOS.produccionProducir];
function navegar(ruta = "/produccion/9", autorizados = permisos) {
  return renderSeguridad(<Routes>
    <Route path="/produccion/nueva" element={<PaginaFormularioProduccion />} />
    <Route path="/produccion/:id/editar" element={<PaginaFormularioProduccion />} />
    <Route path="/produccion/:id/finalizar" element={<PaginaFinalizarProduccion />} />
    <Route path="/produccion/:id" element={<PaginaDetalleProduccion />} />
  </Routes>, autorizados, ruta);
}
async function elegir(etiqueta: RegExp, opcion: RegExp) {
  fireEvent.click(screen.getByRole("button", { name: etiqueta }));
  fireEvent.click(await screen.findByRole("option", { name: opcion }));
}
function usarOrden(orden: OrdenProduccionConsulta) {
  const previo = api.getMockImplementation()!;
  api.mockImplementation(async (ruta, opciones) => ruta === "/api/produccion/9" && !opciones?.method ? orden as never : previo(ruta, opciones));
}
async function prepararFinalizacion() {
  usarOrden(ordenConfirmada);
  const vista = navegar("/produccion/9/finalizar");
  await screen.findByLabelText("Ubicación *");
  fireEvent.change(screen.getByLabelText("Ubicación *"), { target: { value: "7" } });
  return vista;
}
const escrituras = () => api.mock.calls.filter(([, o]) => o?.method);

describe("producción visual", () => {
  beforeEach(() => {
    api.mockReset();
    api.mockImplementation(async (ruta, opciones) => {
      if (ruta === "/api/produccion/referencias") return referenciasProduccion as never;
      if (ruta.includes("/presentaciones")) return presentacionesProduccion(Number(ruta.split("/").at(-2))) as never;
      if (ruta.startsWith("/api/produccion/productos?")) return pagina(referenciasRecetas.productos) as never;
      if (ruta.startsWith("/api/produccion/recetas?")) return pagina([recetaProduccion.encabezado]) as never;
      if (ruta === "/api/produccion/recetas/5") return recetaProduccion as never;
      if (ruta.endsWith("/confirmar")) return ordenConfirmada as never;
      if (ruta.endsWith("/finalizar")) return ordenFinalizada as never;
      if (ruta.endsWith("/anular")) return { ...ordenProduccion, encabezado: { ...ordenProduccion.encabezado, estado: "ANULADA" } } as never;
      if (opciones?.method) return ordenProduccion as never;
      if (ruta.startsWith("/api/produccion?")) return pagina([ordenProduccion.encabezado, { ...ordenConfirmada.encabezado, id: 10 }]) as never;
      return ordenProduccion as never;
    });
  });
  it("lista estados y acciones válidas, con etiquetas para las tarjetas móviles", async () => {
    renderSeguridad(<PaginaProduccion />, permisos);
    const tabla = await screen.findByRole("table", { name: "Listado de producción" });
    expect(within(tabla).getByText("BORRADOR")).toBeDefined();
    expect(within(tabla).getByText("CONFIRMADA")).toBeDefined();
    expect(within(tabla).getAllByRole("link", { name: "Editar" })).toHaveLength(1);
    expect(within(tabla).getAllByRole("link", { name: "Finalizar" })).toHaveLength(1);
    expect(tabla.querySelectorAll('td[data-etiqueta="Acciones"]')).toHaveLength(2);
    expect(tabla.className).toContain("tabla-produccion");
  });
  it("filtra por número, producto, receta, estado y fechas, y limpia", async () => {
    renderSeguridad(<PaginaProduccion />, permisos);
    await screen.findByRole("table");
    fireEvent.change(screen.getByLabelText("Número de orden"), { target: { value: "OP-9" } });
    await elegir(/^Producto/, /HEL-VAI/); await elegir(/^Receta/, /REC-VAI/);
    fireEvent.change(screen.getByLabelText("Estado"), { target: { value: "11" } });
    fireEvent.change(screen.getByLabelText("Fecha desde"), { target: { value: "2026-10-01" } });
    fireEvent.change(screen.getByLabelText("Fecha hasta"), { target: { value: "2026-10-05" } });
    fireEvent.click(screen.getByRole("button", { name: "Buscar producción" }));
    await waitFor(() => expect(api.mock.calls.some(([r]) => r.includes("busqueda=OP-9&productoId=10&recetaId=5&estadoId=11&fechaDesde=2026-10-01&fechaHasta=2026-10-05T23%3A59%3A59.999"))).toBe(true));
    fireEvent.click(screen.getByRole("button", { name: "Limpiar filtros" }));
    expect((screen.getByLabelText("Número de orden") as HTMLInputElement).value).toBe("");
  });
  it("crea una orden escalando 5 L a 15 L sin enviar cantidades base calculadas", async () => {
    navegar("/produccion/nueva");
    await screen.findByLabelText("Receta vigente *");
    await elegir(/^Bodega de producción/, /Producción central/);
    fireEvent.change(screen.getByLabelText("Receta vigente *"), { target: { value: "5" } });
    await waitFor(() => expect((screen.getByRole("button", { name: /^Presentación del producto/ }) as HTMLButtonElement).disabled).toBe(false));
    await elegir(/^Presentación del producto/, /Litro/);
    fireEvent.change(screen.getByLabelText("Cantidad planificada *"), { target: { value: "15" } });
    fireEvent.change(screen.getByLabelText("Fecha inicio *"), { target: { value: "2026-10-08T14:42" } });
    expect(screen.getByText(/15 L = 15,000 ml/)).toBeDefined();
    expect(screen.getByText("9,000 ml")).toBeDefined();
    fireEvent.click(screen.getByRole("button", { name: "Guardar orden" }));
    await waitFor(() => expect(escrituras()).toHaveLength(1));
    const body = JSON.parse(String(escrituras()[0][1]?.body));
    expect(body).toMatchObject({ recetaId: 5, bodegaProduccionId: 2, productoPresentacionId: 11, cantidadPlanificada: 15, fechaInicio: "2026-10-08T14:42" });
    expect(body.cantidadPlanificadaBase).toBeUndefined(); expect(body.detalles).toBeUndefined();
    expect(api.mock.calls.some(([r]) => r.startsWith("/api/recetas"))).toBe(false);
  });
  it("edita un borrador enviando la versión de concurrencia", async () => {
    navegar("/produccion/9/editar");
    const guardar = await screen.findByRole("button", { name: "Guardar orden" });
    await waitFor(() => expect((guardar as HTMLButtonElement).disabled).toBe(false));
    fireEvent.change(screen.getByLabelText("Cantidad planificada *"), { target: { value: "10" } });
    fireEvent.click(guardar);
    await waitFor(() => expect(escrituras()).toHaveLength(1));
    expect(escrituras()[0][0]).toBe("/api/produccion/9");
    expect(JSON.parse(String(escrituras()[0][1]?.body))).toMatchObject({ cantidadPlanificada: 10, versionFila: "AQ==" });
  });
  it("confirma con versionFila actual y reemplaza Editar por Finalizar", async () => {
    navegar();
    fireEvent.click(await screen.findByRole("button", { name: "Confirmar producción" }));
    const dialogo = screen.getByRole("dialog");
    fireEvent.click(within(dialogo).getByRole("button", { name: "Confirmar" }));
    await screen.findByRole("link", { name: "Finalizar producción" });
    expect(screen.queryByRole("link", { name: "Editar" })).toBeNull();
    expect(JSON.parse(String(escrituras()[0][1]?.body))).toEqual({ versionFila: "AQ==" });
  });
  it("anula con confirmación y conserva solo consulta", async () => {
    navegar(); fireEvent.click(await screen.findByRole("button", { name: "Anular" }));
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Anular orden" }));
    await screen.findByText("ANULADA");
    expect(screen.queryByRole("button", { name: "Confirmar producción" })).toBeNull();
  });
  it("no ofrece anular una confirmada que ya tiene movimientos", async () => {
    usarOrden({ ...ordenConfirmada, movimientoIds: [10] }); navegar();
    await screen.findByText("CONFIRMADA");
    expect(screen.queryByRole("button", { name: "Anular" })).toBeNull();
  });
  it("finaliza con ubicación de la bodega y muestra costos y movimientos reales", async () => {
    await prepararFinalizacion();
    expect(screen.queryByRole("option", { name: "Otra ubicación" })).toBeNull();
    fireEvent.click(screen.getByRole("button", { name: "Finalizar producción" }));
    await screen.findByRole("heading", { name: "Producción finalizada" });
    expect(screen.getByText("GTQ 0.00638 / ml")).toBeDefined();
    expect(screen.getAllByText("GTQ 31.90").length).toBeGreaterThan(0);
    expect(screen.getByText("Movimiento #10")).toBeDefined(); expect(screen.getByText("Movimiento #11")).toBeDefined();
    expect(screen.queryByRole("button", { name: "Finalizar producción" })).toBeNull();
    const body = JSON.parse(String(escrituras()[0][1]?.body));
    expect(body).toMatchObject({ ubicacionId: 7, cantidadProducida: 5, versionFila: "Ag==" });
    expect(body.claveOperacion).toMatch(/^[\da-f-]{36}$/i);
  });
  it("stock insuficiente resuelve el nombre y reintenta con la misma clave", async () => {
    const previo = api.getMockImplementation()!;
    let intentos = 0;
    api.mockImplementation(async (r, o) => {
      if (r.endsWith("/finalizar") && intentos++ === 0) throw new ErrorApi("Existencia insuficiente para el ingrediente 20. Disponible: 7000; requerido: 9000.", 409, { codigo: "EXISTENCIA_INSUFICIENTE" });
      return previo(r, o);
    });
    await prepararFinalizacion();
    fireEvent.change(screen.getByLabelText("Cantidad producida (L) *"), { target: { value: "15" } });
    fireEvent.click(screen.getByRole("button", { name: "Finalizar producción" }));
    const mensaje = await screen.findByRole("alert");
    expect(mensaje.textContent).toContain("Leche Entera (cantidades en ml)");
    expect(mensaje.textContent).toContain("Disponible: 7000; requerido: 9000");
    expect((screen.getByLabelText("Cantidad producida (L) *") as HTMLInputElement).value).toBe("15");
    fireEvent.click(screen.getByRole("button", { name: "Finalizar producción" }));
    await screen.findByRole("heading", { name: "Producción finalizada" });
    expect(escrituras()).toHaveLength(2);
    const cuerpos = escrituras().map(([, o]) => JSON.parse(String(o?.body)));
    expect(cuerpos[0].claveOperacion).toBe(cuerpos[1].claveOperacion);
  });
  it("bloquea doble envío mientras la finalización está pendiente", async () => {
    let terminar!: (orden: OrdenProduccionConsulta) => void;
    const pendiente = new Promise<OrdenProduccionConsulta>((resolve) => { terminar = resolve; });
    const previo = api.getMockImplementation()!;
    api.mockImplementation(async (r, o) => r.endsWith("/finalizar") ? pendiente as never : previo(r, o));
    await prepararFinalizacion();
    const boton = screen.getByRole("button", { name: "Finalizar producción" });
    fireEvent.click(boton); fireEvent.click(boton);
    expect(escrituras()).toHaveLength(1);
    await act(async () => terminar(ordenFinalizada));
    await screen.findByRole("heading", { name: "Producción finalizada" });
  });
  it("finalizada es read-only incluso al entrar directamente a finalizar", async () => {
    usarOrden(ordenFinalizada); navegar("/produccion/9/finalizar");
    await screen.findByText(/no admite finalización/);
    expect(screen.queryByLabelText("Ubicación *")).toBeNull(); expect(escrituras()).toHaveLength(0);
  });
  it("solo CONSULTAR ve resultados sin editar, confirmar, anular ni finalizar", async () => {
    usarOrden(ordenFinalizada); navegar("/produccion/9", [PERMISOS.produccionConsultar]);
    await screen.findByRole("heading", { name: "Producción finalizada" });
    expect(screen.queryByRole("button")).toBeNull();
    expect(screen.queryByRole("link", { name: "Editar" })).toBeNull();
    expect(screen.queryByRole("link", { name: "Finalizar producción" })).toBeNull();
  });
  it.each([ordenProduccion, ordenConfirmada])("solo CONSULTAR no ofrece acciones en $encabezado.estado", async (orden) => {
    usarOrden(orden); navegar("/produccion/9", [PERMISOS.produccionConsultar]);
    await screen.findByText(orden.encabezado.estado);
    expect(screen.queryByRole("button", { name: "Confirmar producción" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Anular" })).toBeNull();
    expect(screen.queryByRole("link", { name: "Editar" })).toBeNull();
    expect(screen.queryByRole("link", { name: "Finalizar producción" })).toBeNull();
  });
  it.each([403, 404])("muestra error HTTP %s sin formulario operativo", async (estado) => {
    api.mockRejectedValue(new ErrorApi("No disponible", estado)); navegar();
    await screen.findByText(`Error ${estado}`); expect(escrituras()).toHaveLength(0);
  });
  it("no permite editar una orden confirmada por URL", async () => {
    usarOrden(ordenConfirmada); navegar("/produccion/9/editar");
    await screen.findByText(/no admite edición/); expect(screen.queryByRole("button", { name: "Guardar orden" })).toBeNull();
  });
  it("concurrencia obsoleta conserva formulario y ofrece consultar estado actual", async () => {
    const previo = api.getMockImplementation()!;
    api.mockImplementation(async (r, o) => { if (r.endsWith("/finalizar")) throw new ErrorApi("La orden fue modificada por otro usuario.", 409); return previo(r, o); });
    await prepararFinalizacion(); fireEvent.click(screen.getByRole("button", { name: "Finalizar producción" }));
    await screen.findByText("La orden fue modificada por otro usuario.");
    expect(screen.getByRole("link", { name: "Consultar el estado actual antes de reintentar" })).toBeDefined();
  });
});

function Recurso({ ruta }: { ruta: string }) {
  const carga = useRecursoProduccion<{ nombre: string }>(ruta);
  return <p>{carga.cargando ? "Cargando" : carga.datos?.nombre}</p>;
}
describe("cargas asincrónicas de producción", () => {
  it("descarta una respuesta anterior y aborta al desmontar sin nuevas peticiones", async () => {
    let resolverA!: (dato: { nombre: string }) => void;
    const pendiente = new Promise<{ nombre: string }>((resolve) => { resolverA = resolve; });
    api.mockReset();
    api.mockImplementation(async (ruta) => ruta === "/a" ? pendiente as never : { nombre: "Orden B" } as never);
    // El wrapper permanece estable para probar el mismo hook, no un nuevo montaje.
    function Vista() {
      const [ruta, cambiar] = useState("/a");
      return <><button onClick={() => cambiar("/b")}>Cambiar recurso</button><Recurso ruta={ruta} /></>;
    }
    const vista = renderSeguridad(<Vista />, permisos);
    fireEvent.click(screen.getByText("Cambiar recurso"));
    await screen.findByText("Orden B");
    await act(async () => resolverA({ nombre: "Orden A obsoleta" }));
    expect(screen.queryByText("Orden A obsoleta")).toBeNull();
    expect(api).toHaveBeenCalledTimes(2);
    expect(api.mock.calls[0][1]?.signal?.aborted).toBe(true);
    vista.unmount(); expect(api.mock.calls[1][1]?.signal?.aborted).toBe(true);
  });
});
