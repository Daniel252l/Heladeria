import { Children, cloneElement, isValidElement, type ReactNode } from "react";
import { ErrorApi } from "../../api/clienteHttp";
import { Insignia } from "../../components/ui/Controles";
import { EstadoError } from "../../components/ui/Estados";
import { PaginaSinPermiso } from "../../pages/PaginaSinPermiso";
import { PaginaNoEncontrada } from "../../pages/PaginaNoEncontrada";
import { dineroVenta } from "./utilidadesVentas";
import "./ventas.css";

export const enlaceVenta = "inline-flex min-h-11 items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600";
export const enlaceVentaPrimario = `${enlaceVenta} !border-teal-700 !bg-teal-700 !text-white hover:!bg-teal-800`;
export function EstadoVenta({ estado }: { estado: string }) {
  return <Insignia tono={estado === "COMPLETADA" ? "positivo" : estado === "CONFIRMADA" ? "info" : estado === "ANULADA" ? "negativo" : "neutro"}>{estado}</Insignia>;
}
export function ErrorVenta({ error, recargar }: { error: Error; recargar?: () => void }) {
  if (error instanceof ErrorApi && error.estado === 403) return <PaginaSinPermiso />;
  if (error instanceof ErrorApi && error.estado === 404) return <PaginaNoEncontrada />;
  return <EstadoError mensaje={error.message} reintentar={recargar} />;
}
export function AvisoVenta({ children, error = false }: { children: ReactNode; error?: boolean }) {
  return <div role={error ? "alert" : "status"} className={`break-words rounded-xl border p-4 text-sm leading-6 ${error ? "border-rose-200 bg-rose-50 text-rose-800" : "border-teal-200 bg-teal-50 text-teal-900"}`}>{children}</div>;
}
export function DatoVenta({ etiqueta, children }: { etiqueta: string; children: ReactNode }) {
  return <div className="min-w-0"><dt className="text-xs font-semibold uppercase tracking-wide text-slate-500">{etiqueta}</dt><dd className="mt-1.5 break-words font-medium text-slate-900">{children ?? "—"}</dd></div>;
}
export function TotalesVenta({ subtotal, descuento, impuesto, total, moneda, preview = false }: { subtotal: number; descuento: number; impuesto: number; total: number; moneda: string; preview?: boolean }) {
  return <section aria-label={preview ? "Totales estimados" : "Totales de la venta"} className="space-y-3">
    <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">{preview ? "Vista previa · no definitiva" : "Importes calculados por el servidor"}</p>
    <dl className="space-y-2 text-sm">{[["Subtotal", subtotal], ["Descuento", descuento], ["Impuesto", impuesto]].map(([nombre, valor]) => <div key={nombre} className="flex flex-wrap justify-between gap-2"><dt className="text-slate-500">{nombre}</dt><dd>{dineroVenta(Number(valor), moneda)}</dd></div>)}
      <div className="flex flex-wrap items-center justify-between gap-2 border-t border-slate-200 pt-3 text-xl font-bold text-slate-900"><dt>Total</dt><dd>{dineroVenta(total, moneda)}</dd></div>
    </dl>
  </section>;
}
export function TablaVentas({ columnas, etiqueta, children }: { columnas: string[]; etiqueta: string; children: ReactNode }) {
  const filas = Children.map(children, (fila) => !isValidElement<{ children?: ReactNode }>(fila) ? fila : cloneElement(fila, {}, Children.map(fila.props.children, (celda, i) => isValidElement<{ "data-etiqueta"?: string }>(celda) ? cloneElement(celda, { "data-etiqueta": columnas[i] }) : celda)));
  return <div className="overflow-x-auto"><table role="table" aria-label={etiqueta} className="tabla-ventas w-full text-left text-sm sm:min-w-[48rem]"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr>{columnas.map((c) => <th scope="col" key={c} className="px-4 py-3">{c}</th>)}</tr></thead><tbody className="divide-y divide-slate-100 [&_td]:px-4 [&_td]:py-4">{filas}</tbody></table></div>;
}
