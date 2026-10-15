import { useCallback, useEffect, useId, useRef, useState } from "react";
import { Check, ChevronDown, LoaderCircle, Search } from "lucide-react";
import { solicitar } from "../../api/clienteHttp";
import type { Paginado } from "../../types/api";
import { clases } from "../../utils/clases";

export interface OpcionEntidad {
  id: number | string;
  nombre: string;
  codigo?: string;
}

export function SelectorEntidad({
  etiqueta,
  ruta,
  value,
  onChange,
  opcional = false,
  deshabilitado = false,
  etiquetaActual,
  error,
  placeholder,
  icono,
  compacto = false,
  sinIcono = false,
  opcionesLocales,
  cargandoExterno = false,
}: {
  etiqueta: string;
  ruta?: string;
  value: number | string;
  onChange: (id: string | null) => void;
  opcional?: boolean;
  deshabilitado?: boolean;
  etiquetaActual?: string;
  error?: string;
  placeholder?: string;
  icono?: React.ReactNode;
  compacto?: boolean;
  sinIcono?: boolean;
  opcionesLocales?: OpcionEntidad[];
  cargandoExterno?: boolean;
}) {
  const identificador = useId();
  const contenedor = useRef<HTMLDivElement>(null);
  const solicitudActual = useRef(0);
  const [abierto, setAbierto] = useState(false);
  const [texto, setTexto] = useState("");
  const [busqueda, setBusqueda] = useState("");
  const [opciones, setOpciones] = useState<OpcionEntidad[]>([]);
  const [pagina, setPagina] = useState(1);
  const [totalPaginas, setTotalPaginas] = useState(1);
  const [cargando, setCargando] = useState(false);
  const [errorCarga, setErrorCarga] = useState<string | null>(null);
  const usaOpcionesLocales = opcionesLocales !== undefined;

  const invalidarSolicitudes = useCallback(() => {
    solicitudActual.current++;
  }, []);

  const cargar = useCallback(
    async (paginaSolicitada: number, anexar: boolean) => {
      const solicitud = ++solicitudActual.current;
      if (usaOpcionesLocales) return;
      if (!ruta || deshabilitado) {
        setOpciones([]);
        setCargando(false);
        return;
      }
      setCargando(true);
      setErrorCarga(null);
      try {
        const separador = ruta.includes("?") ? "&" : "?";
        const parametros = new URLSearchParams({
          pagina: String(paginaSolicitada),
          tamanioPagina: "30",
        });
        if (busqueda) parametros.set("busqueda", busqueda);
        const respuesta = await solicitar<Paginado<OpcionEntidad>>(
          `${ruta}${separador}${parametros}`,
        );
        if (solicitud !== solicitudActual.current) return;
        const normalizadas = respuesta.elementos.map((opcion) => {
          const alternativa = opcion as OpcionEntidad & {
            razonSocial?: string;
            nombreCliente?: string;
            nombreProveedor?: string;
            codigoProveedor?: string;
          };
          return {
            ...opcion,
            codigo: opcion.codigo || alternativa.codigoProveedor,
            nombre: opcion.nombre || alternativa.nombreProveedor || alternativa.nombreCliente || alternativa.razonSocial || `Registro ${opcion.id}`,
          };
        });
        setOpciones((actuales) =>
          anexar ? [...actuales, ...normalizadas] : normalizadas,
        );
        setTotalPaginas(respuesta.totalPaginas);
        setPagina(paginaSolicitada);
      } catch (excepcion) {
        if (solicitud === solicitudActual.current)
          setErrorCarga(
            excepcion instanceof Error
              ? excepcion.message
              : "No fue posible cargar opciones.",
          );
      } finally {
        if (solicitud === solicitudActual.current) setCargando(false);
      }
    },
    [busqueda, deshabilitado, ruta, usaOpcionesLocales],
  );

  useEffect(() => {
    // Sincroniza este control con el origen remoto y descarta respuestas obsoletas.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargar(1, false);
    return invalidarSolicitudes;
  }, [cargar, invalidarSolicitudes]);

  useEffect(() => {
    if (!abierto) return;
    const cerrarAlExterior = (evento: PointerEvent) => {
      if (!contenedor.current?.contains(evento.target as Node))
        setAbierto(false);
    };
    document.addEventListener("pointerdown", cerrarAlExterior);
    return () => document.removeEventListener("pointerdown", cerrarAlExterior);
  }, [abierto]);

  const opcionesDisponibles = opcionesLocales ?? opciones;
  const opcionesVisibles = usaOpcionesLocales
    ? opcionesDisponibles.filter((opcion) => {
        const termino = texto.trim().toLocaleLowerCase("es");
        return !termino || `${opcion.codigo ?? ""} ${opcion.nombre}`.toLocaleLowerCase("es").includes(termino);
      })
    : opciones;
  const seleccionada = opcionesDisponibles.find(
    (opcion) => String(opcion.id) === String(value),
  );
  const nombreSeleccionado = value
    ? seleccionada
      ? `${seleccionada.codigo ? `${seleccionada.codigo} · ` : ""}${seleccionada.nombre}`
      : etiquetaActual || `Seleccionado #${value}`
    : placeholder ||
      (opcional
        ? "Sin asignar"
        : `Buscar o seleccionar ${etiqueta.replace(" *", "").toLowerCase()}…`);

  const buscar = () => {
    if (usaOpcionesLocales) return;
    const nuevaBusqueda = texto.trim();
    if (nuevaBusqueda === busqueda) void cargar(1, false);
    else setBusqueda(nuevaBusqueda);
  };

  return (
    <div className="relative min-w-0" ref={contenedor}>
      <label
        id={`${identificador}-etiqueta`}
        className="block text-sm font-medium text-slate-700"
      >
        {etiqueta}
      </label>
      <button
        type="button"
        className={clases(
          "mt-1.5 flex w-full items-center gap-3 rounded-lg border bg-white pr-3 text-left text-sm text-slate-900 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100 disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-400",
          compacto ? "min-h-10" : "min-h-11",
          icono ? "pl-0" : "px-3 py-2",
          error ? "border-rose-400" : "border-slate-300",
        )}
        aria-labelledby={`${identificador}-etiqueta ${identificador}-valor`}
        aria-haspopup="listbox"
        aria-expanded={abierto}
        aria-controls={abierto ? `${identificador}-opciones` : undefined}
        disabled={deshabilitado}
        onClick={() => setAbierto((valor) => !valor)}
      >
        {icono ? (
          <span
            aria-hidden="true"
            className="flex w-11 shrink-0 self-stretch items-center justify-center rounded-l-lg border-r border-slate-200 bg-slate-50/60 text-slate-500"
          >
            {icono}
          </span>
        ) : !sinIcono ? (
          <Search
            className="h-4 w-4 shrink-0 text-slate-400"
            aria-hidden="true"
          />
        ) : null}
        <span
          id={`${identificador}-valor`}
          className={clases(
            "min-w-0 flex-1 truncate",
            !value && "text-slate-500",
          )}
        >
          {nombreSeleccionado}
        </span>
        {(!usaOpcionesLocales && cargando) || cargandoExterno ? (
          <LoaderCircle className="h-4 w-4 animate-spin text-slate-400" />
        ) : (
          <ChevronDown className="h-4 w-4 text-slate-400" />
        )}
      </button>
      {error && <p className="mt-1 text-xs text-rose-600">{error}</p>}

      {abierto && (
        <div className="absolute z-40 mt-2 w-full overflow-hidden rounded-xl border border-slate-200 bg-white shadow-xl">
          <div className="border-b border-slate-100 p-2.5">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                autoFocus
                aria-label={`Buscar ${etiqueta.replace(" *", "").toLowerCase()}`}
                className="min-h-10 w-full rounded-lg border border-slate-300 pl-9 pr-3 text-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
                value={texto}
                onChange={(evento) => setTexto(evento.target.value)}
                onKeyDown={(evento) => {
                  if (evento.key === "Enter") {
                    evento.preventDefault();
                    buscar();
                  }
                  if (evento.key === "Escape") {
                    evento.stopPropagation();
                    setAbierto(false);
                  }
                }}
                placeholder="Buscar…"
              />
            </div>
          </div>
          <div
            id={`${identificador}-opciones`}
            aria-labelledby={`${identificador}-etiqueta`}
            className="max-h-60 overflow-y-auto p-1.5"
            role="listbox"
          >
            {opcional && (
              <Opcion
                activa={!value}
                texto="Sin asignar"
                seleccionar={() => {
                  onChange(null);
                  setAbierto(false);
                }}
              />
            )}
            {(usaOpcionesLocales || !cargando) && opcionesVisibles.length === 0 && (usaOpcionesLocales || !errorCarga) && (
              <p className="px-3 py-5 text-center text-sm text-slate-500">
                No se encontraron opciones.
              </p>
            )}
            {opcionesVisibles.map((opcion) => (
              <Opcion
                key={opcion.id}
                activa={String(opcion.id) === String(value)}
                texto={`${opcion.codigo ? `${opcion.codigo} · ` : ""}${opcion.nombre}`}
                seleccionar={() => {
                  onChange(String(opcion.id));
                  setAbierto(false);
                }}
              />
            ))}
          </div>
          {!usaOpcionesLocales && errorCarga && (
            <p
              role="alert"
              className="border-t border-slate-100 px-3 py-2 text-xs text-rose-700"
            >
              {errorCarga}{" "}
              <button
                type="button"
                className="font-semibold"
                onClick={() => void cargar(1, false)}
              >
                Reintentar
              </button>
            </p>
          )}
          {!usaOpcionesLocales && pagina < totalPaginas && (
            <button
              type="button"
              className="w-full border-t border-slate-100 px-3 py-2.5 text-xs font-semibold text-teal-700 hover:bg-teal-50"
              onClick={() => void cargar(pagina + 1, true)}
              disabled={cargando}
            >
              Cargar más opciones
            </button>
          )}
        </div>
      )}
    </div>
  );
}

function Opcion({
  activa,
  texto,
  seleccionar,
}: {
  activa: boolean;
  texto: string;
  seleccionar: () => void;
}) {
  return (
    <button
      type="button"
      role="option"
      aria-selected={activa}
      className={clases(
        "flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm hover:bg-slate-50",
        activa && "bg-teal-50 font-semibold text-teal-800",
      )}
      onClick={seleccionar}
    >
      <span className="min-w-0 flex-1 truncate">{texto}</span>
      {activa && <Check className="h-4 w-4 shrink-0" aria-hidden="true" />}
    </button>
  );
}
