import { Fragment, useState } from "react";
import { Link } from "react-router-dom";
import { BookOpen, ChevronDown, ChevronRight, Plus, Search, RotateCcw } from "lucide-react";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { PERMISOS } from "../../auth/permisos";
import { Boton, Entrada, Insignia, Seleccion, Tarjeta } from "../../components/ui/Controles";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { PanelFiltros } from "../../components/ui/PanelFiltros";
import { Paginacion } from "../../components/ui/Paginacion";
import { EstadoCargando, EstadoVacio } from "../../components/ui/Estados";
import type { Paginado } from "../../types/api";
import type { RecetaResumen, ReferenciasRecetas } from "../../types/produccion";
import { ErrorProduccion, TablaProduccion, VigenciaReceta } from "./ComponentesProduccion";
import { useRecursoProduccion } from "./useDatosProduccion";
import { numeroProduccion as n } from "./utilidadesProduccion";

const inicial = { busqueda: "", productoTerminadoId: "", estadoId: "", soloVigentes: "", version: "" };
export function PaginaRecetas() {
  const { tienePermiso } = useAutenticacion();
  const [filtros, setFiltros] = useState(inicial);
  const [aplicados, setAplicados] = useState(inicial);
  const [pagina, setPagina] = useState(1);
  const [cerrados, setCerrados] = useState<Set<string>>(new Set());
  const parametros = new URLSearchParams({ pagina: String(pagina), tamanioPagina: "10" });
  Object.entries(aplicados).forEach(([k, v]) => { if (v) parametros.set(k, v); });
  const listado = useRecursoProduccion<Paginado<RecetaResumen>>(`/api/recetas?${parametros}`);
  const refs = useRecursoProduccion<ReferenciasRecetas>("/api/recetas/referencias");
  const grupos = new Map<string, RecetaResumen[]>();
  for (const receta of listado.datos?.elementos ?? []) {
    const clave = `${receta.productoTerminadoId}:${receta.codigo}`;
    grupos.set(clave, [...(grupos.get(clave) ?? []), receta]);
  }
  return <div className="space-y-6">
    <EncabezadoPagina titulo="Recetas" descripcion="Rendimientos, ingredientes e historia de cada producto." icono={<BookOpen />} acciones={tienePermiso(PERMISOS.recetasAdministrar) ? <Link className="inline-flex items-center gap-2 rounded-lg bg-teal-700 px-4 py-3 text-sm font-semibold text-white" to="/recetas/nueva"><Plus className="h-4 w-4" /> Nueva receta</Link> : undefined} />
    <form onSubmit={(e) => { e.preventDefault(); setPagina(1); setAplicados(filtros); }}><PanelFiltros acciones={<><Boton type="submit"><Search className="h-4 w-4" /> Buscar recetas</Boton><Boton type="button" variante="secundario" onClick={() => { setFiltros(inicial); setAplicados(inicial); setPagina(1); }}><RotateCcw className="h-4 w-4" /> Limpiar filtros</Boton></>}>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <Entrada etiqueta="Código o nombre" value={filtros.busqueda} onChange={(e) => setFiltros({ ...filtros, busqueda: e.target.value })} />
        <SelectorEntidad etiqueta="Producto terminado" value={filtros.productoTerminadoId} opcionesLocales={refs.datos?.productos ?? []} opcional onChange={(id) => setFiltros({ ...filtros, productoTerminadoId: id ?? "" })} cargandoExterno={refs.cargando} />
        <Seleccion etiqueta="Estado" value={filtros.estadoId} onChange={(e) => setFiltros({ ...filtros, estadoId: e.target.value })}><option value="">Todos</option>{refs.datos?.estados.map((e) => <option key={e.id} value={e.id}>{e.nombre}</option>)}</Seleccion>
        <Seleccion etiqueta="Vigencia" value={filtros.soloVigentes} onChange={(e) => setFiltros({ ...filtros, soloVigentes: e.target.value })}><option value="">Todas</option><option value="true">Vigentes</option><option value="false">Históricas</option></Seleccion>
        <Entrada etiqueta="Versión" type="number" min={1} step={1} value={filtros.version} onChange={(e) => setFiltros({ ...filtros, version: e.target.value })} />
      </div>
      {refs.error && <ErrorProduccion error={refs.error} reintentar={refs.recargar} />}
    </PanelFiltros></form>
    {listado.cargando ? <EstadoCargando /> : listado.error ? <ErrorProduccion error={listado.error} reintentar={listado.recargar} /> : <Tarjeta className="overflow-hidden">
      {!listado.datos?.elementos.length ? <EstadoVacio titulo="No se encontraron recetas" /> : <TablaProduccion etiqueta="Listado de recetas" columnas={["Código / nombre", "Producto", "Versión", "Rendimiento", "Vigencia", "Estado", "Acciones"]}>
        {[...grupos].map(([clave, versiones]) => <Fragment key={clave}><tr className="bg-slate-50"><td colSpan={7}><button type="button" className="flex items-center gap-2 font-semibold text-slate-800" aria-expanded={!cerrados.has(clave)} aria-label={`Versiones de ${versiones[0].codigo}`} onClick={() => setCerrados((prev) => { const next = new Set(prev); if (next.has(clave)) next.delete(clave); else next.add(clave); return next; })}>{cerrados.has(clave) ? <ChevronRight className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}{versiones[0].codigo} · {versiones[0].productoTerminado}<span className="text-xs font-normal text-slate-500">{versiones.length} en esta página</span></button></td></tr>
          {!cerrados.has(clave) && versiones.sort((a, b) => b.version - a.version).map((r) => <tr key={r.id}><td><span className="pl-4 font-semibold">└ {r.nombre}</span></td><td>{r.productoTerminado}</td><td>v{r.version}</td><td className="whitespace-nowrap">{n.format(r.rendimiento)} {r.abreviaturaRendimiento}</td><td><VigenciaReceta receta={r} /></td><td><Insignia tono={r.estado.toLowerCase() === "activo" ? "positivo" : "neutro"}>{r.estado}</Insignia></td><td><div className="flex gap-3"><Link className="font-semibold text-teal-700" to={`/recetas/${r.id}`}>Ver</Link>{tienePermiso(PERMISOS.recetasAdministrar) && r.esVigente && <Link className="font-semibold text-amber-700" to={`/recetas/${r.id}/editar`}>Editar</Link>}</div></td></tr>)}
        </Fragment>)}
      </TablaProduccion>}
      {listado.datos && <Paginacion {...listado.datos} cambiar={setPagina} entidad="receta" />}
    </Tarjeta>}
  </div>;
}
