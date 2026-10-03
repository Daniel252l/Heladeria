import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type FormEvent,
} from "react";
import { Plus, Search } from "lucide-react";
import { ErrorApi, solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { useToast } from "../../components/toast/useToast";
import {
  AreaTexto,
  Boton,
  Entrada,
  Insignia,
  Seleccion,
  Tarjeta,
} from "../../components/ui/Controles";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import {
  BotonAccionTabla,
  GrupoAccionesTabla,
} from "../../components/ui/BotonAccionTabla";
import {
  EstadoCargando,
  EstadoError,
  EstadoVacio,
} from "../../components/ui/Estados";
import { Modal } from "../../components/ui/Modal";
import { Paginacion } from "../../components/ui/Paginacion";
import type { Estado, Paginado } from "../../types/api";
import type {
  Categoria,
  FormularioCatalogo,
  RegistroCatalogo,
  TipoCatalogo,
} from "../../types/productos";
import { ArbolCategorias } from "./ArbolCategorias";
import {
  construirFilasCategorias,
  idsCategoriasPadre,
} from "./jerarquiaCategorias";

const configuraciones: Record<
  TipoCatalogo,
  { titulo: string; descripcion: string; singular: string; ruta: string }
> = {
  categorias: {
    titulo: "Categorías",
    descripcion: "Organice la jerarquía utilizada para clasificar productos.",
    singular: "categoría",
    ruta: "categorias",
  },
  marcas: {
    titulo: "Marcas",
    descripcion:
      "Mantenga las marcas comerciales disponibles para el catálogo.",
    singular: "marca",
    ruta: "marcas",
  },
  tipos: {
    titulo: "Tipos de producto",
    descripcion:
      "Defina el comportamiento de los productos y su control de existencia.",
    singular: "tipo de producto",
    ruta: "tipos",
  },
  "unidades-medida": {
    titulo: "Unidades de medida",
    descripcion: "Configure unidades, símbolos y precisión para el inventario.",
    singular: "unidad de medida",
    ruta: "unidades-medida",
  },
  presentaciones: {
    titulo: "Presentaciones",
    descripcion:
      "Administre los formatos comerciales disponibles para los productos.",
    singular: "presentación",
    ruta: "presentaciones",
  },
};

const formularioInicial: FormularioCatalogo = {
  codigo: "",
  nombre: "",
  descripcion: "",
  estadoId: 0,
  categoriaPadreId: null,
  manejaExistencia: true,
  abreviatura: "",
  permiteDecimales: false,
  cantidadDecimales: 0,
};

export function PaginaCatalogo({ tipo }: { tipo: TipoCatalogo }) {
  const configuracion = configuraciones[tipo];
  const [datos, setDatos] = useState<Paginado<RegistroCatalogo> | null>(null);
  const [estados, setEstados] = useState<Estado[]>([]);
  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [categoriasExpandidas, setCategoriasExpandidas] = useState<Set<number>>(
    new Set(),
  );
  const categoriasPadreConocidas = useRef<Set<number>>(new Set());
  const [pagina, setPagina] = useState(1);
  const [textoBusqueda, setTextoBusqueda] = useState("");
  const [busqueda, setBusqueda] = useState("");
  const [estadoFiltroId, setEstadoFiltroId] = useState("");
  const [cargando, setCargando] = useState(true);
  const [guardando, setGuardando] = useState(false);
  const [cargandoDetalleId, setCargandoDetalleId] = useState<number | null>(
    null,
  );
  const [errorCarga, setErrorCarga] = useState<string | null>(null);
  const [modalAbierto, setModalAbierto] = useState(false);
  const [registroEditado, setRegistroEditado] =
    useState<RegistroCatalogo | null>(null);
  const [formulario, setFormulario] =
    useState<FormularioCatalogo>(formularioInicial);
  const [errorFormulario, setErrorFormulario] = useState<string | null>(null);
  const [conflictoVersion, setConflictoVersion] = useState(false);
  const [cambioEstado, setCambioEstado] = useState<RegistroCatalogo | null>(
    null,
  );
  const { tienePermiso } = useAutenticacion();
  const { mostrar } = useToast();
  const puedeAdministrar = tienePermiso(PERMISOS.productosAdministrar);

  const cargar = useCallback(async () => {
    setCargando(true);
    setErrorCarga(null);
    const esArbolCategorias = tipo === "categorias";
    const parametros = new URLSearchParams({
      pagina: esArbolCategorias ? "1" : String(pagina),
      tamanioPagina: esArbolCategorias ? "100" : "10",
    });
    if (!esArbolCategorias && busqueda) parametros.set("busqueda", busqueda);
    if (!esArbolCategorias && estadoFiltroId)
      parametros.set("estadoId", estadoFiltroId);
    try {
      const solicitudes: Promise<unknown>[] = [
        solicitar<Paginado<RegistroCatalogo>>(
          `/api/productos/${configuracion.ruta}?${parametros}`,
        ),
        solicitar<Paginado<Estado>>(
          "/api/configuracion/estados?pagina=1&tamanioPagina=100&activo=true",
        ),
      ];
      const resultados = await Promise.all(solicitudes);
      const listado = resultados[0] as Paginado<RegistroCatalogo>;
      setDatos(listado);
      const estadosCargados = (resultados[1] as Paginado<Estado>).elementos;
      setEstados(estadosCargados);
      if (tipo === "categorias") {
        const categoriasCargadas = listado.elementos.filter(esCategoria);
        const padresActuales = idsCategoriasPadre(categoriasCargadas);
        const padresConocidosAntes = categoriasPadreConocidas.current;
        setCategorias(categoriasCargadas);
        setCategoriasExpandidas((actuales) => {
          const siguientes = new Set(
            [...actuales].filter((id) => padresActuales.has(id)),
          );
          for (const id of padresActuales) {
            if (!padresConocidosAntes.has(id)) siguientes.add(id);
          }
          return siguientes;
        });
        categoriasPadreConocidas.current = padresActuales;
      }
    } catch (excepcion) {
      setErrorCarga(
        excepcion instanceof Error
          ? excepcion.message
          : "No fue posible cargar los registros.",
      );
    } finally {
      setCargando(false);
    }
  }, [busqueda, configuracion.ruta, estadoFiltroId, pagina, tipo]);

  useEffect(() => {
    // La carga remota sincroniza filtros y paginación con la API.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargar();
  }, [cargar]);

  const estadoActivo = useMemo(
    () =>
      estados.find((estado) => estado.nombre.trim().toLowerCase() === "activo"),
    [estados],
  );
  const esActivo = (registro: RegistroCatalogo) =>
    registro.estado.trim().toLowerCase() === "activo";

  const filasCategorias = useMemo(
    () =>
      construirFilasCategorias(
        categorias,
        categoriasExpandidas,
        busqueda,
        estadoFiltroId,
      ),
    [busqueda, categorias, categoriasExpandidas, estadoFiltroId],
  );

  const alternarCategoria = (id: number) =>
    setCategoriasExpandidas((actuales) => {
      const siguientes = new Set(actuales);
      if (siguientes.has(id)) siguientes.delete(id);
      else siguientes.add(id);
      return siguientes;
    });

  const expandirCategorias = () =>
    setCategoriasExpandidas(idsCategoriasPadre(categorias));

  const contraerCategorias = () => setCategoriasExpandidas(new Set());

  const abrirNuevo = () => {
    setRegistroEditado(null);
    setFormulario({
      ...formularioInicial,
      estadoId: estadoActivo?.id ?? estados[0]?.id ?? 0,
    });
    setErrorFormulario(null);
    setConflictoVersion(false);
    setModalAbierto(true);
  };

  const abrirEdicion = async (registro: RegistroCatalogo) => {
    setCargandoDetalleId(registro.id);
    try {
      const detalle = await solicitar<RegistroCatalogo>(
        `/api/productos/${configuracion.ruta}/${registro.id}`,
      );
      setRegistroEditado(detalle);
      setFormulario({
        codigo: detalle.codigo,
        nombre: detalle.nombre,
        descripcion:
          "descripcion" in detalle ? (detalle.descripcion ?? "") : "",
        estadoId: detalle.estadoId,
        categoriaPadreId:
          "categoriaPadreId" in detalle ? detalle.categoriaPadreId : null,
        manejaExistencia:
          "manejaExistencia" in detalle ? detalle.manejaExistencia : true,
        abreviatura: "abreviatura" in detalle ? detalle.abreviatura : "",
        permiteDecimales:
          "permiteDecimales" in detalle ? detalle.permiteDecimales : false,
        cantidadDecimales:
          "cantidadDecimales" in detalle ? detalle.cantidadDecimales : 0,
      });
      setErrorFormulario(null);
      setConflictoVersion(false);
      setModalAbierto(true);
    } catch (excepcion) {
      mostrar({
        tipo: "error",
        titulo: "No fue posible abrir el registro",
        descripcion: excepcion instanceof Error ? excepcion.message : undefined,
      });
    } finally {
      setCargandoDetalleId(null);
    }
  };

  const guardar = async (evento: FormEvent) => {
    evento.preventDefault();
    setErrorFormulario(null);
    if (
      !formulario.codigo.trim() ||
      !formulario.nombre.trim() ||
      formulario.estadoId <= 0
    ) {
      setErrorFormulario("Código, nombre y estado son obligatorios.");
      return;
    }
    if (tipo === "unidades-medida" && !formulario.abreviatura.trim()) {
      setErrorFormulario("La abreviatura es obligatoria.");
      return;
    }
    setGuardando(true);
    try {
      const cuerpo = construirSolicitud(
        tipo,
        formulario,
        registroEditado?.versionFila,
      );
      const ruta = registroEditado
        ? `/api/productos/${configuracion.ruta}/${registroEditado.id}`
        : `/api/productos/${configuracion.ruta}`;
      await solicitar<RegistroCatalogo>(ruta, {
        method: registroEditado ? "PUT" : "POST",
        body: JSON.stringify(cuerpo),
      });
      mostrar({
        tipo: "exito",
        titulo: registroEditado ? "Registro actualizado" : "Registro creado",
        descripcion: `La ${configuracion.singular} se guardó correctamente.`,
      });
      setModalAbierto(false);
      await cargar();
    } catch (excepcion) {
      setErrorFormulario(
        excepcion instanceof ErrorApi
          ? excepcion.message
          : "No fue posible guardar el registro.",
      );
      setConflictoVersion(
        excepcion instanceof ErrorApi &&
          excepcion.estado === 409 &&
          Boolean(registroEditado),
      );
    } finally {
      setGuardando(false);
    }
  };

  const confirmarCambioEstado = async () => {
    if (!cambioEstado) return;
    const destino = estados.find(
      (estado) =>
        estado.nombre.trim().toLowerCase() ===
        (esActivo(cambioEstado) ? "inactivo" : "activo"),
    );
    if (!destino) {
      mostrar({
        tipo: "error",
        titulo: "Estado no disponible",
        descripcion: "No existe un estado operativo alterno configurado.",
      });
      setCambioEstado(null);
      return;
    }
    setGuardando(true);
    try {
      await solicitar<RegistroCatalogo>(
        `/api/productos/${configuracion.ruta}/${cambioEstado.id}/estado`,
        {
          method: "PUT",
          body: JSON.stringify({ estadoId: destino.id }),
        },
      );
      mostrar({
        tipo: "exito",
        titulo: "Estado actualizado",
        descripcion: `${cambioEstado.nombre} ahora está ${destino.nombre.toLowerCase()}.`,
      });
      setCambioEstado(null);
      await cargar();
    } catch (excepcion) {
      mostrar({
        tipo: "error",
        titulo: "No fue posible cambiar el estado",
        descripcion: excepcion instanceof Error ? excepcion.message : undefined,
      });
    } finally {
      setGuardando(false);
    }
  };

  return (
    <div className="space-y-6">
      <EncabezadoPagina
        titulo={configuracion.titulo}
        descripcion={configuracion.descripcion}
        acciones={
          puedeAdministrar ? (
            <Boton onClick={abrirNuevo}>
              <Plus className="h-4 w-4" /> Nueva
            </Boton>
          ) : undefined
        }
      />
      <Tarjeta className="overflow-hidden">
        <form
          className="flex flex-col gap-3 border-b border-slate-200 p-4 sm:flex-row sm:items-end sm:justify-between"
          onSubmit={(evento) => {
            evento.preventDefault();
            setPagina(1);
            setBusqueda(textoBusqueda.trim());
          }}
        >
          <div className="relative w-full max-w-md">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              className="min-h-10 w-full rounded-lg border border-slate-300 bg-white py-2 pl-10 pr-3 text-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
              value={textoBusqueda}
              onChange={(evento) => setTextoBusqueda(evento.target.value)}
              placeholder="Buscar por código o nombre"
              aria-label="Buscar registros"
            />
          </div>
          <Seleccion
            etiqueta="Estado"
            value={estadoFiltroId}
            onChange={(evento) => {
              setEstadoFiltroId(evento.target.value);
              setPagina(1);
            }}
          >
            <option value="">Todos</option>
            {estados.map((estado) => (
              <option key={estado.id} value={estado.id}>
                {estado.nombre}
              </option>
            ))}
          </Seleccion>
          <Boton type="submit" variante="secundario">
            Buscar
          </Boton>
        </form>
        {cargando ? (
          <EstadoCargando />
        ) : errorCarga ? (
          <EstadoError mensaje={errorCarga} reintentar={cargar} />
        ) : tipo === "categorias" && !filasCategorias.length ? (
          <EstadoVacio />
        ) : !datos?.elementos.length ? (
          <EstadoVacio />
        ) : (
          <>
            {tipo === "categorias" ? (
              <ArbolCategorias
                filas={filasCategorias}
                expandidas={categoriasExpandidas}
                filtroActivo={Boolean(busqueda || estadoFiltroId)}
                puedeAdministrar={puedeAdministrar}
                cargandoDetalleId={cargandoDetalleId}
                alternar={alternarCategoria}
                expandirTodo={expandirCategorias}
                contraerTodo={contraerCategorias}
                editar={(categoria) => void abrirEdicion(categoria)}
                cambiarEstado={setCambioEstado}
              />
            ) : (
              <>
                <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-200 text-left text-sm">
                <thead className="bg-slate-50 text-xs font-semibold uppercase tracking-wide text-slate-500">
                  <tr>
                    <th className="px-5 py-3">Código</th>
                    <th className="px-5 py-3">Nombre</th>
                    {tipo === "marcas" && (
                      <th className="px-5 py-3">Descripción</th>
                    )}
                    {tipo === "tipos" && (
                      <th className="px-5 py-3">Existencia</th>
                    )}
                    {tipo === "unidades-medida" && (
                      <>
                        <th className="px-5 py-3">Abreviatura</th>
                        <th className="px-5 py-3">Permite decimales</th>
                        <th className="px-5 py-3">Cantidad de decimales</th>
                      </>
                    )}
                    {tipo === "presentaciones" && (
                      <th className="px-5 py-3">Descripción</th>
                    )}
                    <th className="px-5 py-3">Estado</th>
                    {puedeAdministrar && (
                      <th className="px-5 py-3 text-right">Acciones</th>
                    )}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 bg-white">
                  {datos.elementos.map((registro) => (
                    <tr key={registro.id} className="hover:bg-slate-50/70">
                      <td className="whitespace-nowrap px-5 py-4 font-mono text-xs font-semibold text-slate-600">
                        {registro.codigo}
                      </td>
                      <td className="px-5 py-4 font-semibold text-slate-900">
                        {registro.nombre}
                      </td>
                      {tipo === "marcas" && "descripcion" in registro && (
                        <td className="max-w-xs truncate px-5 py-4 text-slate-500">
                          {registro.descripcion || "—"}
                        </td>
                      )}
                      {tipo === "tipos" && "manejaExistencia" in registro && (
                        <td className="px-5 py-4">
                          <Insignia
                            tono={registro.manejaExistencia ? "info" : "neutro"}
                          >
                            {registro.manejaExistencia ? "Sí" : "No"}
                          </Insignia>
                        </td>
                      )}
                      {tipo === "unidades-medida" &&
                        "abreviatura" in registro && (
                          <>
                            <td className="px-5 py-4 text-slate-600">
                              {registro.abreviatura}
                            </td>
                            <td className="px-5 py-4 text-slate-600">
                              <Insignia
                                tono={
                                  registro.permiteDecimales ? "info" : "neutro"
                                }
                              >
                                {registro.permiteDecimales ? "Sí" : "No"}
                              </Insignia>
                            </td>
                            <td className="px-5 py-4 text-slate-600">
                              {registro.cantidadDecimales}
                            </td>
                          </>
                        )}
                      {tipo === "presentaciones" &&
                        "descripcion" in registro && (
                          <td className="max-w-xs truncate px-5 py-4 text-slate-500">
                            {registro.descripcion || "—"}
                          </td>
                        )}
                      <td className="px-5 py-4">
                        <Insignia
                          tono={esActivo(registro) ? "positivo" : "neutro"}
                        >
                          {registro.estado}
                        </Insignia>
                      </td>
                      {puedeAdministrar && (
                        <td className="whitespace-nowrap px-5 py-4 text-right">
                          <GrupoAccionesTabla>
                            <BotonAccionTabla
                              tipo="editar"
                              onClick={() => void abrirEdicion(registro)}
                              etiqueta={`Editar ${registro.nombre}`}
                              disabled={cargandoDetalleId !== null}
                            />
                            <BotonAccionTabla
                              tipo={
                                esActivo(registro) ? "desactivar" : "activar"
                              }
                              onClick={() => setCambioEstado(registro)}
                              etiqueta={`${esActivo(registro) ? "Desactivar" : "Activar"} ${registro.nombre}`}
                              disabled={cargandoDetalleId !== null}
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
          </>
        )}
      </Tarjeta>

      <Modal
        abierto={modalAbierto}
        cerrar={() => !guardando && setModalAbierto(false)}
        titulo={
          registroEditado
            ? `Editar ${configuracion.singular}`
            : `Nueva ${configuracion.singular}`
        }
        descripcion="Los campos marcados son obligatorios."
      >
        <form onSubmit={guardar} className="space-y-5 p-6">
          <div className="grid gap-4 sm:grid-cols-2">
            <Entrada
              etiqueta="Código *"
              value={formulario.codigo}
              onChange={(evento) =>
                setFormulario((actual) => ({
                  ...actual,
                  codigo: evento.target.value,
                }))
              }
              maxLength={tipo === "unidades-medida" ? 20 : 25}
            />
            <Entrada
              etiqueta="Nombre *"
              value={formulario.nombre}
              onChange={(evento) =>
                setFormulario((actual) => ({
                  ...actual,
                  nombre: evento.target.value,
                }))
              }
              maxLength={
                tipo === "tipos" ||
                tipo === "unidades-medida" ||
                tipo === "presentaciones"
                  ? 100
                  : 150
              }
            />
          </div>
          {(tipo === "categorias" ||
            tipo === "marcas" ||
            tipo === "presentaciones") && (
            <AreaTexto
              etiqueta="Descripción"
              rows={3}
              value={formulario.descripcion}
              onChange={(evento) =>
                setFormulario((actual) => ({
                  ...actual,
                  descripcion: evento.target.value,
                }))
              }
              maxLength={tipo === "presentaciones" ? 300 : 500}
            />
          )}
          {tipo === "categorias" && (
            <Seleccion
              etiqueta="Categoría padre"
              value={formulario.categoriaPadreId ?? ""}
              onChange={(evento) =>
                setFormulario((actual) => ({
                  ...actual,
                  categoriaPadreId: evento.target.value
                    ? Number(evento.target.value)
                    : null,
                }))
              }
            >
              <option value="">Sin categoría padre</option>
              {categorias
                .filter((categoria) => categoria.id !== registroEditado?.id)
                .map((categoria) => (
                  <option key={categoria.id} value={categoria.id}>
                    {categoria.nombre}
                  </option>
                ))}
            </Seleccion>
          )}
          {tipo === "tipos" && (
            <label className="flex items-center gap-3 rounded-lg border border-slate-200 p-4 text-sm font-medium text-slate-700">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-slate-300 accent-teal-700"
                checked={formulario.manejaExistencia}
                onChange={(evento) =>
                  setFormulario((actual) => ({
                    ...actual,
                    manejaExistencia: evento.target.checked,
                  }))
                }
              />{" "}
              Maneja existencia
            </label>
          )}
          {tipo === "unidades-medida" && (
            <>
              <Entrada
                etiqueta="Abreviatura *"
                value={formulario.abreviatura}
                onChange={(evento) =>
                  setFormulario((actual) => ({
                    ...actual,
                    abreviatura: evento.target.value,
                  }))
                }
                maxLength={20}
              />
              <div className="grid gap-4 sm:grid-cols-2">
                <label className="flex items-center gap-3 rounded-lg border border-slate-200 p-4 text-sm font-medium text-slate-700">
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-slate-300 accent-teal-700"
                    checked={formulario.permiteDecimales}
                    onChange={(evento) =>
                      setFormulario((actual) => ({
                        ...actual,
                        permiteDecimales: evento.target.checked,
                        cantidadDecimales: evento.target.checked
                          ? actual.cantidadDecimales
                          : 0,
                      }))
                    }
                  />{" "}
                  Permite decimales
                </label>
                <Entrada
                  etiqueta="Cantidad de decimales"
                  type="number"
                  min={0}
                  max={6}
                  disabled={!formulario.permiteDecimales}
                  value={formulario.cantidadDecimales}
                  onChange={(evento) =>
                    setFormulario((actual) => ({
                      ...actual,
                      cantidadDecimales: Number(evento.target.value),
                    }))
                  }
                />
              </div>
            </>
          )}
          <Seleccion
            etiqueta="Estado *"
            value={formulario.estadoId || ""}
            onChange={(evento) =>
              setFormulario((actual) => ({
                ...actual,
                estadoId: Number(evento.target.value),
              }))
            }
          >
            <option value="">Seleccione un estado</option>
            {estados.map((estado) => (
              <option key={estado.id} value={estado.id}>
                {estado.nombre}
              </option>
            ))}
          </Seleccion>
          {errorFormulario && (
            <div
              className="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700"
              role="alert"
            >
              {errorFormulario}
              {conflictoVersion && registroEditado && (
                <button
                  type="button"
                  className="ml-3 font-semibold text-teal-700"
                  onClick={() => void abrirEdicion(registroEditado)}
                >
                  Recargar datos
                </button>
              )}
            </div>
          )}
          <div className="flex justify-end gap-3 border-t border-slate-100 pt-5">
            <Boton
              type="button"
              variante="secundario"
              onClick={() => setModalAbierto(false)}
              disabled={guardando}
            >
              Cancelar
            </Boton>
            <Boton type="submit" cargando={guardando}>
              Guardar
            </Boton>
          </div>
        </form>
      </Modal>

      <Modal
        abierto={Boolean(cambioEstado)}
        cerrar={() => !guardando && setCambioEstado(null)}
        titulo="Confirmar cambio de estado"
        descripcion="El backend verificará las dependencias antes de aplicar el cambio."
        ancho="max-w-md"
      >
        <div className="p-6">
          <p className="text-sm leading-6 text-slate-600">
            ¿Desea{" "}
            {cambioEstado && esActivo(cambioEstado) ? "inactivar" : "activar"}{" "}
            <strong className="text-slate-900">{cambioEstado?.nombre}</strong>?
          </p>
          <div className="mt-6 flex justify-end gap-3">
            <Boton
              variante="secundario"
              onClick={() => setCambioEstado(null)}
              disabled={guardando}
            >
              Cancelar
            </Boton>
            <Boton
              variante={
                cambioEstado && esActivo(cambioEstado) ? "peligro" : "primario"
              }
              cargando={guardando}
              onClick={confirmarCambioEstado}
            >
              Confirmar
            </Boton>
          </div>
        </div>
      </Modal>
    </div>
  );
}

function esCategoria(registro: RegistroCatalogo): registro is Categoria {
  return "categoriaPadreId" in registro && "nivel" in registro;
}

function construirSolicitud(
  tipo: TipoCatalogo,
  formulario: FormularioCatalogo,
  versionFila?: string,
): Record<string, unknown> {
  const base: Record<string, unknown> = {
    codigo: formulario.codigo.trim(),
    nombre: formulario.nombre.trim(),
    estadoId: formulario.estadoId,
  };
  if (versionFila) base.versionFila = versionFila;
  if (tipo === "categorias") {
    base.descripcion = formulario.descripcion.trim() || null;
    base.categoriaPadreId = formulario.categoriaPadreId;
  } else if (tipo === "marcas" || tipo === "presentaciones") {
    base.descripcion = formulario.descripcion.trim() || null;
  } else if (tipo === "tipos") {
    base.manejaExistencia = formulario.manejaExistencia;
  } else if (tipo === "unidades-medida") {
    base.abreviatura = formulario.abreviatura.trim();
    base.permiteDecimales = formulario.permiteDecimales;
    base.cantidadDecimales = formulario.permiteDecimales
      ? formulario.cantidadDecimales
      : 0;
  }
  return base;
}
