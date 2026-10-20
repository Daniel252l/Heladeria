import type { RespuestasReportes } from '../../types/reportes'
import { mostrarFechaCivil, mostrarInstante } from '../../utils/fechas'
import { EstadoReporte, ResumenReporte, TablaReporte } from './ComponentesReportes'
import { cantidadReporte as n, importeReporte as dinero, modalidadReporte } from './utilidadesReportes'

export function ResultadoVentas({ datos, zona }: { datos: RespuestasReportes['ventas']; zona: string }) {
  return <>
    {datos.ventas.totales.map((r) => <ResumenReporte key={`${r.monedaId}:${r.estado}`} titulo={`Totales · ${r.moneda} · ${r.estado}`} valores={[
      ['Cantidad', n(r.cantidad)], ['Subtotal', dinero(r.subtotal, r.moneda)], ['Descuentos', dinero(r.descuentos, r.moneda)],
      ['Impuestos', dinero(r.impuestos, r.moneda)], ['Total', dinero(r.total, r.moneda)], ['Costo', dinero(r.costo, r.moneda)], ['Margen bruto', dinero(r.margenBruto, r.moneda)],
    ]} />)}
    <TablaReporte titulo="Cobros aplicados" filas={datos.cobrosPorForma} columnas={[
      ['Forma de pago', (r) => r.formaPago], ['Moneda', (r) => r.moneda], ['Aplicado', (r) => dinero(r.aplicado, r.moneda)],
    ]} />
    <p className="text-sm text-slate-500">Cobros acumulados de las ventas filtradas; no son ventas por forma de pago ni cobros realizados exclusivamente dentro del período.</p>
    <TablaReporte titulo="Ventas" filas={datos.ventas.datos.elementos} columnas={[
      ['Número', (r) => r.numero], ['Fecha', (r) => mostrarInstante(r.fecha, true, zona)], ['Cliente', (r) => r.cliente ?? 'Sin cliente identificado'],
      ['Modalidad', (r) => modalidadReporte(r.esCredito)], ['Estado de venta', (r) => <EstadoReporte estado={r.estado} />],
      ['Estado de cobro', (r) => <EstadoReporte estado={r.estadoCobro} />], ['Moneda', (r) => r.moneda],
      ['Subtotal', (r) => dinero(r.subtotal, r.moneda)], ['Descuento', (r) => dinero(r.descuentos, r.moneda)],
      ['Impuesto', (r) => dinero(r.impuestos, r.moneda)], ['Total', (r) => dinero(r.total, r.moneda)],
      ['Costo', (r) => dinero(r.costo, r.moneda)], ['Margen bruto', (r) => dinero(r.margenBruto, r.moneda)], ['Cobrado', (r) => dinero(r.cobrado, r.moneda)],
    ]} />
  </>
}
export function ResultadoProductos({ datos }: { datos: RespuestasReportes['productos-vendidos'] }) {
  return <TablaReporte titulo="Productos vendidos" filas={datos.elementos} columnas={[
    ['SKU', (r) => r.sku], ['Producto', (r) => r.producto], ['Presentación', (r) => r.presentacion ?? '—'],
    ['Unidad comercial', (r) => r.unidad ?? '—'], ['Unidad base', (r) => r.unidadBase ?? '—'],
    ['Cantidad', (r) => n(r.cantidad)], ['Cantidad base', (r) => n(r.cantidadBase)], ['Moneda', (r) => r.moneda],
    ['Importe', (r) => dinero(r.importe, r.moneda)], ['Costo', (r) => dinero(r.costo, r.moneda)], ['Margen bruto', (r) => dinero(r.margenBruto, r.moneda)],
  ]} />
}
export function ResultadoCompras({ datos }: { datos: RespuestasReportes['compras'] }) {
  return <>
    {datos.totales.map((r) => <ResumenReporte key={`${r.monedaId}:${r.estado}`} titulo={`Totales · ${r.moneda} · ${r.estado}`} valores={[
      ['Órdenes', n(r.cantidad)], ['Total', dinero(r.total, r.moneda)],
    ]} />)}
    <TablaReporte titulo="Compras" filas={datos.datos.elementos} columnas={[
      ['Orden', (r) => r.numero], ['Fecha', (r) => mostrarFechaCivil(r.fecha)], ['Proveedor', (r) => r.proveedor],
      ['Modalidad', (r) => modalidadReporte(r.esCredito)], ['Estado de compra', (r) => <EstadoReporte estado={r.estado} />],
      ['Moneda', (r) => r.moneda], ['Total', (r) => dinero(r.total, r.moneda)], ['Líneas', (r) => n(r.lineas)],
      ['Líneas recibidas completas', (r) => n(r.lineasRecibidasCompletas)], ['Recepciones', (r) => n(r.recepciones)],
      ['CxP', (r) => r.cuentaPorPagarId === null ? 'Sin cuenta vinculada' : `#${r.cuentaPorPagarId}`],
      ['Saldo por pagar', (r) => dinero(r.saldoPorPagar, r.moneda)],
    ]} />
  </>
}
