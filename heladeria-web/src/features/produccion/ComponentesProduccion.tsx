import { Children, cloneElement, Fragment, isValidElement, type ReactNode } from "react";
import "./produccion.css";
import { Link } from "react-router-dom";
import { ErrorApi } from "../../api/clienteHttp";
import { Boton, Insignia, Tarjeta } from "../../components/ui/Controles";
import { EstadoError } from "../../components/ui/Estados";
import { PaginaSinPermiso } from "../../pages/PaginaSinPermiso";
import { PaginaNoEncontrada } from "../../pages/PaginaNoEncontrada";
import type { IngredienteReceta, RecetaResumen } from "../../types/produccion";
import { fechaProduccion, numeroProduccion as n } from "./utilidadesProduccion";

export function ErrorProduccion({ error, reintentar }: { error: Error; reintentar?: () => void }) {
  if (error instanceof ErrorApi && error.estado === 403) return <PaginaSinPermiso />;
  if (error instanceof ErrorApi && error.estado === 404) return <PaginaNoEncontrada />;
  return <EstadoError mensaje={error.message} reintentar={reintentar} />;
}
export function AvisoProduccion({ children, error = false }: { children: ReactNode; error?: boolean }) {
  return <div role={error ? "alert" : "status"} className={`rounded-xl border p-4 text-sm leading-6 ${error ? "border-rose-200 bg-rose-50 text-rose-800" : "border-teal-200 bg-teal-50 text-teal-900"}`}>{children}</div>;
}
export function EstadoProduccionBadge({ estado }: { estado: string }) {
  const codigo = estado.trim().toUpperCase();
  return <Insignia tono={codigo === "FINALIZADA" ? "positivo" : codigo === "CONFIRMADA" ? "info" : codigo === "ANULADA" ? "negativo" : "neutro"}>{codigo}</Insignia>;
}
export function VigenciaReceta({ receta }: { receta: RecetaResumen }) {
  return <div><Insignia tono={receta.esVigente ? "positivo" : "neutro"}>{receta.esVigente ? "Vigente" : "Histórica"}</Insignia><p className="mt-1 text-xs text-slate-500">{fechaProduccion(receta.vigenteDesde)}{receta.vigenteHasta ? ` — ${fechaProduccion(receta.vigenteHasta)}` : " · Sin cierre"}</p></div>;
}
export function DatoProduccion({ etiqueta, children }: { etiqueta: string; children: ReactNode }) {
  return <div><dt className="text-xs font-semibold uppercase tracking-wide text-slate-500">{etiqueta}</dt><dd className="mt-1.5 font-medium text-slate-900">{children}</dd></div>;
}
export function TablaProduccion({ columnas, children, etiqueta }: { columnas: string[]; children: ReactNode; etiqueta: string }) {
  // Un único DOM conserva acciones y foco al cambiar entre tabla y tarjetas.
  const etiquetar = (filas: ReactNode): ReactNode => Children.map(filas, (fila) => {
    if (!isValidElement<{ children?: ReactNode }>(fila)) return fila;
    if (fila.type === Fragment) return cloneElement(fila, {}, etiquetar(fila.props.children));
    if (fila.type !== "tr") return fila;
    return cloneElement(fila, {}, Children.map(fila.props.children, (celda, indice) =>
      isValidElement<{ colSpan?: number; "data-etiqueta"?: string }>(celda) && celda.type === "td"
        ? cloneElement(celda, { "data-etiqueta": celda.props.colSpan ? undefined : columnas[indice] }) : celda));
  });
  return <div className="overflow-x-auto"><table role="table" aria-label={etiqueta} className="tabla-produccion w-full sm:min-w-[44rem] divide-y divide-slate-200 text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr>{columnas.map((c) => <th key={c} scope="col" className="px-4 py-3">{c}</th>)}</tr></thead><tbody className="divide-y divide-slate-100 [&_td]:px-4 [&_td]:py-4">{etiquetar(children)}</tbody></table></div>;
}
export function IngredientesReceta({ detalles, escala = 1 }: { detalles: IngredienteReceta[]; escala?: number }) {
  return <Tarjeta className="overflow-hidden"><h2 className="border-b border-slate-200 p-5 font-bold">Ingredientes{escala !== 1 ? " escalados" : ""}</h2><TablaProduccion etiqueta="Ingredientes de la receta" columnas={["Ingrediente", "Presentación", "Cantidad", "Factor", "Cantidad base", "Observación"]}>{detalles.map((d) => <tr key={d.id}><td className="font-semibold">{d.productoInsumo}</td><td>{d.presentacion ?? "Unidad base"}</td><td>{escala !== 1 && <span className="text-slate-400">{n.format(d.cantidad)} {d.abreviaturaUnidad} → </span>}{n.format(d.cantidad * escala)} {d.abreviaturaUnidad}</td><td>× {n.format(d.factorConversion)}</td><td>{n.format(d.cantidadBase * escala)} {d.abreviaturaUnidadBase}</td><td>{d.observacion ?? "—"}</td></tr>)}</TablaProduccion></Tarjeta>;
}
export function PieFormularioProduccion({ volver, procesando, texto = "Guardar", disabled = false }: { volver: string; procesando: boolean; texto?: string; disabled?: boolean }) {
  return <footer className="sticky bottom-0 z-10 flex flex-wrap justify-end gap-3 rounded-xl border border-slate-200 bg-white/95 p-4 shadow-sm"><Link to={volver} className="rounded-lg px-4 py-2.5 text-sm font-semibold text-slate-600">Cancelar</Link><Boton type="submit" cargando={procesando} disabled={disabled}>{texto}</Boton></footer>;
}
