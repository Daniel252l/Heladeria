import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Check, ReceiptText, RefreshCw } from "lucide-react";
import { ErrorApi } from "../../api/clienteHttp";
import { ventasApi } from "../../api/ventas";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { AreaTexto, Boton, Tarjeta } from "../../components/ui/Controles";
import { Modal } from "../../components/ui/Modal";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import { EstadoCargando, EstadoVacio } from "../../components/ui/Estados";
import type { VentaConsulta } from "../../types/ventas";
import { mostrarFechaCivil } from "../../utils/fechas";
import { AvisoVenta, DatoVenta, enlaceVenta, ErrorVenta, EstadoVenta, TablaVentas, TotalesVenta } from "./ComponentesVentas";
import { claveIntentoVenta, completarConReconciliacion, hayIntentoVenta, useOperacionVenta, useRecursoVentas } from "./useDatosVentas";
import { costoBaseVenta, dineroVenta, esConcurrencia, fechaVenta, mensajeVenta, numeroVenta as n } from "./utilidadesVentas";
import { CobroAlCompletar, type DatosCobroVenta } from "./CobroAlCompletar";

export function PaginaDetalleVenta() {
  const { id } = useParams();
  const { usuario, sucursalActiva } = useAutenticacion();
  const consulta = useRecursoVentas<VentaConsulta>(`/api/ventas/${id}`);
  if (consulta.cargando) return <EstadoCargando />;
  if (consulta.error) return <ErrorVenta error={consulta.error} recargar={consulta.recargar} />;
  return consulta.datos ? <DetalleVenta key={`${usuario?.id}:${sucursalActiva?.id}:${id}:${consulta.datos.encabezado.versionFila}`} inicial={consulta.datos} /> : null;
}

export function DetalleVenta({ inicial, nueva, editar }: { inicial: VentaConsulta; nueva?: () => void; editar?: (venta: VentaConsulta) => void }) {
  const [venta, setVenta] = useState(inicial);
  const [accion, setAccion] = useState<"confirmar" | "completar" | "anular" | null>(null);
  const [motivo, setMotivo] = useState("");
  const [validacion, setValidacion] = useState("");
  const datosCobro = useRef<DatosCobroVenta>(null);
  const { tienePermiso, usuario } = useAutenticacion();
  const operacion = useOperacionVenta();
  const o = venta.encabezado;
  const requiereCobro = !o.esCredito && o.total > 0;
  const consultar = tienePermiso(PERMISOS.ventasConsultar);
  const administrar = tienePermiso(PERMISOS.ventasAdministrar);
  const operar = tienePermiso(PERMISOS.posOperar);
  const cobrar = tienePermiso(PERMISOS.cajaOperar) && tienePermiso(PERMISOS.cajaCobrar) && tienePermiso(PERMISOS.cajaConsultar);
  const concurrencia = esConcurrencia(operacion.error);
  const clave = claveIntentoVenta(usuario!.id, o.empresaId, o.sucursalId, o.id);
  const abrir = (tipo: typeof accion) => { operacion.limpiarError(); setValidacion(""); setMotivo(""); setAccion(tipo); };
  const cerrar = () => { if (!operacion.procesando) setAccion(null); };
  const recargar = async () => {
    const actual = await operacion.ejecutar(() => ventasApi.obtener(o.id));
    if (actual) { setVenta(actual); setAccion(null); }
  };
  const ejecutar = async () => {
    if (!accion || concurrencia) return;
    if (accion === "anular" && (motivo.trim().length < 3 || motivo.trim().length > 500)) { setValidacion("Indique un motivo de entre 3 y 500 caracteres."); return; }
    const cobro = accion === "completar" && requiereCobro ? datosCobro.current?.obtener() : undefined;
    if (accion === "completar" && requiereCobro && !cobro) { setValidacion("Seleccione una sesión abierta y complete las formas de pago, montos y referencias. La suma aplicada debe coincidir con el total."); return; }
    const resultado = await operacion.ejecutar(() => accion === "completar" ? completarConReconciliacion(venta, clave, cobro ?? undefined)
      : accion === "confirmar" ? ventasApi.confirmar(o.id, o.versionFila) : ventasApi.anular(o.id, o.versionFila, motivo.trim()));
    if (resultado) { setVenta(resultado); setAccion(null); }
  };
  const error = operacion.error;
  // Un 403/404 de escritura también utiliza la experiencia consistente del proyecto.
  if (error instanceof ErrorApi && [403, 404].includes(error.estado)) return <ErrorVenta error={error} />;
  const avisoError = error && <AvisoVenta error><p>{mensajeVenta(error, venta.detalles)}</p>{concurrencia && <p className="mt-1 font-semibold">La venta cambió desde que la abriste. Recarga la información.</p>}{consultar && <Boton type="button" variante="secundario" className="mt-3" cargando={operacion.procesando} onClick={() => void recargar()}>Recargar información</Boton>}</AvisoVenta>;
  return <div className="ventas-vista space-y-6">
    <EncabezadoPagina titulo={o.numeroVenta} descripcion={`${o.sucursal} · ${fechaVenta(o.fechaVenta)}`} icono={<ReceiptText />} acciones={<div className="flex flex-wrap gap-2">
      {consultar && <Link className={enlaceVenta} to="/ventas">Volver a ventas</Link>}
      {consultar && venta.comprobante && ["COMPLETADA", "ANULADA"].includes(o.estado) && <Link className={enlaceVenta} to={`/ventas/${o.id}/comprobante`}>Ver comprobante</Link>}
      {nueva && <Boton variante="secundario" onClick={nueva} disabled={operacion.procesando}>Nueva venta</Boton>}
      {administrar && o.estado === "BORRADOR" && <>
        {operar && (editar ? <Boton variante="secundario" disabled={operacion.procesando} onClick={() => editar(venta)}>Editar borrador</Boton> : <Link className={enlaceVenta} to={`/ventas/${o.id}/editar`}>Editar borrador</Link>)}
        <Boton onClick={() => abrir("confirmar")} disabled={operacion.procesando}>Confirmar venta</Boton>
      </>}
      {o.estado === "CONFIRMADA" && tienePermiso(PERMISOS.ventasVender) && operar && (!requiereCobro || cobrar) && <Boton onClick={() => abrir("completar")} disabled={operacion.procesando}><Check className="h-4 w-4" /> Completar venta</Boton>}
      {["BORRADOR", "CONFIRMADA", "COMPLETADA"].includes(o.estado) && !(o.estado === "COMPLETADA" && (o.esCredito || venta.cobro)) && tienePermiso(PERMISOS.ventasAnular) && <Boton variante="peligro" onClick={() => abrir("anular")} disabled={operacion.procesando}>Anular venta</Boton>}
      {consultar && <Boton variante="secundario" aria-label="Actualizar venta" onClick={() => void recargar()} cargando={operacion.procesando}><RefreshCw className="h-4 w-4" /></Boton>}
    </div>} />
    {!accion && avisoError}
    {venta.cobro && <AvisoVenta>Cobro registrado junto a la venta: pago #{venta.cobro.pagoId}, sesión #{venta.cobro.sesionCajaId}. No requiere aprobación en Caja. La anulación de una venta cobrada requiere una reversión financiera fuera de este ajuste.</AvisoVenta>}
    <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_22rem]">
      <Tarjeta className="p-5"><dl className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
        <DatoVenta etiqueta="Estado"><EstadoVenta estado={o.estado} /></DatoVenta><DatoVenta etiqueta="Sucursal">{o.sucursal}</DatoVenta><DatoVenta etiqueta="Cliente">{o.cliente ?? "Venta sin cliente identificado"}</DatoVenta>
        <DatoVenta etiqueta="Moneda">{o.moneda}</DatoVenta><DatoVenta etiqueta="Usuario">{o.usuario}</DatoVenta><DatoVenta etiqueta="Fecha de venta">{fechaVenta(o.fechaVenta)}</DatoVenta>
        <DatoVenta etiqueta="Modalidad">{o.esCredito ? "Crédito" : "Contado"}</DatoVenta>
        {o.esCredito && <DatoVenta etiqueta="Vencimiento del crédito">{o.fechaVencimientoCredito ? mostrarFechaCivil(o.fechaVencimientoCredito) : "Sin fecha de vencimiento"}</DatoVenta>}
      </dl><p className="mt-5 text-sm text-slate-500">{o.estado === "BORRADOR" ? "Borrador guardado. Puede revisarlo antes de confirmar." : o.estado === "CONFIRMADA" ? "Confirmada. El inventario solo se descuenta al completar la venta." : o.estado === "COMPLETADA" ? "Venta completada. Consulte los movimientos generados más abajo." : "Venta anulada, conservada para consulta."}</p></Tarjeta>
      <Tarjeta className="p-5"><TotalesVenta {...o} /></Tarjeta>
    </div>
    <Tarjeta className="overflow-hidden"><h2 className="border-b border-slate-200 p-5 font-bold">Detalle de productos</h2>
      <TablaVentas etiqueta="Detalle de venta" columnas={["SKU / Producto", "Presentación", "Cantidad", "Conversión", "Precio", "Descuento", "Impuesto", "Subtotal", "Total línea", "Costos"]}>{venta.detalles.map((d) => <tr key={d.id}>
        <td><span className="font-semibold">{d.producto}</span><p className="text-xs text-slate-500">{d.sku}</p></td><td>{d.presentacion ?? "Unidad base"}</td><td>{n.format(d.cantidad)} {d.unidad ?? "—"}</td>
        <td><p>× {d.factorConversion === null ? "—" : n.format(d.factorConversion)}</p><p className="text-xs text-slate-500">{d.cantidadBase === null ? "—" : n.format(d.cantidadBase)} {d.unidadBase ?? "—"}</p></td>
        <td>{dineroVenta(d.precioUnitario, o.moneda)} / {d.unidad ?? "—"}</td><td>{n.format(d.porcentajeDescuento)} %<br />{dineroVenta(d.montoDescuento, o.moneda)}</td><td>{dineroVenta(d.impuesto, o.moneda)}</td><td>{dineroVenta(d.subtotalNeto, o.moneda)}</td><td className="font-semibold">{dineroVenta(d.totalLinea, o.moneda)}</td>
        <td><p>Base: {costoBaseVenta(d.costoUnitarioBase, o.moneda)}{d.costoUnitarioBase !== null && ` / ${d.unidadBase ?? "—"}`}</p><p>Total: {dineroVenta(d.costoTotal, o.moneda)}</p></td>
      </tr>)}</TablaVentas>
    </Tarjeta>
    <Tarjeta className="overflow-hidden"><h2 className="border-b border-slate-200 p-5 font-bold">Movimientos de inventario</h2>{venta.movimientos.length ? <TablaVentas etiqueta="Movimientos de venta" columnas={["Id", "Concepto", "Tipo", "Fecha"]}>{venta.movimientos.map((m) => <tr key={m.id}><td>#{m.id}</td><td>{m.concepto}</td><td>{m.tipo}</td><td>{fechaVenta(m.fecha)}</td></tr>)}</TablaVentas> : <EstadoVacio titulo="Sin movimientos" descripcion="No se han registrado movimientos de inventario para esta venta." />}</Tarjeta>
    <Tarjeta className="p-5"><h2 className="mb-5 font-bold">Histórico de la venta</h2><dl className="grid gap-5 sm:grid-cols-2"><DatoVenta etiqueta="Fecha completada">{fechaVenta(o.fechaCompletada)}</DatoVenta><DatoVenta etiqueta="Fecha de anulación">{fechaVenta(o.fechaAnulacion)}</DatoVenta><DatoVenta etiqueta="Motivo de anulación">{o.motivoAnulacion ?? "—"}</DatoVenta><DatoVenta etiqueta="Clave de operación">{o.claveOperacion ?? "—"}</DatoVenta></dl></Tarjeta>
    <Modal abierto={accion !== null} cerrar={cerrar} titulo={accion === "anular" ? "Anular venta" : accion === "completar" ? "Completar venta" : "Confirmar venta"} ancho={accion === "completar" && requiereCobro ? "max-w-3xl" : "max-w-lg"}>
      <FormularioOperacion abierto={accion !== null} onSubmit={() => void ejecutar()}>
        <p className="font-semibold">{o.numeroVenta} · {dineroVenta(o.total, o.moneda)}</p>
        <p className="text-sm text-slate-600">{accion === "confirmar" ? "La venta quedará confirmada y no podrá editarse. Confirmar no descuenta inventario." : accion === "completar" ? "Se validará la disponibilidad y se descontará el inventario en una única operación." : o.estado === "COMPLETADA" ? "Esta anulación generará una reversión de inventario." : "La venta se conservará como anulada para consulta."}</p>
        {accion === "completar" && hayIntentoVenta(clave) && <AvisoVenta>Hay una clave de cierre conservada. Se consultará el estado antes de reintentar con la misma UUID.</AvisoVenta>}
        {accion === "completar" && (o.esCredito ? <AvisoVenta>Venta a crédito: se genera CxC, sin cobrar el total en Caja.</AvisoVenta> : requiereCobro ? <CobroAlCompletar ref={datosCobro} total={o.total} disabled={operacion.procesando} /> : <AvisoVenta>Sin importe a cobrar. No se generará pago ni movimiento de Caja.</AvisoVenta>)}
        {accion === "completar" && validacion && <AvisoVenta error>{validacion}</AvisoVenta>}
        {accion === "anular" && <AreaTexto etiqueta="Motivo de anulación" value={motivo} onChange={(e) => setMotivo(e.target.value)} required minLength={3} maxLength={500} rows={3} error={validacion} disabled={operacion.procesando} />}
        {avisoError}
        <div className="flex flex-wrap justify-end gap-3 border-t border-slate-200 pt-4"><Boton type="button" variante="secundario" disabled={operacion.procesando} onClick={cerrar}>Cancelar</Boton><Boton type="submit" variante={accion === "anular" ? "peligro" : "primario"} cargando={operacion.procesando} disabled={concurrencia}>{accion === "anular" ? "Anular definitivamente" : accion === "completar" ? "Cerrar venta" : "Confirmar"}</Boton></div>
      </FormularioOperacion>
    </Modal>
  </div>;
}

function FormularioOperacion({ abierto, onSubmit, children }: { abierto: boolean; onSubmit: () => void; children: React.ReactNode }) {
  const ref = useRef<HTMLFormElement>(null);
  useEffect(() => {
    if (!abierto) return;
    const anterior = document.activeElement as HTMLElement | null;
    const dialogo = ref.current?.closest('[role="dialog"]');
    const elementos = () => Array.from(dialogo?.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled), a[href]') ?? []);
    (ref.current?.querySelector<HTMLElement>("textarea, button") ?? elementos()[0])?.focus();
    const teclado = (e: KeyboardEvent) => {
      if (e.key !== "Tab") return;
      const foco = elementos(); const primero = foco[0]; const ultimo = foco.at(-1);
      if (e.shiftKey && document.activeElement === primero) { e.preventDefault(); ultimo?.focus(); }
      if (!e.shiftKey && document.activeElement === ultimo) { e.preventDefault(); primero?.focus(); }
    };
    document.addEventListener("keydown", teclado);
    return () => { document.removeEventListener("keydown", teclado); anterior?.focus(); };
  }, [abierto]);
  return <form ref={ref} className="space-y-4 p-5" onSubmit={(e) => { e.preventDefault(); onSubmit(); }}>{children}</form>;
}
