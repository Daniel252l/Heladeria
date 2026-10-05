import type { OrdenProduccionConsulta, PresentacionesProduccion, RecetaConsulta, ReferenciasProduccion, ReferenciasRecetas } from "../types/produccion";
export const referenciasRecetas: ReferenciasRecetas = {
  productos: [{ id: 10, codigo: "HEL-VAI", nombre: "Helado Vainilla" }, { id: 20, codigo: "LECHE", nombre: "Leche Entera" }, { id: 30, codigo: "AZUCAR", nombre: "Azúcar" }],
  estados: [{ id: 1, codigo: "", nombre: "Activo" }, { id: 3, codigo: "", nombre: "Inactivo" }],
};
export const presentacionesProduccion = (id: number): PresentacionesProduccion => ({
  productoId: id, producto: referenciasRecetas.productos.find((p) => p.id === id)?.nombre ?? "Producto",
  unidadBaseId: 1, unidadBaseCodigo: "ML", unidadBaseNombre: "Mililitro", unidadBaseAbreviatura: "ml",
  presentaciones: [{ productoPresentacionId: id + 1, presentacionId: 1, presentacionCodigo: "LITRO", presentacionNombre: "Litro", unidadMedidaId: 2, unidadCodigo: "L", unidadNombre: "Litro", unidadAbreviatura: "L", factorConversion: 1000, esPresentacionBase: false }],
});
export const recetaProduccion: RecetaConsulta = {
  encabezado: { id: 5, empresaId: 4, productoTerminadoId: 10, productoTerminado: "Helado Vainilla", codigo: "REC-VAI", nombre: "Receta base vainilla", rendimiento: 5, productoPresentacionRendimientoId: 11, unidadRendimientoId: 2, unidadRendimiento: "Litro", abreviaturaRendimiento: "L", factorConversion: 1000, rendimientoBase: 5000, version: 2, vigenteDesde: "2026-10-01", vigenteHasta: null, esVigente: true, estadoId: 1, estado: "Activo", versionFila: "AQ==" },
  detalles: [{ id: 6, productoInsumoId: 20, productoInsumo: "Leche Entera", productoPresentacionId: 21, presentacion: "Litro", unidadMedidaId: 2, unidadMedida: "Litro", abreviaturaUnidad: "L", cantidad: 3, factorConversion: 1000, cantidadBase: 3000, unidadBaseId: 1, unidadBase: "Mililitro", abreviaturaUnidadBase: "ml", observacion: "Fresca" }],
  fueUtilizada: false, unidadBase: "Mililitro", abreviaturaUnidadBase: "ml", presentacionRendimiento: "Litro",
};
export const recetaHistorica: RecetaConsulta = { ...recetaProduccion, fueUtilizada: true, encabezado: { ...recetaProduccion.encabezado, id: 4, version: 1, vigenteDesde: "2026-09-01", vigenteHasta: "2026-09-30", esVigente: false } };
export const referenciasProduccion: ReferenciasProduccion = {
  estados: ["BORRADOR", "CONFIRMADA", "FINALIZADA", "ANULADA"].map((codigo, i) => ({ id: i + 10, codigo, nombre: codigo, permiteEdicion: i === 0, esFinal: i >= 2 })),
  recetasVigentes: [{ id: 5, productoId: 10, producto: "Helado Vainilla", codigo: "REC-VAI", nombre: "Receta base vainilla", version: 2, rendimientoBase: 5000, unidadBase: "Mililitro" }],
  almacenes: [{ bodegaId: 2, bodega: "Producción central", ubicacionId: 7, ubicacion: "Mesa fría" }, { bodegaId: 3, bodega: "Otra bodega", ubicacionId: 8, ubicacion: "Otra ubicación" }],
};
export const ordenProduccion: OrdenProduccionConsulta = {
  encabezado: { id: 9, empresaId: 4, sucursalId: 4, sucursal: "Sucursal principal", bodegaProduccionId: 2, bodega: "Producción central", productoId: 10, producto: "Helado Vainilla", recetaId: 5, receta: "Receta base vainilla", recetaVersion: 2, numeroOrden: "OP-0000000009", cantidadPlanificada: 5, cantidadPlanificadaBase: 5000, cantidadProducida: null, productoPresentacionId: 11, unidadPlanificada: "Litro", abreviaturaUnidadPlanificada: "L", factorConversion: 1000, fechaInicio: "2026-10-01T14:00:00Z", fechaFin: null, estadoId: 10, estado: "BORRADOR", responsableUsuarioId: 1, responsable: "admin", versionFila: "AQ==", unidadBase: "Mililitro", abreviaturaUnidadBase: "ml", presentacion: "Litro" },
  detalles: [{ id: 12, productoInsumoId: 20, productoInsumo: "Leche Entera", productoPresentacionId: 21, presentacion: "Litro", unidadMedidaId: 2, unidadMedida: "Litro", abreviaturaUnidad: "L", cantidadPlanificada: 3, factorConversion: 1000, cantidadPlanificadaBase: 3000, costoPlanificado: null, abreviaturaUnidadBase: "ml" }],
  consumos: [], produccionTerminada: null, movimientoIds: [],
};
export const ordenConfirmada: OrdenProduccionConsulta = { ...ordenProduccion, encabezado: { ...ordenProduccion.encabezado, estadoId: 11, estado: "CONFIRMADA", versionFila: "Ag==" } };
export const ordenFinalizada: OrdenProduccionConsulta = {
  ...ordenConfirmada,
  encabezado: { ...ordenConfirmada.encabezado, estadoId: 12, estado: "FINALIZADA", fechaFin: "2026-10-01T15:00:00Z", cantidadProducida: 5, versionFila: "Aw==" },
  consumos: [{ id: 1, productoId: 20, producto: "Leche Entera", unidadMedidaId: 1, unidadMedida: "Mililitro", cantidadConsumida: 3000, costoUnitario: 0.010633, costoTotal: 31.90, fechaConsumo: "2026-10-01T15:00:00Z" }],
  produccionTerminada: { id: 2, productoId: 10, producto: "Helado Vainilla", cantidadProducida: 5000, costoMateriaPrima: 31.90, costoManoObra: 0, costoIndirecto: 0, costoTotal: 31.90, costoUnitarioBase: 0.00638, fechaFinalizacion: "2026-10-01T15:00:00Z" },
  movimientoIds: [10, 11],
};
export function paginaProduccion<T>(elementos: T[]) { return { elementos, pagina: 1, tamanioPagina: 10, totalRegistros: elementos.length, totalPaginas: 1 }; }
