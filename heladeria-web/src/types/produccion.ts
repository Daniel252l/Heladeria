export interface OpcionProduccion { id: number; codigo: string; nombre: string }
export interface ReferenciasRecetas { productos: OpcionProduccion[]; estados: OpcionProduccion[] }
export interface PresentacionProduccion {
  productoPresentacionId: number; presentacionId: number; presentacionCodigo: string;
  presentacionNombre: string; unidadMedidaId: number; unidadCodigo: string;
  unidadNombre: string; unidadAbreviatura: string; factorConversion: number; esPresentacionBase: boolean;
}
export interface PresentacionesProduccion {
  productoId: number; producto: string; unidadBaseId: number; unidadBaseCodigo: string;
  unidadBaseNombre: string; unidadBaseAbreviatura: string; presentaciones: PresentacionProduccion[];
}
export interface RecetaResumen {
  id: number; empresaId: number; productoTerminadoId: number; productoTerminado: string;
  codigo: string; nombre: string; rendimiento: number; productoPresentacionRendimientoId: number | null;
  unidadRendimientoId: number; unidadRendimiento: string; abreviaturaRendimiento: string;
  factorConversion: number; rendimientoBase: number; version: number; vigenteDesde: string;
  vigenteHasta: string | null; esVigente: boolean; estadoId: number; estado: string; versionFila: string;
}
export interface IngredienteReceta {
  id: number; productoInsumoId: number; productoInsumo: string; productoPresentacionId: number | null;
  presentacion: string | null; unidadMedidaId: number; unidadMedida: string; abreviaturaUnidad: string;
  cantidad: number; factorConversion: number; cantidadBase: number; unidadBaseId: number;
  unidadBase: string; abreviaturaUnidadBase: string; observacion: string | null;
}
export interface RecetaConsulta {
  encabezado: RecetaResumen; detalles: IngredienteReceta[]; fueUtilizada: boolean;
  unidadBase: string; abreviaturaUnidadBase: string; presentacionRendimiento: string | null;
}
export interface OrdenProduccionResumen {
  id: number; empresaId: number; sucursalId: number; sucursal: string;
  bodegaProduccionId: number; bodega: string; productoId: number; producto: string;
  recetaId: number; receta: string; recetaVersion: number; numeroOrden: string;
  cantidadPlanificada: number; cantidadPlanificadaBase: number; cantidadProducida: number | null;
  productoPresentacionId: number | null; unidadPlanificada: string; abreviaturaUnidadPlanificada: string;
  factorConversion: number; fechaInicio: string; fechaFin: string | null; estadoId: number;
  estado: string; responsableUsuarioId: number; responsable: string; versionFila: string;
  unidadBase: string; abreviaturaUnidadBase: string; presentacion: string | null;
}
export interface DetalleProduccion {
  id: number; productoInsumoId: number; productoInsumo: string; productoPresentacionId: number | null;
  presentacion: string | null; unidadMedidaId: number; unidadMedida: string; abreviaturaUnidad: string;
  cantidadPlanificada: number; factorConversion: number; cantidadPlanificadaBase: number;
  costoPlanificado: number | null; abreviaturaUnidadBase: string;
}
export interface ConsumoProduccion {
  id: number; productoId: number; producto: string; unidadMedidaId: number; unidadMedida: string;
  cantidadConsumida: number; costoUnitario: number; costoTotal: number; fechaConsumo: string;
}
export interface ProduccionTerminada {
  id: number; productoId: number; producto: string; cantidadProducida: number;
  costoMateriaPrima: number; costoManoObra: number; costoIndirecto: number; costoTotal: number;
  costoUnitarioBase: number; fechaFinalizacion: string;
}
export interface OrdenProduccionConsulta {
  encabezado: OrdenProduccionResumen; detalles: DetalleProduccion[]; consumos: ConsumoProduccion[];
  produccionTerminada: ProduccionTerminada | null; movimientoIds: number[];
}
export interface ReferenciasProduccion {
  estados: { id: number; codigo: string; nombre: string; permiteEdicion: boolean; esFinal: boolean }[];
  recetasVigentes: { id: number; productoId: number; producto: string; codigo: string; nombre: string; version: number; rendimientoBase: number; unidadBase: string }[];
  almacenes: { bodegaId: number; bodega: string; ubicacionId: number; ubicacion: string }[];
}
