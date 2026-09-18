import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  type FormEvent,
} from "react";
import { ArrowDownToLine, ArrowUpFromLine, CheckCircle2 } from "lucide-react";
import { solicitar } from "../../api/clienteHttp";
import { PERMISOS } from "../../auth/permisos";
import { useAutenticacion } from "../../auth/useAutenticacion";
import { useToast } from "../../components/toast/useToast";
import {
  AreaTexto,
  Boton,
  Entrada,
  Seleccion,
  Tarjeta,
} from "../../components/ui/Controles";
import { DialogoConfirmacion } from "../../components/ui/DialogoConfirmacion";
import { EncabezadoPagina } from "../../components/ui/EncabezadoPagina";
import {
  EstadoCargando,
  EstadoError,
  EstadoVacio,
} from "../../components/ui/Estados";
import { SeccionFormulario } from "../../components/ui/SeccionFormulario";
import { SelectorEntidad } from "../../components/ui/SelectorEntidad";
import type {
  AlmacenInventario,
  ResultadoAjusteInventario,
} from "../../types/inventario";
import type { Producto, ProductoPresentacion } from "../../types/productos";
import { formatoCosto, formatoNumero } from "../../utils/formato";
import { calcularCantidadBase } from "./multimedida";

const inicial = {
  sucursalId: "",
  bodegaId: "",
  ubicacionId: "",
  productoId: "",
  productoPresentacionId: "",
  tipo: "ENTRADA",
  cantidad: "",
  costoUnitario: "",
  referencia: "",
  observaciones: "",
};
type FormularioAjuste = typeof inicial;

export function PaginaAjustesInventario() {
  const [almacenes, setAlmacenes] = useState<AlmacenInventario[]>([]);
  const [form, setForm] = useState(inicial);
  const [producto, setProducto] = useState<Producto | null>(null);
  const [presentaciones, setPresentaciones] = useState<ProductoPresentacion[]>(
    [],
  );
  const [cargando, setCargando] = useState(true);
  const [cargandoProducto, setCargandoProducto] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmado, setConfirmado] = useState<FormularioAjuste | null>(null);
  const [guardando, setGuardando] = useState(false);
  const [resultado, setResultado] = useState<ResultadoAjusteInventario | null>(
    null,
  );
  const { tienePermiso } = useAutenticacion();
  const { mostrar } = useToast();
  const puedeLeerReferencias =
    tienePermiso(PERMISOS.productosConsultar) &&
    tienePermiso(PERMISOS.inventarioConsultar);
  const cargar = useCallback(async () => {
    setCargando(true);
    setError(null);
    try {
      setAlmacenes(
        await solicitar<AlmacenInventario[]>("/api/inventario/almacenes"),
      );
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "No fue posible cargar almacenes.",
      );
    } finally {
      setCargando(false);
    }
  }, []);
  useEffect(() => {
    // Los almacenes remotos solo contienen relaciones accesibles al usuario autenticado.
    // oxlint-disable-next-line react/set-state-in-effect
    if (puedeLeerReferencias) void cargar();
  }, [cargar, puedeLeerReferencias]);
  useEffect(() => {
    if (!form.productoId) return;
    let vigente = true;
    const cargarProducto = async () => {
      setCargandoProducto(true);
      setProducto(null);
      setError(null);
      try {
        const [p, pp] = await Promise.all([
          solicitar<Producto>(`/api/productos/${form.productoId}`),
          solicitar<ProductoPresentacion[]>(
            `/api/productos/${form.productoId}/presentaciones`,
          ),
        ]);
        if (vigente) {
          setProducto(p);
          setPresentaciones(
            pp.filter((v) => v.estado.toLowerCase() === "activo"),
          );
        }
      } catch (e) {
        if (vigente)
          setError(
            e instanceof Error
              ? e.message
              : "No fue posible cargar el producto.",
          );
      } finally {
        if (vigente) setCargandoProducto(false);
      }
    };
    void cargarProducto();
    return () => {
      vigente = false;
    };
  }, [form.productoId]);

  const sucursales = useMemo(
    () => [...new Map(almacenes.map((a) => [a.sucursalId, a])).values()],
    [almacenes],
  );
  const bodegas = [
    ...new Map(
      almacenes
        .filter((a) => String(a.sucursalId) === form.sucursalId)
        .map((a) => [a.bodegaId, a]),
    ).values(),
  ];
  const ubicaciones = almacenes.filter(
    (a) =>
      String(a.sucursalId) === form.sucursalId &&
      String(a.bodegaId) === form.bodegaId,
  );
  const presentacion = presentaciones.find(
    (p) => String(p.id) === form.productoPresentacionId,
  );
  const factor = presentacion?.factorConversion ?? 1;
  const estimada = calcularCantidadBase(Number(form.cantidad), factor);
  const almacen = almacenes.find(
    (a) => String(a.ubicacionId) === form.ubicacionId,
  );
  const validar = (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setResultado(null);
    if (
      !form.sucursalId ||
      !form.bodegaId ||
      !form.ubicacionId ||
      !producto ||
      !estimada ||
      !producto.manejaExistencia ||
      producto.estado.toLowerCase() !== "activo"
    ) {
      setError(
        "Seleccione almacén, un producto activo que maneje existencia y una cantidad positiva.",
      );
      return;
    }
    if (
      form.costoUnitario !== "" &&
      (!Number.isFinite(Number(form.costoUnitario)) ||
        Number(form.costoUnitario) < 0)
    ) {
      setError("El costo no puede ser negativo.");
      return;
    }
    setConfirmado({ ...form });
  };
  const ejecutar = async () => {
    if (!confirmado || guardando) return;
    setGuardando(true);
    try {
      const r = await solicitar<ResultadoAjusteInventario>(
        "/api/inventario/ajustes",
        {
          method: "POST",
          body: JSON.stringify({
            sucursalId: Number(confirmado.sucursalId),
            bodegaId: Number(confirmado.bodegaId),
            ubicacionId: Number(confirmado.ubicacionId),
            productoId: Number(confirmado.productoId),
            productoPresentacionId: confirmado.productoPresentacionId
              ? Number(confirmado.productoPresentacionId)
              : null,
            tipo: confirmado.tipo,
            cantidad: Number(confirmado.cantidad),
            costoUnitario:
              confirmado.tipo === "SALIDA" || confirmado.costoUnitario === ""
                ? null
                : Number(confirmado.costoUnitario),
            referencia: confirmado.referencia.trim() || null,
            observaciones: confirmado.observaciones.trim() || null,
          }),
        },
      );
      setResultado(r);
      setConfirmado(null);
      setForm((f) => ({
        ...f,
        cantidad: "",
        costoUnitario: "",
        referencia: "",
        observaciones: "",
      }));
      mostrar({
        tipo: "exito",
        titulo: "Ajuste aplicado",
        descripcion: `Movimiento ${r.movimientoId} registrado.`,
      });
    } catch (e) {
      setConfirmado(null);
      setError(
        e instanceof Error
          ? e.message
          : "No se pudo confirmar el resultado. Verifique los movimientos antes de reintentar.",
      );
    } finally {
      setGuardando(false);
    }
  };
  if (!puedeLeerReferencias)
    return (
      <EstadoError mensaje="Para preparar ajustes necesita también PRODUCTOS.CONSULTAR e INVENTARIO.CONSULTAR. No se aplica ningún bypass por rol." />
    );
  return (
    <div className="space-y-6">
      <EncabezadoPagina
        titulo="Ajustes de inventario"
        descripcion="Registre entradas o salidas auditables. Las existencias nunca se editan directamente."
      />
      {cargando ? (
        <EstadoCargando />
      ) : error && !almacenes.length ? (
        <EstadoError mensaje={error} reintentar={cargar} />
      ) : !almacenes.length ? (
        <Tarjeta>
          <EstadoVacio
            titulo="No hay almacenes disponibles"
            descripcion="Se necesita una bodega y ubicación activas en una sucursal asignada. Esta pantalla no crea almacenes ni datos iniciales."
          />
        </Tarjeta>
      ) : (
        <form
          onSubmit={validar}
          className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_20rem]"
        >
          <div className="space-y-5">
            <Tarjeta className="space-y-5 p-5">
              <SeccionFormulario titulo="1. Almacén">
                <Seleccion
                  etiqueta="Sucursal *"
                  value={form.sucursalId}
                  onChange={(e) =>
                    setForm((f) => ({
                      ...f,
                      sucursalId: e.target.value,
                      bodegaId: "",
                      ubicacionId: "",
                    }))
                  }
                >
                  <option value="">Seleccione</option>
                  {sucursales.map((a) => (
                    <option key={a.sucursalId} value={a.sucursalId}>
                      {a.sucursal}
                    </option>
                  ))}
                </Seleccion>
                <Seleccion
                  etiqueta="Bodega *"
                  disabled={!form.sucursalId}
                  value={form.bodegaId}
                  onChange={(e) =>
                    setForm((f) => ({
                      ...f,
                      bodegaId: e.target.value,
                      ubicacionId: "",
                    }))
                  }
                >
                  <option value="">Seleccione</option>
                  {bodegas.map((a) => (
                    <option key={a.bodegaId} value={a.bodegaId}>
                      {a.bodega}
                      {a.esBodegaPrincipal ? " · Principal" : ""}
                    </option>
                  ))}
                </Seleccion>
                <Seleccion
                  etiqueta="Ubicación *"
                  disabled={!form.bodegaId}
                  value={form.ubicacionId}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, ubicacionId: e.target.value }))
                  }
                >
                  <option value="">Seleccione</option>
                  {ubicaciones.map((a) => (
                    <option key={a.ubicacionId} value={a.ubicacionId}>
                      {a.ubicacion}
                    </option>
                  ))}
                </Seleccion>
              </SeccionFormulario>
              <SeccionFormulario titulo="2. Producto y multimedida">
                <SelectorEntidad
                  etiqueta="Producto *"
                  ruta="/api/productos"
                  value={form.productoId}
                  etiquetaActual={producto?.nombre}
                  onChange={(v) => {
                    setProducto(null);
                    setPresentaciones([]);
                    setForm((f) => ({
                      ...f,
                      productoId: v ?? "",
                      productoPresentacionId: "",
                    }));
                  }}
                />
                <Seleccion
                  etiqueta="Presentación"
                  disabled={!producto || cargandoProducto}
                  value={form.productoPresentacionId}
                  onChange={(e) =>
                    setForm((f) => ({
                      ...f,
                      productoPresentacionId: e.target.value,
                    }))
                  }
                >
                  <option value="">
                    Unidad base {producto?.abreviaturaUnidadBase ?? ""}
                  </option>
                  {presentaciones.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.presentacion} · {p.factorConversion}{" "}
                      {producto?.abreviaturaUnidadBase}
                    </option>
                  ))}
                </Seleccion>
                <Entrada
                  etiqueta="Cantidad *"
                  type="number"
                  min="0.000001"
                  step="0.000001"
                  value={form.cantidad}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, cantidad: e.target.value }))
                  }
                />
                <Seleccion
                  etiqueta="Tipo *"
                  value={form.tipo}
                  onChange={(e) =>
                    setForm((f) => ({
                      ...f,
                      tipo: e.target.value,
                      costoUnitario:
                        e.target.value === "SALIDA" ? "" : f.costoUnitario,
                    }))
                  }
                >
                  <option value="ENTRADA">ENTRADA</option>
                  <option value="SALIDA">SALIDA</option>
                </Seleccion>
              </SeccionFormulario>
              <SeccionFormulario
                titulo="3. Costo y trazabilidad"
                descripcion="El backend decide si el concepto requiere costo o referencia. El costo se expresa por unidad base."
              >
                <Entrada
                  etiqueta="Costo unitario base"
                  type="number"
                  min="0"
                  step="0.000001"
                  disabled={form.tipo === "SALIDA"}
                  value={form.costoUnitario}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, costoUnitario: e.target.value }))
                  }
                />
                {form.tipo === "SALIDA" && (
                  <p className="text-sm text-slate-500 sm:col-span-2">
                    En las salidas, el backend utiliza el costo promedio
                    vigente.
                  </p>
                )}
                <Entrada
                  etiqueta="Referencia"
                  maxLength={100}
                  value={form.referencia}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, referencia: e.target.value }))
                  }
                />
                <AreaTexto
                  etiqueta="Observaciones"
                  maxLength={500}
                  value={form.observaciones}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, observaciones: e.target.value }))
                  }
                />
              </SeccionFormulario>
              {error && (
                <p
                  role="alert"
                  className="rounded-lg bg-rose-50 p-3 text-sm text-rose-700"
                >
                  {error}
                </p>
              )}
              <div className="flex justify-end">
                <Boton disabled={cargandoProducto || guardando}>
                  Revisar ajuste
                </Boton>
              </div>
            </Tarjeta>
          </div>
          <Tarjeta className="h-fit space-y-5 p-5">
            <span className="inline-flex rounded-lg bg-teal-50 p-3 text-teal-700">
              {form.tipo === "ENTRADA" ? (
                <ArrowDownToLine />
              ) : (
                <ArrowUpFromLine />
              )}
            </span>
            <h3 className="font-bold">Vista previa informativa</h3>
            <p className="text-sm text-slate-500">
              {producto?.nombre ?? "Seleccione un producto"}
            </p>
            <p className="rounded-lg bg-slate-50 p-3 text-sm">
              1 {presentacion?.presentacion ?? "unidad base"} ={" "}
              {formatoNumero.format(factor)}{" "}
              {producto?.abreviaturaUnidadBase ?? "unidad base"}
            </p>
            <div>
              <p className="text-xs uppercase text-slate-400">
                Cantidad base estimada
              </p>
              <p className="mt-1 text-2xl font-bold text-teal-800">
                {estimada == null ? "—" : formatoNumero.format(estimada)}{" "}
                <span className="text-sm">
                  {producto?.abreviaturaUnidadBase}
                </span>
              </p>
            </div>
            <p className="text-xs leading-5 text-slate-500">
              El backend valida factor, precisión, saldo y costo; esta vista no
              reemplaza sus cálculos.
            </p>
          </Tarjeta>
        </form>
      )}
      {resultado && (
        <Tarjeta className="border-emerald-200 p-5">
          <h3 className="flex items-center gap-2 font-bold text-emerald-800">
            <CheckCircle2 className="h-5 w-5" /> Ajuste aplicado
          </h3>
          <dl className="mt-4 grid gap-4 sm:grid-cols-3">
            {[
              ["Movimiento", resultado.movimientoId],
              [
                "Cantidad base",
                `${formatoNumero.format(resultado.cantidadBase)} ${resultado.unidadBase}`,
              ],
              ["Saldo anterior", formatoNumero.format(resultado.saldoAnterior)],
              ["Saldo nuevo", formatoNumero.format(resultado.saldoNuevo)],
              ["Costo unitario", formatoCosto.format(resultado.costoUnitario)],
            ].map(([k, v]) => (
              <div key={k}>
                <dt className="text-xs text-slate-500">{k}</dt>
                <dd className="mt-1 font-semibold">{v}</dd>
              </div>
            ))}
          </dl>
        </Tarjeta>
      )}
      <DialogoConfirmacion
        abierto={Boolean(confirmado)}
        titulo="Confirmar ajuste de inventario"
        descripcion="La operación registrará un movimiento real al confirmar."
        cerrar={() => setConfirmado(null)}
        confirmar={ejecutar}
        cargando={guardando}
        peligro={confirmado?.tipo === "SALIDA"}
      >
        <dl className="space-y-2 text-sm">
          {[
            ["Tipo", confirmado?.tipo],
            ["Producto", producto?.nombre],
            ["Presentación", presentacion?.presentacion ?? "Unidad base"],
            ["Cantidad", confirmado?.cantidad],
            [
              "Cantidad base estimada",
              estimada == null
                ? "—"
                : `${formatoNumero.format(estimada)} ${producto?.abreviaturaUnidadBase}`,
            ],
            ["Sucursal", almacen?.sucursal],
            ["Bodega", almacen?.bodega],
            ["Ubicación", almacen?.ubicacion],
          ].map(([k, v]) => (
            <div key={k} className="flex justify-between gap-3">
              <dt className="text-slate-500">{k}</dt>
              <dd className="text-right font-semibold">{v}</dd>
            </div>
          ))}
        </dl>
      </DialogoConfirmacion>
    </div>
  );
}
