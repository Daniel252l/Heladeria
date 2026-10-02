import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { compraDetalle, compraResumen, pagina, proveedorPrueba, referenciasCompras } from "../../test/datosFaseVisual3";
import { renderSeguridad } from "../../test/renderSeguridad";
import { PaginaCompras } from "./PaginaCompras";

vi.mock("../../api/clienteHttp", async (importOriginal) => ({ ...(await importOriginal<typeof import("../../api/clienteHttp")>()), solicitar: vi.fn() }));
const api = vi.mocked(solicitar);

describe("órdenes de compra", () => {
  beforeEach(() => {
    api.mockReset();
    api.mockImplementation(async (ruta, opciones) => {
      if (ruta === "/api/compras/referencias") return referenciasCompras as never;
      if (ruta.startsWith("/api/compras/proveedores?")) return pagina([proveedorPrueba]) as never;
      if (/\/api\/compras\/\d+$/.test(ruta)) return compraDetalle() as never;
      if (ruta.includes("/confirmar") && opciones?.method === "POST") return compraDetalle("CONFIRMADA") as never;
      if (ruta.startsWith("/api/compras?")) return pagina([compraResumen]) as never;
      return {} as never;
    });
  });

  it("muestra listado, progreso real y estados semánticos", async () => {
    const ordenes = referenciasCompras.estados.map((estado, indice) => ({ ...compraResumen, id: 51 + indice, numeroOrden: `OC-${indice}`, estado: estado.codigo, estadoId: estado.id }));
    api.mockImplementation(async (ruta) => {
      if (ruta === "/api/compras/referencias") return referenciasCompras as never;
      if (/\/api\/compras\/\d+$/.test(ruta)) { const id = Number(ruta.split("/").at(-1)); const orden = ordenes.find((x) => x.id === id)!; return compraDetalle(orden.estado, orden.estado === "RECIBIDA" ? 0 : 7) as never; }
      if (ruta.startsWith("/api/compras?")) return pagina(ordenes) as never;
      return pagina([]) as never;
    });
    renderSeguridad(<PaginaCompras />, [PERMISOS.comprasConsultar, PERMISOS.comprasAdministrar, PERMISOS.comprasRecibir]);
    await screen.findByText("OC-0");
    for (const texto of ["BORRADOR", "CONFIRMADA", "PARCIALMENTE RECIBIDA", "RECIBIDA", "ANULADA"]) expect(screen.getAllByText(texto).length).toBeGreaterThan(0);
    expect(screen.getAllByText("0/1 líneas").length).toBeGreaterThan(0);
    expect(screen.getByText("1/1 líneas")).toBeDefined();
  });

  it("aplica filtros desde una única tarjeta", async () => {
    renderSeguridad(<PaginaCompras />, [PERMISOS.comprasConsultar]);
    await screen.findByText(compraResumen.numeroOrden);
    const llamadasListado = () => api.mock.calls.filter(([ruta]) => ruta.startsWith("/api/compras?")).length;
    expect(llamadasListado()).toBe(1);
    fireEvent.change(screen.getByLabelText("Búsqueda"), { target: { value: "OC-51" } });
    fireEvent.change(screen.getByLabelText("Fecha desde"), { target: { value: "2026-10-01" } });
    fireEvent.click(screen.getByRole("button", { name: "Buscar órdenes" }));
    await waitFor(() => expect(api.mock.calls.some(([ruta]) => ruta.includes("busqueda=OC-51") && ruta.includes("fechaDesde=2026-10-01"))).toBe(true));
    expect(llamadasListado()).toBe(2);
  });

  it("confirma únicamente después del diálogo y oculta mutaciones sin permiso", async () => {
    const vista = renderSeguridad(<PaginaCompras />, [PERMISOS.comprasConsultar, PERMISOS.comprasAdministrar]);
    await screen.findByText(compraResumen.numeroOrden);
    fireEvent.click(screen.getByRole("button", { name: "Confirmar" }));
    expect(api.mock.calls.some(([, opciones]) => opciones?.method === "POST")).toBe(false);
    const dialogo = screen.getByRole("dialog");
    expect(dialogo.textContent).toContain("ya no podrá editar");
    fireEvent.click(within(dialogo).getByRole("button", { name: "Confirmar" }));
    await waitFor(() => expect(api.mock.calls.some(([ruta, opciones]) => ruta.endsWith("/confirmar") && opciones?.method === "POST")).toBe(true));
    vista.unmount();

    renderSeguridad(<PaginaCompras />, [PERMISOS.comprasConsultar]);
    await screen.findByText(compraResumen.numeroOrden);
    expect(screen.queryByRole("button", { name: "Nueva orden" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Editar" })).toBeNull();
    expect(screen.queryByRole("button", { name: "Confirmar" })).toBeNull();
  });
});
