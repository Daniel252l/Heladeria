import { useCallback, useState, type FormEvent } from 'react'
import { apiCartera, carteraApi, leerPaginas } from '../../api/cartera'
import { cajaApi } from '../../api/caja'
import { ErrorApi } from '../../api/clienteHttp'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { AreaTexto, Boton, Entrada, Seleccion } from '../../components/ui/Controles'
import { EstadoCargando, EstadoVacio } from '../../components/ui/Estados'
import type { DetalleCartera, SolicitudPagoCartera, TipoCartera } from '../../types/cartera'
import type { ReferenciasCaja, SesionCaja, SesionCajaResumen } from '../../types/caja'
import { centavos, dineroCaja, operable } from '../caja/utilidadesCaja'
import { useCargaVentas, useRecursoVentas } from '../ventas/useDatosVentas'
import { AvisoCartera, ErrorCartera, ModalCartera } from './ComponentesCartera'
import { admitePago, referenciaInsegura } from './utilidadesCartera'
import { useIntentoCartera } from './useIntentoCartera'

export function FormularioPagoCartera({ tipo, id, intento, cerrar, actualizada }: {
  tipo: TipoCartera; id: number; intento: ReturnType<typeof useIntentoCartera<SolicitudPagoCartera>>;
  cerrar: () => void; actualizada: (cuenta: DetalleCartera, mensaje: string) => void;
}) {
  const { sucursalActiva } = useAutenticacion()
  const cuenta = useRecursoVentas<DetalleCartera>(`${apiCartera(tipo)}/${id}`)
  const referencias = useRecursoVentas<ReferenciasCaja>(`${apiCartera(tipo)}/referencias`)
  const cargar = useCallback((signal: AbortSignal) => leerPaginas<SesionCajaResumen>('/api/caja/sesiones?abierta=true', signal), [])
  const sesiones = useCargaVentas('sesiones-abiertas-cartera', cargar)
  const [sesionId, setSesionId] = useState(String(intento.payload?.sesionCajaId ?? ''))
  const caja = useRecursoVentas<SesionCaja>(sesionId ? `/api/caja/sesiones/${sesionId}` : null)
  const [monto, setMonto] = useState(String(intento.payload?.monto ?? ''))
  const [formaId, setFormaId] = useState(String(intento.payload?.formaPagoId ?? ''))
  const [referencia, setReferencia] = useState(intento.payload?.referencia ?? '')
  const [observacion, setObservacion] = useState(intento.payload?.observacion ?? '')
  const [validacion, setValidacion] = useState('')
  const [confirmando, setConfirmando] = useState(false)
  const c = cuenta.datos?.encabezado; const s = caja.datos
  const formas = referencias.datos?.formasPago.filter((f) => tipo === 'cobrar' || f.codigo === 'EFECTIVO') ?? []
  const forma = formas.find((f) => String(f.id) === formaId)
  const importe = centavos(monto)
  const saldo = c ? Math.round(c.saldoPendiente * 100) : 0
  const habilitada = c && admitePago(c) && s && operable(s) && s.encabezado.monedaId === c.monedaId
  const listo = habilitada && importe !== null && importe > 0 && importe <= saldo && forma && (!forma.requiereReferencia || referencia.trim()) && !referenciaInsegura(referencia)
  const recargar = () => { cuenta.recargar(); caja.recargar(); sesiones.recargar(); referencias.recargar() }
  async function enviar(e?: FormEvent) {
    e?.preventDefault()
    if (!intento.pendiente && !listo) { setValidacion('Revise sesión abierta, moneda, monto positivo no mayor al saldo y referencia segura.'); return }
    if (!intento.pendiente && !confirmando) { setValidacion(''); setConfirmando(true); return }
    const r = await intento.enviar(async () => {
      const [actual, sesion] = await Promise.all([carteraApi.obtener(tipo, id), cajaApi.obtener(Number(sesionId))])
      if (!admitePago(actual.encabezado)) throw new ErrorApi('La cuenta no admite pagos.', 409, { codigo: actual.encabezado.saldoPendiente === 0 || actual.encabezado.estado === 'PAGADA' ? 'CUENTA_YA_PAGADA' : 'VALIDACION_CARTERA' })
      if (!operable(sesion)) throw new ErrorApi('Caja cerrada', 409, { codigo: 'CAJA_NO_ABIERTA' })
      if (actual.encabezado.versionFila !== c?.versionFila) throw new ErrorApi('Cuenta modificada', 409, { codigo: tipo === 'cobrar' ? 'CONCURRENCIA_CXC' : 'CONCURRENCIA_CXP' })
      if (sesion.encabezado.versionFila !== s?.encabezado.versionFila) throw new ErrorApi('Caja modificada', 409, { codigo: 'CONCURRENCIA_CAJA' })
      return { claveOperacion: crypto.randomUUID(), versionFila: actual.encabezado.versionFila, versionCaja: sesion.encabezado.versionFila,
        sesionCajaId: sesion.encabezado.id, monto: importe! / 100, formaPagoId: forma!.id, referencia: referencia.trim() || null, observacion: observacion.trim() || null }
    }, (datos) => carteraApi.aplicar(tipo, id, datos))
    if (r) actualizada(r.cuenta, r.reintento ? 'Operación ya registrada. Resultado recuperado sin duplicarla.' : tipo === 'cobrar' ? 'Abono registrado correctamente.' : 'Pago registrado correctamente.')
    else { setConfirmando(false); recargar() }
  }
  async function consultar() {
    const actual = await intento.ejecutar(() => carteraApi.obtener(tipo, id))
    if (actual) {
      if (actual.historial.some((a) => a.claveOperacion === intento.uuid)) {
        intento.resolver(); actualizada(actual, 'Operación localizada en el historial. No se envió otro pago.')
      } else {
        recargar(); setValidacion('El historial no confirma todavía este intento. Si está disponible, reintente exactamente la misma solicitud. No inicie otra operación.')
      }
    }
  }
  const errorCarga = cuenta.error ?? sesiones.error ?? referencias.error ?? caja.error
  return <ModalCartera titulo={tipo === 'cobrar' ? 'Registrar abono' : 'Registrar pago'} cerrar={() => { if (!intento.procesando) cerrar() }}>
    <form className="space-y-5" onSubmit={(e) => void enviar(e)}>
      {intento.pendiente && <AvisoCartera>Hay una operación pendiente de confirmar. No cambie sus datos ni inicie otro pago. Se conservarán exactamente la UUID y el payload original.</AvisoCartera>}
      {intento.almacenamientoFallido && <AvisoCartera error>No se puede conservar la clave de operación. Habilite el almacenamiento de sesión antes de operar.</AvisoCartera>}
      {cuenta.cargando || sesiones.cargando || referencias.cargando ? <EstadoCargando /> : errorCarga ? <ErrorCartera error={errorCarga} recargar={recargar} /> : c && <>
        <p className="text-sm text-slate-600">{c.numeroDocumentoOrigen} · {c.tercero} · {c.moneda}</p>
        {!admitePago(c) && <AvisoCartera>La cuenta está pagada, anulada o es histórica de solo consulta. No admite un nuevo pago.</AvisoCartera>}
        <dl className="grid gap-3 rounded-xl bg-slate-50 p-4 sm:grid-cols-2" aria-live="polite"><div><dt>Saldo pendiente</dt><dd className="font-bold">{dineroCaja(c.saldoPendiente, c.moneda)}</dd></div><div><dt>Saldo posterior estimado</dt><dd className="font-bold">{importe !== null && importe <= saldo ? dineroCaja((saldo - importe) / 100, c.moneda) : 'Revise el monto'}</dd></div></dl>
        <fieldset disabled={intento.procesando || intento.pendiente || confirmando || !admitePago(c)} className="grid gap-4 sm:grid-cols-2">
          <Seleccion etiqueta="Sesión de caja" required value={sesionId} onChange={(e) => setSesionId(e.target.value)}><option value="">Seleccione una sesión abierta</option>
            {sesiones.datos?.filter((v) => v.esFase8 && v.estado === 'ABIERTA' && !v.fechaCierre && v.empresaId === sucursalActiva?.empresaId && v.sucursalId === sucursalActiva?.id && v.monedaId === c.monedaId).map((v) => <option key={v.id} value={v.id}>{v.caja} · {v.numeroApertura ?? `Sesión ${v.id}`} · {v.moneda}</option>)}
          </Seleccion>
          <Seleccion etiqueta="Forma de pago" required value={formaId} onChange={(e) => { setFormaId(e.target.value); setReferencia('') }}><option value="">Seleccione una forma</option>{formas.map((f) => <option key={f.id} value={f.id}>{f.nombre}</option>)}</Seleccion>
          <Entrada etiqueta="Monto" type="number" min="0.01" max={c.saldoPendiente} step="0.01" required value={monto} onChange={(e) => setMonto(e.target.value)} />
          {forma && (!forma.esEfectivo || forma.requiereReferencia) && <Entrada etiqueta="Referencia de autorización" required={forma.requiereReferencia} maxLength={30} autoComplete="off" value={referencia} onChange={(e) => setReferencia(e.target.value)} ayuda="Solo autorización segura. Nunca PAN, CVV, número de tarjeta ni expiración." error={referenciaInsegura(referencia) ? 'No ingrese información sensible de tarjeta.' : undefined} />}
          <div className="sm:col-span-2"><AreaTexto etiqueta="Observación" maxLength={500} rows={2} value={observacion} onChange={(e) => setObservacion(e.target.value)} /></div>
        </fieldset>
        {sesiones.datos?.length === 0 && <EstadoVacio titulo="No hay sesiones de caja abiertas" descripcion="Debe abrir una caja autorizada antes de registrar la operación." />}
        {sesionId && caja.cargando ? <EstadoCargando mensaje="Consultando caja…" /> : s && <AvisoCartera>{s.encabezado.caja} · {s.encabezado.estado}. Efectivo esperado: {s.efectivoEsperado === null ? 'No disponible' : dineroCaja(s.efectivoEsperado, s.encabezado.moneda)}.
          {tipo === 'pagar' && importe !== null && s.efectivoEsperado !== null && importe > Math.round(s.efectivoEsperado * 100) && <p>El efectivo mostrado parece insuficiente. El servidor comprobará la disponibilidad al enviar.</p>}</AvisoCartera>}
      </>}
      {confirmando && <AvisoCartera>Confirme {tipo === 'cobrar' ? 'el abono' : 'el pago'} de {dineroCaja((importe ?? 0) / 100, c?.moneda ?? null)}. Se verificarán nuevamente cuenta y caja.</AvisoCartera>}
      {validacion && <AvisoCartera error>{validacion}</AvisoCartera>}{intento.error && <ErrorCartera error={intento.error} accion />}
      {intento.pendiente && !intento.payload && <AvisoCartera>La pestaña fue recargada y ya no conserva el formulario. Consulte el historial; si no se localiza la operación, concilie con administración. No se generará otro pago automáticamente.</AvisoCartera>}
      <div className="flex flex-wrap justify-end gap-3">
        <Boton type="button" variante="secundario" disabled={intento.procesando} onClick={cerrar}>Cerrar</Boton>
        {intento.pendiente ? <><Boton type="button" variante="secundario" cargando={intento.procesando} onClick={() => void consultar()}>Consultar resultado</Boton>{intento.payload && <Boton type="button" cargando={intento.procesando} onClick={() => void enviar()}>Reintentar misma operación</Boton>}</>
          : <>{confirmando && <Boton type="button" variante="secundario" disabled={intento.procesando} onClick={() => setConfirmando(false)}>Editar datos</Boton>}
            <Boton type="submit" cargando={intento.procesando} disabled={!listo || cuenta.cargando || caja.cargando || Boolean(errorCarga) || intento.almacenamientoFallido}>{confirmando ? 'Confirmar operación' : 'Revisar operación'}</Boton></>}
      </div>
    </form>
  </ModalCartera>
}
