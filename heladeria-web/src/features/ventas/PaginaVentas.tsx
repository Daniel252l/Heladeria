import { useState } from "react";
import { Link } from "react-router-dom";
import { Monitor, Plus, ReceiptText, RotateCcw, Search } from "lucide-react";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { Boton, Entrada, Seleccion, Tarjeta } from "../../components/ui/Controles";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando, EstadoVacio } from "../../components/ui/Estados";
import { PanelFiltros } from "../../components/ui/PanelFiltros";
import { Paginacion } from "../../components/ui/Paginacion";
import type { Paginado } from "../../types/api";
import type { ReferenciaVenta, VentaResumen } from "../../types/ventas";
import { enlaceVenta, enlaceVentaPrimario, ErrorVenta, EstadoVenta, TablaVentas } from "./ComponentesVentas";
import { useRecursoVentas } from "./useDatosVentas";
import { dineroVenta, fechaVenta } from "./utilidadesVentas";

const inicial = { busqueda: "", estadoId: "", fechaDesde: "", fechaHasta: "" };
export function PaginaVentas() {
  const { tienePermiso } = useAutenticacion();
  const [filtros, setFiltros] = useState(inicial);
  const [aplicados, setAplicados] = useState(inicial);
  const [pagina, setPagina] = useState(1);
  const [tamanio, setTamanio] = useState(10);
  const [errorFiltro, setErrorFiltro] = useState("");
  const parametros = new URLSearchParams({ pagina: String(pagina), tamanioPagina: String(tamanio) });
  Object.entries(aplicados).forEach(([k, v]) => { if (v) parametros.set(k, k === "fechaHasta" ? `${v}T23:59:59.999` : v); });
  const listado = useRecursoVentas<Paginado<VentaResumen>>(`/api/ventas?${parametros}`);
  const estados = useRecursoVentas<ReferenciaVenta[]>("/api/ventas/estados");
  const operar = tienePermiso(PERMISOS.posOperar);
  const administrar = tienePermiso(PERMISOS.ventasAdministrar) && operar;
  return <div className="ventas-vista space-y-6">
    <EncabezadoPagina titulo="Ventas" descripcion="Consulta y seguimiento de ventas en la sucursal activa." icono={<ReceiptText />} acciones={<div className="flex flex-wrap gap-2">{operar && <Link className={enlaceVenta} to="/pos"><Monitor className="h-4 w-4" /> Abrir POS</Link>}{administrar && <Link className={enlaceVentaPrimario} to="/ventas/nueva"><Plus className="h-4 w-4" /> Nueva venta</Link>}</div>} />
    <form onSubmit={(e) => { e.preventDefault(); if (filtros.fechaDesde && filtros.fechaHasta && filtros.fechaDesde > filtros.fechaHasta) { setErrorFiltro("La fecha inicial no puede ser posterior a la final."); return; } setErrorFiltro(""); setAplicados(filtros); setPagina(1); }}>
      <PanelFiltros acciones={<><Boton type="submit"><Search className="h-4 w-4" /> Buscar ventas</Boton><Boton type="button" variante="secundario" onClick={() => { setFiltros(inicial); setAplicados(inicial); setPagina(1); setErrorFiltro(""); }}><RotateCcw className="h-4 w-4" /> Limpiar filtros</Boton></>}>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <Entrada etiqueta="Número o cliente" value={filtros.busqueda} maxLength={150} onChange={(e) => setFiltros({ ...filtros, busqueda: e.target.value })} placeholder="Buscar venta…" />
          <Seleccion etiqueta="Estado" value={filtros.estadoId} disabled={estados.cargando || !!estados.error} onChange={(e) => setFiltros({ ...filtros, estadoId: e.target.value })}><option value="">Todos</option>{estados.datos?.map((s) => <option key={s.id} value={s.id}>{s.codigo}</option>)}</Seleccion>
          <Entrada etiqueta="Fecha desde" type="date" value={filtros.fechaDesde} onChange={(e) => setFiltros({ ...filtros, fechaDesde: e.target.value })} />
          <Entrada etiqueta="Fecha hasta" type="date" value={filtros.fechaHasta} onChange={(e) => setFiltros({ ...filtros, fechaHasta: e.target.value })} />
        </div>{errorFiltro && <p role="alert" className="mt-3 text-sm text-rose-700">{errorFiltro}</p>}{estados.error && <ErrorVenta error={estados.error} recargar={estados.recargar} />}
      </PanelFiltros>
    </form>
    {listado.cargando ? <EstadoCargando /> : listado.error ? <ErrorVenta error={listado.error} recargar={listado.recargar} /> : <Tarjeta className="overflow-hidden">
      <h2 className="border-b border-slate-200 p-5 font-bold">Historial de ventas</h2>
      {!listado.datos?.elementos.length ? <EstadoVacio titulo="No se encontraron ventas" descripcion="Pruebe con otros filtros o cree una nueva venta." /> : <TablaVentas etiqueta="Listado de ventas" columnas={["Número", "Fecha", "Cliente", "Total", "Moneda", "Estado", "Usuario", "Acciones"]}>{listado.datos.elementos.map((v) => <tr key={v.id}>
        <td className="font-semibold">{v.numeroVenta}</td><td>{fechaVenta(v.fechaVenta)}</td><td>{v.cliente ?? "Sin cliente identificado"}</td><td className="font-semibold">{dineroVenta(v.total, v.moneda)}</td><td>{v.moneda}</td><td><EstadoVenta estado={v.estado} /></td><td>{v.usuario}</td>
        <td><div className="flex flex-wrap gap-3"><Link to={`/ventas/${v.id}`} className="font-semibold text-teal-700">Ver detalle</Link>{v.estado === "BORRADOR" && administrar && <Link to={`/ventas/${v.id}/editar`} className="font-semibold text-amber-700">Editar</Link>}</div></td>
      </tr>)}</TablaVentas>}
      {listado.datos && <Paginacion {...listado.datos} cambiar={setPagina} cambiarTamanio={(n) => { setTamanio(n); setPagina(1); }} entidad="venta" />}
    </Tarjeta>}
  </div>;
}
