import type { RespuestasReportes } from '../../types/reportes'
import { mostrarInstante } from '../../utils/fechas'
import { EstadoReporte, ResumenReporte, TablaReporte } from './ComponentesReportes'
import { cantidadReporte as n, costoReporte, importeReporte } from './utilidadesReportes'

export function ResultadoInventario({ datos, sucursal }: { datos: RespuestasReportes['inventario']; sucursal: string }) {
  return <>
    <ResumenReporte titulo="Valor oficial del inventario" valores={[
      ['Valor total', importeReporte(datos.valorInventario, datos.monedaBase)], ['Moneda base', datos.monedaBase ?? 'Sin moneda definida'], ['Sucursal', sucursal],
    ]} />
    <TablaReporte titulo="Inventario actual" filas={datos.datos.elementos} columnas={[
      ['SKU', (r) => r.sku], ['Producto', (r) => r.producto], ['Bodega', (r) => r.bodega], ['Ubicación', (r) => r.ubicacion],
      ['Unidad base', (r) => r.unidadBase], ['Existencia', (r) => n(r.existencia)], ['Reservada', (r) => n(r.reservada)],
      ['Disponible', (r) => n(r.disponible)], ['Costo promedio', (r) => costoReporte(r.costoPromedio, datos.monedaBase)],
      ['Valor inventario', (r) => importeReporte(r.valorInventario, datos.monedaBase)],
    ]} />
  </>
}
export function ResultadoKardex({ datos, zona }: { datos: RespuestasReportes['kardex']; zona: string }) {
  return <>
    <p className="text-sm text-slate-500">El contrato de Kardex no identifica moneda. No se asigna una moneda ni se convierten sus costos.</p>
    <TablaReporte titulo="Kardex" filas={datos.elementos} columnas={[
      ['Fecha', (r) => mostrarInstante(r.fecha, true, zona)], ['Concepto', (r) => r.concepto], ['Tipo', (r) => <EstadoReporte estado={r.tipo} />],
      ['SKU', (r) => r.sku], ['Producto', (r) => r.producto], ['Unidad base', (r) => r.unidadBase], ['Bodega', (r) => r.bodega], ['Ubicación', (r) => r.ubicacion],
      ['Cantidad', (r) => n(r.cantidad)], ['Costo unitario', (r) => costoReporte(r.costoUnitario)], ['Costo', (r) => importeReporte(r.costo, null)],
      ['Saldo cantidad', (r) => n(r.saldoCantidad)], ['Saldo costo', (r) => importeReporte(r.saldoCosto, null)],
      ['Módulo origen', (r) => r.moduloOrigen ?? '—'], ['Documento', (r) => r.documento], ['Usuario', (r) => r.usuario],
    ]} />
  </>
}
