import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { CheckCircle2 } from 'lucide-react'
import { ventasApi } from '../../api/ventas'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { Boton } from '../../components/ui/Controles'
import { Modal } from '../../components/ui/Modal'
import type { VentaConsulta } from '../../types/ventas'
import { CobroAlCompletar, type DatosCobroVenta } from './CobroAlCompletar'
import { AvisoVenta, enlaceVenta, TotalesVenta } from './ComponentesVentas'
import { claveIntentoVenta, completarConReconciliacion, useOperacionVenta } from './useDatosVentas'
import { esConcurrencia, mensajeVenta } from './utilidadesVentas'

// Usa el total persistido y las mismas operaciones del detalle; no envía importes calculados por el catálogo.
export function CierrePos({ inicial, cerrar, nueva }: { inicial: VentaConsulta; cerrar: (v: VentaConsulta) => void; nueva: () => void }) {
  const [venta, setVenta] = useState(inicial)
  const [pagoValido, setPagoValido] = useState(false)
  const { usuario } = useAutenticacion()
  const pago = useRef<DatosCobroVenta>(null)
  const formulario = useRef<HTMLDivElement>(null)
  const operacion = useOperacionVenta()
  const o = venta.encabezado
  const completada = o.estado === 'COMPLETADA'
  const requiereCobro = !o.esCredito && o.total > 0
  useEffect(() => {
    const previo = document.activeElement as HTMLElement | null
    const dialogo = formulario.current?.closest('[role="dialog"]')
    const elementos = () => Array.from(dialogo?.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), select:not(:disabled), a[href]') ?? [])
    elementos()[0]?.focus()
    const teclado = (e: KeyboardEvent) => {
      if (e.key !== 'Tab') return
      const focos = elementos(), primero = focos[0], ultimo = focos.at(-1)
      if (e.shiftKey && document.activeElement === primero) { e.preventDefault(); ultimo?.focus() }
      if (!e.shiftKey && document.activeElement === ultimo) { e.preventDefault(); primero?.focus() }
    }
    document.addEventListener('keydown', teclado)
    return () => { document.removeEventListener('keydown', teclado); previo?.focus() }
  }, [completada])
  const finalizar = async () => {
    const cobro = requiereCobro ? pago.current?.obtener() : undefined
    if (operacion.procesando || (requiereCobro && !cobro) || esConcurrencia(operacion.error)) return
    await operacion.ejecutar(async () => {
      let actual = venta
      if (actual.encabezado.estado === 'BORRADOR') {
        actual = await ventasApi.confirmar(o.id, o.versionFila)
        setVenta(actual) // Si falla el cierre, se conserva el ID y la versión confirmada. Nunca se crea otra venta.
      }
      const resultado = await completarConReconciliacion(actual,
        claveIntentoVenta(usuario!.id, o.empresaId, o.sucursalId, o.id), cobro ?? undefined)
      setVenta(resultado)
      return resultado
    })
  }
  return <Modal abierto titulo={completada ? 'Venta finalizada' : 'Finalizar venta'} ancho="max-w-2xl"
    cerrar={() => { if (!operacion.procesando) cerrar(venta) }}>
    <div ref={formulario} className="space-y-5 p-5">
      {completada ? <>
        <div role="status" className="rounded-xl bg-teal-50 p-5 text-center text-teal-800"><CheckCircle2 className="mx-auto mb-3 h-10 w-10" />
          <p className="font-bold">{o.numeroVenta} completada correctamente</p><p className="mt-2 text-sm">{o.esCredito ? 'La cuenta por cobrar quedó registrada.' : 'Venta registrada. Puede consultar su comprobante.'}</p></div>
        <div className="flex flex-wrap gap-3"><Link className={enlaceVenta} to={`/ventas/${o.id}/comprobante`}>Ver comprobante</Link><Boton onClick={nueva}>Nueva venta</Boton></div>
      </> : <>
        <p className="font-semibold">{o.numeroVenta} · {o.cliente ?? 'Venta sin cliente identificado'}</p>
        <TotalesVenta {...o} />
        {o.esCredito ? <AvisoVenta>Venta a crédito. Se validará el cliente y se generará la cuenta por cobrar sin cobrar en Caja.</AvisoVenta>
          : requiereCobro ? <CobroAlCompletar ref={pago} total={o.total} disabled={operacion.procesando} onValidez={setPagoValido} />
          : <AvisoVenta>Sin importe a cobrar.</AvisoVenta>}
        {operacion.error && <AvisoVenta error>{mensajeVenta(operacion.error, venta.detalles)}<Link className="mt-2 block underline" to={`/ventas/${o.id}`}>Consultar venta guardada</Link></AvisoVenta>}
        <p className="text-xs text-slate-500">Al finalizar se confirma la venta y se registra su inventario y cobro mediante el flujo habitual.</p>
        <Boton className="min-h-12 w-full text-base" cargando={operacion.procesando} disabled={(requiereCobro && !pagoValido) || esConcurrencia(operacion.error)} onClick={() => void finalizar()}>Confirmar y finalizar</Boton>
      </>}
    </div>
  </Modal>
}
