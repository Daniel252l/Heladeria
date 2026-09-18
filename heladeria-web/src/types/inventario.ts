export interface Existencia {
  id: number;
  empresaId: number;
  sucursalId: number;
  sucursal: string;
  bodegaId: number;
  bodega: string;
  ubicacionId: number;
  ubicacion: string;
  productoId: number;
  sku: string;
  producto: string;
  categoriaId: number;
  categoria: string;
  unidadBaseId: number;
  unidadBase: string;
  cantidad: number;
  cantidadReservada: number;
  cantidadDisponible: number;
  costoPromedio: number;
  stockMinimo: number | null;
  bajoMinimo: boolean;
  ultimoMovimiento: string | null;
  versionFila: string;
}

export interface AlmacenInventario {
  sucursalId: number;
  sucursal: string;
  bodegaId: number;
  bodega: string;
  ubicacionId: number;
  ubicacion: string;
  esBodegaPrincipal: boolean;
}

export interface ResultadoAjusteInventario {
  movimientoId: number;
  movimientoDetalleId: number;
  existenciaId: number;
  tipo: "ENTRADA" | "SALIDA";
  cantidadSolicitada: number;
  factorConversion: number;
  cantidadBase: number;
  saldoAnterior: number;
  saldoNuevo: number;
  costoUnitario: number;
  unidadBase: string;
}
