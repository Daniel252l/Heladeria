import { ErrorApi } from "../../api/clienteHttp";
import { formatoCosto, formatoNumero, mostrarFecha } from "../../utils/formato";
import type { LineaCarrito, PresentacionPos, ProductoPos, VentaLinea } from "../../types/ventas";

export const numeroVenta = formatoNumero;
const monetario = new Intl.NumberFormat("es-GT", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
export const dineroVenta = (valor: number | null, moneda: string) => valor === null ? "—" : `${moneda} ${monetario.format(valor)}`;
export const costoBaseVenta = (valor: number | null, moneda: string) => valor === null ? "—" : `${moneda} ${formatoCosto.format(valor)}`;
export const fechaVenta = (valor: string | null) => valor ? mostrarFecha(valor) : "—";
export const clavePresentacion = (p: PresentacionPos) => p.productoPresentacionId === null ? "base" : String(p.productoPresentacionId);
export const claveLinea = (l: LineaCarrito) => `${l.producto.id}:${clavePresentacion(l.presentacion)}`;
export const nombrePresentacion = (p: PresentacionPos) => `${p.nombre} · ${p.abreviatura} · × ${formatoNumero.format(p.factorConversion)}`;
export const precioUtilizable = (p: PresentacionPos) => p.precioValido && p.precio !== null && p.precio > 0;
export const cantidadValida = (v: string) => /^\d+(\.\d{1,6})?$/.test(v) && Number(v) > 0 && Number(v) < 1e13;
export const descuentoValido = (v: string) => /^\d+(\.\d{1,2})?$/.test(v) && Number(v) >= 0 && Number(v) <= 100;
export function agregarLinea(lineas: LineaCarrito[], producto: ProductoPos, presentacion: PresentacionPos): LineaCarrito[] {
  if (!precioUtilizable(presentacion)) return lineas;
  const nueva = { producto, presentacion, cantidad: "1", descuento: "0" };
  const clave = claveLinea(nueva);
  if (!lineas.some((l) => claveLinea(l) === clave)) return [...lineas, nueva];
  return lineas.map((l) => claveLinea(l) === clave ? { ...l, cantidad: String(Number((Number(l.cantidad || 0) + 1).toFixed(6))) } : l);
}
// Aproximación visual de CalculadoraVenta. Nunca se envía ni sustituye importes del servidor.
const redondear = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;
export function previewLinea(l: LineaCarrito) {
  const bruto = redondear(Number(l.cantidad || 0) * (l.presentacion.precio ?? 0));
  const descuentoCatalogo = redondear(bruto * Number(l.descuento || 0) / 100);
  const tasa = l.producto.impuestos.find((i) => i.incluidoEnPrecio)?.porcentaje ?? 0;
  const subtotal = redondear(bruto / (1 + tasa / 100));
  const base = redondear((bruto - descuentoCatalogo) / (1 + tasa / 100));
  const impuesto = redondear(l.producto.impuestos.reduce((s, i) => s + (i.incluidoEnPrecio ? bruto - descuentoCatalogo - base : redondear(base * i.porcentaje / 100)), 0));
  return { subtotal, descuento: redondear(subtotal - base), impuesto, total: redondear(base + impuesto) };
}
export function previewCarrito(lineas: LineaCarrito[]) {
  return lineas.reduce((s, l) => { const v = previewLinea(l); return { subtotal: redondear(s.subtotal + v.subtotal), descuento: redondear(s.descuento + v.descuento), impuesto: redondear(s.impuesto + v.impuesto), total: redondear(s.total + v.total) }; }, { subtotal: 0, descuento: 0, impuesto: 0, total: 0 });
}
export function mensajeVenta(error: Error, lineas: VentaLinea[] = []) {
  if (error instanceof ErrorApi && error.codigo === "EXISTENCIA_INSUFICIENTE") {
    return error.message.replace(/producto\s+(\d+)/gi, (texto, id: string) => lineas.find((l) => l.productoId === Number(id))?.producto ?? texto);
  }
  return error.message;
}
export function esConcurrencia(error: Error | null) {
  return error instanceof ErrorApi && error.estado === 409 && /versi[oó]n|modificad|concurrencia|recargue/i.test(error.message);
}
