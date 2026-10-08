/* oxlint-disable react/only-export-components */
import { Insignia } from "../../components/ui/Controles";
import type { MonedaCompra } from "../../types/compras";
import { mostrarFechaCivil } from "../../utils/fechas";

export function normalizarEstadoCompra(estado: string) {
  return estado.trim().toUpperCase().replaceAll(" ", "_");
}

export function EstadoCompraBadge({ estado }: { estado: string }) {
  const codigo = normalizarEstadoCompra(estado);
  const tono = codigo === "RECIBIDA"
    ? "positivo"
    : codigo === "PARCIALMENTE_RECIBIDA"
      ? "advertencia"
      : codigo === "CONFIRMADA"
        ? "info"
        : codigo === "ANULADA"
          ? "negativo"
          : "neutro";
  return <Insignia tono={tono}>{codigo.replaceAll("_", " ")}</Insignia>;
}

export function esBorrador(estado: string) {
  return normalizarEstadoCompra(estado) === "BORRADOR";
}

export function puedeRecibirse(estado: string) {
  const codigo = normalizarEstadoCompra(estado);
  return codigo === "CONFIRMADA" || codigo === "PARCIALMENTE_RECIBIDA";
}

export function formatoMoneda(valor: number, moneda?: MonedaCompra) {
  if (!moneda) {
    return new Intl.NumberFormat("es-GT", {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(valor);
  }
  return new Intl.NumberFormat("es-GT", {
    style: "currency",
    currency: moneda.codigoISO,
    currencyDisplay: "code",
    minimumFractionDigits: moneda.decimales,
    maximumFractionDigits: moneda.decimales,
  }).format(valor);
}

export function formatoCostoBase(valor: number, moneda?: MonedaCompra) {
  const numero = new Intl.NumberFormat("es-GT", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 6,
  }).format(valor);
  return moneda ? `${moneda.codigoISO} ${numero}` : numero;
}

export function fechaCorta(valor: string) {
  return mostrarFechaCivil(valor);
}
