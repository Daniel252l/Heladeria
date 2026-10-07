import { act, fireEvent, screen, waitFor } from "@testing-library/react";
import { useState } from "react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { renderSeguridad } from "../../test/renderSeguridad";
import { ventaAnulada, ventaCompletada, ventaConfirmada } from "../../test/datosFaseVisual5";
import { claveIntentoVenta, completarConReconciliacion, useRecursoVentas } from "./useDatosVentas";

vi.mock("../../api/clienteHttp", async (original) => ({ ...(await original<typeof import("../../api/clienteHttp")>()), solicitar: vi.fn() }));
const api = vi.mocked(solicitar);
beforeEach(() => { api.mockReset(); sessionStorage.clear(); });
afterEach(() => vi.restoreAllMocks());

describe("cierre seguro de ventas", () => {
  it("la clave se aísla por usuario, empresa, sucursal y venta", () => {
    const claves = [claveIntentoVenta(1, 4, 4, 2), claveIntentoVenta(2, 4, 4, 2), claveIntentoVenta(1, 5, 4, 2), claveIntentoVenta(1, 4, 6, 2), claveIntentoVenta(1, 4, 4, 3)];
    expect(new Set(claves).size).toBe(5);
  });
  it("si no puede persistir la UUID aborta antes de enviar", async () => {
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => { throw new Error("Storage bloqueado"); });
    await expect(completarConReconciliacion(ventaConfirmada, "intento")).rejects.toThrow("No es posible conservar");
    expect(api).not.toHaveBeenCalled();
  });
  it.each([ventaCompletada, ventaAnulada])("reintento consulta $encabezado.estado y nunca repite cierre", async (venta) => {
    sessionStorage.setItem("intento", "7e4cb0d2-b64f-4a59-a4e3-5d6c248b7202");
    api.mockResolvedValue(venta);
    expect(await completarConReconciliacion(ventaConfirmada, "intento")).toBe(venta);
    expect(api).toHaveBeenCalledTimes(1); expect(api.mock.calls[0][1]?.method).toBeUndefined();
  });
  it("si la consulta de reconciliación detecta nueva RowVersion no sobrescribe", async () => {
    sessionStorage.setItem("intento", "7e4cb0d2-b64f-4a59-a4e3-5d6c248b7202");
    api.mockResolvedValue({ ...ventaConfirmada, encabezado: { ...ventaConfirmada.encabezado, versionFila: "OTRA" } });
    await expect(completarConReconciliacion(ventaConfirmada, "intento")).rejects.toThrow("La venta cambió");
    expect(api.mock.calls.some(([, o]) => o?.method)).toBe(false);
  });
  it("403 durante reconciliación conserva UUID y no envía POST", async () => {
    const uuid = "7e4cb0d2-b64f-4a59-a4e3-5d6c248b7202";
    sessionStorage.setItem("intento", uuid); api.mockRejectedValue(new ErrorApi("Sin permiso", 403));
    await expect(completarConReconciliacion(ventaConfirmada, "intento")).rejects.toMatchObject({ estado: 403 });
    expect(sessionStorage.getItem("intento")).toBe(uuid); expect(api.mock.calls.some(([, o]) => o?.method)).toBe(false);
  });
  it("un 500 se trata como respuesta incierta y solo consulta, sin reintento automático", async () => {
    api.mockRejectedValueOnce(new ErrorApi("Servicio no disponible", 500)).mockResolvedValueOnce(ventaConfirmada);
    await expect(completarConReconciliacion(ventaConfirmada, "intento")).rejects.toThrow("La clave se conservó");
    expect(api).toHaveBeenCalledTimes(2); expect(api.mock.calls.filter(([, o]) => o?.method)).toHaveLength(1);
  });
});

function Resultado({ ruta }: { ruta: string }) {
  const resultado = useRecursoVentas<{ nombre: string }>(ruta);
  return <p>{resultado.cargando ? "Cargando" : resultado.datos?.nombre}</p>;
}
function CambiarConsulta() {
  const [ruta, setRuta] = useState("/catalogo-anterior");
  return <><button onClick={() => setRuta("/catalogo-actual")}>Cambiar búsqueda</button><Resultado ruta={ruta} /></>;
}
it("cancela consultas y no reemplaza catálogo actual con respuesta asíncrona anterior", async () => {
  let terminar!: (v: { nombre: string }) => void;
  const pendiente = new Promise<{ nombre: string }>((resolve) => { terminar = resolve; });
  api.mockImplementation(async (ruta) => ruta === "/catalogo-anterior" ? pendiente as never : { nombre: "Resultado actual" } as never);
  const vista = renderSeguridad(<CambiarConsulta />, []);
  fireEvent.click(screen.getByText("Cambiar búsqueda")); await screen.findByText("Resultado actual");
  await act(async () => terminar({ nombre: "Resultado obsoleto" }));
  expect(screen.queryByText("Resultado obsoleto")).toBeNull(); expect(api.mock.calls[0][1]?.signal?.aborted).toBe(true);
  await waitFor(() => expect(api).toHaveBeenCalledTimes(2));
  vista.unmount(); expect(api.mock.calls[1][1]?.signal?.aborted).toBe(true);
});
