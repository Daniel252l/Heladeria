import { fireEvent, render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { ErrorApi } from '../api/clienteHttp'
import { ContextoAutenticacion, type ValorAutenticacion } from '../auth/contextoAutenticacion'
import { PaginaLogin } from './PaginaLogin'

describe('inicio de sesión', () => {
  it('muestra un mensaje específico y seguro cuando ya existe una sesión activa', async () => {
    const iniciarSesion = vi.fn().mockRejectedValue(new ErrorApi(
      'detalle interno que no debe mostrarse',
      409,
      { codigo: 'SESION_ACTIVA' },
    ))
    const valor: ValorAutenticacion = {
      sesion: null,
      usuario: null,
      sucursalActiva: null,
      sucursales: [],
      autenticado: false,
      cargando: false,
      iniciarSesion,
      cerrarSesion: vi.fn(),
      renovarSesion: vi.fn(),
      cambiarContrasena: vi.fn(),
      seleccionarSucursal: vi.fn(),
      tienePermiso: () => false,
    }

    render(
      <MemoryRouter initialEntries={['/login']}>
        <ContextoAutenticacion.Provider value={valor}>
          <PaginaLogin />
        </ContextoAutenticacion.Provider>
      </MemoryRouter>,
    )

    fireEvent.change(screen.getByLabelText('Usuario o correo'), { target: { value: 'admin' } })
    fireEvent.change(screen.getByLabelText('Contraseña'), { target: { value: 'Contrasena#Prueba1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Iniciar sesión' }))

    const alerta = await screen.findByRole('alert')
    expect(alerta.textContent).toBe(
      'Este usuario ya tiene una sesión activa. Cierre la sesión anterior antes de volver a ingresar.',
    )
    expect(alerta.textContent).not.toContain('detalle interno')
    expect(iniciarSesion).toHaveBeenCalledTimes(1)
  })
})
