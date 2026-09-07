import { Link } from 'react-router-dom'

export function PaginaNoEncontrada() {
  return <main className="grid min-h-screen place-items-center bg-slate-100 px-5 text-center"><div><p className="text-sm font-bold uppercase tracking-widest text-teal-700">Error 404</p><h1 className="mt-3 text-3xl font-bold text-slate-950">Página no encontrada</h1><p className="mt-2 text-slate-500">La dirección solicitada no forma parte de esta fase.</p><Link to="/dashboard" className="mt-6 inline-flex min-h-11 items-center justify-center rounded-lg bg-teal-700 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-teal-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2">Volver al inicio</Link></div></main>
}
