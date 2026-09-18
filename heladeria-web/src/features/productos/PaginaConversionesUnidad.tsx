import { useCallback, useEffect, useState, type FormEvent } from "react";
import { Plus } from "lucide-react";
import { solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { useToast } from "../../components/toast/useToast";
import {
  Boton,
  Entrada,
  Insignia,
  Tarjeta,
} from "../../components/ui/Controles";
import { DialogoConfirmacion } from "../../components/ui/DialogoConfirmacion";
import {
  BotonAccionTabla,
  GrupoAccionesTabla,
} from "../../components/ui/BotonAccionTabla";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import {
  EstadoCargando,
  EstadoError,
  EstadoVacio,
} from "../../components/ui/Estados";
import { Modal } from "../../components/ui/Modal";
import { Paginacion } from "../../components/ui/Paginacion";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import type { Paginado } from "../../types/api";
import type { ConversionUnidad } from "../../types/productos";
import { formatoNumero } from "../../utils/formato";

const inicial = {
  unidadOrigenId: "",
  unidadDestinoId: "",
  factor: "",
  estadoId: "",
};
export function PaginaConversionesUnidad() {
  const [datos, setDatos] = useState<Paginado<ConversionUnidad> | null>(null);
  const [pagina, setPagina] = useState(1);
  const [texto, setTexto] = useState("");
  const [busqueda, setBusqueda] = useState("");
  const [filtroEstado, setFiltroEstado] = useState("");
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actual, setActual] = useState<ConversionUnidad | null>(null);
  const [modal, setModal] = useState(false);
  const [form, setForm] = useState(inicial);
  const [guardando, setGuardando] = useState(false);
  const [cambio, setCambio] = useState<ConversionUnidad | null>(null);
  const [estadoDestino, setEstadoDestino] = useState("");
  const { tienePermiso, sucursalActiva } = useAutenticacion();
  const puedeAdministrar = tienePermiso(PERMISOS.productosAdministrar);
  const { mostrar } = useToast();
  const ruta = "/api/productos/conversiones-unidad";
  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    const p = new URLSearchParams({
      pagina: String(pagina),
      tamanioPagina: "10",
    });
    if (busqueda) p.set("busqueda", busqueda);
    if (filtroEstado) p.set("estadoId", filtroEstado);
    try {
      setDatos(await solicitar<Paginado<ConversionUnidad>>(`${ruta}?${p}`));
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "No fue posible cargar conversiones.",
      );
    } finally {
      setCargando(false);
    }
  }, [busqueda, filtroEstado, pagina]);
  useEffect(() => {
    // El listado remoto es la única lectura que ofrece el contrato de conversiones.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargar();
  }, [cargar]);
  const editar = (c?: ConversionUnidad) => {
    setActual(c ?? null);
    setForm(
      c
        ? {
            unidadOrigenId: String(c.unidadOrigenId),
            unidadDestinoId: String(c.unidadDestinoId),
            factor: String(c.factor),
            estadoId: String(c.estadoId),
          }
        : inicial,
    );
    setError(null);
    setModal(true);
  };
  const guardar = async (evento: FormEvent) => {
    evento.preventDefault();
    if (
      !form.unidadOrigenId ||
      !form.unidadDestinoId ||
      !(Number(form.factor) > 0) ||
      !form.estadoId ||
      form.unidadOrigenId === form.unidadDestinoId
    ) {
      setError(
        "Seleccione unidades distintas, un factor positivo y un estado.",
      );
      return;
    }
    setGuardando(true);
    try {
      await solicitar(actual ? `${ruta}/${actual.id}` : ruta, {
        method: actual ? "PUT" : "POST",
        body: JSON.stringify({
          unidadOrigenId: Number(form.unidadOrigenId),
          unidadDestinoId: Number(form.unidadDestinoId),
          factor: Number(form.factor),
          estadoId: Number(form.estadoId),
        }),
      });
      setModal(false);
      mostrar({ tipo: "exito", titulo: "Conversión guardada" });
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
    <div className="space-y-6">
      <EncabezadoPagina
        titulo="Conversiones de unidad"
        descripcion="Equivalencias definidas para la empresa actual. Cada relación aparece una sola vez."
        acciones={
          puedeAdministrar ? (
            <Boton onClick={() => editar()}>
              <Plus className="h-4 w-4" /> Nueva conversión
            </Boton>
          ) : undefined
        }
      />
      <Tarjeta className="overflow-hidden">
        <form
          className="grid gap-4 border-b p-4 sm:grid-cols-3"
          onSubmit={(e) => {
            e.preventDefault();
            setBusqueda(texto.trim());
            setPagina(1);
          }}
        >
          <Entrada
            etiqueta="Buscar unidades"
            value={texto}
            onChange={(e) => setTexto(e.target.value)}
          />
          <SelectorEntidad
            etiqueta="Estado"
            ruta="/api/configuracion/estados"
            value={filtroEstado}
            opcional
            onChange={(v) => {
              setFiltroEstado(v ?? "");
              setPagina(1);
            }}
          />
          <Boton variante="secundario" className="self-end">
            Buscar
          </Boton>
        </form>
        {cargando ? (
          <EstadoCargando />
        ) : error && !modal ? (
          <EstadoError mensaje={error} reintentar={cargar} />
        ) : !datos?.elementos.length ? (
          <EstadoVacio />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="min-w-full text-left text-sm">
                <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                  <tr>
                    {[
                      "Unidad origen",
                      "Unidad destino",
                      "Factor",
                      "Empresa",
                      "Estado",
                      ...(puedeAdministrar ? ["Acciones"] : []),
                    ].map((v) => (
                      <th key={v} className="px-5 py-3">
                        {v}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {datos.elementos.map((c) => (
                    <tr key={c.id}>
                      <td className="px-5 py-4">{c.unidadOrigen}</td>
                      <td className="px-5 py-4">{c.unidadDestino}</td>
                      <td className="px-5 py-4 font-semibold">
                        1 = {formatoNumero.format(c.factor)}
                      </td>
                      <td className="px-5 py-4">{sucursalActiva?.empresa}</td>
                      <td className="px-5 py-4">
                        <Insignia>{c.estado}</Insignia>
                      </td>
                      {puedeAdministrar && (
                        <td className="px-5 py-4 text-right">
                          <GrupoAccionesTabla>
                            <BotonAccionTabla
                              tipo="editar"
                              etiqueta={`Editar conversión ${c.id}`}
                              onClick={() => editar(c)}
                            />
                            <BotonAccionTabla
                              tipo={
                                c.estado.toLowerCase() === "activo"
                                  ? "desactivar"
                                  : "activar"
                              }
                              etiqueta={`${c.estado.toLowerCase() === "activo" ? "Desactivar" : "Activar"} conversión ${c.id}`}
                              onClick={() => {
                                setCambio(c);
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
            <Paginacion
              pagina={datos.pagina}
              totalPaginas={datos.totalPaginas}
              totalRegistros={datos.totalRegistros}
              cambiar={setPagina}
            />
          </>
        )}
      </Tarjeta>
      <Modal
        abierto={modal}
        cerrar={() => !guardando && setModal(false)}
        titulo={actual ? "Editar conversión" : "Nueva conversión"}
      >
        <form className="space-y-4 p-6" onSubmit={guardar}>
          <SelectorEntidad
            etiqueta="Unidad origen *"
            ruta="/api/productos/unidades-medida"
            value={form.unidadOrigenId}
            etiquetaActual={actual?.unidadOrigen}
            onChange={(v) =>
              setForm((f) => ({ ...f, unidadOrigenId: v ?? "" }))
            }
          />
          <SelectorEntidad
            etiqueta="Unidad destino *"
            ruta="/api/productos/unidades-medida"
            value={form.unidadDestinoId}
            etiquetaActual={actual?.unidadDestino}
            onChange={(v) =>
              setForm((f) => ({ ...f, unidadDestinoId: v ?? "" }))
            }
          />
          <Entrada
            etiqueta="Factor *"
            type="number"
            min="0.00000001"
            step="0.00000001"
            value={form.factor}
            onChange={(e) => setForm((f) => ({ ...f, factor: e.target.value }))}
          />
          <SelectorEntidad
            etiqueta="Estado *"
            ruta="/api/configuracion/estados"
            value={form.estadoId}
            etiquetaActual={actual?.estado}
            onChange={(v) => setForm((f) => ({ ...f, estadoId: v ?? "" }))}
          />
          {error && (
            <p role="alert" className="text-sm text-rose-700">
              {error}
            </p>
          )}
          <div className="flex justify-end gap-3">
            <Boton
              type="button"
              variante="secundario"
              disabled={guardando}
              onClick={() => setModal(false)}
            >
              Cancelar
            </Boton>
            <Boton cargando={guardando}>Guardar</Boton>
          </div>
        </form>
      </Modal>
      <DialogoConfirmacion
        abierto={Boolean(cambio)}
        titulo="Cambiar estado de conversión"
        cerrar={() => setCambio(null)}
        confirmar={confirmarEstado}
        cargando={guardando}
      >
        <SelectorEntidad
          etiqueta="Estado destino"
          ruta="/api/configuracion/estados"
          value={estadoDestino}
          onChange={(v) => setEstadoDestino(v ?? "")}
        />
      </DialogoConfirmacion>
    </div>
  );
}
