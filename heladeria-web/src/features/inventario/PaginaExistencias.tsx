import { useCallback, useEffect, useState, type FormEvent } from "react";
import { AlertTriangle, Search } from "lucide-react";
import { solicitar } from "../../api/clienteHttp";
import { useAutenticacion } from "../../auth/useAutenticacion";
import {
  Boton,
  Insignia,
  Seleccion,
  Tarjeta,
} from "../../components/ui/Controles";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import {
  EstadoCargando,
  EstadoError,
  EstadoVacio,
} from "../../components/ui/Estados";
import { Paginacion } from "../../components/ui/Paginacion";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import type { Paginado } from "../../types/api";
import type { Existencia } from "../../types/inventario";
import { formatoCosto, formatoNumero, mostrarFecha } from "../../utils/formato";

export function PaginaExistencias() {
  const { sucursalActiva, sucursales } = useAutenticacion();
  const [datos, setDatos] = useState<Paginado<Existencia> | null>(null);
  const [pagina, setPagina] = useState(1);
  const [sucursalSeleccionadaId, setSucursalSeleccionadaId] = useState<
    number | null
  >(null);
  const [textoBusqueda, setTextoBusqueda] = useState("");
  const [busqueda, setBusqueda] = useState("");
  const [soloPositiva, setSoloPositiva] = useState(false);
  const [soloBajoMinimo, setSoloBajoMinimo] = useState(false);
  const [productoId, setProductoId] = useState("");
  const [categoriaId, setCategoriaId] = useState("");
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const sucursalId = sucursalSeleccionadaId ?? sucursalActiva?.id ?? 0;

  const cargar = useCallback(async () => {
    if (!sucursalId) {
      setDatos(null);
      setError(null);
      setCargando(false);
      return;
    }
    setCargando(true);
    setError(null);
    const parametros = new URLSearchParams({
      pagina: String(pagina),
      tamanioPagina: "10",
      sucursalId: String(sucursalId),
    });
    if (busqueda) parametros.set("busqueda", busqueda);
    if (soloPositiva) parametros.set("soloExistenciaPositiva", "true");
    if (soloBajoMinimo) parametros.set("soloBajoMinimo", "true");
    if (productoId) parametros.set("productoId", productoId);
    if (categoriaId) parametros.set("categoriaId", categoriaId);
    try {
      setDatos(
        await solicitar<Paginado<Existencia>>(
          `/api/inventario/existencias?${parametros}`,
        ),
      );
    } catch (excepcion) {
      setError(
        excepcion instanceof Error
          ? excepcion.message
          : "No fue posible consultar las existencias.",
      );
    } finally {
      setCargando(false);
    }
  }, [
    busqueda,
    categoriaId,
    pagina,
    productoId,
    soloBajoMinimo,
    soloPositiva,
    sucursalId,
  ]);

  useEffect(() => {
    // La carga remota sincroniza la consulta con los filtros seleccionados.
    // oxlint-disable-next-line react/set-state-in-effect
    void cargar();
  }, [cargar]);

  const buscar = (evento: FormEvent) => {
    evento.preventDefault();
    setPagina(1);
    setBusqueda(textoBusqueda.trim());
  };

  return (
    <div className="space-y-6">
      <EncabezadoPagina
        titulo="Existencias"
        descripcion="Disponibilidad real por sucursal, bodega y ubicación. Las cantidades solo cambian mediante movimientos auditables."
      />
      <Tarjeta className="overflow-hidden">
        <form
          className="grid gap-3 border-b border-slate-200 p-4 lg:grid-cols-3 lg:items-end"
          onSubmit={buscar}
        >
          <label className="text-sm font-medium text-slate-700">
            Buscar producto
            <div className="relative mt-1.5">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <input
                className="min-h-11 w-full rounded-lg border border-slate-300 bg-white py-2 pl-10 pr-3 text-sm outline-none focus:border-teal-600 focus:ring-2 focus:ring-teal-100"
                value={textoBusqueda}
                onChange={(evento) => setTextoBusqueda(evento.target.value)}
                placeholder="SKU o nombre"
              />
            </div>
          </label>
          <Seleccion
            etiqueta="Sucursal"
            value={sucursalId || ""}
            onChange={(evento) => {
              setSucursalSeleccionadaId(Number(evento.target.value));
              setPagina(1);
            }}
          >
            {sucursales.map((sucursal) => (
              <option key={sucursal.id} value={sucursal.id}>
                {sucursal.nombre}
              </option>
            ))}
          </Seleccion>
          <SelectorEntidad
            etiqueta="Producto"
            ruta="/api/productos"
            opcional
            value={productoId}
            onChange={(v) => {
              setProductoId(v ?? "");
              setPagina(1);
            }}
          />
          <SelectorEntidad
            etiqueta="Categoría"
            ruta="/api/productos/categorias"
            opcional
            value={categoriaId}
            onChange={(v) => {
              setCategoriaId(v ?? "");
              setPagina(1);
            }}
          />
          <Boton type="submit" variante="secundario">
            Aplicar filtros
          </Boton>
          <div className="flex flex-wrap gap-4 lg:col-span-3">
            <label className="flex items-center gap-2 text-sm text-slate-600">
              <input
                type="checkbox"
                className="h-4 w-4 accent-teal-700"
                checked={soloPositiva}
                onChange={(evento) => {
                  setSoloPositiva(evento.target.checked);
                  setPagina(1);
                }}
              />{" "}
              Solo disponibilidad positiva
            </label>
            <label className="flex items-center gap-2 text-sm text-slate-600">
              <input
                type="checkbox"
                className="h-4 w-4 accent-teal-700"
                checked={soloBajoMinimo}
                onChange={(evento) => {
                  setSoloBajoMinimo(evento.target.checked);
                  setPagina(1);
                }}
              />{" "}
              Solo bajo mínimo
            </label>
          </div>
        </form>
        {cargando ? (
          <EstadoCargando mensaje="Consultando inventario…" />
        ) : error ? (
          <EstadoError mensaje={error} reintentar={cargar} />
        ) : !datos?.elementos.length ? (
          <EstadoVacio
            titulo="Sin existencias para mostrar"
            descripcion="No se encontraron registros con los filtros seleccionados."
          />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="min-w-[88rem] divide-y divide-slate-200 text-left text-sm">
                <thead className="bg-slate-50 text-xs font-semibold uppercase tracking-wide text-slate-500">
                  <tr>
                    <th className="px-5 py-3">Producto</th>
                    <th className="px-5 py-3">Categoría</th>
                    <th className="px-5 py-3">Almacén</th>
                    <th className="px-5 py-3 text-right">Cantidad</th>
                    <th className="px-5 py-3 text-right">Reservado</th>
                    <th className="px-5 py-3 text-right">Disponible</th>
                    <th className="px-5 py-3">Unidad</th>
                    <th className="px-5 py-3 text-right">Costo promedio</th>
                    <th className="px-5 py-3 text-right">Stock mínimo</th>
                    <th className="px-5 py-3">Estado</th>
                    <th className="px-5 py-3">Último movimiento</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 bg-white">
                  {datos.elementos.map((existencia) => (
                    <tr key={existencia.id} className="hover:bg-slate-50/70">
                      <td className="px-5 py-4">
                        <p className="font-semibold text-slate-900">
                          {existencia.producto}
                        </p>
                        <p className="mt-1 font-mono text-xs text-slate-400">
                          {existencia.sku}
                        </p>
                      </td>
                      <td className="px-5 py-4 text-slate-600">
                        {existencia.categoria}
                      </td>
                      <td className="px-5 py-4">
                        <p className="font-medium text-slate-700">
                          {existencia.bodega}
                        </p>
                        <p className="mt-1 text-xs text-slate-400">
                          {existencia.sucursal} · {existencia.ubicacion}
                        </p>
                      </td>
                      <td className="px-5 py-4 text-right tabular-nums text-slate-700">
                        {formatoNumero.format(existencia.cantidad)}
                      </td>
                      <td className="px-5 py-4 text-right tabular-nums text-slate-500">
                        {formatoNumero.format(existencia.cantidadReservada)}
                      </td>
                      <td className="px-5 py-4 text-right font-bold tabular-nums text-slate-900">
                        {formatoNumero.format(existencia.cantidadDisponible)}
                      </td>
                      <td className="px-5 py-4 text-slate-600">
                        {existencia.unidadBase}
                      </td>
                      <td className="px-5 py-4 text-right tabular-nums text-slate-600">
                        {formatoCosto.format(existencia.costoPromedio)}
                      </td>
                      <td className="px-5 py-4 text-right tabular-nums text-slate-600">
                        {existencia.stockMinimo == null
                          ? "—"
                          : formatoNumero.format(existencia.stockMinimo)}
                      </td>
                      <td className="px-5 py-4">
                        {existencia.bajoMinimo ? (
                          <Insignia tono="advertencia">
                            <AlertTriangle className="mr-1 h-3.5 w-3.5" /> Bajo
                            mínimo
                          </Insignia>
                        ) : (
                          <Insignia tono="positivo">Disponible</Insignia>
                        )}
                      </td>
                      <td className="px-5 py-4 text-xs text-slate-500">
                        {mostrarFecha(existencia.ultimoMovimiento)}
                      </td>
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
    </div>
  );
}
