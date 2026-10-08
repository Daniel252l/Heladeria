export interface SucursalSesion {
  id: number
  nombre: string
  empresaId: number
  empresa: string
  esPrincipal: boolean
  zonaHoraria?: string
}

export interface UsuarioSesion {
  id: number
  nombreUsuario: string
  correo: string
  rolId: string
  rol: string
  sucursalActiva: SucursalSesion
  sucursales: SucursalSesion[]
}

export interface RespuestaAutenticacion {
  exito: boolean
  accessToken: string
  expiraAccessTokenUtc: string
  usuario: UsuarioSesion
  requiereCambioContrasena: boolean
}

export interface SesionActual {
  usuario: UsuarioSesion
  requiereCambioContrasena: boolean
  permisos: string[]
}

export interface CredencialesInicioSesion {
  identificador: string
  contrasena: string
  sucursalId?: number
}

export interface CambioContrasena {
  contrasenaActual: string
  nuevaContrasena: string
  confirmacion: string
}
