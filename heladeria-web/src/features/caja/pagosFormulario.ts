import type { FormaCobro, ReferenciasCaja } from '../../types/caja'
import { centavos } from './utilidadesCaja'

export interface LineaPago { formaId: string; aplicado: string; recibido: string; referencia: string }
export function validarPagos(lineas: LineaPago[], referencias: ReferenciasCaja, total: number): FormaCobro[] | null {
  const pagos: FormaCobro[] = []
  for (const l of lineas) {
    const f = referencias.formasPago.find((x) => String(x.id) === l.formaId)
    const a = centavos(l.aplicado); const recibido = f?.esEfectivo ? centavos(l.recibido) : a
    if (!f || a === null || a <= 0 || recibido === null || recibido < a || f.requiereReferencia && !l.referencia.trim()) return null
    pagos.push({ formaPagoId: f.id, montoAplicado: a / 100, montoRecibido: recibido / 100, ...(l.referencia.trim() ? { referencia: l.referencia.trim() } : {}) })
  }
  return pagos.length > 0 && pagos.length <= 10 && new Set(pagos.map((p) => p.formaPagoId)).size === pagos.length
    && pagos.reduce((s, p) => s + Math.round(p.montoAplicado * 100), 0) === Math.round(total * 100) ? pagos : null
}
