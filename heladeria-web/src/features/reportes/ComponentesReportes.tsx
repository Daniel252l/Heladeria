import type { ReactNode } from 'react'
import { ErrorApi } from '../../api/clienteHttp'
import { Insignia, Tarjeta } from '../../components/ui/Controles'
import { EstadoError, EstadoVacio } from '../../components/ui/Estados'
import type { PeriodoReporte } from '../../types/reportes'
import { mostrarFechaCivil } from '../../utils/fechas'
import { TablaVentas } from '../ventas/ComponentesVentas'

export function EstadoReporte({ estado }: { estado: string }) {
  const tono = ['COMPLETADA', 'COBRADA', 'PAGADA', 'VIGENTE', 'ENTRADA'].includes(estado) ? 'positivo'
    : ['ANULADA', 'VENCIDA', 'SALIDA'].includes(estado) ? 'negativo'
    : ['PARCIAL', 'PENDIENTE', 'REQUIERE_REVISION'].includes(estado) ? 'advertencia'
    : ['CONFIRMADA', 'RECIBIDA'].includes(estado) ? 'info' : 'neutro'
  return <Insignia tono={tono}>{estado}</Insignia>
}
export function ErrorReporte({ error, recargar }: { error: Error; recargar: () => void }) {
  const api = error instanceof ErrorApi ? error : null
  const mensaje = api?.estado === 403 ? 'No tiene permisos para consultar este reporte.'
    : api?.estado === 401 ? 'La sesión ha expirado. Vuelva a iniciar sesión.'
    : api?.estado === 400 ? api.message
    : 'No fue posible consultar la información. Intente nuevamente.'
  return <div><EstadoError mensaje={mensaje} reintentar={api?.estado === 403 || api?.estado === 401 ? undefined : recargar} />
    {api?.traceId && <p className="break-all px-5 pb-4 text-xs text-slate-500">Referencia de soporte: {api.traceId}</p>}</div>
}
export function PeriodoConsultado({ periodo, sucursal }: { periodo: PeriodoReporte; sucursal: string }) {
  return <p className="rounded-xl border border-teal-100 bg-teal-50 px-4 py-3 text-sm text-teal-900" aria-label="Período consultado">
    Período consultado: <strong>{mostrarFechaCivil(periodo.fechaDesde)} — {mostrarFechaCivil(periodo.fechaHasta)}</strong>
    {' · '}{sucursal} · {periodo.zonaHoraria}
  </p>
}
export function ResumenReporte({ titulo, valores }: { titulo: string; valores: [string, ReactNode][] }) {
  return <section aria-label={titulo}><h2 className="mb-3 font-semibold text-slate-700">{titulo}</h2>
    <dl className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">{valores.map(([etiqueta, valor]) =>
      <div key={etiqueta} className="min-w-0 rounded-xl border border-slate-200 bg-white p-4">
        <dt className="text-sm text-slate-600">{etiqueta}</dt><dd className="mt-2 break-words text-xl font-bold tabular-nums">{valor}</dd>
      </div>)}</dl>
  </section>
}
export function TablaReporte<T>({ titulo, filas, columnas }: { titulo: string; filas: T[]; columnas: [string, (fila: T) => ReactNode][] }) {
  return <Tarjeta className="min-w-0 overflow-hidden">
    <h2 className="border-b border-slate-200 px-5 py-4 font-bold">{titulo}</h2>
    {filas.length > 0 && <p className="px-5 pt-3 text-xs text-slate-500">Desplace horizontalmente la tabla para consultar todas las columnas.</p>}
    {!filas.length ? <EstadoVacio titulo="No hay registros para estos filtros" descripcion="Revise los criterios aplicados y la sucursal seleccionada." />
      : <div className="reporte-tabla" tabIndex={0} role="region" aria-label={`Desplazar tabla: ${titulo}`}>
        <TablaVentas etiqueta={titulo} columnas={columnas.map(([nombre]) => nombre)}>
          {filas.map((fila, i) => <tr key={i}>{columnas.map(([nombre, celda]) => <td key={nombre}>{celda(fila)}</td>)}</tr>)}
        </TablaVentas>
      </div>}
  </Tarjeta>
}
