import { afterEach, describe, expect, it } from 'vitest'
import { configurarZonaHoraria, fechaHoraLocal, mostrarFecha, mostrarFechaCivil, mostrarInstante } from './fechas'
import { fechaVenta } from '../features/ventas/utilidadesVentas'
import { fechaCorta } from '../features/compras/utilidadesCompras'
import { fechaProduccion } from '../features/produccion/utilidadesProduccion'

describe('política central de fechas', () => {
  afterEach(() => configurarZonaHoraria())
  it('muestra 20:42 UTC como 14:42 Guatemala sin sumar/restar horas manualmente', () => {
    configurarZonaHoraria('America/Guatemala')
    const instante = '2026-10-08T20:42:15.376Z'
    expect(fechaHoraLocal(new Date(instante))).toBe('2026-10-08T14:42')
    const esperado = new Intl.DateTimeFormat('es-GT', { timeZone: 'America/Guatemala', dateStyle: 'medium', timeStyle: 'short' }).format(new Date(instante))
    expect(mostrarFecha(instante)).toBe(esperado)
    expect(fechaVenta(instante)).toBe(esperado)
    expect(mostrarFecha(instante)).toContain('2:42')
    expect(mostrarFecha(instante)).not.toContain('8:42')
  })
  it('un offset explícito y Z representan el mismo instante', () => {
    configurarZonaHoraria('America/Guatemala')
    expect(mostrarFecha('2026-10-08T14:42:15.376-06:00')).toBe(mostrarFecha('2026-10-08T20:42:15.376Z'))
  })
  it('cambiar zona de sesión cambia la presentación sin modificar el dato', () => {
    const fecha = new Date('2026-10-08T20:42:15.376Z')
    configurarZonaHoraria('America/Guatemala')
    expect(fechaHoraLocal(fecha)).toBe('2026-10-08T14:42')
    configurarZonaHoraria('Europe/Madrid')
    expect(fechaHoraLocal(fecha)).toBe('2026-10-08T22:42')
    expect(fecha.toISOString()).toBe('2026-10-08T20:42:15.376Z')
  })
  it('maneja cambio de día y horario estacional mediante IANA', () => {
    configurarZonaHoraria('America/Guatemala')
    expect(fechaHoraLocal(new Date('2026-10-08T02:00:00Z'))).toBe('2026-10-07T20:00')
    configurarZonaHoraria('America/New_York')
    expect(fechaHoraLocal(new Date('2026-07-08T20:00:00Z'))).toBe('2026-07-08T16:00')
    expect(fechaHoraLocal(new Date('2026-12-08T20:00:00Z'))).toBe('2026-12-08T15:00')
  })
  it('no interpreta históricos sin zona arbitrariamente como UTC/local', () => {
    expect(mostrarFecha('2026-10-08T20:42:15.376')).toBe('Fecha sin zona horaria')
    expect(mostrarFecha('fechaZ')).toBe('Fecha no disponible')
    expect(mostrarFecha(null)).toBe('Sin movimientos')
  })
  it('fechas civiles de compra y vigencia conservan su día', () => {
    configurarZonaHoraria('America/Guatemala')
    const esperado = mostrarFechaCivil('2026-10-08')
    expect(esperado).toContain('8')
    expect(fechaCorta('2026-10-08T00:00:00')).toBe(esperado)
    expect(fechaProduccion('2026-10-08')).toBe(esperado)
    configurarZonaHoraria('Asia/Tokyo')
    expect(mostrarFechaCivil('2026-10-08')).toBe(esperado)
    expect(mostrarFechaCivil('2026-02-30')).toBe('Fecha no disponible')
  })
  it('producción comparte política para sus timestamps operativos', () => {
    configurarZonaHoraria('America/Guatemala')
    expect(fechaProduccion('2026-10-08T02:00:00Z')).toBe(mostrarInstante('2026-10-08T02:00:00Z', false))
    expect(fechaProduccion(null)).toBe('—')
  })
  it('rechaza configuración inválida sin reemplazar la zona vigente', () => {
    configurarZonaHoraria('America/Guatemala')
    expect(() => configurarZonaHoraria('Zona/Inexistente')).toThrow()
    expect(fechaHoraLocal(new Date('2026-10-08T20:42:00Z'))).toBe('2026-10-08T14:42')
  })
})
