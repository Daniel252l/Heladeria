import { useState, type FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { Entrada, Seleccion, Tarjeta } from "../../components/ui/Controles";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando } from "../../components/ui/Estados";
import type { OrdenProduccionConsulta, ReferenciasProduccion } from "../../types/produccion";
import { useOperacionProduccion, useRecursoProduccion } from "./useDatosProduccion";
import { AvisoProduccion, ErrorProduccion, PieFormularioProduccion, TablaProduccion } from "./ComponentesProduccion";
import { DetalleProduccion } from "./PaginaDetalleProduccion";
import { cantidadValida, numeroProduccion as n } from "./utilidadesProduccion";

export function PaginaFinalizarProduccion() {
  const { id } = useParams();
  const consulta = useRecursoProduccion<OrdenProduccionConsulta>(`/api/produccion/${id}`);
  const refs = useRecursoProduccion<ReferenciasProduccion>("/api/produccion/referencias");
  if (consulta.cargando) return <EstadoCargando />;
  if (consulta.error) return <ErrorProduccion error={consulta.error} reintentar={consulta.recargar} />;
  if (!consulta.datos) return null;
  if (consulta.datos.encabezado.estado !== "CONFIRMADA") return <AvisoProduccion>Esta orden está {consulta.datos.encabezado.estado.toLowerCase()} y no admite finalización. <Link className="font-semibold underline" to={`/produccion/${id}`}>Consultar orden</Link></AvisoProduccion>;
  if (refs.cargando) return <EstadoCargando />;
  if (refs.error) return <ErrorProduccion error={refs.error} reintentar={refs.recargar} />;
  return refs.datos ? <FinalizarOrden key={`${id}:${consulta.datos.encabezado.versionFila}`} orden={consulta.datos} referencias={refs.datos} /> : null;
}
function FinalizarOrden({ orden, referencias }: { orden: OrdenProduccionConsulta; referencias: ReferenciasProduccion }) {
  const [claveOperacion] = useState(() => crypto.randomUUID());
  const [cantidad, setCantidad] = useState(orden.encabezado.cantidadPlanificada);
  const [ubicacionId, setUbicacionId] = useState(0);
  const [validacion, setValidacion] = useState("");
  const [resultado, setResultado] = useState<OrdenProduccionConsulta | null>(null);
  const operacion = useOperacionProduccion();
  const o = orden.encabezado;
  const escala = cantidad * o.factorConversion / o.cantidadPlanificadaBase;
  const finalizar = async (evento: FormEvent) => {
    evento.preventDefault(); setValidacion("");
    if (!cantidadValida(cantidad) || !referencias.almacenes.some((a) => a.bodegaId === o.bodegaProduccionId && a.ubicacionId === ubicacionId)) { setValidacion("Indique una cantidad positiva y una ubicación de la bodega de producción."); return; }
    const datos = await operacion.ejecutar(() => solicitar<OrdenProduccionConsulta>(`/api/produccion/${o.id}/finalizar`, { method: "POST", body: JSON.stringify({ claveOperacion, ubicacionId, cantidadProducida: cantidad, versionFila: o.versionFila }) }));
    if (datos) setResultado(datos);
  };
  let mensaje = operacion.error?.message;
  if (operacion.error instanceof ErrorApi && operacion.error.codigo === "EXISTENCIA_INSUFICIENTE") {
    for (const detalle of orden.detalles) mensaje = mensaje?.replace(new RegExp(`(?:el ingrediente|ingrediente|producto) ${detalle.productoInsumoId}(?!\\d)`, "gi"), `${detalle.productoInsumo} (cantidades en ${detalle.abreviaturaUnidadBase})`);
  }
  if (resultado) return <DetalleProduccion inicial={resultado} />;
  return <form onSubmit={(e) => void finalizar(e)} className="space-y-6"><EncabezadoPagina titulo="Finalizar producción" descripcion={`${o.numeroOrden} · ${o.producto}`} />
    <AvisoProduccion>Planificada: {n.format(o.cantidadPlanificada)} {o.abreviaturaUnidadPlanificada} = {n.format(o.cantidadPlanificadaBase)} {o.abreviaturaUnidadBase}. Al finalizar se registrarán consumos y entrada del producto terminado.</AvisoProduccion>
    <Tarjeta className="p-4 sm:p-6"><fieldset disabled={operacion.procesando} className="grid min-w-0 gap-5 sm:grid-cols-2"><Entrada etiqueta="Bodega" value={o.bodega} readOnly /><Seleccion etiqueta="Ubicación *" required value={ubicacionId || ""} onChange={(e) => setUbicacionId(Number(e.target.value))}><option value="">Seleccione una ubicación</option>{referencias.almacenes.filter((a) => a.bodegaId === o.bodegaProduccionId).map((a) => <option key={a.ubicacionId} value={a.ubicacionId}>{a.ubicacion}</option>)}</Seleccion><Entrada etiqueta={`Cantidad producida (${o.abreviaturaUnidadPlanificada}) *`} type="number" required min="0.000001" step="0.000001" value={cantidad} onChange={(e) => setCantidad(Number(e.target.value))} /><p className="self-end rounded-lg bg-teal-50 p-3 text-sm text-teal-800">{n.format(cantidad)} {o.abreviaturaUnidadPlanificada} = {n.format(cantidad * o.factorConversion)} {o.abreviaturaUnidadBase}</p></fieldset></Tarjeta>
    <Tarjeta className="overflow-hidden"><h2 className="border-b border-slate-200 p-5 font-bold">Ingredientes requeridos para la cantidad producida</h2><TablaProduccion etiqueta="Consumos requeridos" columnas={["Ingrediente", "Cantidad requerida", "Cantidad base requerida"]}>{orden.detalles.map((d) => <tr key={d.id}><td className="font-semibold">{d.productoInsumo}</td><td>{n.format(d.cantidadPlanificada * escala)} {d.abreviaturaUnidad}</td><td>{n.format(d.cantidadPlanificadaBase * escala)} {d.abreviaturaUnidadBase}</td></tr>)}</TablaProduccion><p className="p-5 text-sm text-slate-500">La disponibilidad y los costos se validan al finalizar. El costo definitivo aparecerá en el resultado.</p></Tarjeta>
    {validacion && <AvisoProduccion error>{validacion}</AvisoProduccion>}{mensaje && <AvisoProduccion error>{mensaje}{operacion.conflicto && <p><Link className="font-semibold underline" to={`/produccion/${o.id}`}>Consultar el estado actual antes de reintentar</Link></p>}</AvisoProduccion>}
    <PieFormularioProduccion volver={`/produccion/${o.id}`} procesando={operacion.procesando} texto="Finalizar producción" />
  </form>;
}
