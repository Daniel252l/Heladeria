import { useEffect, useMemo, useState } from "react";
import { ArrowLeft, CheckCircle2, PackageCheck, Warehouse } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { AreaTexto, Boton, Insignia, Tarjeta } from "../../components/ui/Controles";
import { DialogoConfirmacion } from "../../components/ui/DialogoConfirmacion";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando, EstadoError, EstadoVacio } from "../../components/ui/Estados";
import { SelectorEntidad, type OpcionEntidad } from "../../components/ui/SelectorEntidad";
import type { CompraConsulta, MonedaCompra, RecepcionCompra, ReferenciasCompras } from "../../types/compras";
import type { AlmacenInventario } from "../../types/inventario";
import { formatoNumero } from "../../utils/formato";
import { EstadoCompraBadge, formatoCostoBase, formatoMoneda, puedeRecibirse } from "./utilidadesCompras";

export function PaginaRecepcionCompra() {
  const { id } = useParams();
  const navegar = useNavigate();
  const [claveOperacion] = useState(() => crypto.randomUUID());
  const [compra, setCompra] = useState<CompraConsulta | null>(null);
  const [referencias, setReferencias] = useState<ReferenciasCompras | null>(null);
  const [almacenes, setAlmacenes] = useState<AlmacenInventario[]>([]);
  const [bodegaId, setBodegaId] = useState("");
  const [ubicacionId, setUbicacionId] = useState("");
  const [observaciones, setObservaciones] = useState("");
  const [cantidades, setCantidades] = useState<Record<number, string>>({});
  const [confirmando, setConfirmando] = useState(false);
  const [procesando, setProcesando] = useState(false);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [noEncontrado, setNoEncontrado] = useState(false);
  const [resultado, setResultado] = useState<RecepcionCompra | null>(null);

  useEffect(() => {
    let vigente = true;
    const cargar = async () => {
      if (!id) return;
      try {
        const [orden, listaAlmacenes, refs] = await Promise.all([
          solicitar<CompraConsulta>(`/api/compras/${id}`),
          solicitar<AlmacenInventario[]>("/api/inventario/almacenes"),
          solicitar<ReferenciasCompras>("/api/compras/referencias"),
        ]);
        if (!vigente) return;
        setCompra(orden); setReferencias(refs);
        const disponibles = listaAlmacenes.filter((item) => item.sucursalId === orden.encabezado.sucursalId);
        setAlmacenes(disponibles);
        const principal = disponibles.find((item) => item.esBodegaPrincipal) ?? disponibles[0];
        if (principal) { setBodegaId(String(principal.bodegaId)); setUbicacionId(String(principal.ubicacionId)); }
        setCantidades(Object.fromEntries(orden.detalles.filter((linea) => linea.cantidadPendiente > 0).map((linea) => [linea.id, "0"])));
      } catch (excepcion) {
        if (excepcion instanceof ErrorApi && excepcion.estado === 404) setNoEncontrado(true);
        else setError(excepcion instanceof Error ? excepcion.message : "No fue posible preparar la recepción.");
      } finally { if (vigente) setCargando(false); }
    };
    void cargar();
    return () => { vigente = false; };
  }, [id]);

  const bodegas: OpcionEntidad[] = useMemo(() => Array.from(new Map(almacenes.map((item) => [item.bodegaId, { id: item.bodegaId, nombre: item.bodega, codigo: item.esBodegaPrincipal ? "Principal" : undefined }])).values()), [almacenes]);
  const ubicaciones: OpcionEntidad[] = useMemo(() => almacenes.filter((item) => String(item.bodegaId) === bodegaId).map((item) => ({ id: item.ubicacionId, nombre: item.ubicacion })), [almacenes, bodegaId]);
  const lineasPendientes = compra?.detalles.filter((linea) => linea.cantidadPendiente > 0) ?? [];
  const lineasRecibidas = lineasPendientes.filter((linea) => Number(cantidades[linea.id] ?? 0) > 0);
  const moneda: MonedaCompra | undefined = referencias?.monedas.find((item) => item.id === compra?.encabezado.monedaId);
  const abrirConfirmacion = () => {
    setError(null);
    if (!bodegaId || !ubicacionId) { setError("Seleccione la bodega y la ubicación de ingreso."); return; }
    if (!lineasRecibidas.length) { setError("Ingrese una cantidad mayor que cero en al menos una línea."); return; }
    const excedida = lineasRecibidas.find((linea) => Number(cantidades[linea.id]) > linea.cantidadPendiente);
    if (excedida) { setError(`La cantidad de ${excedida.producto} excede el pendiente de la orden.`); return; }
    setConfirmando(true);
  };
  const aplicar = async () => {
    if (!id) return;
    setProcesando(true); setError(null);
    try {
      const respuesta = await solicitar<RecepcionCompra>(`/api/compras/${id}/recepciones`, { method: "POST", body: JSON.stringify({ claveOperacion, bodegaId: Number(bodegaId), ubicacionId: Number(ubicacionId), observaciones: observaciones.trim() || null, detalles: lineasRecibidas.map((linea) => ({ ordenCompraDetalleId: linea.id, cantidad: Number(cantidades[linea.id]) })) }) });
      setResultado(respuesta); setConfirmando(false);
    } catch (excepcion) { setConfirmando(false); setError(excepcion instanceof Error ? excepcion.message : "La recepción no pudo aplicarse."); }
    finally { setProcesando(false); }
  };

  if (cargando) return <EstadoCargando mensaje="Preparando recepción de compra…" />;
  if (noEncontrado) return <EstadoError mensaje="No se encontró el recurso solicitado." />;
  if (!compra) return <EstadoError mensaje={error ?? "No se encontró el recurso solicitado."} />;
  if (resultado) return <ResultadoRecepcion recepcion={resultado} compra={compra} volver={() => navegar(`/compras/${id}`)} />;
  if (!puedeRecibirse(compra.encabezado.estado)) return <div className="space-y-6"><EncabezadoPagina titulo={`Recepción ${compra.encabezado.numeroOrden}`} descripcion="Esta orden no admite nuevas recepciones." icono={<PackageCheck className="h-7 w-7" />} acciones={<Boton variante="secundario" onClick={() => navegar(`/compras/${id}`)}><ArrowLeft className="h-4 w-4" /> Ver orden</Boton>} /><Tarjeta><EstadoVacio titulo="Recepción no disponible" descripcion={`La orden está en estado ${compra.encabezado.estado}.`} /></Tarjeta></div>;

  const bodega = almacenes.find((item) => String(item.bodegaId) === bodegaId)?.bodega;
  const ubicacion = almacenes.find((item) => String(item.ubicacionId) === ubicacionId)?.ubicacion;
  return <div className="space-y-6">
    <EncabezadoPagina titulo="Recibir productos" descripcion={`${compra.encabezado.numeroOrden} · ${compra.encabezado.proveedor}`} icono={<PackageCheck className="h-7 w-7" />} acciones={<Boton variante="secundario" onClick={() => navegar(`/compras/${id}`)}><ArrowLeft className="h-4 w-4" /> Volver</Boton>} />
    <Tarjeta className="p-5"><div className="grid gap-4 sm:grid-cols-3"><div><p className="text-xs uppercase text-slate-400">Orden</p><p className="mt-1 font-semibold">{compra.encabezado.numeroOrden}</p></div><div><p className="text-xs uppercase text-slate-400">Proveedor</p><p className="mt-1 font-semibold">{compra.encabezado.proveedor}</p></div><div><p className="text-xs uppercase text-slate-400">Estado</p><p className="mt-1"><EstadoCompraBadge estado={compra.encabezado.estado} /></p></div></div></Tarjeta>
    <Tarjeta className="space-y-5 p-5 sm:p-6"><div className="flex items-center gap-3"><Warehouse className="h-5 w-5 text-teal-700" /><div><h2 className="font-bold">Destino de inventario</h2><p className="text-sm text-slate-500">La sucursal se obtiene de la orden.</p></div></div><div className="grid gap-4 md:grid-cols-2"><SelectorEntidad etiqueta="Bodega *" opcionesLocales={bodegas} value={bodegaId} onChange={(valor) => { setBodegaId(valor ?? ""); const primera = almacenes.find((item) => String(item.bodegaId) === valor); setUbicacionId(primera ? String(primera.ubicacionId) : ""); }} /><SelectorEntidad etiqueta="Ubicación *" opcionesLocales={ubicaciones} value={ubicacionId} onChange={(valor) => setUbicacionId(valor ?? "")} /><div className="md:col-span-2"><AreaTexto etiqueta="Observaciones" maxLength={500} rows={3} value={observaciones} onChange={(e) => setObservaciones(e.target.value)} /></div></div></Tarjeta>
    <Tarjeta className="overflow-hidden"><header className="border-b border-slate-200 px-5 py-4"><h2 className="font-bold">Productos pendientes</h2><p className="text-sm text-slate-500">Indique únicamente lo recibido físicamente en este intento.</p></header><div className="overflow-x-auto"><table className="min-w-[75rem] divide-y divide-slate-200 text-left text-sm"><thead className="bg-slate-50 text-xs uppercase text-slate-500"><tr>{["Producto","Ordenado","Recibido","Pendiente","A recibir ahora","Equivalencia","Costo"].map((x) => <th className="px-4 py-3" key={x}>{x}</th>)}</tr></thead><tbody className="divide-y divide-slate-100">{lineasPendientes.map((linea) => { const cantidad = Number(cantidades[linea.id] ?? 0); const netoUnitario = linea.cantidad > 0 ? (linea.cantidad * linea.precioUnitario - linea.descuento) / linea.cantidad : linea.precioUnitario; const costoBase = linea.factorConversion > 0 ? netoUnitario * compra.encabezado.tipoCambio / linea.factorConversion : null; return <tr key={linea.id}><td className="px-4 py-4"><strong>{linea.producto}</strong><span className="block text-xs text-slate-500">{linea.presentacion ?? "Unidad base"}</span></td><td className="px-4 py-4">{formatoNumero.format(linea.cantidad)}</td><td className="px-4 py-4 text-emerald-700">{formatoNumero.format(linea.cantidadRecibida)}</td><td className="px-4 py-4 font-semibold text-amber-700">{formatoNumero.format(linea.cantidadPendiente)}</td><td className="px-4 py-4"><label className="sr-only" htmlFor={`cantidad-${linea.id}`}>Cantidad a recibir de {linea.producto}</label><input id={`cantidad-${linea.id}`} className="min-h-10 w-36 rounded-lg border border-slate-300 px-3 outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-100" type="number" min="0" max={linea.cantidadPendiente} step="0.000001" value={cantidades[linea.id] ?? "0"} onChange={(e) => setCantidades((x) => ({ ...x, [linea.id]: e.target.value }))} /></td><td className="px-4 py-4"><strong>{formatoNumero.format(cantidad)} {linea.presentacion ?? linea.unidadBase}</strong><span className="block text-xs text-teal-700">= {formatoNumero.format(cantidad * linea.factorConversion)} {linea.unidadBase}</span><span className="block text-xs text-slate-400">Factor {formatoNumero.format(linea.factorConversion)}</span></td><td className="px-4 py-4"><span>{formatoMoneda(linea.precioUnitario, moneda)} / {linea.presentacion ?? linea.unidadBase}</span>{costoBase != null && <span className="block text-xs text-slate-500">Base est.: {formatoCostoBase(costoBase, moneda)} / {linea.unidadBase}</span>}</td></tr>; })}</tbody></table></div></Tarjeta>
    {error && <div role="alert" className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">{error}</div>}
    <div className="flex justify-end"><Boton onClick={abrirConfirmacion}><PackageCheck className="h-4 w-4" /> Revisar recepción</Boton></div>
    <DialogoConfirmacion abierto={confirmando} cerrar={() => setConfirmando(false)} confirmar={aplicar} cargando={procesando} titulo="Confirmar recepción" descripcion="Verifique el destino y las cantidades antes de afectar el inventario." textoConfirmar="Aplicar recepción"><dl className="grid grid-cols-2 gap-3 rounded-lg bg-slate-50 p-4 text-sm"><div><dt className="text-slate-500">Orden</dt><dd className="font-semibold">{compra.encabezado.numeroOrden}</dd></div><div><dt className="text-slate-500">Proveedor</dt><dd className="font-semibold">{compra.encabezado.proveedor}</dd></div><div><dt className="text-slate-500">Bodega</dt><dd className="font-semibold">{bodega}</dd></div><div><dt className="text-slate-500">Ubicación</dt><dd className="font-semibold">{ubicacion}</dd></div></dl><ul className="space-y-2 text-sm">{lineasRecibidas.map((linea) => <li key={linea.id} className="rounded-lg border border-slate-200 p-3"><strong>{linea.producto}</strong><span className="block text-slate-600">{formatoNumero.format(Number(cantidades[linea.id]))} {linea.presentacion ?? linea.unidadBase} = {formatoNumero.format(Number(cantidades[linea.id]) * linea.factorConversion)} {linea.unidadBase}</span><span className="block text-xs text-slate-400">Pendiente después: {formatoNumero.format(linea.cantidadPendiente - Number(cantidades[linea.id]))}</span></li>)}</ul></DialogoConfirmacion>
  </div>;
}

function ResultadoRecepcion({ recepcion, compra, volver }: { recepcion: RecepcionCompra; compra: CompraConsulta; volver: () => void }) {
  return <div className="space-y-6"><EncabezadoPagina titulo="Recepción aplicada" descripcion="El servidor confirmó el ingreso y su trazabilidad." icono={<CheckCircle2 className="h-7 w-7" />} /><Tarjeta className="p-6"><div className="flex items-center gap-3"><Insignia tono="positivo">{recepcion.estado}</Insignia><strong className="text-lg">{recepcion.numeroRecepcion}</strong></div><dl className="mt-6 grid gap-4 sm:grid-cols-2"><div><dt className="text-xs uppercase text-slate-400">Orden</dt><dd className="font-semibold">{compra.encabezado.numeroOrden}</dd></div><div><dt className="text-xs uppercase text-slate-400">Líneas recibidas</dt><dd className="font-semibold">{recepcion.detalles.length}</dd></div><div className="sm:col-span-2"><dt className="text-xs uppercase text-slate-400">Cantidades recibidas</dt><dd className="mt-2 space-y-1">{recepcion.detalles.map((detalle) => { const linea = compra.detalles.find((item) => item.id === detalle.ordenCompraDetalleId); return <p key={detalle.id}>{linea?.producto ?? `Producto #${detalle.productoId}`}: <strong>{formatoNumero.format(detalle.cantidadRecibida)} {linea?.presentacion ?? linea?.unidadBase}</strong></p>; })}</dd></div><div className="sm:col-span-2"><dt className="text-xs uppercase text-slate-400">Movimientos generados</dt><dd className="font-semibold">{recepcion.movimientoIds.length ? recepcion.movimientoIds.join(", ") : "El backend no devolvió identificadores."}</dd></div></dl><div className="mt-6 flex justify-end"><Boton onClick={volver}>Ver orden e historial</Boton></div></Tarjeta></div>;
}
