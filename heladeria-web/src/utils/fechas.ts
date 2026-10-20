// La API entrega la zona efectiva de la sucursal. Nunca usar implícitamente la zona del navegador.
let zonaHoraria = 'UTC'

export function configurarZonaHoraria(zona = 'UTC') {
  new Intl.DateTimeFormat('es-GT', { timeZone: zona }).format(0) // validar antes de reemplazar
  zonaHoraria = zona
}

export function mostrarInstante(valor: string | null, conHora = true, zona = zonaHoraria) {
  if (!valor) return 'Sin movimientos'
  // No adivinar si un timestamp histórico/servidor antiguo sin offset era UTC o local.
  if (!/(Z|[+-]\d{2}:\d{2})$/i.test(valor)) return 'Fecha sin zona horaria'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return 'Fecha no disponible'
  return new Intl.DateTimeFormat('es-GT', {
    timeZone: zona, dateStyle: 'medium', ...(conHora ? { timeStyle: 'short' } as const : {}),
  }).format(fecha)
}

export const mostrarFecha = (valor: string | null) => mostrarInstante(valor)

// Emisión/entrega de documentos y vigencias no representan instantes; no desplazar el día.
export function mostrarFechaCivil(valor: string) {
  const dia = valor.slice(0, 10)
  if (!/^\d{4}-\d{2}-\d{2}$/.test(dia)) return 'Fecha no disponible'
  const fecha = new Date(`${dia}T00:00:00Z`)
  if (Number.isNaN(fecha.getTime()) || fecha.toISOString().slice(0, 10) !== dia) return 'Fecha no disponible'
  return new Intl.DateTimeFormat('es-GT', { timeZone: 'UTC', dateStyle: 'medium' }).format(fecha)
}

// Solo prellenado de campos civiles/planificados: nunca establece la hora de una operación.
export function fechaHoraLocal(valor = new Date()) {
  const partes = new Intl.DateTimeFormat('en-CA', { timeZone: zonaHoraria,
    year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
  }).formatToParts(valor)
  const dato = (tipo: Intl.DateTimeFormatPartTypes) => partes.find((p) => p.type === tipo)!.value
  return `${dato('year')}-${dato('month')}-${dato('day')}T${dato('hour')}:${dato('minute')}`
}
