import { solicitar } from "./clienteHttp";
import type { GuardarVenta, VentaConsulta, CobroAlCompletarSolicitud } from "../types/ventas";

export const ventasApi = {
  obtener: (id: number, signal?: AbortSignal) => solicitar<VentaConsulta>(`/api/ventas/${id}`, { signal }),
  guardar: (datos: GuardarVenta, existente?: { id: number; versionFila: string }) =>
    solicitar<VentaConsulta>(existente ? `/api/ventas/${existente.id}` : "/api/ventas", {
      method: existente ? "PUT" : "POST", body: JSON.stringify(existente ? { ...datos, versionFila: existente.versionFila } : datos),
    }),
  confirmar: (id: number, versionFila: string) => solicitar<VentaConsulta>(`/api/ventas/${id}/confirmar`, { method: "POST", body: JSON.stringify({ versionFila }) }),
  completar: (id: number, versionFila: string, claveOperacion: string, cobro?: CobroAlCompletarSolicitud) => solicitar<VentaConsulta>(`/api/ventas/${id}/completar`, { method: "POST", body: JSON.stringify({ versionFila, claveOperacion, ...(cobro ? { cobro } : {}) }) }),
  anular: (id: number, versionFila: string, motivo: string) => solicitar<VentaConsulta>(`/api/ventas/${id}/anular`, { method: "POST", body: JSON.stringify({ versionFila, motivo }) }),
};
