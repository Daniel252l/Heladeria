import { Plus, Trash2 } from 'lucide-react'
import { Boton, Entrada, Seleccion } from '../../components/ui/Controles'
import type { ReferenciasCaja } from '../../types/caja'
import type { LineaPago } from './pagosFormulario'
import { centavos, dineroCaja } from './utilidadesCaja'

export function CamposPago({ lineas, cambiar, referencias, disabled }: {
  lineas: LineaPago[]; cambiar: (lineas: LineaPago[]) => void; referencias: ReferenciasCaja; disabled?: boolean
}) {
  const actualizar = (i: number, datos: Partial<LineaPago>) => cambiar(lineas.map((l, j) => j === i ? { ...l, ...datos } : l))
  return <fieldset disabled={disabled} className="min-w-0 space-y-4">
    {lineas.map((l, i) => {
      const forma = referencias.formasPago.find((f) => String(f.id) === l.formaId); const a = centavos(l.aplicado); const recibido = centavos(l.recibido)
      return <fieldset key={i} className="min-w-0 rounded-xl border border-slate-200 p-3 sm:p-4"><legend className="px-1 text-sm font-semibold">Forma de pago {i + 1}</legend>
        <div className="grid gap-4 sm:grid-cols-2">
          <Seleccion etiqueta={`Forma de pago ${i + 1}`} aria-label={`Forma de pago ${i + 1}`} value={l.formaId} onChange={(e) => actualizar(i, { formaId: e.target.value, referencia: '' })} required>
            <option value="">Seleccione forma</option>{referencias.formasPago.map((f) => <option key={f.id} value={f.id} disabled={lineas.some((otra, j) => j !== i && otra.formaId === String(f.id))}>{f.nombre}</option>)}
          </Seleccion>
          <Entrada etiqueta={`Monto aplicado ${i + 1}`} type="number" min="0.01" max="9999999999.99" step="0.01" required value={l.aplicado} onChange={(e) => actualizar(i, { aplicado: e.target.value })} />
          {forma?.esEfectivo ? <><Entrada etiqueta={`Monto recibido ${i + 1}`} type="number" min="0.01" max="9999999999.99" step="0.01" required value={l.recibido} onChange={(e) => actualizar(i, { recibido: e.target.value })} />
            <p className="self-center text-sm font-semibold" aria-live="polite">Vuelto estimado: {a !== null && recibido !== null && recibido >= a ? dineroCaja((recibido - a) / 100, referencias.moneda) : 'Revise el monto recibido'}</p></>
            : <p className="self-center text-sm text-slate-600">Monto recibido: {a === null ? '—' : dineroCaja(a / 100, referencias.moneda)}. Debe coincidir con el aplicado; sin vuelto.</p>}
          <div><Entrada etiqueta={`Referencia/autorización ${forma?.requiereReferencia ? 'requerida' : 'opcional'} ${i + 1}`} required={forma?.requiereReferencia} autoComplete="off" maxLength={30} value={l.referencia} onChange={(e) => actualizar(i, { referencia: e.target.value })} /><p className="mt-1 text-xs text-slate-500">No ingresar datos de tarjeta.</p></div>
        </div>
        {lineas.length > 1 && <Boton type="button" variante="sutil" className="mt-3" onClick={() => cambiar(lineas.filter((_, j) => j !== i))}><Trash2 size={16} /> Quitar forma {i + 1}</Boton>}
      </fieldset>
    })}
    {lineas.length < Math.min(10, referencias.formasPago.length) && <Boton type="button" variante="secundario" onClick={() => cambiar([...lineas, { formaId: String(referencias.formasPago.find((f) => !lineas.some((l) => l.formaId === String(f.id)))!.id), aplicado: '', recibido: '', referencia: '' }])}><Plus size={16} /> Agregar forma de pago</Boton>}
  </fieldset>
}
