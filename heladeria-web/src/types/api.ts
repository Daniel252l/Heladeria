export interface Paginado<T> {
  elementos: T[]
  pagina: number
  tamanioPagina: number
  totalRegistros: number
  totalPaginas: number
}

export interface ApiResponse {
  exito: boolean
  mensaje: string
  codigo?: string
}

export interface ErrorApiRespuesta {
  exito?: boolean
  mensaje?: string
  codigo?: string
  traceId?: string
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

export interface Estado {
  id: number
  nombre: string
  activo: boolean
  claseEstadoId: number
  claseEstado: string
  moduloId: string | null
}

export interface OpcionSelect {
  valor: string
  etiqueta: string
}
