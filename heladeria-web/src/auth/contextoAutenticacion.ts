import { createContext } from 'react'
import type {
  CambioContrasena,
  CredencialesInicioSesion,
  SesionActual,
  SucursalSesion,
  UsuarioSesion,
} from '../types/auth'

export interface ValorAutenticacion {
  sesion: SesionActual | null
  usuario: UsuarioSesion | null
  sucursalActiva: SucursalSesion | null
  sucursales: SucursalSesion[]
  autenticado: boolean
  cargando: boolean
  iniciarSesion: (credenciales: CredencialesInicioSesion) => Promise<boolean>
  cerrarSesion: () => Promise<void>
  renovarSesion: () => Promise<string | null>
  cambiarContrasena: (solicitud: CambioContrasena) => Promise<void>
  seleccionarSucursal: (sucursalId: number) => Promise<void>
  tienePermiso: (codigo: string) => boolean
}

export const ContextoAutenticacion = createContext<ValorAutenticacion | null>(null)
