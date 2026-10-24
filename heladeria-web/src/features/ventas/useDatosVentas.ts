import { useCallback, useEffect, useRef, useState } from "react";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { ventasApi } from "../../api/ventas";
import { useAutenticacion } from "../../auth/useAutenticacion";
import type { VentaConsulta, CobroAlCompletarSolicitud } from "../../types/ventas";

export function useCargaVentas<T>(clave: string, cargar: (signal: AbortSignal) => Promise<T>) {
  const { usuario, sucursalActiva } = useAutenticacion();
  const contexto = `${usuario?.id}:${sucursalActiva?.empresaId}:${sucursalActiva?.id}:${clave}`;
  const [revision, setRevision] = useState(0);
  const [resultado, setResultado] = useState<{ contexto: string; datos: T | null; error: Error | null }>({ contexto: "", datos: null, error: null });
  useEffect(() => {
    const control = new AbortController();
    // Inicio de una carga externa; nunca aceptar una respuesta de otra sucursal o búsqueda.
    // oxlint-disable-next-line react/set-state-in-effect
    setResultado({ contexto: "", datos: null, error: null });
    void cargar(control.signal).then(
      (datos) => { if (!control.signal.aborted) setResultado({ contexto, datos, error: null }); },
      (e: unknown) => { if (!control.signal.aborted) setResultado({ contexto, datos: null, error: e instanceof Error ? e : new Error("No fue posible cargar la información.") }); },
    );
    return () => control.abort();
  }, [contexto, cargar, revision]);
  return { datos: resultado.contexto === contexto ? resultado.datos : null, error: resultado.contexto === contexto ? resultado.error : null,
    cargando: resultado.contexto !== contexto, recargar: () => setRevision((n) => n + 1) };
}
export function useRecursoVentas<T>(ruta: string | null) {
  const cargar = useCallback((signal: AbortSignal) => ruta ? solicitar<T>(ruta, { signal }) : Promise.resolve(null), [ruta]);
  return useCargaVentas(ruta ?? "sin-recurso", cargar);
}
export function useOperacionVenta() {
  const montado = useRef(false);
  const ocupado = useRef(false);
  const [procesando, setProcesando] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  useEffect(() => { montado.current = true; return () => { montado.current = false; }; }, []);
  const ejecutar = async <T,>(accion: () => Promise<T>): Promise<T | undefined> => {
    if (ocupado.current) return;
    ocupado.current = true; setProcesando(true); setError(null);
    try { const resultado = await accion(); return montado.current ? resultado : undefined; }
    catch (e) { if (montado.current) setError(e instanceof Error ? e : new Error("No fue posible completar la operación.")); }
    finally { ocupado.current = false; if (montado.current) setProcesando(false); }
  };
  return { ejecutar, procesando, error, limpiarError: () => setError(null) };
}

// Solo guarda la UUID técnica, nunca tokens, carrito, clientes ni precios.
export const claveIntentoVenta = (usuario: number, empresa: number, sucursal: number, venta: number) => `heladeria:venta:cierre:${usuario}:${empresa}:${sucursal}:${venta}`;
export function hayIntentoVenta(clave: string) {
  try { return sessionStorage.getItem(clave) !== null; } catch { return false; }
}
function guardarClave(clave: string, uuid: string) {
  try { sessionStorage.setItem(clave, uuid); }
  catch { throw new Error("No es posible conservar la clave de cierre. Habilite el almacenamiento de sesión antes de completar la venta."); }
}
export async function completarConReconciliacion(venta: VentaConsulta, clave: string, cobro?: CobroAlCompletarSolicitud): Promise<VentaConsulta> {
  let uuid: string | null;
  try { uuid = sessionStorage.getItem(clave); }
  catch { throw new Error("No se pudo recuperar la clave del cierre. No se enviará una nueva operación."); }
  const conciliar = (actual: VentaConsulta) => {
    if (cobro && actual.encabezado.estado === "COMPLETADA") {
      const registrado = actual.cobro;
      if (actual.encabezado.claveOperacion !== uuid || !registrado || registrado.claveOperacion !== uuid
        || registrado.sesionCajaId !== cobro.sesionCajaId || registrado.pagos.length !== cobro.pagos.length
        || registrado.pagos.some((p) => !cobro.pagos.some((q) => p.formaPagoId === q.formaPagoId
          && p.montoAplicado === q.montoAplicado && p.montoRecibido === q.montoRecibido && (p.referencia?.trim() || '') === (q.referencia?.trim() || ''))))
        throw new ErrorApi("La venta ya fue completada con otro cobro. Consulte el resultado; no se enviará otro pago.", 409, { codigo: "COBRO_INVALIDO" });
    }
    return actual;
  };
  if (uuid) {
    // Un reintento siempre consulta antes. Si la consulta falla, no se envía el POST.
    const actual = await ventasApi.obtener(venta.encabezado.id);
    if (actual.encabezado.estado === "COMPLETADA" || actual.encabezado.estado === "ANULADA") return conciliar(actual);
    if (actual.encabezado.versionFila !== venta.encabezado.versionFila || actual.encabezado.estado !== "CONFIRMADA")
      throw new ErrorApi("La venta cambió desde que la abriste. Recarga la información.", 409, { codigo: "CONFLICTO_VENTAS" });
  } else {
    uuid = crypto.randomUUID();
    guardarClave(clave, uuid); // Persistir ANTES de enviar: también sobrevive a recargar la pestaña.
  }
  try {
    return await ventasApi.completar(venta.encabezado.id, venta.encabezado.versionFila, uuid, cobro);
  } catch (error) {
    const respuestaIncierta = !(error instanceof ErrorApi) || error.estado >= 500 || error.estado === 408;
    if (!respuestaIncierta) throw error;
    // Una respuesta perdida no autoriza un segundo cierre automático.
    try {
      const actual = await ventasApi.obtener(venta.encabezado.id);
      if (["COMPLETADA", "ANULADA"].includes(actual.encabezado.estado)) return conciliar(actual);
    } catch { /* Conservamos la clave y mostramos incertidumbre; nunca un reintento ciego. */ }
    throw new Error("No se pudo confirmar el resultado del cierre. La clave se conservó. Consulte el estado antes de reintentar; se usará la misma operación.");
  }
}
