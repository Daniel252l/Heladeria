import { useCallback, useMemo, useState } from 'react'
import { AlertCircle, CheckCircle2, Info, X } from 'lucide-react'
import { ContextoToast, type ToastNuevo, type TipoToast } from './contextoToast'

interface ToastVisible extends ToastNuevo {
  id: number
  tipo: TipoToast
}

let siguienteId = 1

export function ProveedorToast({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<ToastVisible[]>([])
  const quitar = useCallback((id: number) => {
    setToasts((actuales) => actuales.filter((toast) => toast.id !== id))
  }, [])
  const mostrar = useCallback((nuevo: ToastNuevo) => {
    const id = siguienteId++
    setToasts((actuales) => [...actuales, { ...nuevo, id, tipo: nuevo.tipo ?? 'info' }])
    window.setTimeout(() => quitar(id), 4600)
  }, [quitar])
  const valor = useMemo(() => ({ mostrar }), [mostrar])

  return (
    <ContextoToast.Provider value={valor}>
      {children}
      <div className="fixed right-4 top-4 z-[70] flex w-[min(92vw,24rem)] flex-col gap-3" aria-live="polite" aria-atomic="false">
        {toasts.map((toast) => <ToastItem key={toast.id} toast={toast} cerrar={() => quitar(toast.id)} />)}
      </div>
    </ContextoToast.Provider>
  )
}

function ToastItem({ toast, cerrar }: { toast: ToastVisible; cerrar: () => void }) {
  const estilo = {
    exito: 'border-emerald-200 text-emerald-700',
    error: 'border-rose-200 text-rose-700',
    info: 'border-sky-200 text-sky-700',
  }[toast.tipo]
  const Icono = toast.tipo === 'exito' ? CheckCircle2 : toast.tipo === 'error' ? AlertCircle : Info
  return (
    <div className={`flex gap-3 rounded-xl border bg-white p-4 shadow-lg ${estilo}`} role={toast.tipo === 'error' ? 'alert' : 'status'}>
      <Icono className="mt-0.5 h-5 w-5 shrink-0" aria-hidden="true" />
      <div className="min-w-0 flex-1">
        <p className="text-sm font-bold text-slate-900">{toast.titulo}</p>
        {toast.descripcion && <p className="mt-1 text-sm leading-5 text-slate-600">{toast.descripcion}</p>}
      </div>
      <button className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700" onClick={cerrar} aria-label="Cerrar notificación"><X className="h-4 w-4" /></button>
    </div>
  )
}
