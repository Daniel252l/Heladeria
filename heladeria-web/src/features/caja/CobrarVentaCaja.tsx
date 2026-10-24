import { useState, type FormEvent } from 'react'
import { cajaApi } from '../../api/caja'
import { ErrorApi } from '../../api/clienteHttp'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { Boton, Entrada } from '../../components/ui/Controles'
import { EstadoCargando, EstadoVacio } from '../../components/ui/Estados'
import { Paginacion } from '../../components/ui/Paginacion'
import type { Paginado } from '../../types/api'
import type { FormaCobro, ReferenciasCaja, SesionCaja, VentaPendienteCaja } from '../../types/caja'
import { AvisoCaja, ErrorCaja, ModalCaja, TablaCaja } from './ComponentesCaja'
import { centavos, dineroCaja, fechaCaja } from './utilidadesCaja'
import { claveIntentoCobro, CobroIncierto, cobrarConReconciliacion, useOperacionCaja, useRecursoCaja } from './useDatosCaja'
import { CamposPago } from './CamposPago'
import { validarPagos, type LineaPago } from './pagosFormulario'

export function CobrarVentaCaja({ sesion, cerrar, actualizada }: { sesion: SesionCaja; cerrar: () => void; actualizada: (s: SesionCaja, mensaje: string) => void }) {
  const [venta, setVenta] = useState<VentaPendienteCaja | null>(null)
  const refs = useRecursoCaja<ReferenciasCaja>('/api/caja/referencias')
  // El formulario controla su cierre mientras está enviando una operación.
  return venta && refs.datos ? <FormularioCobro key={venta.id} venta={venta} sesion={sesion} referencias={refs.datos} cerrar={cerrar} actualizada={actualizada} />
    : <ModalCaja titulo="Ventas pendientes de cobro" cerrar={cerrar}>{refs.cargando ? <EstadoCargando /> : refs.error ? <ErrorCaja error={refs.error} recargar={refs.recargar} /> : <Pendientes seleccionar={setVenta} />}</ModalCaja>
}
function Pendientes({ seleccionar }: { seleccionar: (v: VentaPendienteCaja) => void }) {
  const [pagina, setPagina] = useState(1); const [texto, setTexto] = useState(''); const [busqueda, setBusqueda] = useState('')
  const datos = useRecursoCaja<Paginado<VentaPendienteCaja>>(`/api/caja/ventas-pendientes?${new URLSearchParams({ busqueda, pagina: String(pagina), tamanioPagina: '10' })}`)
  return <div className="caja-vista space-y-4"><form className="flex flex-wrap items-end gap-3" onSubmit={(e) => { e.preventDefault(); setBusqueda(texto.trim()); setPagina(1) }}><Entrada etiqueta="Buscar venta pendiente" maxLength={150} value={texto} onChange={(e) => setTexto(e.target.value)} /><Boton type="submit">Buscar ventas</Boton></form>
    {datos.cargando ? <EstadoCargando /> : datos.error ? <ErrorCaja error={datos.error} recargar={datos.recargar} /> : datos.datos && <>
      {!datos.datos.elementos.length ? <EstadoVacio titulo="No hay ventas pendientes" descripcion="Solo se muestran ventas elegibles para cobro según el servidor." /> : <TablaCaja etiqueta="Ventas pendientes" columnas={['Venta', 'Fecha', 'Cliente', 'Total', 'Pendiente', 'Moneda', 'Acciones']}>
        {datos.datos.elementos.map((v) => <tr key={v.id}><td>{v.numeroVenta}</td><td>{fechaCaja(v.fecha)}</td><td>{v.cliente ?? 'Sin cliente identificado'}</td><td>{dineroCaja(v.total, v.moneda)}</td><td>{dineroCaja(v.saldoPendiente, v.moneda)}</td><td>{v.moneda}</td><td><Boton onClick={() => seleccionar(v)}>Cobrar {v.numeroVenta}</Boton></td></tr>)}
      </TablaCaja>}<Paginacion {...datos.datos} cambiar={setPagina} entidad="venta" /></>}
  </div>
}
function PendientesActualizados() {
  const datos = useRecursoCaja<Paginado<VentaPendienteCaja>>('/api/caja/ventas-pendientes?pagina=1&tamanioPagina=10')
  return <section aria-label="Pendientes actualizados" className="rounded-lg border border-slate-200 p-3 text-sm">
    <h3 className="font-semibold">Pendientes actualizados</h3>
    {datos.cargando ? <EstadoCargando /> : datos.error ? <ErrorCaja error={datos.error} recargar={datos.recargar} /> : datos.datos && <>
      <p>{datos.datos.totalRegistros} ventas pendientes. Cierre este formulario para seleccionar otra venta.</p>
      <ul>{datos.datos.elementos.map((v) => <li key={v.id}>{v.numeroVenta} · {dineroCaja(v.saldoPendiente, v.moneda)}</li>)}</ul>
    </>}
  </section>
}
function FormularioCobro({ venta, sesion, referencias, cerrar, actualizada }: { venta: VentaPendienteCaja; sesion: SesionCaja; referencias: ReferenciasCaja; cerrar: () => void; actualizada: (s: SesionCaja, mensaje: string) => void }) {
  const { usuario, sucursalActiva } = useAutenticacion(); const op = useOperacionCaja()
  const [lineas, setLineas] = useState<LineaPago[]>([{ formaId: String(referencias.formasPago[0]?.id ?? ''), aplicado: venta.total.toFixed(2), recibido: venta.total.toFixed(2), referencia: '' }])
  const [confirmar, setConfirmar] = useState(false); const [validacion, setValidacion] = useState(''); const [bloqueada, setBloqueada] = useState(false)
  const total = Math.round(venta.total * 100)
  const aplicado = lineas.reduce((s, l) => s + (centavos(l.aplicado) ?? 0), 0)
  const incierto = op.error instanceof CobroIncierto
  const pagosValidos = (): FormaCobro[] | null => validarPagos(lineas, referencias, venta.total)
  async function enviar(e: FormEvent) {
    e.preventDefault(); if (bloqueada || incierto) return
    const pagos = pagosValidos()
    if (!pagos || !usuario || !sucursalActiva) { setValidacion('Revise formas de pago, montos, referencias requeridas y la suma exacta del total.'); return }
    setValidacion('')
    if (!confirmar) { setConfirmar(true); return }
    const clave = claveIntentoCobro(usuario.id, sucursalActiva.empresaId, sucursalActiva.id, sesion.encabezado.id, venta.id)
    const resultado = await op.ejecutar(async () => {
      try { return await cobrarConReconciliacion(sesion, venta, pagos, clave) }
      catch (error) {
        // No conservar una referencia rechazada ni volver a enviarla automáticamente.
        setLineas((ls) => ls.map((l) => ({ ...l, referencia: '' }))); setConfirmar(false)
        if (error instanceof ErrorApi && error.codigo === 'VENTA_YA_COBRADA') setBloqueada(true)
        throw error
      }
    })
    if (resultado) actualizada(resultado.sesion, resultado.reintento ? 'Cobro ya registrado: resultado verificado, sin duplicarlo.' : `Cobro registrado para ${resultado.cobro.numeroVenta}. Vuelto: ${dineroCaja(resultado.cobro.detalles.reduce((s, d) => s + Math.round(d.vuelto * 100), 0) / 100, resultado.cobro.moneda)}.`)
  }
  async function consultar() {
    const resultado = await op.ejecutar(() => cajaApi.obtener(sesion.encabezado.id))
    if (resultado) actualizada(resultado, resultado.cobros.some((c) => c.ventaId === venta.id) ? 'La venta ya tiene un cobro registrado en esta sesión.' : 'Sesión actualizada. Consulte los pendientes antes de reintentar; se conservará la misma UUID.')
  }
  return <ModalCaja titulo={`Cobrar ${venta.numeroVenta}`} cerrar={() => { if (!op.procesando) cerrar() }}><form className="space-y-5" onSubmit={(e) => void enviar(e)}>
    <p className="text-sm text-slate-600">{venta.cliente ?? 'Sin cliente identificado'} · {venta.moneda}</p>
    <dl className="grid gap-3 rounded-xl bg-slate-50 p-4 sm:grid-cols-3" aria-live="polite">{[['Total venta', total], ['Total aplicado', aplicado], ['Diferencia por asignar', total - aplicado]].map(([n, v]) => <div key={n}><dt className="text-sm text-slate-600">{n}</dt><dd className="font-bold tabular-nums">{dineroCaja(Number(v) / 100, venta.moneda)}</dd></div>)}</dl>
    <CamposPago lineas={lineas} cambiar={setLineas} referencias={referencias} disabled={op.procesando || confirmar || incierto || bloqueada} />
    {confirmar && <AvisoCaja>Confirme el cobro de {dineroCaja(venta.total, venta.moneda)}. El servidor validará montos, vuelto y elegibilidad de la venta.</AvisoCaja>}
    {validacion && <AvisoCaja error>{validacion}</AvisoCaja>}{op.error && <ErrorCaja error={op.error} />}
    {bloqueada && <><AvisoCaja>La venta ya fue cobrada. No se ofrecerá otro cobro con una UUID nueva.</AvisoCaja><PendientesActualizados /></>}
    <div className="flex flex-wrap justify-end gap-3"><Boton type="button" variante="secundario" disabled={op.procesando} onClick={cerrar}>Cancelar</Boton>
      {confirmar && <Boton type="button" variante="secundario" disabled={op.procesando} onClick={() => setConfirmar(false)}>Editar pago</Boton>}
      {(op.error || bloqueada) && <Boton type="button" variante="secundario" cargando={op.procesando} onClick={() => void consultar()}>Consultar resultado y pendientes</Boton>}
      {!bloqueada && !incierto && <Boton type="submit" cargando={op.procesando} disabled={aplicado !== total || !pagosValidos()}>{confirmar ? 'Confirmar cobro' : 'Revisar cobro'}</Boton>}
    </div>
  </form></ModalCaja>
}
