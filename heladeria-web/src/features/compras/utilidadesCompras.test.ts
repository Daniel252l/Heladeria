import { describe, expect, it } from "vitest";
import type { MonedaCompra } from "../../types/compras";
import { formatoCostoBase } from "./utilidadesCompras";

const quetzal: MonedaCompra = {
  id: 1,
  codigoISO: "GTQ",
  nombre: "Quetzal",
  esMonedaBase: true,
  decimales: 2,
};

describe("formato de costo unitario base", () => {
  it.each([
    [0.0245, "GTQ 0.0245"],
    [0.025, "GTQ 0.025"],
    [0.022727, "GTQ 0.022727"],
    [1.5, "GTQ 1.50"],
  ])("conserva hasta seis decimales significativos para %s", (valor, esperado) => {
    expect(formatoCostoBase(valor, quetzal)).toBe(esperado);
  });
});
