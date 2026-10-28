import type { Paginado } from './api'
import type { CobrosFormaReporte, PeriodoReporte, SesionCajaReporte, TotalVentasReporte } from './reportes'
export interface FiltroDiario { fecha?: string; sucursalId?: number; cajaId?: number; formaPagoId?: number; esCredito?: boolean; pagina: number; tamanioPagina: number; paginaOperaciones: number; paginaCajas: number }
export interface VentaDiario { id: number; numero: string; fecha: string; cliente: string | null; esCredito: boolean; estado: string; total: number; moneda: string; usuario: string; tieneComprobante: boolean; formasPago: string[] }
export interface OperacionDiario { id: number; fecha: string; tipo: string; documento: string | null; referencia: string | null; formaPago: string | null; moneda: string | null; ingreso: number | null; egreso: number | null; apertura: number; usuario: string }
export interface ReporteDiario {
  periodo: PeriodoReporte; desdeUtc: string; hastaUtcExclusivo: string; generadoUtc: string; sucursal: string;
  ventasTotales: TotalVentasReporte[]; cobrosPorForma: CobrosFormaReporte[];
  finanzas: { monedaId: number | null; moneda: string | null; aperturas: number; ingresos: number; egresos: number; cobrosVentas: number; cobrosCxc: number; pagosCxp: number; sinClasificar: number }[];
  creditosGenerados: { monedaId: number; moneda: string; cantidad: number; importeOriginal: number }[];
  ventas: Paginado<VentaDiario>; operaciones: Paginado<OperacionDiario>; cajas: Paginado<SesionCajaReporte>;
}
