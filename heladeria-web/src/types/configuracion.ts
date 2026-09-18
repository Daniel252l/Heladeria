export interface RegistroConfiguracion {
  id: number | string;
  nombre?: string;
  estadoId?: number;
  estado?: string;
  activo?: boolean;
  [propiedad: string]: unknown;
}

export type TipoMantenimientoConfiguracion =
  | "estados"
  | "clases-estados"
  | "tipos"
  | "clases-tipos"
  | "modulos"
  | "acciones"
  | "niveles-acceso"
  | "empresas"
  | "sucursales";
