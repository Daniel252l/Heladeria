import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { compraDetalle, recepcionPrueba, referenciasCompras } from "../../test/datosFaseVisual3";
import { renderSeguridad } from "../../test/renderSeguridad";
import { PaginaRecepcionCompra } from "./PaginaRecepcionCompra";

vi.mock("../../api/clienteHttp", async (importOriginal) => ({ ...(await importOriginal<typeof import("../../api/clienteHttp")>()), solicitar: vi.fn() }));
const api = vi.mocked(solicitar);
const uuid = "11111111-1111-4111-8111-111111111111";

describe("recepciones de compra", () => {
  beforeEach(() => {
    api.mockReset();
    vi.spyOn(crypto, "randomUUID").mockReturnValue(uuid);
    api.mockImplementation(async (ruta, opciones) => {
      if (ruta === "/api/compras/51") return compraDetalle("PARCIALMENTE_RECIBIDA", 7) as never;
      if (ruta === "/api/compras/referencias") return referenciasCompras as never;
      if (ruta === "/api/inventario/almacenes") return [{ sucursalId: 41, sucursal: "Sucursal Central", bodegaId: 4, bodega: "Bodega principal", ubicacionId: 8, ubicacion: "Congelador A", esBodegaPrincipal: true }] as never;
      if (ruta.endsWith("/recepciones") && opciones?.method === "POST") return recepcionPrueba as never;
      return {} as never;
    });
  });

  function renderPagina() {
    return renderSeguridad(<Routes><Route path="/compras/:id/recibir" element={<PaginaRecepcionCompra />} /></Routes>, [PERMISOS.comprasRecibir], "/compras/51/recibir");
  }

  it("aplica una recepción parcial con una única clave de operación", async () => {
    renderPagina();
    const cantidad = await screen.findByLabelText("Cantidad a recibir de Base de vainilla");
    fireEvent.change(cantidad, { target: { value: "3" } });
    expect(screen.getByText(/3,000 ml/)).toBeDefined();
    fireEvent.click(screen.getByRole("button", { name: "Revisar recepción" }));
    const dialogo = screen.getByRole("dialog");
    expect(dialogo.textContent).toContain("Pendiente después: 4");
    fireEvent.click(within(dialogo).getByRole("button", { name: "Aplicar recepción" }));
    await screen.findByRole("heading", { name: "Recepción aplicada" });
    const llamada = api.mock.calls.find(([ruta, opciones]) => ruta.endsWith("/recepciones") && opciones?.method === "POST")!;
    expect(JSON.parse(String(llamada[1]?.body))).toMatchObject({ claveOperacion: uuid, bodegaId: 4, ubicacionId: 8, detalles: [{ ordenCompraDetalleId: 501, cantidad: 3 }] });
    expect(crypto.randomUUID).toHaveBeenCalledTimes(1);
  });

  it("acepta exactamente todo el pendiente como recepción final", async () => {
    renderPagina();
    fireEvent.change(await screen.findByLabelText("Cantidad a recibir de Base de vainilla"), { target: { value: "7" } });
    fireEvent.click(screen.getByRole("button", { name: "Revisar recepción" }));
    fireEvent.click(within(screen.getByRole("dialog")).getByRole("button", { name: "Aplicar recepción" }));
    await screen.findByText("REC-000081");
    const llamada = api.mock.calls.find(([ruta, opciones]) => ruta.endsWith("/recepciones") && opciones?.method === "POST")!;
    expect(JSON.parse(String(llamada[1]?.body)).detalles[0].cantidad).toBe(7);
  });

  it("rechaza en cliente una cantidad superior al pendiente y no llama al backend", async () => {
    renderPagina();
    fireEvent.change(await screen.findByLabelText("Cantidad a recibir de Base de vainilla"), { target: { value: "8" } });
    fireEvent.click(screen.getByRole("button", { name: "Revisar recepción" }));
    await screen.findByText(/excede el pendiente/);
    await waitFor(() => expect(api.mock.calls.some(([, opciones]) => opciones?.method === "POST")).toBe(false));
  });
});
