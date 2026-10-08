import { formatoCostoBase, formatoMoneda } from "../compras/utilidadesCompras";
import type { MonedaCompra } from "../../types/compras";
import type { PresentacionesProduccion } from "../../types/produccion";
import { fechaHoraLocal, mostrarFechaCivil, mostrarInstante } from "../../utils/fechas";

const quetzal = { codigoISO: "GTQ", decimales: 2 } as MonedaCompra;
export const numeroProduccion = new Intl.NumberFormat("es-GT", { maximumFractionDigits: 6 });
export const dineroProduccion = (valor: number) => formatoMoneda(valor, quetzal);
export const costoBaseProduccion = (valor: number) => formatoCostoBase(valor, quetzal);
export const cantidadValida = (valor: number) => Number.isFinite(valor) && valor > 0 && valor < 1e13;
export function fechaProduccion(valor: string | null) {
  if (!valor) return "—";
  return valor.length === 10 ? mostrarFechaCivil(valor) : mostrarInstante(valor, false);
}
export const fechaLocal = fechaHoraLocal;
export function medidaProduccion(datos: PresentacionesProduccion | undefined, id: number | null) {
  if (!datos?.unidadBaseId) return null;
  if (id === null) return { factor: 1, unidad: datos.unidadBaseAbreviatura, nombre: "Unidad base" };
  const p = datos.presentaciones.find((item) => item.productoPresentacionId === id);
  return p && p.factorConversion > 0 ? { factor: p.factorConversion, unidad: p.unidadAbreviatura, nombre: p.presentacionNombre } : null;
}
export function opcionesPresentacion(datos: PresentacionesProduccion | undefined) {
  if (!datos?.unidadBaseId) return [];
  return [{ id: "base", nombre: `Unidad base · ${datos.unidadBaseAbreviatura} · × 1` },
    ...datos.presentaciones.map((p) => ({ id: p.productoPresentacionId, nombre: `${p.presentacionNombre} · ${p.unidadAbreviatura} · × ${numeroProduccion.format(p.factorConversion)}` }))];
}
