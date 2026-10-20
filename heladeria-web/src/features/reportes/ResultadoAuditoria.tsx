import type { RespuestasReportes } from '../../types/reportes'
import { mostrarInstante } from '../../utils/fechas'
import { EstadoReporte, TablaReporte } from './ComponentesReportes'

export function ResultadoAuditoria({ datos, zona }: { datos: RespuestasReportes['auditoria']; zona: string }) {
  // Nunca iterar las propiedades recibidas: una respuesta ampliada no debe exponer información sensible.
  return <TablaReporte titulo="Registros de auditoría" filas={datos.elementos} columnas={[
    ['Fecha / hora', (r) => mostrarInstante(r.fecha, true, zona)], ['Usuario', (r) => r.usuario ?? (r.usuarioId === null ? '—' : `Usuario #${r.usuarioId}`)],
    ['Módulo', (r) => r.modulo ?? '—'], ['Entidad', (r) => r.entidad], ['ID entidad', (r) => r.entidadId ?? '—'], ['Acción', (r) => r.accion],
    ['Empresa', (r) => `#${r.empresaId}`], ['Sucursal', (r) => `#${r.sucursalId}`], ['Resultado', (r) => <EstadoReporte estado={r.resultado} />],
  ]} />
}
