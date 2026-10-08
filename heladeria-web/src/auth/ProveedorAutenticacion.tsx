import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import {
  configurarRenovacion,
  establecerAccessToken,
  solicitar,
} from '../api/clienteHttp'
import { useToast } from '../components/toast/useToast'
import type {
  CambioContrasena,
  CredencialesInicioSesion,
  RespuestaAutenticacion,
  SesionActual,
} from '../types/auth'
import { ContextoAutenticacion } from './contextoAutenticacion'
import type { ApiResponse } from '../types/api'
import { configurarZonaHoraria } from '../utils/fechas'

export function ProveedorAutenticacion({ children }: { children: React.ReactNode }) {
  const [sesion, setSesion] = useState<SesionActual | null>(null)
  const [cargando, setCargando] = useState(true)
  const inicializado = useRef(false)
  const { mostrar } = useToast()

  const limpiarSesion = useCallback(() => {
    configurarZonaHoraria()
    establecerAccessToken(null)
    setSesion(null)
  }, [])

  const aplicarRespuesta = useCallback(async (respuesta: RespuestaAutenticacion) => {
    establecerAccessToken(respuesta.accessToken)
    if (respuesta.requiereCambioContrasena) {
      configurarZonaHoraria(respuesta.usuario.sucursalActiva.zonaHoraria)
      setSesion({
        usuario: respuesta.usuario,
        requiereCambioContrasena: true,
        permisos: [],
      })
      return
    }
    try {
      const actual = await solicitar<SesionActual>('/api/autenticacion/sesion', {
        omitirRenovacion: true,
      })
      configurarZonaHoraria(actual.usuario.sucursalActiva.zonaHoraria)
      setSesion(actual)
    } catch (excepcion) {
      configurarZonaHoraria()
      establecerAccessToken(null)
      setSesion(null)
      throw excepcion
    }
  }, [])

  const renovarSesion = useCallback(async () => {
    try {
      const respuesta = await solicitar<RespuestaAutenticacion>('/api/autenticacion/renovar', {
        method: 'POST',
        omitirAutorizacion: true,
        omitirRenovacion: true,
      })
      await aplicarRespuesta(respuesta)
      return respuesta.accessToken
    } catch {
      limpiarSesion()
      return null
    }
  }, [aplicarRespuesta, limpiarSesion])

  useEffect(() => {
    configurarRenovacion(renovarSesion, () => {
      limpiarSesion()
      mostrar({
        tipo: 'info',
        titulo: 'La sesión ha expirado',
        descripcion: 'Inicie sesión nuevamente para continuar.',
      })
    })
    if (inicializado.current) return
    inicializado.current = true
    renovarSesion().finally(() => setCargando(false))
  }, [limpiarSesion, mostrar, renovarSesion])

  const iniciarSesion = useCallback(async (credenciales: CredencialesInicioSesion) => {
    const respuesta = await solicitar<RespuestaAutenticacion>('/api/autenticacion/iniciar-sesion', {
      method: 'POST',
      body: JSON.stringify(credenciales),
      omitirAutorizacion: true,
      omitirRenovacion: true,
    })
    await aplicarRespuesta(respuesta)
    return respuesta.requiereCambioContrasena
  }, [aplicarRespuesta])

  const cerrarSesion = useCallback(async () => {
    try {
      await solicitar<void>('/api/autenticacion/cerrar-sesion', {
        method: 'POST',
        omitirRenovacion: true,
      })
    } finally {
      limpiarSesion()
    }
  }, [limpiarSesion])

  const cambiarContrasena = useCallback(async (solicitud: CambioContrasena) => {
    const identificador = sesion?.usuario.nombreUsuario
    if (!identificador) throw new Error('No existe una sesión para cambiar la contraseña.')
    await solicitar<ApiResponse>('/api/autenticacion/cambiar-contrasena', {
      method: 'POST',
      body: JSON.stringify(solicitud),
      omitirRenovacion: true,
    })
    limpiarSesion()
    await iniciarSesion({ identificador, contrasena: solicitud.nuevaContrasena })
  }, [iniciarSesion, limpiarSesion, sesion?.usuario.nombreUsuario])

  const seleccionarSucursal = useCallback(async (sucursalId: number) => {
    const respuesta = await solicitar<RespuestaAutenticacion>('/api/autenticacion/seleccionar-sucursal', {
      method: 'POST',
      body: JSON.stringify({ sucursalId }),
    })
    await aplicarRespuesta(respuesta)
  }, [aplicarRespuesta])

  const tienePermiso = useCallback((codigo: string) => (
    sesion?.permisos.includes(codigo) ?? false
  ), [sesion?.permisos])

  const valor = useMemo(() => ({
    sesion,
    usuario: sesion?.usuario ?? null,
    sucursalActiva: sesion?.usuario.sucursalActiva ?? null,
    sucursales: sesion?.usuario.sucursales ?? [],
    autenticado: Boolean(sesion),
    cargando,
    iniciarSesion,
    cerrarSesion,
    renovarSesion,
    cambiarContrasena,
    seleccionarSucursal,
    tienePermiso,
  }), [
    cambiarContrasena,
    cargando,
    cerrarSesion,
    iniciarSesion,
    renovarSesion,
    seleccionarSucursal,
    sesion,
    tienePermiso,
  ])

  return <ContextoAutenticacion.Provider value={valor}>{children}</ContextoAutenticacion.Provider>
}
