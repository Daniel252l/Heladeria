import { Construction } from 'lucide-react'
import { Tarjeta } from '../components/ui/Controles'
import { EncabezadoPagina } from '../components/ui/EncabezadoPagina'

export function PaginaPendiente({ titulo, descripcion }: { titulo: string; descripcion: string }) {
  return (
    <div className="space-y-7">
      <EncabezadoPagina titulo={titulo} descripcion={descripcion} />
      <Tarjeta className="p-8 text-center">
        <span className="mx-auto grid h-14 w-14 place-items-center rounded-xl bg-amber-50 text-amber-700"><Construction className="h-7 w-7" /></span>
        <h2 className="mt-5 text-lg font-bold text-slate-900">Estructura preparada</h2>
        <p className="mx-auto mt-2 max-w-xl text-sm leading-6 text-slate-500">Esta ruta forma parte de la navegación aprobada, pero su pantalla funcional no corresponde a Fase Visual 1.</p>
      </Tarjeta>
    </div>
  )
}
