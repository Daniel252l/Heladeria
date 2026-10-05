import { Link, useParams } from "react-router-dom";
import { useState } from "react";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { PERMISOS } from "../../auth/permisos";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando } from "../../components/ui/Estados";
import { Tarjeta } from "../../components/ui/Controles";
import { Paginacion } from "../../components/ui/Paginacion";
import type { RecetaConsulta, RecetaResumen } from "../../types/produccion";
import type { Paginado } from "../../types/api";
import { useRecursoProduccion } from "./useDatosProduccion";
import { AvisoProduccion, DatoProduccion, ErrorProduccion, IngredientesReceta, VigenciaReceta } from "./ComponentesProduccion";
import { numeroProduccion as n } from "./utilidadesProduccion";

export function PaginaDetalleReceta() {
  const { id } = useParams();
  const consulta = useRecursoProduccion<RecetaConsulta>(`/api/recetas/${id}`);
  if (consulta.cargando) return <EstadoCargando />;
  if (consulta.error) return <ErrorProduccion error={consulta.error} reintentar={consulta.recargar} />;
  return consulta.datos ? <DetalleReceta receta={consulta.datos} /> : null;
}
function DetalleReceta({ receta }: { receta: RecetaConsulta }) {
  const { tienePermiso } = useAutenticacion();
  const r = receta.encabezado;
  const [pagina, setPagina] = useState(1);
  const historia = useRecursoProduccion<Paginado<RecetaResumen>>(`/api/recetas?pagina=${pagina}&tamanioPagina=10&productoTerminadoId=${r.productoTerminadoId}&codigo=${encodeURIComponent(r.codigo)}`);
  return <div className="space-y-6">
    <EncabezadoPagina titulo={r.nombre} descripcion={`${r.codigo} · ${r.productoTerminado} · v${r.version}`} acciones={<div className="flex gap-3"><Link to="/recetas" className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm">Volver</Link>{tienePermiso(PERMISOS.recetasAdministrar) && r.esVigente && <Link to={`/recetas/${r.id}/editar`} className="rounded-lg bg-teal-700 px-4 py-2 text-sm font-semibold text-white">Editar receta</Link>}</div>} />
    {receta.fueUtilizada && <AvisoProduccion>Esta receta ya fue utilizada en producción. Los cambios crearán una nueva versión y conservarán la versión histórica.</AvisoProduccion>}
    <Tarjeta className="p-5"><dl className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4"><DatoProduccion etiqueta="Producto terminado">{r.productoTerminado}</DatoProduccion><DatoProduccion etiqueta="Versión">v{r.version}</DatoProduccion><DatoProduccion etiqueta="Vigencia"><VigenciaReceta receta={r} /></DatoProduccion><DatoProduccion etiqueta="Estado">{r.estado}</DatoProduccion><DatoProduccion etiqueta="Rendimiento">{n.format(r.rendimiento)} {r.abreviaturaRendimiento}</DatoProduccion><DatoProduccion etiqueta="Presentación">{receta.presentacionRendimiento ?? "Unidad base"}</DatoProduccion><DatoProduccion etiqueta="Factor">× {n.format(r.factorConversion)}</DatoProduccion><DatoProduccion etiqueta="Rendimiento base">{n.format(r.rendimientoBase)} {receta.abreviaturaUnidadBase}</DatoProduccion></dl></Tarjeta>
    <IngredientesReceta detalles={receta.detalles} />
    <Tarjeta className="overflow-hidden"><h2 className="border-b border-slate-200 p-5 font-bold">Historial de versiones</h2>{historia.cargando ? <EstadoCargando /> : historia.error ? <ErrorProduccion error={historia.error} reintentar={historia.recargar} /> : <ol className="divide-y divide-slate-100">{historia.datos?.elementos.filter((v) => v.codigo === r.codigo).sort((a, b) => b.version - a.version).map((v) => <li key={v.id} className="flex flex-wrap items-center justify-between gap-3 px-5 py-4"><Link className="font-semibold text-teal-700" to={`/recetas/${v.id}`}>v{v.version} · {v.nombre}{v.id === r.id ? " · Viendo esta versión" : ""}</Link><VigenciaReceta receta={v} /></li>)}</ol>}{historia.datos && <Paginacion {...historia.datos} cambiar={setPagina} entidad="versión" />}</Tarjeta>
  </div>;
}
