import { useEffect, useRef, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { PERMISOS } from '../../auth/permisos'
import { ErrorApi } from '../../api/clienteHttp'
import { Insignia, Tarjeta } from '../../components/ui/Controles'
import { Modal } from '../../components/ui/Modal'
import { EstadoError } from '../../components/ui/Estados'
import { PaginaSinPermiso } from '../../pages/PaginaSinPermiso'
import { PaginaNoEncontrada } from '../../pages/PaginaNoEncontrada'
import type { SesionCaja } from '../../types/caja'
import { dineroCaja, ingresosCaja, mensajeCaja, egresosCaja, enlaceCaja, tiposCaja } from './utilidadesCaja'
export { AvisoVenta as AvisoCaja, DatoVenta as DatoCaja, TablaVentas as TablaCaja } from '../ventas/ComponentesVentas'
import './caja.css'

export function NavegacionCaja() {
  const { tienePermiso } = useAutenticacion()
  return <nav aria-label="Secciones de caja" className="flex flex-wrap gap-2">
    <Link className={enlaceCaja} to="/caja">Resumen</Link><Link className={enlaceCaja} to="/caja/cajas">Cajas</Link>
    <Link className={enlaceCaja} to="/caja/sesiones">Sesiones e histórico</Link>
    {tienePermiso(PERMISOS.cajaAbrir) && <Link className={enlaceCaja} to="/caja/abrir">Abrir caja</Link>}
  </nav>
}
export function EstadoCaja({ estado }: { estado: string }) {
  return <Insignia tono={['ABIERTA', 'Activo', 'Vigente'].includes(estado) ? 'positivo' : 'neutro'}>{estado}</Insignia>
}
export function TipoMovimientoCaja({ tipo }: { tipo: string }) {
  const tono = tipo === 'APERTURA_CAJA' ? 'info'
    : ['EGRESO_CAJA', 'PAGO_CUENTA_POR_PAGAR'].includes(tipo) ? 'advertencia'
    : ['INGRESO_CAJA', 'COBRO_VENTA', 'COBRO_CUENTA_POR_COBRAR'].includes(tipo) ? 'positivo' : 'neutro'
  return <Insignia tono={tono}>{tiposCaja[tipo] ?? tipo}</Insignia>
}
export function ErrorCaja({ error, recargar, titulo }: { error: Error; recargar?: () => void; titulo?: string }) {
  if (error instanceof ErrorApi && error.estado === 403) return <PaginaSinPermiso />
  if (error instanceof ErrorApi && error.estado === 404) return <PaginaNoEncontrada />
  return <EstadoError titulo={titulo} mensaje={`${mensajeCaja(error)}${error instanceof ErrorApi && error.codigo ? ` (${error.codigo})` : ''}`} reintentar={recargar} />
}
export function DiferenciaCaja({ valor, moneda }: { valor: number; moneda: string | null }) {
  return <span className={valor < 0 ? 'text-amber-800' : valor > 0 ? 'text-teal-800' : 'text-slate-700'}>
    {valor > 0 ? 'Sobrante: +' : valor < 0 ? 'Faltante: ' : 'Sin diferencia: '}{dineroCaja(valor, moneda)}
  </span>
}
export function ResumenCaja({ sesion }: { sesion: SesionCaja }) {
  const moneda = sesion.encabezado.moneda
  const cifras: [string, number | null][] = [['Efectivo esperado', sesion.efectivoEsperado], ['Saldo teórico', sesion.saldoTeorico],
    ['Saldo inicial', sesion.encabezado.saldoInicial], ['Ingresos operativos', ingresosCaja(sesion)], ['Egresos', egresosCaja(sesion)]]
  return <section aria-label="Resumen financiero" className="space-y-3"><dl className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
    {cifras.map(([nombre, valor]) => <div key={nombre} className={`min-w-0 rounded-xl border p-4 ${nombre === 'Efectivo esperado' ? 'border-teal-200 bg-teal-50' : 'border-slate-200 bg-white'}`}>
      <dt className="text-sm text-slate-600">{nombre}</dt><dd className="mt-2 break-words text-xl font-bold tabular-nums text-slate-900">{valor === null ? 'No disponible' : dineroCaja(valor, moneda)}</dd></div>)}
  </dl><p className="text-sm text-slate-600">El saldo teórico incluye todas las formas de pago. El efectivo esperado es únicamente el dinero en caja; no incluye tarjeta. La apertura no es un ingreso operativo.</p></section>
}
export function SeccionCaja({ titulo, children }: { titulo: string; children: ReactNode }) {
  return <Tarjeta className="min-w-0 overflow-hidden"><h2 className="border-b border-slate-200 px-4 py-4 font-bold text-slate-900 sm:px-5">{titulo}</h2>{children}</Tarjeta>
}
export function ModalCaja({ titulo, cerrar, children }: { titulo: string; cerrar: () => void; children: ReactNode }) {
  const contenido = useRef<HTMLDivElement>(null)
  useEffect(() => {
    const anterior = document.activeElement as HTMLElement | null
    const dialogo = contenido.current?.closest<HTMLElement>('[role="dialog"]')
    const overflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    const controles = () => Array.from(dialogo?.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled), a[href], [tabindex="0"]') ?? [])
    controles()[0]?.focus()
    const atrapar = (e: KeyboardEvent) => {
      if (e.key !== 'Tab') return
      const lista = controles(); const primero = lista[0]; const ultimo = lista.at(-1)
      if (e.shiftKey && document.activeElement === primero) { e.preventDefault(); ultimo?.focus() }
      if (!e.shiftKey && document.activeElement === ultimo) { e.preventDefault(); primero?.focus() }
    }
    dialogo?.addEventListener('keydown', atrapar)
    return () => { dialogo?.removeEventListener('keydown', atrapar); document.body.style.overflow = overflow; if (anterior?.isConnected) anterior.focus() }
  }, [])
  return <Modal abierto titulo={titulo} cerrar={cerrar} ancho="max-w-3xl"><div ref={contenido} className="caja-modal min-w-0 p-4 sm:p-6">{children}</div></Modal>
}
