import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { solicitar, ErrorApi } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { PaginaProductos } from "../../pages/PaginaProductos";
import { paginaPrueba, productoPrueba, presentacionPrueba } from "../../test/datosFaseVisual2";
import { renderSeguridad } from "../../test/renderSeguridad";
import type { ComponenteVenta } from "../../types/productos";
import { validarComposicion, type FilaComponente } from "./utilidadesComposicion";

vi.mock("../../api/clienteHttp", async (original) => ({ ...(await original<typeof import("../../api/clienteHttp")>()), solicitar: vi.fn() }));
const api = vi.mocked(solicitar);
const compuesto = { ...productoPrueba, id: 40, nombre: "Vaca Loca", sku: "VACA-LOCA", unidadBase: "Unidad", abreviaturaUnidadBase: "un", componentes: [] as ComponenteVenta[] };
const componente: ComponenteVenta = { productoId: 17, producto: "Helado de vainilla", sku: "HEL-VAI", productoPresentacionId: 23,
  presentacion: "Bola", cantidad: 2, factorConversion: 60, cantidadBase: 120, unidad: "Bola", abreviaturaUnidad: "bola", unidadBaseId: 9, unidadBase: "Mililitro", abreviaturaUnidadBase: "ml" };
const escritura = () => api.mock.calls.filter(([,o]) => o?.method === "PUT" || o?.method === "POST");
beforeEach(() => {
  compuesto.componentes = []; api.mockReset();
  api.mockImplementation(async (ruta, o) => {
    if (o?.method) return compuesto as never;
    if (ruta === "/api/productos/referencias") return { tiposSeguimiento: [{ id: 10, codigo: "NINGUNO", nombre: "Ninguno" }], metodosValuacion: [{ id: 11, codigo: "PROMEDIO", nombre: "Promedio" }] } as never;
    if (ruta.startsWith("/api/productos?soloComponentesVenta=true")) return paginaPrueba([productoPrueba]) as never;
    if (ruta.startsWith("/api/productos?")) return paginaPrueba([compuesto]) as never;
    if (ruta === "/api/productos/40") return compuesto as never;
    if (ruta === "/api/productos/17") return { ...productoPrueba, esVendible: false } as never;
    if (ruta === "/api/productos/17/presentaciones") return [{ ...presentacionPrueba, presentacion: "Bola", factorConversion: 60, unidadMedida: "Bola", abreviaturaUnidadMedida: "bola", permiteVenta: false }] as never;
    return paginaPrueba([{ id: 6, nombre: "Activo" }, { id: 7, nombre: "Helados" }, { id: 8, nombre: "Inventariable" }, { id: 9, nombre: "Unidad" }]) as never;
  });
});
async function abrir() {
  renderSeguridad(<PaginaProductos />, [PERMISOS.productosConsultar, PERMISOS.productosAdministrar]);
  fireEvent.click(await screen.findByRole("button", { name: "Editar Vaca Loca" }));
  return within(await screen.findByRole("dialog", { name: "Editar producto" }));
}
async function agregar() {
  fireEvent.click(screen.getByRole("button", { name: "Agregar componente" }));
  fireEvent.click(screen.getByRole("button", { name: /Producto\/Insumo 1/ }));
  fireEvent.click(await screen.findByRole("option", { name: /Helado de vainilla/ }));
  await waitFor(() => expect((screen.getByLabelText("Presentación 1") as HTMLSelectElement).disabled).toBe(false));
}
describe("composición de venta en Productos", () => {
  it("crea producto y composición juntos en el POST existente", async () => {
    renderSeguridad(<PaginaProductos />, [PERMISOS.productosConsultar, PERMISOS.productosAdministrar]);
    fireEvent.click(await screen.findByRole("button", { name: "Nuevo producto" }));
    const modal = within(await screen.findByRole("dialog", { name: "Nuevo producto" }));
    fireEvent.change(modal.getByLabelText("Nombre del producto *"), { target: { value: "Vaca Loca nueva" } });
    fireEvent.change(modal.getByLabelText("SKU / Código *"), { target: { value: "VACA-NUEVA" } });
    for (const [campo, opcion] of [["Categoría", "Helados"], ["Tipo de producto", "Inventariable"], ["Unidad base", "Unidad"], ["Estado", "Activo"]]) {
      fireEvent.click(modal.getByRole("button", { name: new RegExp(`^${campo} \\*`) }));
      fireEvent.click(await modal.findByRole("option", { name: opcion }));
    }
    fireEvent.change(modal.getByLabelText("Tipo de seguimiento *"), { target: { value: "10" } });
    fireEvent.change(modal.getByLabelText("Método de valuación *"), { target: { value: "11" } });
    await agregar(); fireEvent.click(modal.getByRole("button", { name: "Guardar" }));
    await waitFor(() => expect(escritura()).toHaveLength(1));
    expect(escritura()[0][0]).toBe("/api/productos"); expect(escritura()[0][1]?.method).toBe("POST");
    expect(JSON.parse(String(escritura()[0][1]?.body))).toMatchObject({ nombre: "Vaca Loca nueva", componentes: [{ productoId: 17, productoPresentacionId: null, cantidad: 1 }] });
  });
  it("agrega insumo no vendible, presentación factor60 y envía cantidad numérica en PUT", async () => {
    await abrir(); await agregar();
    fireEvent.change(screen.getByLabelText("Presentación 1"), { target: { value: "23" } });
    fireEvent.change(screen.getByLabelText("Cantidad 1"), { target: { value: "2" } });
    expect(screen.getByText("2 bola = 120 ml")).toBeDefined();
    fireEvent.click(screen.getByRole("button", { name: "Guardar" }));
    await waitFor(() => expect(escritura()).toHaveLength(1));
    expect(JSON.parse(String(escritura()[0][1]?.body))).toMatchObject({ esCombo: true, componentes: [{ productoId: 17, productoPresentacionId: 23, cantidad: 2 }] });
  });
  it("edita composición existente y conserva presentación sin tocarla", async () => {
    compuesto.componentes = [componente]; await abrir(); await screen.findByText("2 bola = 120 ml");
    fireEvent.click(screen.getByRole("button", { name: "Guardar" }));
    await waitFor(() => expect(escritura()).toHaveLength(1));
    expect(JSON.parse(String(escritura()[0][1]?.body)).componentes).toEqual([{ productoId: 17, productoPresentacionId: 23, cantidad: 2 }]);
  });
  it("elimina el último componente enviando array vacío", async () => {
    compuesto.componentes = [componente]; await abrir(); fireEvent.click(screen.getByRole("button", { name: "Eliminar componente 1" }));
    expect(screen.queryByLabelText("Cantidad 1")).toBeNull(); fireEvent.click(screen.getByRole("button", { name: "Guardar" }));
    await waitFor(() => expect(escritura()).toHaveLength(1));
    expect(JSON.parse(String(escritura()[0][1]?.body))).toMatchObject({ esCombo: false, componentes: [] });
  });
  it("bloquea guardar componente sin producto", async () => {
    await abrir(); fireEvent.click(screen.getByRole("button", { name: "Agregar componente" }));
    fireEvent.click(screen.getByRole("button", { name: "Guardar" }));
    expect(await screen.findByRole("alert")).toHaveProperty("textContent", expect.stringContaining("Seleccione el producto")); expect(escritura()).toHaveLength(0);
  });
  it("conserva filas y cantidad ante rechazo del backend", async () => {
    compuesto.componentes = [componente]; await abrir(); await screen.findByText("2 bola = 120 ml");
    const previa = api.getMockImplementation()!;
    api.mockImplementation((r,o) => o?.method ? Promise.reject(new ErrorApi("Componente inactivo", 400)) : previa(r,o));
    fireEvent.change(screen.getByLabelText("Cantidad 1"), { target: { value: "3" } });
    fireEvent.click(screen.getByRole("button", { name: "Guardar" }));
    expect(await screen.findByText("Componente inactivo")).toBeDefined(); expect((screen.getByLabelText("Cantidad 1") as HTMLInputElement).value).toBe("3");
  });
  it("consulta muestra equivalencia y no ofrece edición", async () => {
    compuesto.componentes = [componente]; renderSeguridad(<PaginaProductos />, [PERMISOS.productosConsultar]);
    fireEvent.click(await screen.findByRole("button", { name: "Ver detalle de Vaca Loca" }));
    expect(await screen.findByText("2 bola = 120 ml")).toBeDefined(); expect(screen.queryByRole("button", { name: "Agregar componente" })).toBeNull();
  });
  it.each(["0", "-1", ".0000001", "", "texto"])("valida cantidad %s", (cantidad) => {
    expect(validarComposicion([{ clave: "1", productoId: 17, productoPresentacionId: null, cantidad, nombre: "Helado", presentacion: null }])).not.toBeNull();
  });
  it("valida autorreferencia y duplicados sin rechazar presentaciones distintas", () => {
    const fila: FilaComponente = { clave: "1", productoId: 17, productoPresentacionId: null, cantidad: "1", nombre: "Helado", presentacion: null };
    expect(validarComposicion([fila], 17)).toContain("sí mismo");
    expect(validarComposicion([fila, { ...fila, clave: "2" }])).toContain("repetir");
    expect(validarComposicion([fila, { ...fila, clave: "2", productoPresentacionId: 23 }])).toBeNull();
  });
});
