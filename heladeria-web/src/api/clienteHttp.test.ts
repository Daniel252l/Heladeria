import { beforeEach, describe, expect, it, vi } from 'vitest'
import { respuestaJson } from '../test/datosAutenticacion'

describe('cliente HTTP y renovación', () => {
  beforeEach(() => {
    vi.resetModules()
    vi.restoreAllMocks()
  })

  it('renueva y repite exactamente una vez una petición que recibe 401', async () => {
    let llamadasRecurso = 0
    let llamadasRenovacion = 0
    const fetchSimulado = vi.fn(async (entrada: RequestInfo | URL, opciones?: RequestInit) => {
      const ruta = new URL(String(entrada)).pathname
      if (ruta === '/api/autenticacion/renovar') {
        llamadasRenovacion += 1
        expect(opciones?.credentials).toBe('include')
        return respuestaJson({ accessToken: 'token-nuevo' })
      }
      llamadasRecurso += 1
      const autorizacion = new Headers(opciones?.headers).get('Authorization')
      return llamadasRecurso === 1
        ? respuestaJson({ codigo: 'NO_AUTENTICADO' }, 401)
        : respuestaJson({ valor: 'correcto', autorizacion })
    })
    vi.stubGlobal('fetch', fetchSimulado)
    const cliente = await import('./clienteHttp')
    configurarRenovador(cliente)
    cliente.establecerAccessToken('token-expirado')

    const resultado = await cliente.solicitar<{ valor: string; autorizacion: string }>('/api/recurso')

    expect(resultado).toEqual({ valor: 'correcto', autorizacion: 'Bearer token-nuevo' })
    expect(llamadasRenovacion).toBe(1)
    expect(llamadasRecurso).toBe(2)
  })

  it('limpia la autenticación cuando la renovación también recibe 401', async () => {
    let expiraciones = 0
    const autorizaciones: Array<string | null> = []
    vi.stubGlobal('fetch', vi.fn(async (entrada: RequestInfo | URL, opciones?: RequestInit) => {
      const ruta = new URL(String(entrada)).pathname
      autorizaciones.push(new Headers(opciones?.headers).get('Authorization'))
      if (ruta === '/api/autenticacion/renovar' || ruta === '/api/recurso') {
        return respuestaJson({ codigo: 'NO_AUTENTICADO' }, 401)
      }
      return respuestaJson({ exito: true })
    }))
    const cliente = await import('./clienteHttp')
    configurarRenovador(cliente, () => { expiraciones += 1 })
    cliente.establecerAccessToken('token-expirado')

    await expect(cliente.solicitar('/api/recurso')).rejects.toMatchObject({ estado: 401 })
    await cliente.solicitar('/api/comprobar', { omitirRenovacion: true })

    expect(expiraciones).toBe(1)
    expect(autorizaciones.at(-1)).toBeNull()
  })

  it('comparte una sola renovación entre tres peticiones 401 concurrentes', async () => {
    let llamadasRenovacion = 0
    let liberarRenovacion: (() => void) | undefined
    const esperaRenovacion = new Promise<void>((resolver) => { liberarRenovacion = resolver })
    vi.stubGlobal('fetch', vi.fn(async (entrada: RequestInfo | URL, opciones?: RequestInit) => {
      const ruta = new URL(String(entrada)).pathname
      const autorizacion = new Headers(opciones?.headers).get('Authorization')
      if (ruta === '/api/autenticacion/renovar') {
        llamadasRenovacion += 1
        await esperaRenovacion
        return respuestaJson({ accessToken: 'token-compartido' })
      }
      return autorizacion === 'Bearer token-compartido'
        ? respuestaJson({ ruta })
        : respuestaJson({ codigo: 'NO_AUTENTICADO' }, 401)
    }))
    const cliente = await import('./clienteHttp')
    configurarRenovador(cliente)
    cliente.establecerAccessToken('token-expirado')

    const solicitudes = [1, 2, 3].map((id) => cliente.solicitar(`/api/recurso-${id}`))
    await vi.waitFor(() => expect(llamadasRenovacion).toBe(1))
    liberarRenovacion?.()
    await expect(Promise.all(solicitudes)).resolves.toHaveLength(3)

    expect(llamadasRenovacion).toBe(1)
  })
})

interface ClienteHttp {
  solicitar<T>(ruta: string, opciones?: { method?: string; omitirAutorizacion?: boolean; omitirRenovacion?: boolean }): Promise<T>
  establecerAccessToken(token: string | null): void
  configurarRenovacion(renovador: () => Promise<string | null>, alExpirar: () => void): void
}

function configurarRenovador(cliente: ClienteHttp, alExpirar: () => void = () => undefined) {
  cliente.configurarRenovacion(async () => {
    try {
      const respuesta = await cliente.solicitar<{ accessToken: string }>('/api/autenticacion/renovar', {
        method: 'POST',
        omitirAutorizacion: true,
        omitirRenovacion: true,
      })
      cliente.establecerAccessToken(respuesta.accessToken)
      return respuesta.accessToken
    } catch {
      cliente.establecerAccessToken(null)
      return null
    }
  }, alExpirar)
}
