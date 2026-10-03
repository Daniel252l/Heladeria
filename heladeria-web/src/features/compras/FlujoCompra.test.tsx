import { fireEvent, screen, waitFor } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { compraDetalle, pagina, presentacionesCompra, presentacionesCompraAlterno, productoCompra, productoCompraAlterno, proveedorPrueba, referenciasCompras } from "../../test/datosFaseVisual3";
import { renderSeguridad } from "../../test/renderSeguridad";
import { PaginaDetalleCompra } from "./PaginaDetalleCompra";
import { PaginaFormularioCompra } from "./PaginaFormularioCompra";

vi.mock("../../api/clienteHttp", async (importOriginal) => ({ ...(await importOriginal<typeof import("../../api/clienteHttp")>()), solicitar: vi.fn() }));
const api = vi.mocked(solicitar);

describe("creación y detalle de compra", () => {
  beforeEach(() => {
    api.mockReset();
    api.mockImplementation(async (ruta, opciones) => {
      if (ruta === "/api/compras/referencias") return referenciasCompras as never;
      if (ruta.startsWith("/api/compras/proveedores?")) return pagina([proveedorPrueba]) as never;
      if (ruta.startsWith("/api/productos?")) return pagina([productoCompra, productoCompraAlterno]) as never;
      if (ruta === "/api/compras/productos/17/presentaciones") return presentacionesCompra as never;
      if (ruta === "/api/compras/productos/18/presentaciones") return presentacionesCompraAlterno as never;
      if (ruta === "/api/compras" && opciones?.method === "POST") return compraDetalle() as never;
      if (ruta === "/api/compras/51") return compraDetalle("PARCIALMENTE_RECIBIDA", 7) as never;
      return {} as never;
    });
  });

  it("crea una orden con selectores únicos y detalle multimedida", async () => {
    renderSeguridad(<Routes><Route path="/compras/nueva" element={<PaginaFormularioCompra />} /></Routes>, [PERMISOS.comprasAdministrar], "/compras/nueva");
    await screen.findByRole("heading", { name: "Nueva orden de compra" });
    fireEvent.click(screen.getByLabelText("Proveedor *"));
    fireEvent.click(await screen.findByRole("option", { name: /PROV-007.*Lácteos del Valle/ }));
    fireEvent.click(screen.getByLabelText("Producto *"));
    fireEvent.click(await screen.findByRole("option", { name: "Base de vainilla" }));
    await waitFor(() => expect(api).toHaveBeenCalledWith("/api/compras/productos/17/presentaciones"));
    fireEvent.click(screen.getByLabelText("Presentación *"));
    expect(await screen.findByRole("option", { name: /Litro.*L.*1,000/ })).toBeDefined();
    expect(screen.getByRole("option", { name: /Vaso.*VASO.*250/ })).toBeDefined();
    fireEvent.click(screen.getByRole("option", { name: /Litro.*L.*1,000/ }));
    fireEvent.change(screen.getByLabelText("Cantidad *"), { target: { value: "4" } });
    fireEvent.change(screen.getByLabelText("Precio unitario *"), { target: { value: "25" } });
    expect(document.body.textContent).toContain("4 L=4,000 ml");
    expect(document.body.textContent).toContain("GTQ 0.025 / ml");
    fireEvent.click(screen.getByLabelText("Presentación *"));
    fireEvent.click(screen.getByRole("option", { name: /Vaso.*VASO.*250/ }));
    expect(document.body.textContent).toContain("4 vaso=1,000 ml");
    fireEvent.click(screen.getByLabelText("Presentación *"));
    fireEvent.click(screen.getByRole("option", { name: /Litro.*L.*1,000/ }));
    fireEvent.click(screen.getByRole("button", { name: /Guardar borrador/ }));
    await waitFor(() => expect(api.mock.calls.some(([ruta, opciones]) => ruta === "/api/compras" && opciones?.method === "POST")).toBe(true));
    const llamada = api.mock.calls.find(([ruta, opciones]) => ruta === "/api/compras" && opciones?.method === "POST")!;
    expect(JSON.parse(String(llamada[1]?.body))).toMatchObject({ sucursalId: 4, proveedorId: 7, monedaId: 1, tipoCambio: 1, detalles: [{ productoId: 17, productoPresentacionId: 23, cantidad: 4, precioUnitario: 25 }] });
  });

  it("mantiene unidad base y Litro cuando el contrato de compras devuelve una presentación", async () => {
    const productoHelado = { ...productoCompra, id: 1, nombre: "Helado Chocolate", sku: "HEL-CHO-001" };
    const catalogoHelado = {
      ...presentacionesCompra,
      productoId: 1,
      producto: "Helado Chocolate",
      presentaciones: [{ ...presentacionesCompra.presentaciones[0], productoPresentacionId: 1, presentacionId: 1 }],
    };
    const base = api.getMockImplementation()!;
    api.mockImplementation((ruta, opciones) => {
      if (ruta.startsWith("/api/productos?")) return Promise.resolve(pagina([productoHelado])) as never;
      if (ruta === "/api/compras/productos/1/presentaciones") return Promise.resolve(catalogoHelado) as never;
      return base(ruta, opciones);
    });
    renderSeguridad(<Routes><Route path="/compras/nueva" element={<PaginaFormularioCompra />} /></Routes>, [PERMISOS.comprasAdministrar], "/compras/nueva");
    await screen.findByRole("heading", { name: "Nueva orden de compra" });
    fireEvent.click(screen.getByLabelText("Proveedor *"));
    fireEvent.click(await screen.findByRole("option", { name: /PROV-007.*Lácteos del Valle/ }));
    fireEvent.click(screen.getByLabelText("Producto *"));
    fireEvent.click(await screen.findByRole("option", { name: "Helado Chocolate" }));
    await waitFor(() => expect(api).toHaveBeenCalledWith("/api/compras/productos/1/presentaciones"));
    fireEvent.click(screen.getByLabelText("Presentación *"));
    expect(screen.getByRole("option", { name: "Unidad base · ml · × 1" })).toBeDefined();
    const litro = screen.getByRole("option", { name: "Litro · L · × 1,000" });
    expect(screen.queryByRole("option", { name: /Vaso/ })).toBeNull();
    fireEvent.click(litro);
    fireEvent.change(screen.getByLabelText("Cantidad *"), { target: { value: "4" } });
    fireEvent.change(screen.getByLabelText("Precio unitario *"), { target: { value: "25" } });
    expect(document.body.textContent).toContain("4 L=4,000 ml");
    const texto = document.body.textContent?.replaceAll("\u00a0", " ") ?? "";
    expect(texto).toContain("GTQ 25.00 / L");
    expect(texto).toContain("GTQ 0.025 / ml");
    fireEvent.click(screen.getByRole("button", { name: /Guardar borrador/ }));
    await waitFor(() => expect(api.mock.calls.some(([ruta, opciones]) => ruta === "/api/compras" && opciones?.method === "POST")).toBe(true));
    const llamada = api.mock.calls.find(([ruta, opciones]) => ruta === "/api/compras" && opciones?.method === "POST")!;
    expect(JSON.parse(String(llamada[1]?.body)).detalles[0].productoPresentacionId).toBe(1);
  });

  it("limpia la presentación anterior mientras carga un producto distinto", async () => {
    let resolver!: (valor: typeof presentacionesCompraAlterno) => void;
    const pendiente = new Promise<typeof presentacionesCompraAlterno>((resolve) => { resolver = resolve; });
    const base = api.getMockImplementation()!;
    api.mockImplementation((ruta, opciones) => ruta === "/api/compras/productos/18/presentaciones" ? pendiente as never : base(ruta, opciones));
    renderSeguridad(<Routes><Route path="/compras/nueva" element={<PaginaFormularioCompra />} /></Routes>, [PERMISOS.comprasAdministrar], "/compras/nueva");
    await screen.findByRole("heading", { name: "Nueva orden de compra" });
    fireEvent.click(screen.getByLabelText("Producto *"));
    fireEvent.click(await screen.findByRole("option", { name: "Base de vainilla" }));
    await waitFor(() => expect((screen.getByLabelText("Presentación *") as HTMLButtonElement).disabled).toBe(false));
    fireEvent.click(screen.getByLabelText("Presentación *"));
    fireEvent.click(screen.getByRole("option", { name: /Litro/ }));
    fireEvent.click(screen.getByLabelText("Producto *"));
    fireEvent.click(screen.getByRole("option", { name: "Base de fresa" }));
    expect((screen.getByLabelText("Presentación *") as HTMLButtonElement).disabled).toBe(true);
    expect(screen.getByLabelText("Presentación *").textContent).toContain("Cargando presentaciones");
    expect(screen.getByLabelText("Presentación *").textContent).not.toContain("Litro");
    resolver(presentacionesCompraAlterno);
    await waitFor(() => expect((screen.getByLabelText("Presentación *") as HTMLButtonElement).disabled).toBe(false));
    expect(screen.getByLabelText("Presentación *").textContent).toContain("Unidad base");
  });

  it("bloquea la línea y no asume factor uno si falla el catálogo", async () => {
    const base = api.getMockImplementation()!;
    api.mockImplementation((ruta, opciones) => ruta === "/api/compras/productos/17/presentaciones" ? Promise.reject(new Error("No fue posible cargar las presentaciones.")) : base(ruta, opciones));
    renderSeguridad(<Routes><Route path="/compras/nueva" element={<PaginaFormularioCompra />} /></Routes>, [PERMISOS.comprasAdministrar], "/compras/nueva");
    await screen.findByRole("heading", { name: "Nueva orden de compra" });
    fireEvent.click(screen.getByLabelText("Producto *"));
    fireEvent.click(await screen.findByRole("option", { name: "Base de vainilla" }));
    expect(await screen.findByText("No fue posible cargar las presentaciones.")).toBeDefined();
    expect((screen.getByLabelText("Presentación *") as HTMLButtonElement).disabled).toBe(true);
    expect(document.body.textContent).not.toContain("Factor: 1");
  });

  it("muestra trazabilidad, equivalencias e historial expandible", async () => {
    renderSeguridad(<Routes><Route path="/compras/:id" element={<PaginaDetalleCompra />} /></Routes>, [PERMISOS.comprasConsultar, PERMISOS.comprasRecibir], "/compras/51");
    await screen.findByRole("heading", { name: /Orden OC-00000051/ });
    expect(screen.getByText("PARCIALMENTE RECIBIDA")).toBeDefined();
    expect(document.body.textContent).toContain("10 Litro = 10,000 ml");
    fireEvent.click(screen.getByRole("button", { name: /REC-000081/ }));
    expect(await screen.findByText(/Movimientos generados/)).toBeDefined();
    expect(document.body.textContent).toContain("GTQ 0.0245 / ml");
    expect(screen.getByText("9001")).toBeDefined();
    expect(screen.getByRole("button", { name: /Recibir pendiente/ })).toBeDefined();
  });

  it("muestra el 404 de alcance sin revelar detalles", async () => {
    api.mockRejectedValue(new ErrorApi("Proveedor secreto no existe", 404));
    renderSeguridad(<Routes><Route path="/compras/:id" element={<PaginaDetalleCompra />} /></Routes>, [PERMISOS.comprasConsultar], "/compras/999");
    await screen.findByText("No se encontró el recurso solicitado.");
    expect(screen.queryByText("Proveedor secreto no existe")).toBeNull();
  });
});
