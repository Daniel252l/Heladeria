import { useEffect, useRef, useState } from 'react'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { useOperacionVenta } from '../ventas/useDatosVentas'
import { respuestaIncierta } from './utilidadesCartera'

type Payload = { claveOperacion: string }
// Solo memoria de esta pestaña. El almacenamiento persistente contiene únicamente UUID.
const pendientes = new Map<string, Payload>()
export function useIntentoCartera<T extends Payload>(operacion: string) {
  const { usuario, sucursalActiva } = useAutenticacion()
  const clave = `heladeria:cartera:${usuario?.id}:${sucursalActiva?.empresaId}:${sucursalActiva?.id}:${operacion}`
  const op = useOperacionVenta()
  const montado = useRef(false)
  useEffect(() => { montado.current = true; return () => { montado.current = false } }, [])
  const [, actualizar] = useState(0)
  let uuid: string | null = null; let almacenamientoFallido = false
  try { uuid = sessionStorage.getItem(clave) } catch { almacenamientoFallido = true }
  const conservado = pendientes.get(clave) as T | undefined
  const payload = uuid && conservado?.claveOperacion === uuid ? conservado : undefined
  const pendiente = Boolean(uuid)
  useEffect(() => {
    if (!pendiente) return
    const advertir = (e: BeforeUnloadEvent) => { e.preventDefault(); e.returnValue = '' }
    window.addEventListener('beforeunload', advertir)
    return () => window.removeEventListener('beforeunload', advertir)
  }, [pendiente])
  function resolver() {
    sessionStorage.removeItem(clave)
    pendientes.delete(clave)
    actualizar((n) => n + 1)
  }
  async function enviar<R>(preparar: () => Promise<T>, post: (datos: T) => Promise<R>): Promise<R | undefined> {
    return op.ejecutar(async () => {
      if (almacenamientoFallido) throw new Error('No se pudo conservar la identidad de la operación.')
      const marcador = sessionStorage.getItem(clave)
      const memoria = pendientes.get(clave) as T | undefined
      let datos = marcador && memoria?.claveOperacion === marcador ? memoria : undefined
      if (marcador && !datos) throw new Error('Debe consultar el resultado del intento previo; su formulario ya no está disponible.')
      if (!datos) {
        datos = Object.freeze(await preparar())
        // Una lectura previa puede terminar después de salir o cambiar sucursal.
        if (!montado.current) throw new Error('El contexto cambió antes del envío. No se registró la operación.')
        sessionStorage.setItem(clave, datos.claveOperacion) // Antes del primer POST.
        pendientes.set(clave, datos)
      }
      actualizar((n) => n + 1)
      try {
        const resultado = await post(datos)
        resolver()
        return resultado
      } catch (e) {
        // Un timeout/5xx no autoriza cambiar versiones, payload o UUID.
        // Un 401/403/429 del reintento tampoco demuestra qué pasó con el primer POST.
        if (!marcador && !respuestaIncierta(e)) resolver()
        throw e
      }
    })
  }
  return { ...op, enviar, resolver, pendiente, payload, uuid, almacenamientoFallido }
}
