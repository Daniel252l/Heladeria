import { useContext } from 'react'
import { ContextoToast } from './contextoToast'

export function useToast() {
  const contexto = useContext(ContextoToast)
  if (!contexto) throw new Error('useToast debe utilizarse dentro de ProveedorToast.')
  return contexto
}
