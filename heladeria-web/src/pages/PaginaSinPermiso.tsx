import { ArrowLeft, ShieldX } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Tarjeta } from '../components/ui/Controles'

export function PaginaSinPermiso() {
  return <div className="grid min-h-[65vh] place-items-center"><Tarjeta className="max-w-lg p-8 text-center"><span className="mx-auto grid h-14 w-14 place-items-center rounded-full bg-rose-50 text-rose-600"><ShieldX className="h-7 w-7" /></span><p className="mt-5 text-xs font-bold uppercase tracking-[0.18em] text-rose-600">Error 403</p><h1 className="mt-2 text-xl font-bold text-slate-900">Acceso no autorizado</h1><p className="mt-2 text-sm leading-6 text-slate-500">Su sesión es válida, pero no tiene el permiso requerido para ver este módulo.</p><Link className="mt-6 inline-flex min-h-10 items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600" to="/dashboard"><ArrowLeft className="h-4 w-4" /> Volver al dashboard</Link></Tarjeta></div>
}
