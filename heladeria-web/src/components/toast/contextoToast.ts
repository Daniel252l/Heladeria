import { createContext } from 'react'

export type TipoToast = 'exito' | 'error' | 'info'

export interface ToastNuevo {
  titulo: string
  descripcion?: string
  tipo?: TipoToast
}

export interface ValorToast {
  mostrar: (toast: ToastNuevo) => void
}

export const ContextoToast = createContext<ValorToast | null>(null)
