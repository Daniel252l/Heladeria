import { render } from '@testing-library/react'
import { vi } from 'vitest'
import { MemoryRouter } from 'react-router-dom'
import { ContextoAutenticacion, type ValorAutenticacion } from '../auth/contextoAutenticacion'
import { ProveedorToast } from '../components/toast/ProveedorToast'
import { sesionActual } from './datosAutenticacion'

export function renderSeguridad(children: React.ReactNode, permisos: string[], ruta = '/') {
  const valor: ValorAutenticacion = {
    sesion: { ...sesionActual, permisos }, usuario: sesionActual.usuario,
    sucursalActiva: sesionActual.usuario.sucursalActiva, sucursales: sesionActual.usuario.sucursales,
    autenticado: true, cargando: false,
    iniciarSesion: vi.fn(), cerrarSesion: vi.fn(), renovarSesion: vi.fn(), cambiarContrasena: vi.fn(), seleccionarSucursal: vi.fn(),
    tienePermiso: (permiso) => permisos.includes(permiso),
  }
  return render(<MemoryRouter initialEntries={[ruta]}><ContextoAutenticacion.Provider value={valor}><ProveedorToast>{children}</ProveedorToast></ContextoAutenticacion.Provider></MemoryRouter>)
}
