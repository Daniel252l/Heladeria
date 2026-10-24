import { useState, type FormEvent } from 'react'
import { cajaApi } from '../../api/caja'
import { Boton, Entrada, Seleccion, AreaTexto } from '../../components/ui/Controles'
import type { SesionCaja } from '../../types/caja'
import { AvisoCaja, DiferenciaCaja, ErrorCaja, ModalCaja } from './ComponentesCaja'
import { centavos, dineroCaja, ingresosCaja, egresosCaja } from './utilidadesCaja'
import { useOperacionCaja } from './useDatosCaja'

export function FormularioSesionCaja({ tipo, sesion, cerrar, actualizada, operacionInicial = 'INGRESO' }: {
  tipo: 'movimiento' | 'arqueo' | 'cierre'; sesion: SesionCaja; cerrar: () => void; actualizada: (s: SesionCaja) => void;
  operacionInicial?: 'INGRESO' | 'EGRESO';
}) {
  const op = useOperacionCaja(); const [validacion, setValidacion] = useState('')
  const [operacion, setOperacion] = useState(operacionInicial); const [importe, setImporte] = useState('')
  const [motivo, setMotivo] = useState(''); const [observacion, setObservacion] = useState('')
  const vigente = sesion.arqueos.find((a) => a.vigente)
  const moneda = sesion.encabezado.moneda; const n = centavos(importe)
  const contado = n === null ? null : n / 100
  async function enviar(e: FormEvent) {
    e.preventDefault(); setValidacion('')
    const { id, versionFila } = sesion.encabezado
    if (tipo === 'movimiento' && (n === null || n <= 0 || motivo.trim().length < 3)) { setValidacion('Indique un importe positivo y un motivo de al menos tres caracteres.'); return }
    if (tipo === 'movimiento' && operacion === 'EGRESO' && (sesion.efectivoEsperado === null || n! > Math.round(sesion.efectivoEsperado * 100))) { setValidacion('El egreso supera el efectivo esperado disponible.'); return }
    if (tipo === 'arqueo' && n === null) { setValidacion('Indique el efectivo contado, cero o mayor, con hasta dos decimales.'); return }
    if (tipo === 'cierre' && !vigente) { setValidacion('Se requiere un arqueo vigente después del último movimiento.'); return }
    const r = await op.ejecutar(() => tipo === 'movimiento' ? cajaApi.movimiento(id, { versionFila, tipo: operacion, monto: n! / 100, motivo, observacion })
      : tipo === 'arqueo' ? cajaApi.arqueo(id, versionFila, n! / 100, observacion) : cajaApi.cerrar(id, versionFila, vigente!.id))
    if (r) actualizada(r)
  }
  async function recargar() { const r = await op.ejecutar(() => cajaApi.obtener(sesion.encabezado.id)); if (r) actualizada(r) }
  return <ModalCaja titulo={tipo === 'movimiento' ? 'Movimiento manual' : tipo === 'arqueo' ? 'Registrar arqueo' : 'Cerrar sesión de caja'} cerrar={() => { if (!op.procesando) cerrar() }}>
    <form className="space-y-5" onSubmit={(e) => void enviar(e)}><fieldset disabled={op.procesando} className="space-y-4">
      {tipo === 'movimiento' && <><AvisoCaja>Ingreso o egreso de efectivo. No es una venta ni un pago. Disponible: {dineroCaja(sesion.efectivoEsperado ?? 0, moneda)}.</AvisoCaja>
        <Seleccion etiqueta="Tipo de movimiento" value={operacion} onChange={(e) => setOperacion(e.target.value === 'EGRESO' ? 'EGRESO' : 'INGRESO')}><option value="INGRESO">Ingreso</option><option value="EGRESO">Egreso</option></Seleccion>
        <Entrada etiqueta="Monto" type="number" min="0.01" max="9999999999.99" step="0.01" required value={importe} onChange={(e) => setImporte(e.target.value)} />
        <Entrada etiqueta="Motivo" required minLength={3} maxLength={150} value={motivo} onChange={(e) => setMotivo(e.target.value)} />
      </>}
      {tipo === 'arqueo' && <><p>Efectivo esperado: <strong>{dineroCaja(sesion.efectivoEsperado ?? 0, moneda)}</strong></p>
        <Entrada etiqueta="Efectivo contado" type="number" min="0" max="9999999999.99" step="0.01" required value={importe} onChange={(e) => setImporte(e.target.value)} />
        {contado !== null && <p aria-live="polite">Vista previa · <DiferenciaCaja valor={(n! - Math.round((sesion.efectivoEsperado ?? 0) * 100)) / 100} moneda={moneda} /></p>}
        <p className="text-sm text-slate-600">El servidor confirma la diferencia. No se generan ajustes automáticos.</p>
      </>}
      {tipo !== 'cierre' && <AreaTexto etiqueta={tipo === 'arqueo' ? 'Observaciones' : 'Observación'} maxLength={tipo === 'arqueo' ? 500 : 140} value={observacion} onChange={(e) => setObservacion(e.target.value)} />}
      {tipo === 'cierre' && (vigente ? <div className="space-y-3"><AvisoCaja>Al confirmar, esta sesión quedará en modo de solo lectura.</AvisoCaja>
        <dl className="grid gap-3 sm:grid-cols-2">{[['Efectivo esperado', vigente.esperado], ['Efectivo contado', vigente.contado], ['Ingresos operativos', ingresosCaja(sesion)], ['Egresos', egresosCaja(sesion)]].map(([nombre, valor]) => <div key={nombre}><dt className="text-sm text-slate-500">{nombre}</dt><dd className="font-semibold">{dineroCaja(Number(valor), moneda)}</dd></div>)}</dl>
        <DiferenciaCaja valor={vigente.diferencia} moneda={moneda} /><p className="text-sm text-slate-600">El cierre conserva el arqueo y sus diferencias, sin ajustar efectivo automáticamente.</p>
      </div> : <AvisoCaja error>Registre un arqueo vigente antes de cerrar.</AvisoCaja>)}
    </fieldset>{validacion && <AvisoCaja error>{validacion}</AvisoCaja>}{op.error && <ErrorCaja error={op.error} recargar={() => void recargar()} />}
    <div className="flex flex-wrap justify-end gap-3"><Boton variante="secundario" type="button" disabled={op.procesando} onClick={cerrar}>Cancelar</Boton>
      <Boton type="submit" cargando={op.procesando} disabled={tipo === 'cierre' && !vigente}>{tipo === 'movimiento' ? 'Registrar movimiento' : tipo === 'arqueo' ? 'Guardar arqueo' : 'Confirmar cierre'}</Boton></div></form>
  </ModalCaja>
}
