import { ErrorApi } from '../../api/clienteHttp'
import { PERMISOS } from '../../auth/permisos'
import type { CuentaCartera, TipoCartera } from '../../types/cartera'

export const permisoConsulta = (tipo: TipoCartera) => tipo === 'cobrar' ? PERMISOS.cxcConsultar : PERMISOS.cxpConsultar
export const permitePago = (tipo: TipoCartera, tiene: (p: string) => boolean) =>
  tiene(permisoConsulta(tipo)) && tiene(PERMISOS.cajaConsultar) && tiene(PERMISOS.cajaOperar) &&
  (tipo === 'cobrar' ? tiene(PERMISOS.cxcCobrar) && tiene(PERMISOS.cajaCobrar) : tiene(PERMISOS.cxpPagar))
export const admitePago = (c: CuentaCartera) => c.operable && c.saldoPendiente > 0 && ['PENDIENTE', 'PARCIAL'].includes(c.estado)
export const mensajesCartera: Record<string, string> = {
  CUENTA_YA_PAGADA: 'La cuenta ya está pagada. No se registró un nuevo pago.',
  ABONO_SUPERA_SALDO: 'El abono supera el saldo pendiente. Revise el importe actualizado.',
  PAGO_SUPERA_SALDO: 'El pago supera el saldo pendiente. Revise el importe actualizado.',
  FORMA_PAGO_INVALIDA: 'La forma de pago no está permitida para esta operación.',
  CLAVE_OPERACION_CONFLICTIVA: 'La clave ya corresponde a otra solicitud. Consulte el historial antes de continuar.',
  CONCURRENCIA_CXC: 'La cuenta cambió. Recargue los datos y revise el abono antes de enviarlo nuevamente.',
  CONCURRENCIA_CXP: 'La cuenta o compra cambió. Recargue los datos y revise la operación.',
  CONCURRENCIA_CAJA: 'La sesión de caja cambió. Recargue los datos antes de preparar otra operación.',
  CAJA_NO_ABIERTA: 'La sesión de caja ya no está abierta. Seleccione una sesión abierta.',
  OPERACION_FINANCIERA_EXISTENTE: 'Ya existe una operación financiera para este documento. Consulte su detalle.',
  DOCUMENTO_FINANCIERO_DUPLICADO: 'Ya existe una factura para esta compra o ese número de documento.',
  VALIDACION_CXC: 'No se pudo validar el abono. Revise los datos de la cuenta y del formulario.',
  VALIDACION_CXP: 'No se pudo validar la operación. Revise la cuenta, compra y datos del formulario.',
  VALIDACION_CARTERA: 'La operación no cumple las condiciones de cartera. Revise los datos; no se harán correcciones automáticas.',
  SALDO_INSUFICIENTE: 'La caja no tiene efectivo suficiente para este pago.',
  REFERENCIA_REQUERIDA: 'Esta forma de pago requiere una referencia de autorización segura.',
}
export const mensajeCartera = (e: Error) => e instanceof ErrorApi
  ? (mensajesCartera[e.codigo ?? ''] ?? (e.estado >= 500 ? 'No fue posible confirmar la operación con el servidor.' : e.message))
  : 'No se pudo confirmar la comunicación. Consulte el resultado antes de iniciar otra operación.'
export const respuestaIncierta = (e: unknown) => !(e instanceof ErrorApi) || e.estado >= 500 || e.estado === 408
// Prevención de entrada accidental, no una regla financiera ni almacenamiento de datos de tarjeta.
export const referenciaInsegura = (v: string) => Boolean(v.trim()) &&
  (!/^[A-Za-z0-9._-]{1,30}$/.test(v.trim()) || (v.match(/\d/g)?.length ?? 0) >= 13 || /cvv|cvc|pan|tarjeta/i.test(v))
