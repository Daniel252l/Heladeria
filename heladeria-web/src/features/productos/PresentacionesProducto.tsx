import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Plus } from "lucide-react";
import { solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { useToast } from "../../components/toast/useToast";
import { Boton, Entrada, Insignia } from "../../components/ui/Controles";
import { DialogoConfirmacion } from "../../components/ui/DialogoConfirmacion";
import {
  BotonAccionTabla,
  GrupoAccionesTabla,
} from "../../components/ui/BotonAccionTabla";
import {
  EstadoCargando,
  EstadoError,
  EstadoVacio,
} from "../../components/ui/Estados";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import type { Producto, ProductoPresentacion } from "../../types/productos";
import { formatoCosto, formatoNumero } from "../../utils/formato";

const inicial = {
  presentacionId: "",
  unidadMedidaId: "",
  factorConversion: "1",
  esPresentacionBase: false,
  permiteCompra: true,
  permiteVenta: true,
  precioVentaEstandar: "",
  estadoId: "",
};
export function PresentacionesProducto({ producto }: { producto: Producto }) {
  const [datos, setDatos] = useState<ProductoPresentacion[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [cargando, setCargando] = useState(true);
  const [editado, setEditado] = useState<ProductoPresentacion | null>(null);
  const [form, setForm] = useState(inicial);
  const [abierto, setAbierto] = useState(false);
  const [guardando, setGuardando] = useState(false);
  const [cambio, setCambio] = useState<ProductoPresentacion | null>(null);
  const [estadoDestino, setEstadoDestino] = useState("");
  const { tienePermiso } = useAutenticacion();
  const puedeAdministrar = tienePermiso(PERMISOS.productosAdministrar);
  const { mostrar } = useToast();
  const ruta = `/api/productos/${producto.id}/presentaciones`;
  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      setDatos(await solicitar<ProductoPresentacion[]>(ruta));
    } catch (e) {
      setError(
        e instanceof Error
          ? e.message
          : "No fue posible cargar las presentaciones.",
      );
    } finally {
      setCargando(false);
    }
  }, [ruta]);
  useEffect(() => {
    // La colección remota pertenece al producto seleccionado.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargar();
  }, [cargar]);
  const editar = async (p?: ProductoPresentacion) => {
    if (p) {
      const vigentes = await solicitar<ProductoPresentacion[]>(ruta);
      const actual = vigentes.find((v) => v.id === p.id);
      if (!actual) {
        setError("La presentación ya no existe. Recargue los datos.");
        return;
      }
      setEditado(actual);
      setForm({
        presentacionId: String(actual.presentacionId),
        unidadMedidaId: String(actual.unidadMedidaId),
        factorConversion: String(actual.factorConversion),
        esPresentacionBase: actual.esPresentacionBase,
        permiteCompra: actual.permiteCompra,
        permiteVenta: actual.permiteVenta,
        precioVentaEstandar:
          actual.precioVentaEstandar == null
            ? ""
            : String(actual.precioVentaEstandar),
        estadoId: String(actual.estadoId),
      });
    } else {
      setEditado(null);
      setForm(inicial);
    }
    setError(null);
    setAbierto(true);
  };
  const guardar = async (evento: FormEvent) => {
    evento.preventDefault();
    if (
      !form.presentacionId ||
      !form.unidadMedidaId ||
      !(Number(form.factorConversion) > 0) ||
      !form.estadoId
    ) {
      setError("Complete la presentación, unidad, factor positivo y estado.");
      return;
    }
    setGuardando(true);
    try {
      await solicitar(editado ? `${ruta}/${editado.id}` : ruta, {
        method: editado ? "PUT" : "POST",
        body: JSON.stringify({
          ...form,
          presentacionId: Number(form.presentacionId),
          unidadMedidaId: Number(form.unidadMedidaId),
          factorConversion: Number(form.factorConversion),
          precioVentaEstandar:
            form.precioVentaEstandar === ""
              ? null
              : Number(form.precioVentaEstandar),
          estadoId: Number(form.estadoId),
          ...(editado ? { versionFila: editado.versionFila } : {}),
        }),
      });
      setAbierto(false);
      mostrar({ tipo: "exito", titulo: "Presentación guardada" });
      await cargar();
    } catch (e) {
      setError(e instanceof Error ? e.message : "No fue posible guardar.");
    } finally {
      setGuardando(false);
    }
  };
  const confirmarEstado = async () => {
    if (!cambio || !estadoDestino) return;
    setGuardando(true);
    try {
      await solicitar(`${ruta}/${cambio.id}/estado`, {
        method: "PUT",
        body: JSON.stringify({ estadoId: Number(estadoDestino) }),
      });
      setCambio(null);
      await cargar();
    } catch (e) {
      mostrar({
        tipo: "error",
        titulo: "Cambio rechazado",
        descripcion: e instanceof Error ? e.message : undefined,
      });
    } finally {
      setGuardando(false);
    }
  };
  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="font-bold text-slate-900">
            Presentaciones del producto
          </h3>
          <p className="text-sm text-slate-500">
            Factores reales respecto de {producto.abreviaturaUnidadBase}; no son
            conversiones inventadas.
          </p>
        </div>
        {puedeAdministrar && (
          <Boton onClick={() => void editar()}>
            <Plus className="h-4 w-4" /> Agregar presentación
          </Boton>
        )}
      </div>
      {cargando ? (
        <EstadoCargando />
      ) : error && !abierto ? (
        <EstadoError mensaje={error} reintentar={cargar} />
      ) : !datos.length ? (
        <EstadoVacio titulo="Sin presentaciones" />
      ) : (
        <div className="overflow-x-auto rounded-xl border border-slate-200">
          <table className="min-w-[60rem] text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase text-slate-500">
              <tr>
                {[
                  "Presentación",
                  "Unidad",
                  "Equivalencia",
                  "Base",
                  "Compra",
                  "Venta",
                  "Precio estándar",
                  "Estado",
                  ...(puedeAdministrar ? ["Acciones"] : []),
                ].map((v) => (
                  <th key={v} className="px-4 py-3">
                    {v}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {datos.map((p) => (
                <tr key={p.id}>
                  <td className="px-4 py-3 font-semibold">{p.presentacion}</td>
                  <td className="px-4 py-3">{p.unidadMedida}</td>
                  <td className="px-4 py-3">
                    1 {p.presentacion} ={" "}
                    {formatoNumero.format(p.factorConversion)}{" "}
                    {producto.abreviaturaUnidadBase}
                  </td>
                  {[p.esPresentacionBase, p.permiteCompra, p.permiteVenta].map(
                    (v, i) => (
                      <td key={i} className="px-4 py-3">
                        <Insignia tono={v ? "positivo" : "neutro"}>
                          {v ? "Sí" : "No"}
                        </Insignia>
                      </td>
                    ),
                  )}
                  <td className="px-4 py-3">
                    {p.precioVentaEstandar == null
                      ? "—"
                      : formatoCosto.format(p.precioVentaEstandar)}
                  </td>
                  <td className="px-4 py-3">{p.estado}</td>
                  {puedeAdministrar && (
                    <td className="px-4 py-3 text-right">
                      <GrupoAccionesTabla>
                        <BotonAccionTabla
                          tipo="editar"
                          etiqueta={`Editar ${p.presentacion}`}
                          onClick={() => void editar(p)}
                        />
                        <BotonAccionTabla
                          tipo={
                            p.estado.toLowerCase() === "activo"
                              ? "desactivar"
                              : "activar"
                          }
                          etiqueta={`${p.estado.toLowerCase() === "activo" ? "Desactivar" : "Activar"} ${p.presentacion}`}
                          onClick={() => {
                            setCambio(p);
                            setEstadoDestino("");
                          }}
                        />
                      </GrupoAccionesTabla>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {abierto && (
        <form
          onSubmit={guardar}
          className="space-y-4 rounded-xl border border-teal-200 bg-teal-50/30 p-5"
        >
          <h4 className="font-bold">
            {editado ? "Editar presentación" : "Nueva presentación"}
          </h4>
          <div className="grid gap-4 sm:grid-cols-2">
            <SelectorEntidad
              etiqueta="Presentación *"
              ruta="/api/productos/presentaciones"
              value={form.presentacionId}
              etiquetaActual={editado?.presentacion}
              onChange={(v) =>
                setForm((f) => ({ ...f, presentacionId: v ?? "" }))
              }
            />
            <SelectorEntidad
              etiqueta="Unidad de entrada *"
              ruta="/api/productos/unidades-medida"
              value={form.unidadMedidaId}
              etiquetaActual={editado?.unidadMedida}
              onChange={(v) =>
                setForm((f) => ({ ...f, unidadMedidaId: v ?? "" }))
              }
            />
            <Entrada
              etiqueta={`Factor hacia ${producto.abreviaturaUnidadBase} *`}
              type="number"
              min="0.000001"
              step="0.000001"
              value={form.factorConversion}
              onChange={(e) =>
                setForm((f) => ({ ...f, factorConversion: e.target.value }))
              }
            />
            <Entrada
              etiqueta="Precio estándar"
              type="number"
              min="0"
              step="0.01"
              value={form.precioVentaEstandar}
              onChange={(e) =>
                setForm((f) => ({ ...f, precioVentaEstandar: e.target.value }))
              }
            />
            <SelectorEntidad
              etiqueta="Estado *"
              ruta="/api/configuracion/estados"
              value={form.estadoId}
              etiquetaActual={editado?.estado}
              onChange={(v) => setForm((f) => ({ ...f, estadoId: v ?? "" }))}
            />
          </div>
          <div className="flex flex-wrap gap-4">
            {(
              [
                ["esPresentacionBase", "Presentación base"],
                ["permiteCompra", "Permite compra"],
                ["permiteVenta", "Permite venta"],
              ] as const
            ).map(([k, label]) => (
              <label key={k} className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form[k]}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, [k]: e.target.checked }))
                  }
                />
                {label}
              </label>
            ))}
          </div>
          {error && (
            <p role="alert" className="text-sm text-rose-700">
              {error}
              {editado && (
                <button
                  type="button"
                  className="ml-3 font-semibold text-teal-700"
                  onClick={() => void editar(editado)}
                >
                  Recargar detalle
                </button>
              )}
            </p>
          )}
          <div className="flex justify-end gap-3">
            <Boton
              type="button"
              variante="secundario"
              onClick={() => setAbierto(false)}
              disabled={guardando}
            >
              Cancelar
            </Boton>
            <Boton cargando={guardando}>Guardar</Boton>
          </div>
        </form>
      )}
      <DialogoConfirmacion
        abierto={Boolean(cambio)}
        titulo="Cambiar estado de presentación"
        cerrar={() => setCambio(null)}
        confirmar={confirmarEstado}
        cargando={guardando}
        peligro
      >
        <SelectorEntidad
          etiqueta="Estado destino"
          ruta="/api/configuracion/estados"
          value={estadoDestino}
          onChange={(v) => setEstadoDestino(v ?? "")}
        />
      </DialogoConfirmacion>
    </section>
  );
}
