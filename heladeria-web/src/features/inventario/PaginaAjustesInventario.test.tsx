import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import {
  paginaPrueba,
  productoPrueba,
  presentacionPrueba,
} from "../../test/datosFaseVisual2";
import { renderSeguridad } from "../../test/renderSeguridad";
import { PaginaAjustesInventario } from "./PaginaAjustesInventario";
import { calcularCantidadBase } from "./multimedida";

vi.mock("../../api/clienteHttp", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../../api/clienteHttp")>()),
  solicitar: vi.fn(),
}));
const solicitarSimulado = vi.mocked(solicitar);
const permisos = [
  PERMISOS.inventarioAdministrar,
  PERMISOS.inventarioConsultar,
  PERMISOS.productosConsultar,
];
describe("ajustes y multimedida", () => {
  beforeEach(() => {
    solicitarSimulado.mockReset();
    solicitarSimulado.mockImplementation(async (ruta, opciones) => {
      if (ruta === "/api/inventario/almacenes")
        return [
          {
            sucursalId: 41,
            sucursal: "Central",
            bodegaId: 42,
            bodega: "Principal",
            ubicacionId: 43,
            ubicacion: "Congelador",
            esBodegaPrincipal: true,
          },
        ] as never;
      if (ruta === "/api/productos/17") return productoPrueba as never;
      if (ruta === "/api/productos/17/presentaciones")
        return [presentacionPrueba] as never;
      if (opciones?.method === "POST")
        return {
          movimientoId: 99,
          cantidadBase: 3000,
          saldoAnterior: 0,
          saldoNuevo: 3000,
          costoUnitario: 2,
          unidadBase: "ml",
        } as never;
      return paginaPrueba([productoPrueba]) as never;
    });
  });
  it("no envía el ajuste hasta confirmar y usa las relaciones y factor reales", async () => {
    renderSeguridad(<PaginaAjustesInventario />, permisos);
    fireEvent.change(await screen.findByLabelText("Sucursal *"), {
      target: { value: "41" },
    });
    fireEvent.change(screen.getByLabelText("Bodega *"), {
      target: { value: "42" },
    });
    fireEvent.change(screen.getByLabelText("Ubicación *"), {
      target: { value: "43" },
    });
    const selectorProducto = screen.getByLabelText("Producto *");
    fireEvent.click(selectorProducto);
    fireEvent.click(
      await screen.findByRole("option", { name: "Helado de vainilla" }),
    );
    await waitFor(() =>
      expect(
        screen.getByLabelText("Presentación").hasAttribute("disabled"),
      ).toBe(false),
    );
    fireEvent.change(screen.getByLabelText("Presentación"), {
      target: { value: "23" },
    });
    fireEvent.change(screen.getByLabelText("Cantidad *"), {
      target: { value: "3" },
    });
    fireEvent.change(screen.getByLabelText("Costo unitario base"), {
      target: { value: "2" },
    });
    fireEvent.click(screen.getByRole("button", { name: "Revisar ajuste" }));
    const dialogo = await screen.findByRole("dialog");
    expect(dialogo.textContent).toContain("3,000 ml");
    expect(
      solicitarSimulado.mock.calls.some(
        ([, opciones]) => opciones?.method === "POST",
      ),
    ).toBe(false);
    fireEvent.click(within(dialogo).getByRole("button", { name: "Confirmar" }));
    await screen.findByRole("heading", { name: "Ajuste aplicado" });
    const llamada = solicitarSimulado.mock.calls.find(
      ([, opciones]) => opciones?.method === "POST",
    )!;
    expect(llamada[0]).toBe("/api/inventario/ajustes");
    expect(JSON.parse(String(llamada[1]?.body))).toMatchObject({
      sucursalId: 41,
      bodegaId: 42,
      ubicacionId: 43,
      productoId: 17,
      productoPresentacionId: 23,
      cantidad: 3,
      costoUnitario: 2,
    });
  });
  it("muestra estado vacío sin crear almacenes automáticamente", async () => {
    solicitarSimulado.mockResolvedValueOnce([] as never);
    renderSeguridad(<PaginaAjustesInventario />, permisos);
    await screen.findByText("No hay almacenes disponibles");
    expect(screen.queryByRole("button", { name: "Revisar ajuste" })).toBeNull();
    expect(solicitarSimulado).toHaveBeenCalledTimes(1);
  });
  it.each([
    [3, 1000, 3000],
    [2, 250, 500],
    [0, 1000, null],
    [-1, 250, null],
    [1, 0, null],
    [1, Infinity, null],
  ])(
    "calcula solamente una vista informativa para %s × %s",
    (cantidad, factor, esperado) => {
      expect(calcularCantidadBase(cantidad, factor)).toBe(esperado);
    },
  );
});
