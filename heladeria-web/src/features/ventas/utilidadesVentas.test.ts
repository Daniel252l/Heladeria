import { describe, expect, it } from "vitest";
import { ErrorApi } from "../../api/clienteHttp";
import { productoPos, ventaConfirmada } from "../../test/datosFaseVisual5";
import { agregarLinea, cantidadValida, costoBaseVenta, descuentoValido, dineroVenta, mensajeVenta, previewCarrito, previewLinea } from "./utilidadesVentas";

describe("reglas exclusivamente visuales de ventas", () => {
  it.each([[0.0245, "GTQ 0.0245"], [0.025, "GTQ 0.025"], [0.022727, "GTQ 0.022727"], [1.5, "GTQ 1.50"]] as const)("conserva costo base %s", (valor, esperado) => expect(costoBaseVenta(valor, "GTQ")).toBe(esperado));
  it("mantiene moneda comercial a dos decimales y null como raya", () => { expect(dineroVenta(25, "GTQ")).toBe("GTQ 25.00"); expect(costoBaseVenta(null, "GTQ")).toBe("—"); });
  it.each(["", "0", "-1", "1.1234567", "NaN", "Infinity"])("rechaza cantidad inválida %s", (valor) => expect(cantidadValida(valor)).toBe(false));
  it.each(["1", "0.000001", "1.5"])("acepta cantidad válida %s", (valor) => expect(cantidadValida(valor)).toBe(true));
  it.each(["-1", "100.01", "2.333", ""])("rechaza descuento inválido %s", (valor) => expect(descuentoValido(valor)).toBe(false));
  it("consolida misma presentación y conserva líneas de distintas unidades", () => {
    const una = agregarLinea([], productoPos, productoPos.presentaciones[1]);
    const dos = agregarLinea(una, productoPos, productoPos.presentaciones[1]);
    const tres = agregarLinea(dos, productoPos, productoPos.presentaciones[2]);
    expect(tres).toHaveLength(2); expect(tres.map((l) => l.cantidad)).toEqual(["2", "1"]);
  });
  it("no agrega precio inválido", () => expect(agregarLinea([], productoPos, { ...productoPos.presentaciones[1], precioValido: false })).toEqual([]));
  it("preview de impuesto incluido y adicional sigue la semántica de backend", () => {
    const producto = { ...productoPos, impuestos: [{ id: 1, codigo: "IVA", nombre: "IVA", porcentaje: 12, incluidoEnPrecio: true }, { id: 2, codigo: "AD", nombre: "Adicional", porcentaje: 3, incluidoEnPrecio: false }] };
    const linea = { producto, presentacion: { ...productoPos.presentaciones[1], precio: 112 }, cantidad: "2", descuento: "10" };
    expect(previewLinea(linea)).toEqual({ subtotal: 200, descuento: 20, impuesto: 27, total: 207 });
    expect(previewCarrito([linea, linea])).toEqual({ subtotal: 400, descuento: 40, impuesto: 54, total: 414 });
  });
  it("resuelve productoId de stock insuficiente sin inventar disponibilidad", () => {
    expect(mensajeVenta(new ErrorApi("Existencia insuficiente para producto 1. Ninguna línea fue descontada.", 409, { codigo: "EXISTENCIA_INSUFICIENTE" }), ventaConfirmada.detalles)).toBe("Existencia insuficiente para Helado Chocolate. Ninguna línea fue descontada.");
  });
});
