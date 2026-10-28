import type { FormaCobro } from './caja'
export interface CobroAlCompletarSolicitud { sesionCajaId: number; versionCaja: string; pagos: FormaCobro[] }
export interface ReferenciaVenta { id: number; codigo: string; nombre: string }
export interface AlmacenVenta { bodegaId: number; bodega: string; ubicacionId: number; ubicacion: string }
export interface ReferenciasVentas {
  empresaId: number; sucursalId: number; monedaId: number; moneda: string; tipoCambio: number;
  estados: ReferenciaVenta[]; clientes: ReferenciaVenta[]; categorias: ReferenciaVenta[]; almacenes: AlmacenVenta[];
}
export interface ImpuestoPos { id: number; codigo: string; nombre: string; porcentaje: number; incluidoEnPrecio: boolean }
export interface PresentacionPos {
  productoPresentacionId: number | null; codigo: string; nombre: string; unidadMedidaId: number;
  unidad: string; abreviatura: string; factorConversion: number; precio: number | null;
  fuentePrecio: string | null; precioValido: boolean;
}
export interface ProductoPos {
  id: number; sku: string; nombre: string; imagen: string | null; categoriaId: number; categoria: string;
  unidadBaseId: number; unidadBase: string; abreviaturaBase: string; manejaExistencia: boolean;
  permiteDescuento: boolean; disponibleBase: number | null; moneda: string;
  presentaciones: PresentacionPos[]; impuestos: ImpuestoPos[];
}
export interface VentaResumen {
  esCredito: boolean; fechaVencimientoCredito: string | null;
  id: number; empresaId: number; sucursalId: number; sucursal: string; bodegaId: number | null; ubicacionId: number | null;
  numeroVenta: string; fechaVenta: string; clienteId: number | null; cliente: string | null;
  monedaId: number; moneda: string; tipoCambio: number; subtotal: number; descuento: number; impuesto: number; total: number;
  estado: string; estadoId: number; usuarioCreacionId: number; usuario: string; versionFila: string;
  fechaCompletada: string | null; fechaAnulacion: string | null; motivoAnulacion: string | null; claveOperacion: string | null;
}
export interface VentaLinea {
  id: number; numeroLinea: number; productoId: number; sku: string; producto: string;
  productoPresentacionId: number | null; presentacion: string | null; cantidad: number;
  factorConversion: number | null; cantidadBase: number | null; unidad: string | null; unidadBase: string | null;
  manejaExistencia: boolean | null; precioUnitario: number; fuentePrecio: string | null; subtotalNeto: number | null;
  porcentajeDescuento: number; montoDescuento: number; descuentoPrecioCatalogo: number | null;
  impuesto: number; totalLinea: number | null; costoUnitarioBase: number | null; costoTotal: number | null;
  impuestos: { impuestoId: number; codigo: string; nombre: string; porcentaje: number; incluidoEnPrecio: boolean; baseImponible: number; monto: number }[];
}
export interface VentaConsulta {
  comprobante?: { empresa: string; razonSocial: string; direccionSucursal: string; telefonoSucursal: string; codigoCliente: string | null; usuarioFinalizacion: string | null; zonaHoraria: string; cuentaPorCobrarId: number | null; saldoInicialCredito: number | null; pagos: { formaPago: string; aplicado: number; recibido: number | null; vuelto: number | null; esEfectivo: boolean | null }[] } | null;
  cobro?: { pagoId: number; sesionCajaId: number; claveOperacion: string; pagos: FormaCobro[] } | null;
  encabezado: VentaResumen; detalles: VentaLinea[];
  movimientos: { id: number; concepto: string; tipo: string; fecha: string }[];
}
export interface GuardarVenta {
  esCredito: boolean; fechaVencimientoCredito: string | null;
  sucursalId: number; bodegaId: number; ubicacionId: number; clienteId: number | null; monedaId: number;
  detalles: { productoId: number; productoPresentacionId: number | null; cantidad: number; porcentajeDescuento: number }[];
}
export interface LineaCarrito { producto: ProductoPos; presentacion: PresentacionPos; cantidad: string; descuento: string }
