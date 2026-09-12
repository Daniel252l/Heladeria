import type { RespuestaAutenticacion, SesionActual } from '../types/auth'

export const respuestaAutenticacion: RespuestaAutenticacion = {
  exito: true,
  accessToken: 'access-token-renovado',
  expiraAccessTokenUtc: '2026-09-12T12:00:00Z',
  requiereCambioContrasena: false,
  usuario: {
    id: 1,
    nombreUsuario: 'admin',
    correo: 'admin@heladeria.local',
    rolId: 'ADMIN',
    rol: 'Administrador',
    sucursalActiva: {
      id: 4,
      nombre: 'Sucursal principal',
      empresaId: 4,
      empresa: 'Heladería',
      esPrincipal: true,
    },
    sucursales: [{
      id: 4,
      nombre: 'Sucursal principal',
      empresaId: 4,
      empresa: 'Heladería',
      esPrincipal: true,
    }],
  },
}

export const sesionActual: SesionActual = {
  usuario: respuestaAutenticacion.usuario,
  requiereCambioContrasena: false,
  permisos: ['INVENTARIO.CONSULTAR'],
}

export function respuestaJson(datos: unknown, estado = 200) {
  return new Response(JSON.stringify(datos), {
    status: estado,
    headers: { 'Content-Type': 'application/json' },
  })
}
