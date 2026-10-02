import { entorno } from '../configuracion/entorno'
import type { ErrorApiRespuesta } from '../types/api'

type RenovadorSesion = () => Promise<string | null>
type ManejadorSesionExpirada = () => void

export interface OpcionesSolicitud extends RequestInit {
  omitirAutorizacion?: boolean
  omitirRenovacion?: boolean
}

export class ErrorApi extends Error {
  readonly estado: number
  readonly codigo?: string
  readonly traceId?: string
  readonly erroresValidacion: Record<string, string[]>

  constructor(
    mensaje: string,
    estado: number,
    detalle: ErrorApiRespuesta = {},
  ) {
    super(mensaje)
    this.name = 'ErrorApi'
    this.estado = estado
    this.codigo = detalle.codigo
    this.traceId = detalle.traceId
    this.erroresValidacion = detalle.errors ?? {}
  }
}

let accessToken: string | null = null
let renovarSesion: RenovadorSesion | null = null
let manejarSesionExpirada: ManejadorSesionExpirada | null = null
let renovacionEnCurso: Promise<string | null> | null = null

export function establecerAccessToken(token: string | null) {
  accessToken = token
}

export function configurarRenovacion(
  renovador: RenovadorSesion,
  alExpirar: ManejadorSesionExpirada,
) {
  renovarSesion = renovador
  manejarSesionExpirada = alExpirar
}

export async function solicitar<T>(
  ruta: string,
  opciones: OpcionesSolicitud = {},
): Promise<T> {
  const respuesta = await ejecutar(ruta, opciones)

  if (
    respuesta.status === 401 &&
    !opciones.omitirRenovacion &&
    renovarSesion
  ) {
    const iniciaRenovacion = renovacionEnCurso === null
    if (iniciaRenovacion) {
      renovacionEnCurso = renovarSesion().finally(() => {
        renovacionEnCurso = null
      })
    }
    const tokenRenovado = await renovacionEnCurso

    if (tokenRenovado) {
      establecerAccessToken(tokenRenovado)
      return procesarRespuesta<T>(await ejecutar(ruta, opciones))
    }

    establecerAccessToken(null)
    if (iniciaRenovacion) manejarSesionExpirada?.()
  }

  return procesarRespuesta<T>(respuesta)
}

async function ejecutar(ruta: string, opciones: OpcionesSolicitud) {
  const {
    omitirAutorizacion: _omitirAutorizacion,
    omitirRenovacion: _omitirRenovacion,
    ...configuracion
  } = opciones
  const encabezados = new Headers(configuracion.headers)
  encabezados.set('Accept', 'application/json')
  if (configuracion.body && !encabezados.has('Content-Type')) {
    encabezados.set('Content-Type', 'application/json')
  }
  if (!opciones.omitirAutorizacion && accessToken) {
    encabezados.set('Authorization', `Bearer ${accessToken}`)
  }

  return fetch(construirUrl(ruta), {
    ...configuracion,
    credentials: 'include',
    headers: encabezados,
  })
}

async function procesarRespuesta<T>(respuesta: Response): Promise<T> {
  if (!respuesta.ok) {
    const detalle = await leerJsonSeguro<ErrorApiRespuesta>(respuesta)
    const primerError = detalle.errors
      ? Object.values(detalle.errors).flat()[0]
      : undefined
    throw new ErrorApi(
      detalle.mensaje || primerError || detalle.detail || detalle.title || mensajePorEstado(respuesta.status),
      respuesta.status,
      detalle,
    )
  }

  if (respuesta.status === 204) {
    return undefined as T
  }

  return leerJsonSeguro<T>(respuesta)
}

async function leerJsonSeguro<T>(respuesta: Response): Promise<T> {
  const texto = await respuesta.text()
  if (!texto) return {} as T
  try {
    return JSON.parse(texto) as T
  } catch {
    return {} as T
  }
}

function construirUrl(ruta: string) {
  const base = entorno.apiUrl.endsWith('/') ? entorno.apiUrl : `${entorno.apiUrl}/`
  return new URL(ruta.replace(/^\//, ''), base)
}

function mensajePorEstado(estado: number) {
  if (estado === 400) return 'Revise los datos ingresados.'
  if (estado === 401) return 'La sesión no es válida o ha expirado.'
  if (estado === 403) return 'No tiene permiso para realizar esta acción.'
  if (estado === 404) return 'No se encontró el recurso solicitado.'
  if (estado === 409) return 'La operación entra en conflicto con los datos actuales.'
  if (estado === 429) return 'Demasiadas solicitudes. Espere un momento e intente nuevamente.'
  return 'No fue posible completar la solicitud.'
}
