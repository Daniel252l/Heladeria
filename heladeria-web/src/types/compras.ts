export interface Proveedor {
  id: number;
  empresaId: number;
  codigoProveedor: string;
  nombreProveedor: string;
  nit: string | null;
  telefono: string | null;
  correo: string | null;
  direccion: string | null;
  diasCredito: number;
  limiteCredito: number | null;
  monedaId: number | null;
  estadoId: number;
  estado: string;
}

export interface ProveedorProducto {
  id: number;
  proveedorId: number;
  productoId: number;
  producto: string;
  codigoProveedor: string | null;
  precioUltimaCompra: number | null;
  esPreferido: boolean;
  estadoId: number;
  estado: string;
}

export interface CompraResumen {
  esCredito: boolean | null;
  id: number;
  empresaId: number;
  sucursalId: number;
  sucursal: string;
  proveedorId: number;
  proveedor: string;
  numeroOrden: string;
  fechaEmision: string;
  monedaId: number;
  tipoCambio: number;
  subTotal: number;
  impuestos: number;
  totalEstimado: number;
  estadoId: number;
  estado: string;
}

export interface CompraDetalle {
  id: number;
  numeroLinea: number;
  productoId: number;
  producto: string;
  productoPresentacionId: number | null;
  presentacion: string | null;
  unidadBaseId: number;
  unidadBase: string;
  cantidad: number;
  factorConversion: number;
  cantidadBase: number;
  precioUnitario: number;
  descuento: number;
  impuesto: number;
  totalLinea: number;
  cantidadRecibida: number;
  cantidadPendiente: number;
}

export interface RecepcionDetalle {
  id: number;
  ordenCompraDetalleId: number;
  productoId: number;
  cantidadRecibida: number;
  costoUnitarioBase: number;
}

export interface RecepcionCompra {
  id: number;
  ordenCompraId: number;
  numeroRecepcion: string;
  fechaRecepcion: string;
  estado: string;
  observaciones: string | null;
  detalles: RecepcionDetalle[];
  movimientoIds: number[];
}

export interface CompraConsulta {
  encabezado: CompraResumen;
  fechaEntregaEsperada: string | null;
  detalles: CompraDetalle[];
  recepciones: RecepcionCompra[];
}

export interface MonedaCompra {
  id: number;
  codigoISO: string;
  nombre: string;
  esMonedaBase: boolean;
  decimales: number;
}

export interface EstadoCompra {
  id: number;
  flujo: string;
  codigo: string;
  permiteEdicion: boolean;
  esFinal: boolean;
}

export interface SerieCompra {
  id: number;
  serie: string;
  prefijo: string | null;
  longitud: number;
}

export interface ReferenciasCompras {
  monedas: MonedaCompra[];
  estados: EstadoCompra[];
  series: SerieCompra[];
}

export interface PresentacionCompra {
  productoPresentacionId: number;
  presentacionId: number;
  presentacionCodigo: string;
  presentacionNombre: string;
  unidadMedidaId: number;
  unidadCodigo: string;
  unidadNombre: string;
  unidadAbreviatura: string;
  factorConversion: number;
  esPresentacionBase: boolean;
  activo: boolean;
}

export interface ProductoPresentacionesCompra {
  productoId: number;
  producto: string;
  unidadBaseId: number;
  unidadBaseCodigo: string;
  unidadBaseNombre: string;
  unidadBaseAbreviatura: string;
  presentaciones: PresentacionCompra[];
}

export interface GuardarProveedor {
  codigoProveedor: string;
  nombreProveedor: string;
  nit: string | null;
  telefono: string | null;
  correo: string | null;
  direccion: string | null;
  diasCredito: number;
  limiteCredito: number | null;
  monedaId: number | null;
  estadoId: number;
}

export interface GuardarProveedorProducto {
  productoId: number;
  codigoProveedor: string | null;
  precioUltimaCompra: number | null;
  esPreferido: boolean;
  estadoId: number;
}

export interface CompraLineaSolicitud {
  productoId: number;
  productoPresentacionId: number | null;
  cantidad: number;
  precioUnitario: number;
  porcentajeDescuento: number;
}

export interface GuardarCompra {
  esCredito: boolean | null;
  sucursalId: number;
  proveedorId: number;
  fechaEmision: string;
  fechaEntregaEsperada: string | null;
  monedaId: number;
  tipoCambio: number;
  detalles: CompraLineaSolicitud[];
}
