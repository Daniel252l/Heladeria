import { useState } from "react";
import { Link } from "react-router-dom";
import { Factory, Plus, Search, RotateCcw } from "lucide-react";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { Boton, Entrada, Seleccion, Tarjeta } from "../../components/ui/Controles";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { PanelFiltros } from "../../components/ui/PanelFiltros";
import { Paginacion } from "../../components/ui/Paginacion";
import { EstadoCargando, EstadoVacio } from "../../components/ui/Estados";
import type { Paginado } from "../../types/api";
import type { OrdenProduccionResumen, ReferenciasProduccion } from "../../types/produccion";
import { ErrorProduccion, EstadoProduccionBadge, TablaProduccion } from "./ComponentesProduccion";
import { useRecursoProduccion } from "./useDatosProduccion";
import { fechaProduccion, numeroProduccion as n } from "./utilidadesProduccion";

const inicial = { busqueda: "", productoId: "", recetaId: "", estadoId: "", fechaDesde: "", fechaHasta: "" };
export function PaginaProduccion() {
  const { tienePermiso } = useAutenticacion();
  const [filtros, setFiltros] = useState(inicial);
  const [aplicados, setAplicados] = useState(inicial);
  const [pagina, setPagina] = useState(1);
  const parametros = new URLSearchParams({ pagina: String(pagina), tamanioPagina: "10" });
  Object.entries(aplicados).forEach(([k, v]) => { if (v) parametros.set(k, k === "fechaHasta" ? `${v}T23:59:59.999` : v); });
  const listado = useRecursoProduccion<Paginado<OrdenProduccionResumen>>(`/api/produccion?${parametros}`);
  const refs = useRecursoProduccion<ReferenciasProduccion>("/api/produccion/referencias");
  const [errorFiltro, setErrorFiltro] = useState("");
  return <div className="space-y-6"><EncabezadoPagina titulo="Órdenes de producción" descripcion="Planificación, consumos y costos de elaboración." icono={<Factory />} acciones={tienePermiso(PERMISOS.produccionAdministrar) ? <Link className="inline-flex items-center gap-2 rounded-lg bg-teal-700 px-4 py-3 text-sm font-semibold text-white" to="/produccion/nueva"><Plus className="h-4 w-4" /> Nueva orden de producción</Link> : undefined} />
    <form onSubmit={(e) => { e.preventDefault(); if (filtros.fechaDesde && filtros.fechaHasta && filtros.fechaDesde > filtros.fechaHasta) { setErrorFiltro("La fecha inicial no puede ser posterior a la final."); return; } setErrorFiltro(""); setAplicados(filtros); setPagina(1); }}><PanelFiltros acciones={<><Boton type="submit"><Search className="h-4 w-4" /> Buscar producción</Boton><Boton type="button" variante="secundario" onClick={() => { setFiltros(inicial); setAplicados(inicial); setPagina(1); setErrorFiltro(""); }}><RotateCcw className="h-4 w-4" /> Limpiar filtros</Boton></>}><div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
      <Entrada etiqueta="Número de orden" value={filtros.busqueda} onChange={(e) => setFiltros({ ...filtros, busqueda: e.target.value })} />
      <SelectorEntidad etiqueta="Producto" ruta="/api/produccion/productos" opcional value={filtros.productoId} onChange={(id) => setFiltros({ ...filtros, productoId: id ?? "" })} />
      <SelectorEntidad etiqueta="Receta" ruta="/api/produccion/recetas" opcional value={filtros.recetaId} onChange={(id) => setFiltros({ ...filtros, recetaId: id ?? "" })} />
      <Seleccion etiqueta="Estado" disabled={refs.cargando || !!refs.error} value={filtros.estadoId} onChange={(e) => setFiltros({ ...filtros, estadoId: e.target.value })}><option value="">{refs.cargando ? "Cargando estados…" : "Todos"}</option>{refs.datos?.estados.map((estado) => <option key={estado.id} value={estado.id}>{estado.nombre}</option>)}</Seleccion>
      <Entrada etiqueta="Fecha desde" type="date" value={filtros.fechaDesde} onChange={(e) => setFiltros({ ...filtros, fechaDesde: e.target.value })} />
      <Entrada etiqueta="Fecha hasta" type="date" value={filtros.fechaHasta} onChange={(e) => setFiltros({ ...filtros, fechaHasta: e.target.value })} />
    </div>{errorFiltro && <p role="alert" className="mt-3 text-sm text-rose-700">{errorFiltro}</p>}{refs.error && <ErrorProduccion error={refs.error} reintentar={refs.recargar} />}</PanelFiltros></form>
    {listado.cargando ? <EstadoCargando /> : listado.error ? <ErrorProduccion error={listado.error} reintentar={listado.recargar} /> : <Tarjeta className="overflow-hidden">{!listado.datos?.elementos.length ? <EstadoVacio titulo="No se encontraron órdenes" /> : <TablaProduccion etiqueta="Listado de producción" columnas={["Orden", "Producto", "Receta", "Versión", "Planificada", "Estado", "Fecha", "Responsable", "Acciones"]}>{listado.datos.elementos.map((o) => <tr key={o.id}><td className="font-semibold">{o.numeroOrden}</td><td>{o.producto}</td><td>{o.receta}</td><td>v{o.recetaVersion}</td><td className="whitespace-nowrap">{n.format(o.cantidadPlanificada)} {o.abreviaturaUnidadPlanificada}</td><td><EstadoProduccionBadge estado={o.estado} /></td><td>{fechaProduccion(o.fechaInicio)}</td><td>{o.responsable}</td><td><div className="flex gap-3"><Link className="font-semibold text-teal-700" to={`/produccion/${o.id}`}>Ver</Link>{o.estado === "BORRADOR" && tienePermiso(PERMISOS.produccionAdministrar) && <Link className="font-semibold text-amber-700" to={`/produccion/${o.id}/editar`}>Editar</Link>}{o.estado === "CONFIRMADA" && tienePermiso(PERMISOS.produccionProducir) && <Link className="font-semibold text-teal-700" to={`/produccion/${o.id}/finalizar`}>Finalizar</Link>}</div></td></tr>)}</TablaProduccion>}{listado.datos && <Paginacion {...listado.datos} cambiar={setPagina} entidad="orden" />}</Tarjeta>}
  </div>;
}
