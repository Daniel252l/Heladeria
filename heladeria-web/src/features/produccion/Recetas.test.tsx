import { fireEvent, screen, waitFor } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { renderSeguridad } from "../../test/renderSeguridad";
import { paginaProduccion as pagina, presentacionesProduccion, recetaHistorica, recetaProduccion, referenciasRecetas } from "../../test/datosFaseVisual4";
import { PaginaRecetas } from "./PaginaRecetas";
import { PaginaDetalleReceta } from "./PaginaDetalleReceta";
import { PaginaFormularioReceta } from "./PaginaFormularioReceta";

vi.mock("../../api/clienteHttp", async (original) => ({ ...(await original<typeof import("../../api/clienteHttp")>()), solicitar: vi.fn() }));
const api = vi.mocked(solicitar);
const permisos = [PERMISOS.recetasConsultar, PERMISOS.recetasAdministrar];
function formulario(editar = false) {
  return renderSeguridad(<Routes><Route path="/recetas/nueva" element={<PaginaFormularioReceta />} /><Route path="/recetas/:id/editar" element={<PaginaFormularioReceta />} /><Route path="/recetas/:id" element={<PaginaDetalleReceta />} /></Routes>, permisos, editar ? "/recetas/5/editar" : "/recetas/nueva");
}
async function elegir(etiqueta: RegExp, opcion: RegExp) {
  fireEvent.click(screen.getByRole("button", { name: etiqueta }));
  fireEvent.click(await screen.findByRole("option", { name: opcion }));
}
async function completarNueva() {
  formulario();
  await screen.findByLabelText("Código *");
  await elegir(/^Producto terminado \*/, /HEL-VAI/);
  fireEvent.change(screen.getByLabelText("Código *"), { target: { value: "REC-NUEVA" } });
  fireEvent.change(screen.getByLabelText("Nombre *"), { target: { value: "Nueva vainilla" } });
  await elegir(/^Ingrediente 1 \*/, /LECHE/);
  await waitFor(() => expect((screen.getByRole("button", { name: "Guardar receta" }) as HTMLButtonElement).disabled).toBe(false));
}
describe("recetas visuales", () => {
  beforeEach(() => {
    api.mockReset();
    api.mockImplementation(async (ruta, opciones) => {
      if (ruta === "/api/recetas/referencias") return referenciasRecetas as never;
      if (ruta.includes("/presentaciones")) return presentacionesProduccion(Number(ruta.split("/").at(-2))) as never;
      if (opciones?.method) return recetaProduccion as never;
      if (ruta.startsWith("/api/recetas?")) return pagina([recetaProduccion.encabezado, recetaHistorica.encabezado]) as never;
      if (ruta === "/api/recetas/4") return recetaHistorica as never;
      return recetaProduccion as never;
    });
  });
  it("agrupa versiones del listado y permite contraerlas", async () => {
    renderSeguridad(<PaginaRecetas />, permisos);
    const grupo = await screen.findByRole("button", { name: "Versiones de REC-VAI" });
    expect(screen.getByText("v2")).toBeDefined(); expect(screen.getByText("v1")).toBeDefined();
    fireEvent.click(grupo); expect(screen.queryByText("v1")).toBeNull();
  });
  it("aplica todos los filtros y limpiar restablece la consulta paginada", async () => {
    renderSeguridad(<PaginaRecetas />, permisos);
    await screen.findByRole("button", { name: "Versiones de REC-VAI" });
    fireEvent.change(screen.getByLabelText("Código o nombre"), { target: { value: "vainilla" } });
    await elegir(/^Producto terminado/, /HEL-VAI/);
    fireEvent.change(screen.getByLabelText("Estado"), { target: { value: "1" } });
    fireEvent.change(screen.getByLabelText("Vigencia"), { target: { value: "false" } });
    fireEvent.change(screen.getByLabelText("Versión"), { target: { value: "1" } });
    fireEvent.click(screen.getByRole("button", { name: "Buscar recetas" }));
    await waitFor(() => expect(api.mock.calls.some(([ruta]) => ruta.includes("busqueda=vainilla&productoTerminadoId=10&estadoId=1&soloVigentes=false&version=1"))).toBe(true));
    fireEvent.click(screen.getByRole("button", { name: "Limpiar filtros" }));
    await waitFor(() => expect((screen.getByLabelText("Versión") as HTMLInputElement).value).toBe(""));
  });
  it("crea una receta con ingrediente en unidad base y envía solo campos contractuales", async () => {
    await completarNueva();
    fireEvent.click(screen.getByRole("button", { name: "Guardar receta" }));
    await waitFor(() => expect(api.mock.calls.some(([, o]) => o?.method === "POST")).toBe(true));
    const body = JSON.parse(String(api.mock.calls.find(([, o]) => o?.method === "POST")![1]?.body));
    expect(body.detalles[0]).toEqual({ productoInsumoId: 20, productoPresentacionId: null, cantidad: 1, observacion: null });
    expect(body.factorConversion).toBeUndefined(); expect(body.rendimientoBase).toBeUndefined();
  });
  it("muestra multimedida y conserva los IDs de las presentaciones", async () => {
    await completarNueva();
    await elegir(/^Presentación de rendimiento/, /Litro/);
    await elegir(/^Presentación ingrediente 1/, /Litro/);
    fireEvent.change(screen.getByLabelText("Cantidad de rendimiento *"), { target: { value: "5" } });
    fireEvent.change(screen.getByLabelText("Cantidad ingrediente 1 *"), { target: { value: "3" } });
    expect(screen.getByText(/5 L = 5,000 ml/)).toBeDefined(); expect(screen.getByText(/3 L × 1,000 = 3,000 ml/)).toBeDefined();
    fireEvent.click(screen.getByRole("button", { name: "Guardar receta" }));
    await waitFor(() => expect(api.mock.calls.some(([, o]) => o?.method === "POST")).toBe(true));
    const body = JSON.parse(String(api.mock.calls.find(([, o]) => o?.method === "POST")![1]?.body));
    expect(body.productoPresentacionRendimientoId).toBe(11); expect(body.detalles[0].productoPresentacionId).toBe(21);
  });
  it("rechaza ingredientes duplicados antes de enviar", async () => {
    await completarNueva();
    fireEvent.click(screen.getByRole("button", { name: "Agregar ingrediente" }));
    await elegir(/^Ingrediente 2 \*/, /LECHE/);
    fireEvent.click(screen.getByRole("button", { name: "Guardar receta" }));
    expect(await screen.findByRole("alert")).toHaveProperty("textContent", "No se puede repetir un ingrediente dentro de la receta.");
    expect(api.mock.calls.some(([, o]) => o?.method === "POST")).toBe(false);
  });
  it("rechaza el producto terminado como ingrediente", async () => {
    await completarNueva(); await elegir(/^Ingrediente 1 \*/, /HEL-VAI/);
    await waitFor(() => expect((screen.getByRole("button", { name: "Guardar receta" }) as HTMLButtonElement).disabled).toBe(false));
    fireEvent.click(screen.getByRole("button", { name: "Guardar receta" }));
    expect(await screen.findByText("El producto terminado no puede ser un ingrediente.")).toBeDefined();
  });
  it("rechaza una receta vacía", async () => {
    await completarNueva(); fireEvent.click(screen.getByRole("button", { name: "Eliminar ingrediente 1" }));
    await waitFor(() => expect((screen.getByRole("button", { name: "Guardar receta" }) as HTMLButtonElement).disabled).toBe(false));
    fireEvent.click(screen.getByRole("button", { name: "Guardar receta" }));
    expect(await screen.findByText("La receta debe contener al menos un ingrediente.")).toBeDefined();
  });
  it.each([0, -1])("rechaza cantidad de ingrediente %s incluso omitiendo validación nativa", async (cantidad) => {
    await completarNueva();
    fireEvent.change(screen.getByLabelText("Cantidad ingrediente 1 *"), { target: { value: String(cantidad) } });
    fireEvent.submit(screen.getByRole("button", { name: "Guardar receta" }).closest("form")!);
    expect(await screen.findByText("Revise cantidades, unidades base y presentaciones de los ingredientes.")).toBeDefined();
    expect(api.mock.calls.some(([, o]) => o?.method)).toBe(false);
  });
  it("muestra una versión histórica con acceso al historial y sin editar", async () => {
    renderSeguridad(<Routes><Route path="/recetas/:id" element={<PaginaDetalleReceta />} /></Routes>, permisos, "/recetas/4");
    await screen.findByRole("heading", { name: "Historial de versiones" });
    expect(screen.queryByRole("link", { name: "Editar receta" })).toBeNull();
    expect(screen.getAllByText("Histórica").length).toBeGreaterThan(0);
  });
  it("explica el versionado y navega al nuevo ID devuelto por PUT", async () => {
    const previo = api.getMockImplementation()!;
    api.mockImplementation(async (ruta, opciones) => {
      if (ruta === "/api/recetas/5" && !opciones?.method) return { ...recetaProduccion, fueUtilizada: true } as never;
      if (opciones?.method === "PUT" || ruta === "/api/recetas/7") return { ...recetaProduccion, encabezado: { ...recetaProduccion.encabezado, id: 7, version: 3 } } as never;
      return previo(ruta, opciones);
    });
    formulario(true);
    const guardar = await screen.findByRole("button", { name: "Crear nueva versión" });
    await waitFor(() => expect((guardar as HTMLButtonElement).disabled).toBe(false));
    expect(screen.getByText(/Esta receta ya fue utilizada/)).toBeDefined();
    fireEvent.click(guardar);
    await waitFor(() => expect(api.mock.calls.some(([ruta]) => ruta === "/api/recetas/7")).toBe(true));
    const body = JSON.parse(String(api.mock.calls.find(([, o]) => o?.method === "PUT")![1]?.body));
    expect(body.versionFila).toBe("AQ==");
  });
  it("muestra el error de vigencia sin perder lo escrito", async () => {
    const previo = api.getMockImplementation()!;
    api.mockImplementation(async (ruta, opciones) => { if (opciones?.method === "PUT") throw new ErrorApi("La nueva versión debe iniciar después de la versión utilizada.", 400); return previo(ruta, opciones); });
    formulario(true);
    await screen.findByLabelText("Nombre *");
    fireEvent.change(screen.getByLabelText("Nombre *"), { target: { value: "Cambio pendiente" } });
    const guardar = screen.getByRole("button", { name: "Guardar receta" });
    await waitFor(() => expect((guardar as HTMLButtonElement).disabled).toBe(false));
    fireEvent.click(guardar);
    expect(await screen.findByText(/La nueva versión debe iniciar después/)).toBeDefined();
    expect((screen.getByLabelText("Nombre *") as HTMLInputElement).value).toBe("Cambio pendiente");
  });
  it("solo CONSULTAR no muestra acciones de administración", async () => {
    renderSeguridad(<PaginaRecetas />, [PERMISOS.recetasConsultar]);
    await screen.findByRole("table", { name: "Listado de recetas" });
    expect(screen.queryByRole("link", { name: "Nueva receta" })).toBeNull();
    expect(screen.queryByRole("link", { name: "Editar" })).toBeNull();
  });
  it("rechaza producto sin unidad base o presentación obsoleta", async () => {
    const previo = api.getMockImplementation()!;
    api.mockImplementation(async (ruta, opciones) => ruta.includes("/presentaciones") ? { ...presentacionesProduccion(10), unidadBaseId: 0, presentaciones: [] } as never : previo(ruta, opciones));
    formulario(true);
    const guardar = await screen.findByRole("button", { name: "Guardar receta" });
    await waitFor(() => expect((guardar as HTMLButtonElement).disabled).toBe(false));
    fireEvent.click(guardar);
    expect(await screen.findByText("Revise cantidades, unidades base y presentaciones de los ingredientes.")).toBeDefined();
    expect(api.mock.calls.some(([, o]) => o?.method === "PUT")).toBe(false);
  });
  it("reintenta una carga fallida", async () => {
    api.mockRejectedValueOnce(new Error("Servicio temporalmente no disponible"));
    renderSeguridad(<PaginaRecetas />, permisos);
    const alert = await screen.findByText("Servicio temporalmente no disponible");
    expect(alert).toBeDefined();
    fireEvent.click(screen.getByRole("button", { name: "Reintentar" }));
    expect(await screen.findByRole("table", { name: "Listado de recetas" })).toBeDefined();
  });
});
