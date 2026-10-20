import { useCallback, useEffect, useRef, useState } from "react";
import { ArrowLeft, Ban, CheckCircle2, ChevronDown, ChevronUp, ClipboardList, PackageCheck, Pencil } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { Boton, Insignia, Tarjeta } from "../../components/ui/Controles";
import { DialogoConfirmacion } from "../../components/ui/DialogoConfirmacion";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando, EstadoError, EstadoVacio } from "../../components/ui/Estados";
import type { CompraConsulta, MonedaCompra, RecepcionCompra, ReferenciasCompras } from "../../types/compras";
import { formatoNumero, mostrarFecha } from "../../utils/formato";
import { esBorrador, EstadoCompraBadge, fechaCorta, formatoCostoBase, formatoMoneda, normalizarEstadoCompra, puedeRecibirse } from "./utilidadesCompras";

export function PaginaDetalleCompra({ enfocarRecepciones = false }: { enfocarRecepciones?: boolean }) {
  const { id } = useParams();
  const navegar = useNavigate();
  const { tienePermiso } = useAutenticacion();
  const [compra, setCompra] = useState<CompraConsulta | null>(null);
  const [referencias, setReferencias] = useState<ReferenciasCompras | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [noEncontrado, setNoEncontrado] = useState(false);
  const [accion, setAccion] = useState<"confirmar" | "anular" | null>(null);
  const [procesando, setProcesando] = useState(false);
  const recepcionesRef = useRef<HTMLDivElement>(null);

  const cargar = useCallback(async () => {
    if (!id) return;
    setCargando(true); setError(null); setNoEncontrado(false);
    try {
      const [detalle, refs] = await Promise.all([solicitar<CompraConsulta>(`/api/compras/${id}`), solicitar<ReferenciasCompras>("/api/compras/referencias")]);
      setCompra(detalle); setReferencias(refs);
    } catch (excepcion) {
      if (excepcion instanceof ErrorApi && excepcion.estado === 404) setNoEncontrado(true);
      else setError(excepcion instanceof Error ? excepcion.message : "No fue posible consultar la orden.");
    } finally { setCargando(false); }
  }, [id]);
  useEffect(() => {
    // Sincroniza el detalle con el identificador de la ruta.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargar();
  }, [cargar]);
  useEffect(() => { if (!cargando && enfocarRecepciones) recepcionesRef.current?.scrollIntoView({ behavior: "smooth" }); }, [cargando, enfocarRecepciones]);

  const ejecutar = async () => {
    if (!accion || !id) return;
    setProcesando(true); setError(null);
    try {
      if (accion === "confirmar") await solicitar(`/api/compras/${id}/confirmar`, { method: "POST" });
      else await solicitar(`/api/compras/${id}/estado`, { method: "PUT", body: JSON.stringify({ estado: "ANULADA" }) });
      setAccion(null); await cargar();
    } catch (excepcion) { setAccion(null); setError(excepcion instanceof Error ? excepcion.message : "El cambio de estado fue rechazado."); }
    finally { setProcesando(false); }
  };

  if (cargando) return <EstadoCargando mensaje="Consultando orden de compra…" />;
  if (noEncontrado) return <EstadoError mensaje="No se encontró el recurso solicitado." />;
  if (!compra) return <EstadoError mensaje={error ?? "No se encontró el recurso solicitado."} reintentar={cargar} />;
  const { encabezado, detalles } = compra;
  const moneda = referencias?.monedas.find((item) => item.id === encabezado.monedaId);
  const administra = tienePermiso(PERMISOS.comprasAdministrar);
  const recibe = tienePermiso(PERMISOS.comprasRecibir);
  const estado = normalizarEstadoCompra(encabezado.estado);
  return <div className="space-y-6">
    <EncabezadoPagina titulo={`Orden ${encabezado.numeroOrden}`} descripcion={`${encabezado.proveedor} · ${encabezado.sucursal}`} icono={<ClipboardList className="h-7 w-7" />} acciones={<div className="flex flex-wrap gap-2"><Boton variante="secundario" onClick={() => navegar("/compras")}><ArrowLeft className="h-4 w-4" /> Volver</Boton>{administra && esBorrador(encabezado.estado) && <Boton variante="secundario" onClick={() => navegar(`/compras/${id}/editar`)}><Pencil className="h-4 w-4" /> Editar</Boton>}{administra && esBorrador(encabezado.estado) && <Boton onClick={() => setAccion("confirmar")}><CheckCircle2 className="h-4 w-4" /> Confirmar</Boton>}{recibe && puedeRecibirse(encabezado.estado) && <Boton onClick={() => navegar(`/compras/${id}/recibir`)}><PackageCheck className="h-4 w-4" /> {estado === "PARCIALMENTE_RECIBIDA" ? "Recibir pendiente" : "Recibir productos"}</Boton>}{administra && (estado === "BORRADOR" || estado === "CONFIRMADA") && <Boton variante="peligro" onClick={() => setAccion("anular")}><Ban className="h-4 w-4" /> Anular</Boton>}</div>} />
    {error && <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">{error}</div>}
    <Tarjeta className="p-5 sm:p-6"><div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-4"><Dato etiqueta="Estado"><EstadoCompraBadge estado={encabezado.estado} /></Dato><Dato etiqueta="Fecha de emisión" valor={fechaCorta(encabezado.fechaEmision)} /><Dato etiqueta="Entrega esperada" valor={compra.fechaEntregaEsperada ? fechaCorta(compra.fechaEntregaEsperada) : "No definida"} /><Dato etiqueta="Moneda" valor={moneda ? `${moneda.codigoISO} / ${moneda.nombre}` : `Moneda #${encabezado.monedaId}`} /><Dato etiqueta="Tipo de cambio" valor={formatoNumero.format(encabezado.tipoCambio)} /><Dato etiqueta="Proveedor" valor={encabezado.proveedor} /><Dato etiqueta="Sucursal" valor={encabezado.sucursal} /><Dato etiqueta="Modalidad" valor={encabezado.esCredito === true ? "Crédito" : encabezado.esCredito === false ? "Contado" : "Sin definir"} /></div></Tarjeta>
    <Tarjeta className="overflow-hidden"><header className="border-b border-slate-200 px-5 py-4"><h2 className="font-bold text-slate-950">Detalle de productos</h2><p className="text-sm text-slate-500">Cantidades ordenadas, equivalencias y avance de recepción.</p></header><div className="overflow-x-auto"><table className="min-w-[92rem] divide-y divide-slate-200 text-left text-sm"><thead className="bg-slate-50 text-xs uppercase tracking-wide text-slate-500"><tr>{["Producto","Presentación","Cantidad","Factor","Cantidad base","Precio unitario","Descuento","Impuesto","Total línea","Recibido","Pendiente"].map((x) => <th className="px-4 py-3" key={x}>{x}</th>)}</tr></thead><tbody className="divide-y divide-slate-100">{detalles.map((linea) => <tr key={linea.id}><td className="px-4 py-4"><span className="font-semibold text-slate-900">{linea.producto}</span><span className="block text-xs text-slate-400">Línea {linea.numeroLinea}</span></td><td className="px-4 py-4 text-slate-600">{linea.presentacion ?? "Unidad base"}</td><td className="px-4 py-4 font-semibold tabular-nums">{formatoNumero.format(linea.cantidad)}</td><td className="px-4 py-4 tabular-nums text-slate-600">× {formatoNumero.format(linea.factorConversion)}</td><td className="px-4 py-4"><strong>{formatoNumero.format(linea.cantidadBase)} {linea.unidadBase}</strong><span className="block text-xs text-teal-700">{formatoNumero.format(linea.cantidad)} {linea.presentacion ?? linea.unidadBase} = {formatoNumero.format(linea.cantidadBase)} {linea.unidadBase}</span></td><td className="px-4 py-4 tabular-nums">{formatoMoneda(linea.precioUnitario, moneda)}</td><td className="px-4 py-4 tabular-nums">{formatoMoneda(linea.descuento, moneda)}</td><td className="px-4 py-4 tabular-nums">{formatoMoneda(linea.impuesto, moneda)}</td><td className="px-4 py-4 font-semibold tabular-nums">{formatoMoneda(linea.totalLinea, moneda)}</td><td className="px-4 py-4 tabular-nums text-emerald-700">{formatoNumero.format(linea.cantidadRecibida)}</td><td className="px-4 py-4 font-semibold tabular-nums text-amber-700">{formatoNumero.format(linea.cantidadPendiente)}</td></tr>)}</tbody></table></div></Tarjeta>
    <Tarjeta className="p-5"><h2 className="font-bold text-slate-950">Resumen de orden</h2><dl className="mt-4 ml-auto max-w-md space-y-3 text-sm"><FilaResumen etiqueta="Subtotal" valor={formatoMoneda(encabezado.subTotal, moneda)} /><FilaResumen etiqueta="Impuestos" valor={formatoMoneda(encabezado.impuestos, moneda)} /><div className="flex justify-between border-t border-slate-200 pt-3 text-lg font-bold"><dt>Total estimado</dt><dd className="text-teal-800">{formatoMoneda(encabezado.totalEstimado, moneda)}</dd></div></dl>{estado === "RECIBIDA" && <p className="mt-4 text-right"><Insignia tono="positivo"><CheckCircle2 className="mr-1 h-4 w-4" /> Recepción completa</Insignia></p>}</Tarjeta>
    <div ref={recepcionesRef}><SeccionRecepciones compra={compra} moneda={moneda} /></div>
    <DialogoConfirmacion abierto={Boolean(accion)} cerrar={() => setAccion(null)} confirmar={ejecutar} cargando={procesando} peligro={accion === "anular"} titulo={accion === "confirmar" ? "Confirmar orden" : "Anular orden"} descripcion={accion === "confirmar" ? "Después de confirmar la orden ya no podrá editar sus detalles." : "Esta acción no elimina la orden y quedará registrada en auditoría."} textoConfirmar={accion === "confirmar" ? "Confirmar" : "Anular orden"}><p className="rounded-lg bg-slate-50 p-3 text-sm">{encabezado.numeroOrden} · {encabezado.proveedor}</p></DialogoConfirmacion>
  </div>;
}

function Dato({ etiqueta, valor, children }: { etiqueta: string; valor?: string; children?: React.ReactNode }) { return <div><dt className="text-xs font-semibold uppercase tracking-wide text-slate-400">{etiqueta}</dt><dd className="mt-1.5 font-medium text-slate-800">{children ?? valor}</dd></div>; }
function FilaResumen({ etiqueta, valor }: { etiqueta: string; valor: string }) { return <div className="flex justify-between"><dt className="text-slate-600">{etiqueta}</dt><dd className="font-semibold">{valor}</dd></div>; }

function SeccionRecepciones({ compra, moneda }: { compra: CompraConsulta; moneda?: MonedaCompra }) {
  return <Tarjeta className="overflow-hidden"><header className="border-b border-slate-200 px-5 py-4"><h2 className="font-bold text-slate-950">Recepciones</h2><p className="text-sm text-slate-500">Historial de ingresos y movimientos de inventario.</p></header>{!compra.recepciones.length ? <EstadoVacio titulo="Aún no hay recepciones" descripcion="Las recepciones confirmadas aparecerán aquí con su trazabilidad." /> : <div className="divide-y divide-slate-100">{compra.recepciones.map((recepcion) => <RecepcionAcordeon key={recepcion.id} recepcion={recepcion} compra={compra} moneda={moneda} />)}</div>}</Tarjeta>;
}

function RecepcionAcordeon({ recepcion, compra, moneda }: { recepcion: RecepcionCompra; compra: CompraConsulta; moneda?: MonedaCompra }) {
  const [abierta, setAbierta] = useState(false);
  return <section><button type="button" className="flex w-full items-center justify-between gap-4 px-5 py-4 text-left hover:bg-slate-50" aria-expanded={abierta} onClick={() => setAbierta((x) => !x)}><div><span className="font-semibold text-slate-900">{recepcion.numeroRecepcion}</span><span className="ml-3"><Insignia tono="positivo">{recepcion.estado}</Insignia></span><p className="mt-1 text-xs text-slate-500">{mostrarFecha(recepcion.fechaRecepcion)} · {recepcion.detalles.length} líneas</p></div>{abierta ? <ChevronUp className="h-5 w-5 text-slate-400" /> : <ChevronDown className="h-5 w-5 text-slate-400" />}</button>{abierta && <div className="space-y-4 bg-slate-50/60 px-5 py-4"><p className="text-sm text-slate-600"><strong>Observaciones:</strong> {recepcion.observaciones || "Sin observaciones"}</p><div className="overflow-x-auto"><table className="w-full min-w-[42rem] text-sm"><thead className="text-left text-xs uppercase text-slate-500"><tr><th className="py-2">Producto</th><th className="py-2">Cantidad recibida</th><th className="py-2">Costo unitario base</th></tr></thead><tbody>{recepcion.detalles.map((detalle) => { const linea = compra.detalles.find((item) => item.id === detalle.ordenCompraDetalleId); return <tr key={detalle.id} className="border-t border-slate-200"><td className="py-3 font-medium">{linea?.producto ?? `Producto #${detalle.productoId}`}</td><td className="py-3">{formatoNumero.format(detalle.cantidadRecibida)} {linea?.presentacion ?? linea?.unidadBase}</td><td className="py-3">{formatoCostoBase(detalle.costoUnitarioBase, moneda)} / {linea?.unidadBase}</td></tr>; })}</tbody></table></div><p className="text-sm text-slate-600"><strong>Movimientos generados:</strong> {recepcion.movimientoIds.length ? recepcion.movimientoIds.join(", ") : "El backend no devolvió movimientos."}</p></div>}</section>;
}
