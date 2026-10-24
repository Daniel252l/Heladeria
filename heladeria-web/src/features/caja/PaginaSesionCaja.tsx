import { useState } from 'react'
import { Link, useParams, useSearchParams } from 'react-router-dom'
import { RefreshCw, ArrowDownLeft, ArrowUpRight, Calculator, LockKeyhole } from 'lucide-react'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { PERMISOS } from '../../auth/permisos'
import { Boton } from '../../components/ui/Controles'
import { EncabezadoPagina } from '../../components/ui/EncabezadoPagina'
import { EstadoCargando } from '../../components/ui/Estados'
import { PaginaNoEncontrada } from '../../pages/PaginaNoEncontrada'
import type { SesionCaja } from '../../types/caja'
import { useRecursoCaja } from './useDatosCaja'
import { AvisoCaja, DatoCaja, DiferenciaCaja, ErrorCaja, EstadoCaja, NavegacionCaja, ResumenCaja, SeccionCaja, TablaCaja, TipoMovimientoCaja } from './ComponentesCaja'
import { dineroCaja, fechaCaja, operable, tiposCaja, totalTipo } from './utilidadesCaja'
import { FormularioSesionCaja } from './FormulariosSesionCaja'
import { CobrarVentaCaja } from './CobrarVentaCaja'

export function PaginaSesionCaja() {
  const { id } = useParams(); const valido = /^\d+$/.test(id ?? '') && Number.isSafeInteger(Number(id)) && Number(id) > 0
  const datos = useRecursoCaja<SesionCaja>(valido ? `/api/caja/sesiones/${id}` : null)
  if (!valido) return <PaginaNoEncontrada />
  if (datos.cargando) return <EstadoCargando />
  if (datos.error) return <ErrorCaja error={datos.error} recargar={datos.recargar} />
  return datos.datos && <SesionOperativa key={`${datos.datos.encabezado.id}:${datos.datos.encabezado.versionFila}`} inicial={datos.datos} recargar={datos.recargar} />
}
function SesionOperativa({ inicial, recargar }: { inicial: SesionCaja; recargar: () => void }) {
  const [parametros] = useSearchParams()
  const solicitada = parametros.get('accion')
  const { tienePermiso, sucursalActiva } = useAutenticacion()
  const [sesion, setSesion] = useState(inicial); const [mensaje, setMensaje] = useState('')
  const [modal, setModal] = useState<'cobro' | 'ingreso' | 'egreso' | 'arqueo' | 'cierre' | null>(
    solicitada === 'ingreso' || solicitada === 'egreso' || solicitada === 'arqueo' || solicitada === 'cierre' ? solicitada : null)
  const e = sesion.encabezado; const activa = operable(sesion); const moneda = e.moneda
  const operar = tienePermiso(PERMISOS.cajaOperar)
  const manual = operar && tienePermiso(PERMISOS.cajaMovimientoManual)
  const permisos = { cobro: operar && tienePermiso(PERMISOS.cajaCobrar), ingreso: manual, egreso: manual, arqueo: operar, cierre: tienePermiso(PERMISOS.cajaCerrar) }
  const vigente = sesion.arqueos.find((a) => a.vigente)
  function actualizada(nueva: SesionCaja, texto = 'Sesión actualizada con los datos confirmados por el servidor.') { setSesion(nueva); setModal(null); setMensaje(texto) }
  return <div className="caja-vista space-y-6"><EncabezadoPagina titulo={e.caja} descripcion={`${e.numeroApertura ?? `Sesión #${e.id}`} · ${sucursalActiva?.nombre ?? ''}`} acciones={<Boton variante="secundario" disabled={modal !== null} onClick={recargar}><RefreshCw size={18} /> Recargar sesión</Boton>} /><NavegacionCaja />
    {mensaje && <AvisoCaja>{mensaje}</AvisoCaja>}
    <SeccionCaja titulo="Sesión de caja"><dl className="grid gap-5 p-4 sm:grid-cols-2 sm:p-5 xl:grid-cols-4"><DatoCaja etiqueta="Estado"><EstadoCaja estado={e.estado} /></DatoCaja><DatoCaja etiqueta="Apertura">{e.numeroApertura ?? 'Histórica'}</DatoCaja><DatoCaja etiqueta="Fecha apertura">{fechaCaja(e.fechaApertura)}</DatoCaja><DatoCaja etiqueta="Usuario apertura">{e.usuarioApertura}</DatoCaja><DatoCaja etiqueta="Moneda">{moneda ?? 'No disponible'}</DatoCaja><DatoCaja etiqueta="Saldo inicial">{dineroCaja(e.saldoInicial, moneda)}</DatoCaja></dl></SeccionCaja>
    {!e.esFase8 && <AvisoCaja>Sesión histórica: solo lectura. Los importes operativos no disponibles requieren revisión explícita; no se reconstruyen automáticamente.</AvisoCaja>}
    <ResumenCaja sesion={sesion} />
    {activa ? <section aria-label="Acciones de caja" className="space-y-3 rounded-xl border border-slate-200 bg-white p-4"><div className="caja-acciones grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
      {permisos.ingreso && <Boton onClick={() => setModal('ingreso')}><ArrowDownLeft size={18} /> Registrar ingreso</Boton>}
      {permisos.egreso && <Boton variante="secundario" onClick={() => setModal('egreso')}><ArrowUpRight size={18} /> Registrar egreso</Boton>}
      {permisos.arqueo && <Boton variante="secundario" onClick={() => setModal('arqueo')}><Calculator size={18} /> Registrar arqueo</Boton>}
      {permisos.cierre && <Boton variante="peligro" disabled={!vigente} onClick={() => setModal('cierre')}><LockKeyhole size={18} /> Cerrar sesión de caja</Boton>}
    </div>{permisos.cierre && !vigente && <p className="text-sm text-slate-600">Para cerrar se requiere un arqueo vigente después del último movimiento.</p>}
    {permisos.cobro && <details className="border-t border-slate-200 pt-3"><summary className="cursor-pointer rounded font-semibold text-slate-600 focus-visible:outline-2 focus-visible:outline-teal-600">Cobros anteriores pendientes</summary>
      <p className="my-3 text-sm text-slate-600">Solo para ventas que quedaron completadas sin pago en el flujo anterior. Las nuevas ventas de contado se cobran al completar en Ventas/POS.</p>
      <Boton variante="secundario" onClick={() => setModal('cobro')}>Cobrar venta anterior</Boton>
    </details>}</section>
      : <AvisoCaja>Sesión en modo de solo lectura. No se permiten nuevos cobros, movimientos, arqueos ni cierres.</AvisoCaja>}
    {e.fechaCierre && <SeccionCaja titulo="Cierre confirmado"><dl className="grid gap-4 p-4 sm:grid-cols-2 xl:grid-cols-4">
      <DatoCaja etiqueta="Número cierre">{e.numeroCierre}</DatoCaja><DatoCaja etiqueta="Fecha cierre">{fechaCaja(e.fechaCierre)}</DatoCaja><DatoCaja etiqueta="Usuario cierre">{sesion.usuarioCierre ?? (e.usuarioCierreId ? `Usuario #${e.usuarioCierreId}` : '—')}</DatoCaja>
      <DatoCaja etiqueta="Efectivo esperado final">{e.efectivoEsperadoCierre === null ? '—' : dineroCaja(e.efectivoEsperadoCierre, moneda)}</DatoCaja><DatoCaja etiqueta="Efectivo contado final">{e.efectivoContadoCierre === null ? '—' : dineroCaja(e.efectivoContadoCierre, moneda)}</DatoCaja>
      <DatoCaja etiqueta="Diferencia cierre">{e.diferenciaCierre === null ? '—' : <DiferenciaCaja valor={e.diferenciaCierre} moneda={moneda} />}</DatoCaja>
      <DatoCaja etiqueta="Ingresos finales">{e.totalIngresosCierre === null ? '—' : dineroCaja(e.totalIngresosCierre, moneda)}</DatoCaja><DatoCaja etiqueta="Egresos finales">{e.totalEgresosCierre === null ? '—' : dineroCaja(e.totalEgresosCierre, moneda)}</DatoCaja>
    </dl></SeccionCaja>}
    <SeccionCaja titulo="Totales por tipo"><dl className="grid gap-4 p-4 sm:grid-cols-2 xl:grid-cols-4">{Object.entries(tiposCaja).map(([codigo, nombre]) => <DatoCaja key={codigo} etiqueta={nombre}>{dineroCaja(totalTipo(sesion, codigo), moneda)}</DatoCaja>)}</dl></SeccionCaja>
    <SeccionCaja titulo="Totales por forma de pago"><TablaCaja etiqueta="Totales por forma de pago" columnas={['Forma', 'Efectivo', 'Saldo inicial', 'Ingresos', 'Egresos', 'Neto']}>
      {sesion.totalesPorFormaPago.map((f) => <tr key={f.formaPagoId ?? 'sin-forma'}><td>{f.formaPago}</td><td>{f.esEfectivo ? 'Sí' : 'No'}</td><td>{dineroCaja(f.saldoInicial, moneda)}</td><td>{dineroCaja(f.ingresos, moneda)}</td><td>{dineroCaja(f.egresos, moneda)}</td><td>{dineroCaja(f.neto, moneda)}</td></tr>)}
    </TablaCaja>{!sesion.totalesPorFormaPago.length && <p className="p-4 text-sm text-slate-600">Sin movimientos por forma de pago.</p>}</SeccionCaja>
    <SeccionCaja titulo="Movimientos"><TablaCaja etiqueta="Movimientos de caja" columnas={['Documento', 'Tipo', 'Fecha', 'Monto', 'Forma', 'Referencia', 'Usuario', 'Venta', 'Observación']}>
      {sesion.movimientos.map((m) => <tr key={m.id}><td>{m.numeroDocumento ?? '—'}</td><td><TipoMovimientoCaja tipo={m.tipo} /></td><td>{fechaCaja(m.fecha)}</td><td>{dineroCaja(m.monto, moneda)}</td><td>{m.formaPago ?? '—'}</td><td>{m.referencia ?? '—'}</td><td>{m.usuario}</td><td>{m.ventaId && tienePermiso(PERMISOS.ventasConsultar) ? <Link className="font-semibold text-teal-700" to={`/ventas/${m.ventaId}`}>{m.numeroVenta}</Link> : m.numeroVenta ?? '—'}</td><td>{m.observacion ?? '—'}</td></tr>)}
    </TablaCaja>{!sesion.movimientos.length && <p className="p-4 text-sm text-slate-600">Sin movimientos registrados.</p>}</SeccionCaja>
    <SeccionCaja titulo="Cobros registrados">{!sesion.cobros.length ? <p className="p-4 text-sm text-slate-600">Sin cobros registrados.</p> : <div className="divide-y divide-slate-200">{sesion.cobros.map((c) => <section aria-label={`Cobro ${c.numeroVenta}`} className="p-4" key={c.id}>
      <h3 className="font-semibold">{c.numeroVenta} · {dineroCaja(c.total, c.moneda)}</h3><p className="mb-3 text-sm text-slate-600">{fechaCaja(c.fecha)} · {sesion.movimientos.find((m) => m.pagoId === c.id && m.usuarioId === c.usuarioId)?.usuario ?? 'Nombre de usuario no disponible'}</p>
      <TablaCaja etiqueta={`Detalle cobro ${c.numeroVenta}`} columnas={['Forma', 'Aplicado', 'Recibido', 'Vuelto', 'Referencia']}>
        {c.detalles.map((d) => <tr key={d.id}><td>{d.formaPago}</td><td>{dineroCaja(d.aplicado, c.moneda)}</td><td>{dineroCaja(d.recibido, c.moneda)}</td><td>{dineroCaja(d.vuelto, c.moneda)}</td><td>{d.referencia ?? '—'}</td></tr>)}
      </TablaCaja></section>)}</div>}</SeccionCaja>
    <SeccionCaja titulo="Arqueos"><TablaCaja etiqueta="Histórico de arqueos" columnas={['Fecha', 'Esperado', 'Contado', 'Diferencia', 'Usuario', 'Observaciones', 'Vigente']}>
      {sesion.arqueos.map((a) => <tr key={a.id}><td>{fechaCaja(a.fecha)}</td><td>{dineroCaja(a.esperado, moneda)}</td><td>{dineroCaja(a.contado, moneda)}</td><td><DiferenciaCaja valor={a.diferencia} moneda={moneda} /></td><td>{a.usuario}</td><td>{a.observaciones ?? '—'}</td><td>{a.vigente ? <EstadoCaja estado="Vigente" /> : 'No'}</td></tr>)}
    </TablaCaja>{!sesion.arqueos.length && <p className="p-4 text-sm text-slate-600">No hay arqueos registrados.</p>}</SeccionCaja>
    {activa && modal && permisos[modal] && (modal === 'cobro' ? <CobrarVentaCaja sesion={sesion} cerrar={() => setModal(null)} actualizada={actualizada} />
      : <FormularioSesionCaja tipo={modal === 'ingreso' || modal === 'egreso' ? 'movimiento' : modal} operacionInicial={modal === 'egreso' ? 'EGRESO' : 'INGRESO'} sesion={sesion} cerrar={() => setModal(null)} actualizada={actualizada} />)}
  </div>
}
