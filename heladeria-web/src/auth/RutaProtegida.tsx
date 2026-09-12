import type { ReactNode } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { EstadoCargando } from '../components/ui/Estados'
import { useAutenticacion } from './useAutenticacion'

export function RutaProtegida() {
  const { autenticado, cargando, sesion } = useAutenticacion()
  const ubicacion = useLocation()

  if (cargando) {
    return <div className="grid min-h-screen place-items-center bg-slate-100"><EstadoCargando mensaje="Validando sesión…" /></div>
  }
  if (!autenticado) return <Navigate to="/login" replace state={{ desde: ubicacion.pathname }} />
  if (sesion?.requiereCambioContrasena && ubicacion.pathname !== '/cambiar-contrasena') {
    return <Navigate to="/cambiar-contrasena" replace />
  }
  return <Outlet />
}

export function RutaConPermiso({ permiso, children }: { permiso: string; children?: ReactNode }) {
  const { autenticado, cargando, sesion, tienePermiso } = useAutenticacion()
  const ubicacion = useLocation()

  if (cargando) {
    return <div className="grid min-h-screen place-items-center bg-slate-100"><EstadoCargando mensaje="Validando autorización…" /></div>
  }
  if (!autenticado) return <Navigate to="/login" replace state={{ desde: ubicacion.pathname }} />
  if (sesion?.requiereCambioContrasena) return <Navigate to="/cambiar-contrasena" replace />
  if (!tienePermiso(permiso)) return <Navigate to="/403" replace state={{ desde: ubicacion.pathname }} />

  return children ?? <Outlet />
}
