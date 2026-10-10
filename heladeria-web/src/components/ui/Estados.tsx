import { AlertCircle, Inbox, LoaderCircle, RefreshCw } from 'lucide-react'
import { Boton } from './Controles'

export function EstadoCargando({ mensaje = 'Cargando información…' }: { mensaje?: string }) {
  return (
    <div className="flex min-h-52 items-center justify-center gap-3 text-sm text-slate-500" role="status">
      <LoaderCircle className="h-5 w-5 animate-spin text-teal-700" aria-hidden="true" />
      {mensaje}
    </div>
  )
}

export function EstadoVacio({
  titulo = 'No hay registros',
  descripcion = 'Pruebe con otros filtros o agregue el primer registro.',
  accion,
}: {
  titulo?: string
  descripcion?: string
  accion?: React.ReactNode
}) {
  return (
    <div className="flex min-h-52 flex-col items-center justify-center px-6 text-center">
      <span className="mb-3 rounded-full bg-slate-100 p-3 text-slate-500"><Inbox className="h-6 w-6" aria-hidden="true" /></span>
      <p className="font-semibold text-slate-800">{titulo}</p>
      <p className="mt-1 max-w-md text-sm text-slate-500">{descripcion}</p>
      {accion && <div className="mt-4">{accion}</div>}
    </div>
  )
}

export function EstadoError({ mensaje, reintentar, titulo = 'No pudimos cargar la información' }: { mensaje: string; reintentar?: () => void; titulo?: string }) {
  return (
    <div className="flex min-h-52 flex-col items-center justify-center px-6 text-center" role="alert">
      <span className="mb-3 rounded-full bg-rose-50 p-3 text-rose-600"><AlertCircle className="h-6 w-6" aria-hidden="true" /></span>
      <p className="font-semibold text-slate-800">{titulo}</p>
      <p className="mt-1 max-w-md text-sm text-slate-500">{mensaje}</p>
      {reintentar && <Boton variante="secundario" className="mt-4" onClick={reintentar}><RefreshCw className="h-4 w-4" /> Reintentar</Boton>}
    </div>
  )
}
