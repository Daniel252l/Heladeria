import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { StrictMode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { estadosGenerales, pagina, productoCompra, proveedorProductoPrueba, proveedorPrueba, referenciasCompras } from "../../test/datosFaseVisual3";
import { renderSeguridad } from "../../test/renderSeguridad";
import { PaginaDetalleProveedor } from "./PaginaDetalleProveedor";
import { PaginaProveedores } from "./PaginaProveedores";

vi.mock("../../api/clienteHttp", async (importOriginal) => ({ ...(await importOriginal<typeof import("../../api/clienteHttp")>()), solicitar: vi.fn() }));
const api = vi.mocked(solicitar);

describe("proveedores y productos asociados", () => {
  beforeEach(() => {
    api.mockReset();
    api.mockImplementation(async (ruta, opciones) => {
      if (ruta === "/api/compras/referencias") return referenciasCompras as never;
      if (ruta.startsWith("/api/configuracion/estados")) return pagina(estadosGenerales) as never;
      if (ruta.startsWith("/api/compras/proveedores/7/productos?")) return pagina([proveedorProductoPrueba]) as never;
      if (ruta === "/api/compras/proveedores/7") return proveedorPrueba as never;
      if (ruta.startsWith("/api/compras/proveedores?")) return pagina([proveedorPrueba]) as never;
      if (ruta.startsWith("/api/productos?")) return pagina([productoCompra]) as never;
      if (opciones?.method) return proveedorPrueba as never;
      return {} as never;
    });
  });

  it("mantiene finita la carga y consulta una vez por acción legítima", async () => {
    api.mockImplementation(async (ruta) => {
      if (ruta === "/api/compras/referencias") return referenciasCompras as never;
      if (ruta.startsWith("/api/configuracion/estados")) return pagina(estadosGenerales) as never;
      if (ruta.startsWith("/api/compras/proveedores?")) {
        const parametros = new URL(`http://prueba${ruta}`).searchParams;
        return { ...pagina([proveedorPrueba]), pagina: Number(parametros.get("pagina")), totalRegistros: 11, totalPaginas: 2 } as never;
      }
      return {} as never;
    });
    const llamadasListado = () => api.mock.calls.filter(([ruta, opciones]) => ruta.startsWith("/api/compras/proveedores?") && !opciones?.method).length;

    renderSeguridad(<PaginaProveedores />, [PERMISOS.proveedoresConsultar]);
    await screen.findByText(proveedorPrueba.nombreProveedor);
    expect(llamadasListado()).toBe(1);
    expect(screen.queryByText("Cargando información…")).toBeNull();
    expect(api.mock.calls.filter(([ruta]) => ruta === "/api/compras/referencias")).toHaveLength(1);
    expect(api.mock.calls.filter(([ruta]) => ruta.startsWith("/api/configuracion/estados"))).toHaveLength(1);

    fireEvent.change(screen.getByLabelText("Código o nombre"), { target: { value: "Lácteos" } });
    expect(llamadasListado()).toBe(1);
    fireEvent.click(screen.getByRole("button", { name: "Buscar proveedores" }));
    await waitFor(() => expect(llamadasListado()).toBe(2));

    fireEvent.click(screen.getByRole("button", { name: "Limpiar filtros" }));
    await waitFor(() => expect(llamadasListado()).toBe(3));
    fireEvent.click(screen.getByRole("button", { name: "Página siguiente" }));
    await waitFor(() => expect(llamadasListado()).toBe(4));
    fireEvent.change(screen.getByLabelText("Registros por página"), { target: { value: "20" } });
    await waitFor(() => expect(llamadasListado()).toBe(5));
  });

  it("se estabiliza después de la doble ejecución de efectos de StrictMode", async () => {
    renderSeguridad(<StrictMode><PaginaProveedores /></StrictMode>, [PERMISOS.proveedoresConsultar]);
    await screen.findByText(proveedorPrueba.nombreProveedor);
    const contar = (predicado: (ruta: string) => boolean) => api.mock.calls.filter(([ruta]) => predicado(ruta)).length;
    const listados = contar((ruta) => ruta.startsWith("/api/compras/proveedores?"));
    const referencias = contar((ruta) => ruta === "/api/compras/referencias");
    const estados = contar((ruta) => ruta.startsWith("/api/configuracion/estados"));
    expect(listados).toBeGreaterThanOrEqual(1); expect(listados).toBeLessThanOrEqual(2);
    expect(referencias).toBeGreaterThanOrEqual(1); expect(referencias).toBeLessThanOrEqual(2);
    expect(estados).toBeGreaterThanOrEqual(1); expect(estados).toBeLessThanOrEqual(2);
    await Promise.resolve();
    expect(contar((ruta) => ruta.startsWith("/api/compras/proveedores?"))).toBe(listados);
    expect(contar((ruta) => ruta === "/api/compras/referencias")).toBe(referencias);
    expect(contar((ruta) => ruta.startsWith("/api/configuracion/estados"))).toBe(estados);
  });

  it("crea, edita y desactiva proveedores mediante los endpoints reales", async () => {
    renderSeguridad(<PaginaProveedores />, [PERMISOS.proveedoresConsultar, PERMISOS.proveedoresAdministrar]);
    await screen.findByText(proveedorPrueba.nombreProveedor);
    fireEvent.click(screen.getByRole("button", { name: "Nuevo proveedor" }));
    fireEvent.change(screen.getByLabelText("Código proveedor *"), { target: { value: "PROV-008" } });
    fireEvent.change(screen.getByLabelText("Nombre *"), { target: { value: "Frutas Frescas" } });
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: /Guardar/ }));
    await waitFor(() => expect(api.mock.calls.some(([ruta, opciones]) => ruta === "/api/compras/proveedores" && opciones?.method === "POST")).toBe(true));

    fireEvent.click(screen.getByRole("button", { name: "Editar" }));
    fireEvent.change(screen.getByLabelText("Nombre *"), { target: { value: "Lácteos Actualizados" } });
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: /Guardar/ }));
    await waitFor(() => expect(api.mock.calls.some(([ruta, opciones]) => ruta === "/api/compras/proveedores/7" && opciones?.method === "PUT")).toBe(true));

    fireEvent.click(screen.getByRole("button", { name: "Desactivar" }));
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Confirmar" }));
    await waitFor(() => expect(api.mock.calls.some(([ruta, opciones]) => ruta === "/api/compras/proveedores/7/estado" && opciones?.method === "PUT" && JSON.parse(String(opciones.body)).estadoId === 3)).toBe(true));
  });

  it("muestra y crea asociaciones sin inventar presentación ni unidad", async () => {
    renderSeguridad(<Routes><Route path="/proveedores/:id" element={<PaginaDetalleProveedor />} /></Routes>, [PERMISOS.proveedoresConsultar, PERMISOS.proveedoresAdministrar], "/proveedores/7");
    await screen.findByRole("heading", { name: proveedorPrueba.nombreProveedor });
    expect(screen.getByText(proveedorProductoPrueba.producto)).toBeDefined();
    expect(screen.getByText(/no expone presentación ni unidad de compra/)).toBeDefined();
    fireEvent.click(screen.getByRole("button", { name: "Asociar producto" }));
    fireEvent.click(screen.getByLabelText("Producto *"));
    fireEvent.click(await screen.findByRole("option", { name: "Base de vainilla" }));
    fireEvent.change(screen.getByLabelText("Código del proveedor"), { target: { value: "BASE-1L" } });
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: /Guardar/ }));
    await waitFor(() => expect(api.mock.calls.some(([ruta, opciones]) => ruta === "/api/compras/proveedores/7/productos" && opciones?.method === "POST")).toBe(true));
  });
});
