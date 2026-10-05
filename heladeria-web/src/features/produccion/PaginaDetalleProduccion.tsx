import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { solicitar } from "../../api/clienteHttp";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { PERMISOS } from "../../auth/permisos";
import { Boton, Tarjeta } from "../../components/ui/Controles";
import { DialogoConfirmacion } from "../../components/ui/DialogoConfirmacion";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando, EstadoVacio } from "../../components/ui/Estados";
import type { OrdenProduccionConsulta } from "../../types/produccion";
import { useOperacionProduccion, useRecursoProduccion } from "./useDatosProduccion";
import { AvisoProduccion, DatoProduccion, ErrorProduccion, EstadoProduccionBadge, TablaProduccion } from "./ComponentesProduccion";
import { costoBaseProduccion, dineroProduccion, fechaProduccion, numeroProduccion as n } from "./utilidadesProduccion";

export function PaginaDetalleProduccion() {
  const { id } = useParams();
  const consulta = useRecursoProduccion<OrdenProduccionConsulta>(`/api/produccion/${id}`);
  if (consulta.cargando) return <EstadoCargando />;
  if (consulta.error) return <ErrorProduccion error={consulta.error} reintentar={consulta.recargar} />;
  return consulta.datos ? <DetalleProduccion key={`${id}:${consulta.datos.encabezado.versionFila}`} inicial={consulta.datos} /> : null;
}
export function DetalleProduccion({ inicial }: { inicial: OrdenProduccionConsulta }) {
  const [orden, setOrden] = useState(inicial);
  const [accion, setAccion] = useState<"confirmar" | "anular" | null>(null);
  const { tienePermiso } = useAutenticacion();
  const operacion = useOperacionProduccion();
  const o = orden.encabezado;
  const administrar = tienePermiso(PERMISOS.produccionAdministrar);
  const puedeAnular = administrar && ["BORRADOR", "CONFIRMADA"].includes(o.estado) && !orden.consumos.length && !orden.produccionTerminada && !orden.movimientoIds.length;
  const ejecutar = async () => {
    if (!accion) return;
    const resultado = await operacion.ejecutar(() => solicitar<OrdenProduccionConsulta>(`/api/produccion/${o.id}/${accion}`, { method: "POST", body: JSON.stringify({ versionFila: o.versionFila }) }));
    if (resultado) { setOrden(resultado); setAccion(null); }
  };
  return <div className="space-y-6"><EncabezadoPagina titulo={o.numeroOrden} descripcion={`${o.producto} · ${o.receta} v${o.recetaVersion}`} acciones={<div className="flex flex-wrap gap-2"><Link to="/produccion" className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm">Volver</Link>{administrar && o.estado === "BORRADOR" && <><Link to={`/produccion/${o.id}/editar`} className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm">Editar</Link><Boton onClick={() => setAccion("confirmar")}>Confirmar producción</Boton></>}{o.estado === "CONFIRMADA" && tienePermiso(PERMISOS.produccionProducir) && <Link to={`/produccion/${o.id}/finalizar`} className="rounded-lg bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white">Finalizar producción</Link>}{puedeAnular && <Boton variante="peligro" onClick={() => setAccion("anular")}>Anular</Boton>}</div>} />
    <Tarjeta className="p-5"><dl className="grid gap-6 sm:grid-cols-2 xl:grid-cols-4"><DatoProduccion etiqueta="Estado"><EstadoProduccionBadge estado={o.estado} /></DatoProduccion><DatoProduccion etiqueta="Sucursal / bodega">{o.sucursal} · {o.bodega}</DatoProduccion><DatoProduccion etiqueta="Receta">{tienePermiso(PERMISOS.recetasConsultar) ? <Link to={`/recetas/${o.recetaId}`} className="text-teal-700">{o.receta} · v{o.recetaVersion}</Link> : `${o.receta} · v${o.recetaVersion}`}</DatoProduccion><DatoProduccion etiqueta="Responsable">{o.responsable}</DatoProduccion><DatoProduccion etiqueta="Cantidad planificada">{n.format(o.cantidadPlanificada)} {o.abreviaturaUnidadPlanificada}</DatoProduccion><DatoProduccion etiqueta="Equivalencia base">{n.format(o.cantidadPlanificadaBase)} {o.abreviaturaUnidadBase}</DatoProduccion><DatoProduccion etiqueta="Presentación / factor">{o.presentacion ?? "Unidad base"} · × {n.format(o.factorConversion)}</DatoProduccion><DatoProduccion etiqueta="Fechas">Inicio: {fechaProduccion(o.fechaInicio)}<br />Fin: {fechaProduccion(o.fechaFin)}</DatoProduccion></dl></Tarjeta>
    <Tarjeta className="overflow-hidden"><h2 className="border-b border-slate-200 p-5 font-bold">Ingredientes planificados</h2><TablaProduccion etiqueta="Ingredientes planificados" columnas={["Ingrediente", "Presentación", "Cantidad", "Factor", "Cantidad base", "Costo planificado"]}>{orden.detalles.map((d) => <tr key={d.id}><td className="font-semibold">{d.productoInsumo}</td><td>{d.presentacion ?? "Unidad base"}</td><td>{n.format(d.cantidadPlanificada)} {d.abreviaturaUnidad}</td><td>× {n.format(d.factorConversion)}</td><td>{n.format(d.cantidadPlanificadaBase)} {d.abreviaturaUnidadBase}</td><td>{d.costoPlanificado === null ? "No disponible" : dineroProduccion(d.costoPlanificado)}</td></tr>)}</TablaProduccion></Tarjeta>
    {orden.produccionTerminada && <Tarjeta className="p-5"><h2 className="mb-5 text-lg font-bold text-teal-800">Producción finalizada</h2><dl className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4"><DatoProduccion etiqueta="Producto">{orden.produccionTerminada.producto}</DatoProduccion><DatoProduccion etiqueta="Cantidad producida">{n.format(orden.produccionTerminada.cantidadProducida)} {o.abreviaturaUnidadBase}</DatoProduccion><DatoProduccion etiqueta="Costo materia prima">{dineroProduccion(orden.produccionTerminada.costoMateriaPrima)}</DatoProduccion><DatoProduccion etiqueta="Costo unitario base">{costoBaseProduccion(orden.produccionTerminada.costoUnitarioBase)} / {o.abreviaturaUnidadBase}</DatoProduccion><DatoProduccion etiqueta="Mano de obra">{dineroProduccion(orden.produccionTerminada.costoManoObra)}</DatoProduccion><DatoProduccion etiqueta="Costo indirecto">{dineroProduccion(orden.produccionTerminada.costoIndirecto)}</DatoProduccion><DatoProduccion etiqueta="Costo total">{dineroProduccion(orden.produccionTerminada.costoTotal)}</DatoProduccion><DatoProduccion etiqueta="Finalización">{fechaProduccion(orden.produccionTerminada.fechaFinalizacion)}</DatoProduccion></dl></Tarjeta>}
    <Tarjeta className="overflow-hidden"><h2 className="border-b border-slate-200 p-5 font-bold">Consumos reales</h2>{!orden.consumos.length ? <EstadoVacio titulo="Sin consumos registrados" descripcion="Los consumos se registran al finalizar la producción." /> : <TablaProduccion etiqueta="Consumos reales" columnas={["Producto", "Cantidad base", "Costo unitario base", "Costo total", "Fecha"]}>{orden.consumos.map((c) => { const unidad = orden.detalles.find((d) => d.productoInsumoId === c.productoId)?.abreviaturaUnidadBase || c.unidadMedida; return <tr key={c.id}><td>{c.producto}</td><td>{n.format(c.cantidadConsumida)} {unidad}</td><td>{costoBaseProduccion(c.costoUnitario)} / {unidad}</td><td>{dineroProduccion(c.costoTotal)}</td><td>{fechaProduccion(c.fechaConsumo)}</td></tr>; })}</TablaProduccion>}</Tarjeta>
    <Tarjeta className="p-5"><h2 className="font-bold">Movimientos generados</h2><p className="mt-1 text-sm text-slate-500">Referencias de trazabilidad de esta orden.</p>{orden.movimientoIds.length ? <ul className="mt-4 flex flex-wrap gap-3">{orden.movimientoIds.map((id) => <li key={id} className="rounded-lg bg-slate-100 px-4 py-2 text-sm font-semibold">Movimiento #{id}</li>)}</ul> : <p className="mt-4 text-sm text-slate-500">Aún no hay movimientos.</p>}</Tarjeta>
    <DialogoConfirmacion abierto={!!accion} titulo={accion === "confirmar" ? "¿Confirmar esta orden de producción?" : "¿Anular esta orden de producción?"} descripcion={accion === "confirmar" ? "La orden quedará confirmada y no podrá editarse." : "La orden se conservará para consulta."} cerrar={() => setAccion(null)} confirmar={() => void ejecutar()} cargando={operacion.procesando} peligro={accion === "anular"} textoConfirmar={accion === "anular" ? "Anular orden" : "Confirmar"}><p>{o.numeroOrden} · {o.producto}</p>{operacion.error && <AvisoProduccion error>{operacion.error.message}</AvisoProduccion>}</DialogoConfirmacion>
  </div>;
}
