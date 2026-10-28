export interface RegistroCatalogoBase {
  id: number;
  empresaId: number;
  codigo: string;
  nombre: string;
  estadoId: number;
  estado: string;
  versionFila: string;
}

export interface Categoria extends RegistroCatalogoBase {
  categoriaPadreId: number | null;
  descripcion: string | null;
  nivel: number;
}

export interface Marca extends RegistroCatalogoBase {
  descripcion: string | null;
}

export interface TipoProducto extends RegistroCatalogoBase {
  manejaExistencia: boolean;
}

export interface UnidadMedida extends RegistroCatalogoBase {
  abreviatura: string;
  permiteDecimales: boolean;
  cantidadDecimales: number;
}

export interface Presentacion extends RegistroCatalogoBase {
  descripcion: string | null;
}

export type RegistroCatalogo =
  | Categoria
  | Marca
  | TipoProducto
  | UnidadMedida
  | Presentacion;

export type TipoCatalogo =
  | "categorias"
  | "marcas"
  | "tipos"
  | "unidades-medida"
  | "presentaciones";

export interface FormularioCatalogo {
  codigo: string;
  nombre: string;
  descripcion: string;
  estadoId: number;
  categoriaPadreId: number | null;
  manejaExistencia: boolean;
  abreviatura: string;
  permiteDecimales: boolean;
  cantidadDecimales: number;
}

export interface Producto {
  id: number;
  empresaId: number;
  sku: string;
  nombre: string;
  descripcion: string | null;
  categoriaId: number;
  categoria: string;
  marcaId: number | null;
  marca: string | null;
  tipoProductoId: number;
  tipoProducto: string;
  manejaExistencia: boolean;
  unidadBaseId: number;
  unidadBase: string;
  abreviaturaUnidadBase: string;
  tipoSeguimientoId: number;
  tipoSeguimiento: string;
  metodoValuacionId: number;
  metodoValuacion: string;
  controlaVencimiento: boolean;
  modelo: string | null;
  imagen: string | null;
  peso: number | null;
  esComprable: boolean;
  esVendible: boolean;
  permiteDescuento: boolean;
  esCombo: boolean;
  componentes?: ComponenteVenta[];
  precioVentaReferencial: number | null;
  estadoId: number;
  estado: string;
  versionFila: string;
}

export interface ProductoPresentacion {
  id: number;
  productoId: number;
  presentacionId: number;
  presentacion: string;
  unidadMedidaId: number;
  unidadMedida: string;
  abreviaturaUnidadMedida?: string;
  factorConversion: number;
  esPresentacionBase: boolean;
  permiteCompra: boolean;
  permiteVenta: boolean;
  precioVentaEstandar: number | null;
  estadoId: number;
  estado: string;
  versionFila: string;
}

export interface ComponenteVenta {
  productoId: number;
  sku: string;
  producto: string;
  productoPresentacionId: number | null;
  presentacion: string | null;
  cantidad: number;
  factorConversion: number;
  cantidadBase: number;
  unidad: string;
  abreviaturaUnidad: string;
  unidadBaseId: number;
  unidadBase: string;
  abreviaturaUnidadBase: string;
}

export interface OpcionProducto {
  id: number;
  codigo: string;
  nombre: string;
}

export interface ReferenciasProducto {
  tiposSeguimiento: OpcionProducto[];
  metodosValuacion: OpcionProducto[];
}

export interface ConversionUnidad {
  id: number;
  empresaId: number;
  unidadOrigenId: number;
  unidadOrigen: string;
  unidadDestinoId: number;
  unidadDestino: string;
  factor: number;
  estadoId: number;
  estado: string;
}
