import type { Paginado } from "../types/api";
import type { CompraConsulta, CompraResumen, ProductoPresentacionesCompra, Proveedor, ProveedorProducto, RecepcionCompra, ReferenciasCompras } from "../types/compras";
import type { Producto } from "../types/productos";

export const referenciasCompras: ReferenciasCompras = {
  monedas: [{ id: 1, codigoISO: "GTQ", nombre: "Quetzal", esMonedaBase: true, decimales: 2 }],
  estados: [
    { id: 10, flujo: "COMPRA", codigo: "BORRADOR", permiteEdicion: true, esFinal: false },
    { id: 11, flujo: "COMPRA", codigo: "CONFIRMADA", permiteEdicion: false, esFinal: false },
    { id: 12, flujo: "COMPRA", codigo: "PARCIALMENTE_RECIBIDA", permiteEdicion: false, esFinal: false },
    { id: 13, flujo: "COMPRA", codigo: "RECIBIDA", permiteEdicion: false, esFinal: true },
    { id: 14, flujo: "COMPRA", codigo: "ANULADA", permiteEdicion: false, esFinal: true },
  ],
  series: [{ id: 1, serie: "OC", prefijo: "OC-", longitud: 8 }],
};

export const compraResumen: CompraResumen = {
  esCredito: false,
  id: 51, empresaId: 4, sucursalId: 41, sucursal: "Sucursal Central", proveedorId: 7,
  proveedor: "Lácteos del Valle", numeroOrden: "OC-00000051", fechaEmision: "2026-10-01T00:00:00",
  monedaId: 1, tipoCambio: 1, subTotal: 250, impuestos: 30, totalEstimado: 280, estadoId: 10, estado: "BORRADOR",
};

export function compraDetalle(estado = "BORRADOR", pendiente = 7): CompraConsulta {
  const encabezado = { ...compraResumen, estado, estadoId: referenciasCompras.estados.find((e) => e.codigo === estado)?.id ?? 10 };
  return {
    encabezado,
    fechaEntregaEsperada: "2026-10-10T00:00:00",
    detalles: [{ id: 501, numeroLinea: 1, productoId: 17, producto: "Base de vainilla", productoPresentacionId: 23, presentacion: "Litro", unidadBaseId: 8, unidadBase: "ml", cantidad: 10, factorConversion: 1000, cantidadBase: 10000, precioUnitario: 25, descuento: 0, impuesto: 30, totalLinea: 280, cantidadRecibida: 10 - pendiente, cantidadPendiente: pendiente }],
    recepciones: pendiente < 10 ? [recepcionPrueba] : [],
  };
}

export const recepcionPrueba: RecepcionCompra = {
  id: 81, ordenCompraId: 51, numeroRecepcion: "REC-000081", fechaRecepcion: "2026-10-01T12:00:00", estado: "APLICADA", observaciones: "Parcial", detalles: [{ id: 91, ordenCompraDetalleId: 501, productoId: 17, cantidadRecibida: 3, costoUnitarioBase: 0.0245 }], movimientoIds: [9001],
};

export const proveedorPrueba: Proveedor = {
  id: 7, empresaId: 4, codigoProveedor: "PROV-007", nombreProveedor: "Lácteos del Valle", nit: "123456-7", telefono: "5555-0101", correo: "compras@lacteos.test", direccion: "Zona 1", diasCredito: 30, limiteCredito: 50000, monedaId: 1, estadoId: 1, estado: "Activo",
};

export const proveedorProductoPrueba: ProveedorProducto = {
  id: 71, proveedorId: 7, productoId: 17, producto: "Base de vainilla", codigoProveedor: "BV-1L", precioUltimaCompra: 25, esPreferido: true, estadoId: 1, estado: "Activo",
};

export const productoCompra: Producto = {
  id: 17, empresaId: 4, sku: "BASE-VAI", nombre: "Base de vainilla", descripcion: null, categoriaId: 1, categoria: "Bases", marcaId: null, marca: null, tipoProductoId: 1, tipoProducto: "Insumo", manejaExistencia: true, unidadBaseId: 8, unidadBase: "Mililitro", abreviaturaUnidadBase: "ml", tipoSeguimientoId: 1, tipoSeguimiento: "Normal", metodoValuacionId: 1, metodoValuacion: "Promedio", controlaVencimiento: false, modelo: null, imagen: null, peso: null, esComprable: true, esVendible: false, permiteDescuento: false, esCombo: false, precioVentaReferencial: null, estadoId: 1, estado: "Activo", versionFila: "AQ==",
};

export const presentacionesCompra: ProductoPresentacionesCompra = {
  productoId: 17, producto: "Base de vainilla", unidadBaseId: 8, unidadBaseCodigo: "ML", unidadBaseNombre: "Mililitro", unidadBaseAbreviatura: "ml",
  presentaciones: [
    { productoPresentacionId: 23, presentacionId: 3, presentacionCodigo: "LITRO", presentacionNombre: "Litro", unidadMedidaId: 3, unidadCodigo: "L", unidadNombre: "Litro", unidadAbreviatura: "L", factorConversion: 1000, esPresentacionBase: false, activo: true },
    { productoPresentacionId: 24, presentacionId: 4, presentacionCodigo: "VASO", presentacionNombre: "Vaso", unidadMedidaId: 4, unidadCodigo: "VASO", unidadNombre: "Vaso", unidadAbreviatura: "vaso", factorConversion: 250, esPresentacionBase: false, activo: true },
  ],
};

export const productoCompraAlterno: Producto = { ...productoCompra, id: 18, sku: "BASE-FRE", nombre: "Base de fresa" };
export const presentacionesCompraAlterno: ProductoPresentacionesCompra = { ...presentacionesCompra, productoId: 18, producto: "Base de fresa", presentaciones: [] };

export const estadosGenerales = [{ id: 1, nombre: "Activo", activo: true, claseEstadoId: 1, claseEstado: "GENERAL", moduloId: null }, { id: 3, nombre: "Inactivo", activo: true, claseEstadoId: 1, claseEstado: "GENERAL", moduloId: null }];

export function pagina<T>(elementos: T[]): Paginado<T> { return { elementos, pagina: 1, tamanioPagina: 10, totalRegistros: elementos.length, totalPaginas: elementos.length ? 1 : 0 }; }
