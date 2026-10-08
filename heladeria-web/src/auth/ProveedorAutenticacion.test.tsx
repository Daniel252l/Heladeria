import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { respuestaAutenticacion, respuestaJson, sesionActual } from '../test/datosAutenticacion'

describe('restauración de sesión', () => {
  beforeEach(() => {
    vi.resetModules()
    vi.restoreAllMocks()
  })

  it('aplica zona de sesión al restaurar, cambiar sucursal y cerrar sesión', async () => {
    let zona = 'America/Guatemala'
    vi.stubGlobal('fetch', vi.fn(async (entrada: RequestInfo | URL) => {
      const ruta = new URL(String(entrada)).pathname
      if (ruta === '/api/autenticacion/seleccionar-sucursal') zona = 'Europe/Madrid'
      if (ruta === '/api/autenticacion/sesion') return respuestaJson({ ...sesionActual, usuario: {
        ...sesionActual.usuario, sucursalActiva: { ...sesionActual.usuario.sucursalActiva, zonaHoraria: zona },
      } })
      return respuestaJson(respuestaAutenticacion)
    }))
    const [{ ProveedorAutenticacion }, { useAutenticacion }, { ProveedorToast }, { fechaHoraLocal }] = await Promise.all([
      import('./ProveedorAutenticacion'), import('./useAutenticacion'), import('../components/toast/ProveedorToast'), import('../utils/fechas'),
    ])
    function Pantalla() {
      const auth = useAutenticacion()
      return <><output>{auth.cargando ? 'cargando' : fechaHoraLocal(new Date('2026-10-08T20:42:00Z'))}</output>
        <button onClick={() => void auth.seleccionarSucursal(9)}>Cambiar zona</button>
        <button onClick={() => void auth.cerrarSesion()}>Salir</button></>
    }
    render(<ProveedorToast><ProveedorAutenticacion><Pantalla /></ProveedorAutenticacion></ProveedorToast>)
    await screen.findByText('2026-10-08T14:42')
    fireEvent.click(screen.getByText('Cambiar zona'))
    await screen.findByText('2026-10-08T22:42')
    fireEvent.click(screen.getByText('Salir'))
    await screen.findByText('2026-10-08T20:42')
  })

  it('restaura usuario, permisos y sucursal mediante refresh al iniciar sin access token', async () => {
    const fetchSimulado = vi.fn(async (entrada: RequestInfo | URL, opciones?: RequestInit) => {
      const ruta = new URL(String(entrada)).pathname
      if (ruta === '/api/autenticacion/renovar') {
        expect(opciones?.method).toBe('POST')
        expect(opciones?.credentials).toBe('include')
        expect(new Headers(opciones?.headers).has('Authorization')).toBe(false)
        return respuestaJson(respuestaAutenticacion)
      }
      if (ruta === '/api/autenticacion/sesion') {
        expect(new Headers(opciones?.headers).get('Authorization')).toBe('Bearer access-token-renovado')
        return respuestaJson(sesionActual)
      }
      return respuestaJson({}, 404)
    })
    vi.stubGlobal('fetch', fetchSimulado)
    const [{ ProveedorAutenticacion }, { useAutenticacion }, { ProveedorToast }] = await Promise.all([
      import('./ProveedorAutenticacion'),
      import('./useAutenticacion'),
      import('../components/toast/ProveedorToast'),
    ])

    function EstadoSesion() {
      const autenticacion = useAutenticacion()
      return <div data-testid="sesion">{autenticacion.cargando ? 'cargando' : `${autenticacion.usuario?.nombreUsuario}:${autenticacion.sesion?.permisos.join(',')}:${autenticacion.sucursalActiva?.nombre}`}</div>
    }

    render(<ProveedorToast><ProveedorAutenticacion><EstadoSesion /></ProveedorAutenticacion></ProveedorToast>)

    expect(screen.getByTestId('sesion').textContent).toBe('cargando')
    await waitFor(() => expect(screen.getByTestId('sesion').textContent).toBe('admin:INVENTARIO.CONSULTAR:Sucursal principal'))
    expect(fetchSimulado).toHaveBeenCalledTimes(2)
  })
})
