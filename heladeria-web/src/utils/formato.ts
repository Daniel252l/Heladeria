export const formatoNumero = new Intl.NumberFormat('es-GT', {
  maximumFractionDigits: 6,
})

export const formatoCosto = new Intl.NumberFormat('es-GT', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 6,
})

export { mostrarFecha } from './fechas'
