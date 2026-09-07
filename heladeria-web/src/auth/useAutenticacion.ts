import { useContext } from 'react'
import { ContextoAutenticacion } from './contextoAutenticacion'

export function useAutenticacion() {
  const contexto = useContext(ContextoAutenticacion)
  if (!contexto) throw new Error('useAutenticacion debe utilizarse dentro de ProveedorAutenticacion.')
  return contexto
}
