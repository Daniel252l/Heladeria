import { useCallback, useEffect, useRef, useState } from "react";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { useAutenticacion } from "../../auth/useAutenticacion";
import type { PresentacionesProduccion } from "../../types/produccion";

export function useCargaProduccion<T>(clave: string, cargar: (signal: AbortSignal) => Promise<T>) {
  const { sucursalActiva } = useAutenticacion();
  const contexto = `${sucursalActiva?.id}:${clave}`;
  const [intento, setIntento] = useState(0);
  const [resultado, setResultado] = useState<{ contexto: string; datos: T | null; error: Error | null; cargando: boolean }>({ contexto: "", datos: null, error: null, cargando: true });
  useEffect(() => {
    const control = new AbortController();
    // La solicitud externa se cancela al cambiar el contexto o desmontar la vista.
    // oxlint-disable-next-line react/set-state-in-effect
    setResultado({ contexto, datos: null, error: null, cargando: true });
    void cargar(control.signal).then(
      (datos) => { if (!control.signal.aborted) setResultado({ contexto, datos, error: null, cargando: false }); },
      (error: unknown) => { if (!control.signal.aborted) setResultado({ contexto, datos: null, error: error instanceof Error ? error : new Error("No fue posible cargar la información."), cargando: false }); },
    );
    return () => control.abort();
  }, [cargar, contexto, intento]);
  return { datos: resultado.contexto === contexto ? resultado.datos : null, error: resultado.contexto === contexto ? resultado.error : null,
    cargando: resultado.contexto !== contexto || resultado.cargando, recargar: () => setIntento((n) => n + 1) };
}

export function useRecursoProduccion<T>(ruta: string | null) {
  const cargar = useCallback((signal: AbortSignal) => ruta ? solicitar<T>(ruta, { signal }) : Promise.resolve(null), [ruta]);
  return useCargaProduccion(ruta ?? "sin-recurso", cargar);
}

export function usePresentacionesProduccion(ids: number[], prefijo = "/api/recetas") {
  const clave = [...new Set(ids.filter((id) => id > 0))].sort((a, b) => a - b).join(",");
  const cargar = useCallback(async (signal: AbortSignal) => {
    const datos = await Promise.all(clave ? clave.split(",").map((id) => solicitar<PresentacionesProduccion>(`${prefijo}/productos/${id}/presentaciones`, { signal })) : []);
    return Object.fromEntries(datos.map((dato) => [dato.productoId, dato])) as Record<number, PresentacionesProduccion>;
  }, [clave, prefijo]);
  return useCargaProduccion(`${prefijo}:${clave}`, cargar);
}

export function useOperacionProduccion() {
  const montado = useRef(false);
  const ocupado = useRef(false);
  const [procesando, setProcesando] = useState(false);
  const [error, setError] = useState<Error | null>(null);
  useEffect(() => { montado.current = true; return () => { montado.current = false; }; }, []);
  const ejecutar = async <T,>(operacion: () => Promise<T>): Promise<T | undefined> => {
    if (ocupado.current) return;
    ocupado.current = true;
    setProcesando(true); setError(null);
    try { const resultado = await operacion(); return montado.current ? resultado : undefined; }
    catch (excepcion) { if (montado.current) setError(excepcion instanceof Error ? excepcion : new Error("No fue posible completar la operación.")); }
    finally { ocupado.current = false; if (montado.current) setProcesando(false); }
  };
  return { ejecutar, procesando, error, conflicto: error instanceof ErrorApi && error.estado === 409 };
}
