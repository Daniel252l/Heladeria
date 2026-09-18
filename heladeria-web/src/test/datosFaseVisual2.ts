import type { Producto, ProductoPresentacion } from '../types/productos'
export const productoPrueba: Producto = {
  id: 17, empresaId: 4, sku: 'HELADO-VAINILLA', nombre: 'Helado de vainilla', descripcion: 'Helado de prueba',
  categoriaId: 7, categoria: 'Helados', marcaId: null, marca: null, tipoProductoId: 8, tipoProducto: 'Inventariable', manejaExistencia: true,
  unidadBaseId: 9, unidadBase: 'Mililitro', abreviaturaUnidadBase: 'ml', tipoSeguimientoId: 10, tipoSeguimiento: 'Ninguno', metodoValuacionId: 11, metodoValuacion: 'Promedio', controlaVencimiento: false,
  modelo: null, imagen: null, peso: null, esComprable: true, esVendible: true, permiteDescuento: true, esCombo: false, precioVentaReferencial: 25,
  estadoId: 6, estado: 'Activo', versionFila: 'AQIDBA==',
}
export const presentacionPrueba: ProductoPresentacion = {
  id: 23, productoId: 17, presentacionId: 13, presentacion: 'Litro', unidadMedidaId: 14, unidadMedida: 'Litro', factorConversion: 1000,
  esPresentacionBase: false, permiteCompra: true, permiteVenta: true, precioVentaEstandar: 25, estadoId: 6, estado: 'Activo', versionFila: 'BQYHCA==',
}
export function paginaPrueba<T>(elementos: T[]) { return { elementos, pagina: 1, tamanioPagina: 30, totalRegistros: elementos.length, totalPaginas: elementos.length ? 1 : 0 } }
