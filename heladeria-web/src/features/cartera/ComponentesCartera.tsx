import { Link } from 'react-router-dom'
import { ErrorApi } from '../../api/clienteHttp'
import { Insignia } from '../../components/ui/Controles'
import { EstadoError, EstadoVacio } from '../../components/ui/Estados'
import { PaginaSinPermiso } from '../../pages/PaginaSinPermiso'
import { PaginaNoEncontrada } from '../../pages/PaginaNoEncontrada'
import type { CuentaCartera, ResumenCartera, TipoCartera } from '../../types/cartera'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { PERMISOS } from '../../auth/permisos'
import { dineroCaja } from '../caja/utilidadesCaja'
import { mensajeCartera } from './utilidadesCartera'
export { AvisoCaja as AvisoCartera, DatoCaja as DatoCartera, SeccionCaja as SeccionCartera, TablaCaja as TablaCartera, ModalCaja as ModalCartera } from '../caja/ComponentesCaja'

export function ErrorCartera({ error, recargar, accion = false }: { error: Error; recargar?: () => void; accion?: boolean }) {
  if (error instanceof ErrorApi && error.estado === 403) return <PaginaSinPermiso />
  if (error instanceof ErrorApi && error.estado === 404) return <PaginaNoEncontrada />
  return <EstadoError titulo={accion ? 'No se pudo completar la operación' : undefined}
    mensaje={`${mensajeCartera(error)}${error instanceof ErrorApi && error.codigo ? ` (${error.codigo})` : ''}`} reintentar={recargar} />
}
export function EstadoCartera({ estado }: { estado: string }) {
  const tono = estado === 'PAGADA' || estado === 'VIGENTE' || estado === 'APLICADO' ? 'positivo'
    : estado === 'VENCIDA' ? 'negativo' : estado === 'PARCIAL' || estado === 'PENDIENTE' ? 'advertencia' : 'neutro'
  return <Insignia tono={tono}><span className="whitespace-nowrap">{estado}</span></Insignia>
}
export function ResumenCarteraVista({ datos }: { datos: ResumenCartera[] }) {
  if (!datos.length) return <EstadoVacio titulo="Sin saldos pendientes para estos filtros" descripcion="El resumen no contiene monedas con cuentas pendientes." />
  return <section aria-label="Resumen de cartera" className="space-y-4">{datos.map((r) => <section key={r.monedaId} aria-label={`Resumen ${r.moneda}`}>
    <h2 className="mb-2 text-sm font-semibold text-slate-600">Resumen · {r.moneda}</h2>
    <dl className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5">{[
      ['Total pendiente', dineroCaja(r.totalPendiente, r.moneda)], ['Total vigente', dineroCaja(r.totalVigente, r.moneda)],
      ['Total vencido', dineroCaja(r.totalVencido, r.moneda)], ['Cuentas pendientes', r.cantidadPendientes], ['Cuentas vencidas', r.cantidadVencidas],
    ].map(([etiqueta, valor], i) => <div key={etiqueta} className={`min-w-0 rounded-xl border p-4 ${i === 0 ? 'border-teal-200 bg-teal-50' : 'border-slate-200 bg-white'}`}>
      <dt className="text-sm text-slate-600">{etiqueta}</dt><dd className="mt-2 break-words text-xl font-bold tabular-nums">{valor}</dd>
    </div>)}</dl>
  </section>)}</section>
}
export function OrigenCartera({ cuenta, tipo }: { cuenta: CuentaCartera; tipo: TipoCartera }) {
  const { tienePermiso } = useAutenticacion()
  const ruta = tipo === 'cobrar' && tienePermiso(PERMISOS.ventasConsultar) ? `/ventas/${cuenta.documentoOrigenId}` : null
  return ruta ? <Link className="font-semibold text-teal-800 underline" to={ruta}>{cuenta.numeroDocumentoOrigen}</Link> : <span className="font-semibold">{cuenta.numeroDocumentoOrigen}</span>
}
