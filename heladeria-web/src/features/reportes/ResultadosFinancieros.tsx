import type { RespuestasReportes } from '../../types/reportes'
import { mostrarFechaCivil, mostrarInstante } from '../../utils/fechas'
import { DiferenciaCaja } from '../caja/ComponentesCaja'
import { EstadoReporte, ResumenReporte, TablaReporte } from './ComponentesReportes'
import { cantidadReporte as n, importeReporte as dinero } from './utilidadesReportes'

export function ResultadoCaja({ datos, zona }: { datos: RespuestasReportes['caja']; zona: string }) {
  return <>
    <p className="rounded-xl border border-teal-100 bg-teal-50 p-4 text-sm text-teal-900">La apertura se presenta separada de los ingresos operativos. Tarjeta y otras formas no efectivas no incrementan el efectivo esperado.</p>
    <TablaReporte titulo="Resumen por forma de pago y moneda" filas={datos.totales} columnas={[
      ['Forma de pago', (r) => r.formaPago ?? 'Sin definir'], ['Moneda', (r) => r.moneda ?? 'Sin definir'],
      ['Efectivo', (r) => r.esEfectivo === null ? 'Sin definir' : r.esEfectivo ? 'Sí' : 'No'],
      ['Apertura', (r) => dinero(r.apertura, r.moneda)], ['Ingresos operativos', (r) => dinero(r.ingresosOperativos, r.moneda)], ['Egresos operativos', (r) => dinero(r.egresosOperativos, r.moneda)],
    ]} />
    <TablaReporte titulo="Sesiones de caja" filas={datos.datos.elementos} columnas={[
      ['Caja', (r) => r.caja], ['Apertura', (r) => mostrarInstante(r.apertura, true, zona)], ['Cierre', (r) => r.cierre ? mostrarInstante(r.cierre, true, zona) : '—'],
      ['Usuario', (r) => r.usuario], ['Estado', (r) => <EstadoReporte estado={r.estado} />], ['Moneda', (r) => r.moneda ?? 'Sin definir'],
      ['Saldo inicial', (r) => dinero(r.saldoInicial, r.moneda)], ['Ingresos operativos', (r) => dinero(r.ingresosOperativos, r.moneda)],
      ['Egresos operativos', (r) => dinero(r.egresosOperativos, r.moneda)], ['Saldo teórico', (r) => dinero(r.saldoTeorico, r.moneda)],
      ['Efectivo esperado', (r) => dinero(r.efectivoEsperado, r.moneda)], ['Efectivo contado', (r) => dinero(r.efectivoContado, r.moneda)],
      ['Diferencia', (r) => r.diferencia === null ? '—' : <DiferenciaCaja valor={r.diferencia} moneda={r.moneda} />],
      ['Conciliable', (r) => r.conciliable ? 'Sí' : 'No'],
    ]} />
  </>
}
export function ResultadoCartera({ datos, pagar }: { datos: RespuestasReportes['cuentas-por-cobrar']; pagar: boolean }) {
  return <>
    {datos.totales.map((r) => <ResumenReporte key={r.monedaId} titulo={`Resumen · ${r.moneda}`} valores={[
      ['Cantidad de cuentas', n(r.cantidad)], ['Cuentas pendientes', n(r.cantidadPendientes)], ['Pendiente', dinero(r.pendiente, r.moneda)],
      ['Vigente', dinero(r.vigente, r.moneda)], ['Vencido', dinero(r.vencido, r.moneda)],
    ]} />)}
    <TablaReporte titulo={`Cuentas por ${pagar ? 'pagar' : 'cobrar'}`} filas={datos.datos.elementos} columnas={[
      [pagar ? 'Proveedor' : 'Cliente', (r) => r.tercero], [pagar ? 'Factura / documento' : 'Documento', (r) => r.documento],
      ...(pagar ? [['Compra origen', (r) => r.compraId === null ? '—' : `Compra #${r.compraId}`] satisfies [string, (r: RespuestasReportes['cuentas-por-pagar']['datos']['elementos'][number]) => string]] : []),
      ['Emisión', (r) => mostrarFechaCivil(r.emision)], ['Vencimiento', (r) => mostrarFechaCivil(r.vencimiento)], ['Moneda', (r) => r.moneda],
      ['Importe original', (r) => dinero(r.importeOriginal, r.moneda)], ['Aplicado', (r) => dinero(r.aplicado, r.moneda)], ['Saldo', (r) => dinero(r.saldo, r.moneda)],
      ['Estado', (r) => <EstadoReporte estado={r.estado} />], ['Clasificación', (r) => <EstadoReporte estado={r.clasificacion} />],
    ]} />
  </>
}
