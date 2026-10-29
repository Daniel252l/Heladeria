import { forwardRef, useEffect, useImperativeHandle, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { Seleccion } from '../../components/ui/Controles'
import { EstadoCargando } from '../../components/ui/Estados'
import { Paginacion } from '../../components/ui/Paginacion'
import type { Paginado } from '../../types/api'
import type { ReferenciasCaja, SesionCajaResumen } from '../../types/caja'
import type { CobroAlCompletarSolicitud } from '../../types/ventas'
import { CamposPago } from '../caja/CamposPago'
import { validarPagos, type LineaPago } from '../caja/pagosFormulario'
import { AvisoCaja, ErrorCaja } from '../caja/ComponentesCaja'
import { enlaceCaja } from '../caja/utilidadesCaja'
import { useRecursoVentas } from './useDatosVentas'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { PERMISOS } from '../../auth/permisos'

export interface DatosCobroVenta { obtener: () => CobroAlCompletarSolicitud | null }
export const CobroAlCompletar = forwardRef<DatosCobroVenta, { total: number; disabled: boolean; onValidez?: (valida: boolean) => void }>(function CobroAlCompletar({ total, disabled, onValidez }, ref) {
  const [pagina, setPagina] = useState(1)
  const referencias = useRecursoVentas<ReferenciasCaja>('/api/caja/referencias')
  const sesiones = useRecursoVentas<Paginado<SesionCajaResumen>>(`/api/caja/sesiones?abierta=true&pagina=${pagina}&tamanioPagina=20`)
  const formulario = useRef<DatosCobroVenta>(null)
  const { tienePermiso } = useAutenticacion()
  useEffect(() => { if (referencias.cargando || sesiones.cargando || referencias.error || sesiones.error) onValidez?.(false) }, [pagina, referencias.cargando, sesiones.cargando, referencias.error, sesiones.error, onValidez])
  useImperativeHandle(ref, () => ({ obtener: () => formulario.current?.obtener() ?? null }))
  if (referencias.cargando || sesiones.cargando) return <EstadoCargando />
  const error = referencias.error ?? sesiones.error
  if (error) return <ErrorCaja error={error} recargar={() => { referencias.recargar(); sesiones.recargar() }} />
  if (!referencias.datos || !sesiones.datos) return null
  if (!sesiones.datos.elementos.length) return <AvisoCaja>No hay una sesión abierta para cobrar. La venta permanecerá confirmada.{tienePermiso(PERMISOS.cajaAbrir) && <Link className={enlaceCaja + ' mt-3'} to="/caja/abrir">Abrir caja</Link>}</AvisoCaja>
  return <><Formulario key={pagina} ref={formulario} onValidez={onValidez} total={total} disabled={disabled} referencias={referencias.datos} sesiones={sesiones.datos.elementos} />
    {sesiones.datos.totalPaginas > 1 && <Paginacion {...sesiones.datos} cambiar={setPagina} entidad="sesión" />}</>
})
const Formulario = forwardRef<DatosCobroVenta, { total: number; disabled: boolean; referencias: ReferenciasCaja; sesiones: SesionCajaResumen[]; onValidez?: (valida: boolean) => void }>(
  function Formulario({ total, disabled, referencias, sesiones, onValidez }, ref) {
    const validas = sesiones.filter((s) => s.esFase8 && s.estado === 'ABIERTA' && s.monedaId === referencias.monedaId)
    const [sesionId, setSesionId] = useState(validas.length === 1 ? String(validas[0].id) : '')
    // No presuponer efectivo: el operador elige la forma real de pago.
    const [lineas, setLineas] = useState<LineaPago[]>([{ formaId: '', aplicado: total.toFixed(2), recibido: total.toFixed(2), referencia: '' }])
    const esValido = validas.some((s) => String(s.id) === sesionId) && validarPagos(lineas, referencias, total) !== null
    useEffect(() => { onValidez?.(esValido) }, [esValido, onValidez])
    useImperativeHandle(ref, () => ({ obtener: () => {
      const sesion = validas.find((s) => String(s.id) === sesionId); const pagos = validarPagos(lineas, referencias, total)
      return sesion && pagos ? { sesionCajaId: sesion.id, versionCaja: sesion.versionFila, pagos } : null
    } }))
    return <fieldset disabled={disabled} className="min-w-0 space-y-4">
      <AvisoCaja>El cobro y el inventario se registrarán juntos al completar. No tendrá que aprobar esta venta en Caja.</AvisoCaja>
      <Seleccion etiqueta="Sesión de caja" required value={sesionId} onChange={(e) => setSesionId(e.target.value)}><option value="">Seleccione una sesión abierta</option>{validas.map((s) => <option value={s.id} key={s.id}>{s.caja} · {s.numeroApertura}</option>)}</Seleccion>
      <CamposPago lineas={lineas} cambiar={setLineas} referencias={referencias} />
    </fieldset>
  })
