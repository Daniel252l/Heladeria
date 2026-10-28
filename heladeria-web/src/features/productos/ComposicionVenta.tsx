import { useEffect, useState } from "react";
import { Layers, Plus, Trash2 } from "lucide-react";
import { solicitar } from "../../api/clienteHttp";
import { Boton, Entrada, Seleccion } from "../../components/ui/Controles";
import { SeccionFormulario } from "../../components/ui/SeccionFormulario";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import type { Producto, ProductoPresentacion } from "../../types/productos";
import type { FilaComponente } from "./utilidadesComposicion";
import { formatoNumero } from "../../utils/formato";


export function ComposicionVenta({ filas, cambiar, agregar, quitar, deshabilitado }: {
  filas: FilaComponente[];
  cambiar: (clave: string, cambios: Partial<FilaComponente>) => void;
  agregar: () => void;
  quitar: (clave: string) => void;
  deshabilitado: boolean;
}) {
  return <SeccionFormulario compacto titulo="Composición de venta" icono={<Layers className="h-4 w-4" />}
    descripcion="El inventario de estos componentes se descontará automáticamente al completar la venta.">
    <div className="min-w-0 space-y-3 sm:col-span-2">
      <p className="text-xs text-slate-500">Cantidades por una unidad base del producto vendido. Sin componentes, conserva su descarga habitual. Admite insumos y empaques no vendibles.</p>
      {filas.map((fila, i) => <Fila key={fila.clave} fila={fila} numero={i + 1} cambiar={cambiar} quitar={quitar} deshabilitado={deshabilitado} />)}
      <Boton type="button" variante="secundario" onClick={agregar} disabled={deshabilitado || filas.length >= 100}>
        <Plus className="h-4 w-4" /> Agregar componente
      </Boton>
    </div>
  </SeccionFormulario>;
}

function Fila({ fila, numero, cambiar, quitar, deshabilitado }: {
  fila: FilaComponente; numero: number; cambiar: (clave: string, cambios: Partial<FilaComponente>) => void;
  quitar: (clave: string) => void; deshabilitado: boolean;
}) {
  const [datos, setDatos] = useState<{ producto: Producto; presentaciones: ProductoPresentacion[] } | null>(null);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => {
    if (!fila.productoId) return;
    let vigente = true;
    Promise.all([solicitar<Producto>(`/api/productos/${fila.productoId}`),
      solicitar<ProductoPresentacion[]>(`/api/productos/${fila.productoId}/presentaciones`)])
      .then(([producto, presentaciones]) => { if (vigente) { setDatos({ producto, presentaciones }); setError(null); } })
      .catch((e: unknown) => { if (vigente) setError(e instanceof Error ? e.message : "No se pudo cargar el componente."); });
    return () => { vigente = false; };
  }, [fila.productoId]);
  const actual = datos?.producto.id === fila.productoId ? datos : null;
  const opciones = actual?.presentaciones.filter((p) => p.estado.toLowerCase() === "activo") ?? [];
  const presentacion = opciones.find((p) => p.id === fila.productoPresentacionId);
  const factor = fila.productoPresentacionId === null ? 1 : presentacion?.factorConversion;
  const unidad = fila.productoPresentacionId === null ? actual?.producto.abreviaturaUnidadBase
    : presentacion?.abreviaturaUnidadMedida || presentacion?.unidadMedida;
  return <fieldset className="min-w-0 rounded-lg border border-slate-200 p-3" disabled={deshabilitado}>
    <legend className="px-1 text-xs font-semibold text-slate-600">Componente {numero}</legend>
    <div className="grid min-w-0 gap-3 sm:grid-cols-2">
      <SelectorEntidad compacto etiqueta={`Producto/Insumo ${numero}`} ruta="/api/productos?soloComponentesVenta=true"
        value={fila.productoId || ""} etiquetaActual={actual?.producto.nombre || fila.nombre} deshabilitado={deshabilitado}
        onChange={(id) => cambiar(fila.clave, { productoId: Number(id), productoPresentacionId: null, nombre: "", presentacion: null })} />
      <Seleccion etiqueta={`Presentación ${numero}`} value={fila.productoPresentacionId ?? ""}
        disabled={!actual || deshabilitado} onChange={(e) => cambiar(fila.clave, { productoPresentacionId: e.target.value ? Number(e.target.value) : null })}>
        <option value="">Unidad base{actual ? ` · ${actual.producto.abreviaturaUnidadBase} · × 1` : ""}</option>
        {fila.productoPresentacionId !== null && !presentacion && <option value={fila.productoPresentacionId}>{fila.presentacion || "Presentación no disponible"}</option>}
        {opciones.map((p) => <option key={p.id} value={p.id}>{p.presentacion} · {p.abreviaturaUnidadMedida || p.unidadMedida} · × {formatoNumero.format(p.factorConversion)}</option>)}
      </Seleccion>
      <Entrada etiqueta={`Cantidad ${numero}`} type="number" min="0.000001" step="0.000001" value={fila.cantidad}
        onChange={(e) => cambiar(fila.clave, { cantidad: e.target.value })} />
      <div className="flex min-w-0 items-end justify-between gap-2">
        <p className="break-words text-xs text-slate-600" aria-live="polite">{actual && factor && Number(fila.cantidad) > 0
          ? `${formatoNumero.format(Number(fila.cantidad))} ${unidad} = ${formatoNumero.format(Number(fila.cantidad) * factor)} ${actual.producto.abreviaturaUnidadBase}`
          : "Seleccione producto, presentación y cantidad."}</p>
        <Boton type="button" variante="sutil" aria-label={`Eliminar componente ${numero}`} onClick={() => quitar(fila.clave)}><Trash2 className="h-4 w-4" /></Boton>
      </div>
    </div>
    {error && <p role="alert" className="mt-2 text-sm text-rose-700">{error}</p>}
  </fieldset>;
}
