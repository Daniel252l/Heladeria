import type { ProductoPos, ReferenciasVentas, VentaConsulta } from "../types/ventas";

export const referenciasVentas: ReferenciasVentas = {
  empresaId: 4, sucursalId: 4, monedaId: 6, moneda: "GTQ", tipoCambio: 1,
  estados: ["BORRADOR", "CONFIRMADA", "COMPLETADA", "ANULADA"].map((codigo, i) => ({ id: 12 + i, codigo, nombre: codigo })),
  clientes: [], categorias: [{ id: 20, codigo: "HELADOS", nombre: "Helados artesanales" }, { id: 30, codigo: "BEBIDAS", nombre: "Bebidas" }],
  almacenes: [{ bodegaId: 8, bodega: "Bodega de venta", ubicacionId: 16, ubicacion: "Congelador" }],
};
export const productoPos: ProductoPos = {
  id: 1, sku: "HEL-CHO-001", nombre: "Helado Chocolate", imagen: null, categoriaId: 20, categoria: "Helados artesanales",
  unidadBaseId: 11, unidadBase: "Mililitro", abreviaturaBase: "ml", manejaExistencia: true, permiteDescuento: true,
  disponibleBase: 18500, moneda: "GTQ", impuestos: [],
  presentaciones: [
    { productoPresentacionId: null, codigo: "ML", nombre: "Unidad base", unidadMedidaId: 11, unidad: "Mililitro", abreviatura: "ml", factorConversion: 1, precio: 0.05, fuentePrecio: "PRODUCTO_REFERENCIAL", precioValido: true },
    { productoPresentacionId: 1, codigo: "LITRO", nombre: "Litro", unidadMedidaId: 12, unidad: "Litro", abreviatura: "L", factorConversion: 1000, precio: 25, fuentePrecio: "PRESENTACION", precioValido: true },
    { productoPresentacionId: 2, codigo: "VASO", nombre: "Vaso", unidadMedidaId: 13, unidad: "Vaso", abreviatura: "vaso", factorConversion: 250, precio: 8, fuentePrecio: "PRESENTACION", precioValido: true },
  ],
};
export const ventaBorrador: VentaConsulta = {
  encabezado: { id: 2, empresaId: 4, sucursalId: 4, sucursal: "Sucursal principal", bodegaId: 8, ubicacionId: 16, esCredito: false, fechaVencimientoCredito: null,
    numeroVenta: "VT-0000000002", fechaVenta: "2026-10-07T15:00:00Z", clienteId: null, cliente: null,
    monedaId: 6, moneda: "GTQ", tipoCambio: 1, subtotal: 500, descuento: 0, impuesto: 0, total: 500, estado: "BORRADOR", estadoId: 12,
    usuarioCreacionId: 1, usuario: "admin", versionFila: "AQ==", fechaCompletada: null, fechaAnulacion: null, motivoAnulacion: null, claveOperacion: null },
  detalles: [{ id: 5, numeroLinea: 1, productoId: 1, sku: "HEL-CHO-001", producto: "Helado Chocolate", productoPresentacionId: 1,
    presentacion: "Litro", cantidad: 20, factorConversion: 1000, cantidadBase: 20000, unidad: "L", unidadBase: "ml", manejaExistencia: true,
    precioUnitario: 25, fuentePrecio: "PRESENTACION", subtotalNeto: 500, porcentajeDescuento: 0, montoDescuento: 0,
    descuentoPrecioCatalogo: 0, impuesto: 0, totalLinea: 500, costoUnitarioBase: null, costoTotal: null, impuestos: [] }], movimientos: [],
};
export const ventaConfirmada: VentaConsulta = { ...ventaBorrador, encabezado: { ...ventaBorrador.encabezado, estado: "CONFIRMADA", estadoId: 13, versionFila: "Ag==" } };
export const ventaCompletada: VentaConsulta = { ...ventaConfirmada, encabezado: { ...ventaConfirmada.encabezado, estado: "COMPLETADA", estadoId: 14, versionFila: "Aw==", fechaCompletada: "2026-10-07T16:00:00Z", claveOperacion: "7e4cb0d2-b64f-4a59-a4e3-5d6c248b7202" },
  detalles: ventaConfirmada.detalles.map((d) => ({ ...d, costoUnitarioBase: 0.0245, costoTotal: 490 })),
  movimientos: [{ id: 25, concepto: "VENTA-SALIDA", tipo: "SALIDA", fecha: "2026-10-07T16:00:00Z" }],
};
export const ventaAnulada: VentaConsulta = { ...ventaCompletada, encabezado: { ...ventaCompletada.encabezado, estado: "ANULADA", estadoId: 15, versionFila: "BA==", fechaAnulacion: "2026-10-07T17:00:00Z", motivoAnulacion: "Cliente desistió" },
  movimientos: [...ventaCompletada.movimientos, { id: 26, concepto: "VENTA-ANULACION-ENTRADA", tipo: "ENTRADA", fecha: "2026-10-07T17:00:00Z" }],
};
export const paginaVentas = <T,>(elementos: T[], pagina = 1, totalPaginas = 1) => ({ elementos, pagina, tamanioPagina: 10, totalRegistros: elementos.length, totalPaginas });
