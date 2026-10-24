import { useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { cajaApi } from '../../api/caja'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { Boton, Entrada, Seleccion, Tarjeta } from '../../components/ui/Controles'
import { EncabezadoPagina } from '../../components/ui/EncabezadoPagina'
import { EstadoCargando } from '../../components/ui/Estados'
import { SelectorEntidad } from '../../components/ui/SelectorEntidad'
import type { ReferenciasCaja } from '../../types/caja'
import { useOperacionCaja, useRecursoCaja } from './useDatosCaja'
import { AvisoCaja, ErrorCaja, NavegacionCaja } from './ComponentesCaja'
import { centavos, dineroCaja, fechaCaja } from './utilidadesCaja'

export function PaginaAbrirCaja() {
  const { usuario, sucursalActiva } = useAutenticacion()
  return <AbrirCaja key={`${usuario?.id}:${sucursalActiva?.id}`} />
}
function AbrirCaja() {
  const [parametros] = useSearchParams()
  const [cajaId, setCajaId] = useState(parametros.get('cajaId') ?? '')
  const op = useOperacionCaja()
  const refs = useRecursoCaja<ReferenciasCaja>(`/api/caja/referencias${cajaId ? '?cajaId=' + encodeURIComponent(cajaId) : ''}`)
  return <div className="caja-vista space-y-6"><EncabezadoPagina titulo="Abrir caja" descripcion="Registre el efectivo inicial de la sesión; no constituye una venta." /><NavegacionCaja />
    <Tarjeta className="max-w-3xl space-y-5 p-4 sm:p-6">
      <SelectorEntidad etiqueta="Caja" ruta="/api/caja/cajas" value={cajaId} onChange={(id) => setCajaId(id ?? '')} deshabilitado={op.procesando} />
      {refs.cargando ? <EstadoCargando /> : refs.error ? <ErrorCaja error={refs.error} recargar={refs.recargar} /> : refs.datos &&
        <FormularioApertura key={cajaId} cajaId={cajaId} referencias={refs.datos} op={op} />}
    </Tarjeta>
  </div>
}
function FormularioApertura({ cajaId, referencias, op }: { cajaId: string; referencias: ReferenciasCaja; op: ReturnType<typeof useOperacionCaja> }) {
  const navegar = useNavigate()
  const cierre = referencias.saldoUltimoCierre
  const [saldo, setSaldo] = useState(cierre ? cierre.efectivoContado.toFixed(2) : '0')
  const [validacion, setValidacion] = useState('')
  async function abrir(e: FormEvent) {
    e.preventDefault(); const n = centavos(saldo)
    if (!Number.isSafeInteger(Number(cajaId)) || Number(cajaId) <= 0 || n === null) { setValidacion('Seleccione caja e indique un saldo inicial válido con hasta dos decimales.'); return }
    setValidacion('')
    const resultado = await op.ejecutar(() => cajaApi.abrir(Number(cajaId), referencias.monedaId, n / 100))
    if (resultado) navegar(`/caja/sesiones/${resultado.encabezado.id}`)
  }
  return <form className="space-y-5" onSubmit={(e) => void abrir(e)}>
      {cierre ? <AvisoCaja><p className="font-semibold">Saldo del último cierre: {dineroCaja(cierre.efectivoContado, referencias.moneda)}</p><p>{cierre.numeroCierre} · {fechaCaja(cierre.fechaCierre)}. Efectivo físico contado de esta caja. Puede corregir el saldo inicial si el conteo actual es distinto.</p></AvisoCaja>
        : <p className="text-sm text-slate-600">Sin cierre anterior válido para sugerir un saldo. Indique el efectivo físico inicial.</p>}
      <fieldset disabled={op.procesando} className="grid gap-5 sm:grid-cols-2">
        <Seleccion etiqueta="Moneda" value={referencias.monedaId} disabled><option value={referencias.monedaId}>{referencias.moneda}</option></Seleccion>
        <Entrada etiqueta="Saldo inicial" required type="number" min="0" max="9999999999.99" step="0.01" value={saldo} onChange={(e) => setSaldo(e.target.value)} />
      </fieldset>{validacion && <AvisoCaja error>{validacion}</AvisoCaja>}{op.error && <ErrorCaja error={op.error} titulo="No se pudo abrir la caja" />}<Boton type="submit" cargando={op.procesando}>Confirmar apertura</Boton>
    </form>
}
