import { soloOperacionPos } from '../auth/inicioOperativo';
import { lazy, Suspense, type ReactNode } from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import { EstadoCargando } from "../components/ui/Estados";
import { PERMISOS } from "../auth/permisos";
import { useAutenticacion } from "../auth/useAutenticacion";
import { RutaConPermiso, RutaProtegida } from "../auth/RutaProtegida";
import { PaginaCatalogo } from "../features/catalogos/PaginaCatalogo";
import { PaginaExistencias } from "../features/inventario/PaginaExistencias";
import { PaginaAjustesInventario } from "../features/inventario/PaginaAjustesInventario";
import { PaginaMantenimientoConfiguracion } from "../features/configuracion/PaginaMantenimientoConfiguracion";
import { PaginaConversionesUnidad } from "../features/productos/PaginaConversionesUnidad";
import { PaginaCompras } from "../features/compras/PaginaCompras";
import { PaginaDetalleCompra } from "../features/compras/PaginaDetalleCompra";
import { PaginaDetalleProveedor } from "../features/compras/PaginaDetalleProveedor";
import { PaginaFormularioCompra } from "../features/compras/PaginaFormularioCompra";
import { PaginaProveedores } from "../features/compras/PaginaProveedores";
import { PaginaRecepcionCompra } from "../features/compras/PaginaRecepcionCompra";
import { PaginaRecetas } from "../features/produccion/PaginaRecetas";
import { PaginaDetalleReceta } from "../features/produccion/PaginaDetalleReceta";
import { PaginaFormularioReceta } from "../features/produccion/PaginaFormularioReceta";
import { PaginaProduccion } from "../features/produccion/PaginaProduccion";
import { PaginaDetalleProduccion } from "../features/produccion/PaginaDetalleProduccion";
import { PaginaFormularioProduccion } from "../features/produccion/PaginaFormularioProduccion";
import { PaginaFinalizarProduccion } from "../features/produccion/PaginaFinalizarProduccion";
import { LayoutAutenticado } from "../layouts/LayoutAutenticado";
import { PaginaCambiarContrasena } from "../pages/PaginaCambiarContrasena";
import { PaginaDashboard } from "../pages/PaginaDashboard";
import { PaginaLogin } from "../pages/PaginaLogin";
import { PaginaNoEncontrada } from "../pages/PaginaNoEncontrada";
import { PaginaProductos } from "../pages/PaginaProductos";
import { PaginaSinPermiso } from "../pages/PaginaSinPermiso";
import type { TipoCartera } from "../types/cartera";
import type { TipoReporte } from "../types/reportes";
import { reportes } from "../features/reportes/configuracionReportes";
const PaginaReportes = lazy(() => import("../features/reportes/PaginaReportes").then((m) => ({ default: m.PaginaReportes })));
const PaginaReporteDiario = lazy(() => import("../features/reportes/PaginaReporteDiario").then((m) => ({ default: m.PaginaReporteDiario })));
const PaginaComprobanteVenta = lazy(() => import("../features/ventas/PaginaComprobanteVenta").then((m) => ({ default: m.PaginaComprobanteVenta })));
function VistaReporte({ tipo }: { tipo: TipoReporte }) {
  const { usuario, sucursalActiva } = useAutenticacion();
  return <RutaConPermiso permiso={tipo === "auditoria" ? PERMISOS.auditoriaConsultar : PERMISOS.reportesConsultar}>
    <Suspense fallback={<EstadoCargando />}><PaginaReportes key={`${tipo}:${usuario?.id}:${sucursalActiva?.empresaId}:${sucursalActiva?.id}`} tipo={tipo} /></Suspense>
  </RutaConPermiso>;
}
const PaginaCartera = lazy(() => import("../features/cartera/PaginaCartera").then((m) => ({ default: m.PaginaCartera })));
const PaginaDetalleCartera = lazy(() => import("../features/cartera/PaginaDetalleCartera").then((m) => ({ default: m.PaginaDetalleCartera })));
const PaginaFacturaProveedor = lazy(() => import("../features/cartera/PaginaFacturaProveedor").then((m) => ({ default: m.PaginaFacturaProveedor })));
function VistaCartera({ tipo, children }: { tipo: TipoCartera; children: ReactNode }) {
  const { usuario, sucursalActiva } = useAutenticacion();
  return <RutaConPermiso permiso={tipo === "cobrar" ? PERMISOS.cxcConsultar : PERMISOS.cxpConsultar}><Suspense fallback={<EstadoCargando />}><div key={`${tipo}:${usuario?.id}:${sucursalActiva?.empresaId}:${sucursalActiva?.id}`}>{children}</div></Suspense></RutaConPermiso>;
}
const PaginaVentas = lazy(() => import("../features/ventas/PaginaVentas").then((m) => ({ default: m.PaginaVentas })));
const PaginaCaja = lazy(() => import("../features/caja/PaginaCaja").then((m) => ({ default: m.PaginaCaja })));
const PaginaCajas = lazy(() => import("../features/caja/PaginaCajas").then((m) => ({ default: m.PaginaCajas })));
const PaginaSesionesCaja = lazy(() => import("../features/caja/PaginaSesionesCaja").then((m) => ({ default: m.PaginaSesionesCaja })));
const PaginaSesionCaja = lazy(() => import("../features/caja/PaginaSesionCaja").then((m) => ({ default: m.PaginaSesionCaja })));
const PaginaAbrirCaja = lazy(() => import("../features/caja/PaginaAbrirCaja").then((m) => ({ default: m.PaginaAbrirCaja })));
function VistaCaja({ children }: { children: ReactNode }) {
  const { usuario, sucursalActiva } = useAutenticacion();
  return <RutaConPermiso permiso={PERMISOS.cajaConsultar}><Suspense fallback={<EstadoCargando />}><div key={`${usuario?.id}:${sucursalActiva?.empresaId}:${sucursalActiva?.id}`}>{children}</div></Suspense></RutaConPermiso>;
}
const PaginaDetalleVenta = lazy(() => import("../features/ventas/PaginaDetalleVenta").then((m) => ({ default: m.PaginaDetalleVenta })));
const PaginaPos = lazy(() => import("../features/ventas/PaginaPos").then((m) => ({ default: m.PaginaPos })));
function CargaVentas({ children }: { children: ReactNode }) {
  return <Suspense fallback={<EstadoCargando />}>{children}</Suspense>;
}

export function EnrutadorAplicacion() {
  return (
    <Routes>
      <Route path="/login" element={<PaginaLogin />} />
      <Route element={<RutaProtegida />}>
        <Route path="/ventas/:id/comprobante" element={<RutaConPermiso permiso={PERMISOS.ventasConsultar}><CargaVentas><PaginaComprobanteVenta /></CargaVentas></RutaConPermiso>} />
        <Route
          path="/cambiar-contrasena"
          element={<PaginaCambiarContrasena />}
        />
        <Route element={<LayoutAutenticado />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<InicioSegunPermisos />} />
          <Route path="403" element={<PaginaSinPermiso />} />
          <Route path="reportes" element={<RutaConPermiso permiso={PERMISOS.reportesConsultar}><Navigate to="/reportes/ventas" replace /></RutaConPermiso>} />
          <Route path="reportes/diario" element={<RutaConPermiso permiso={PERMISOS.reportesConsultar}><CargaVentas><PaginaReporteDiario /></CargaVentas></RutaConPermiso>} />
          {reportes.map(({ tipo }) => <Route key={tipo} path={`reportes/${tipo}`} element={<VistaReporte tipo={tipo} />} />)}
          <Route path="auditoria" element={<VistaReporte tipo="auditoria" />} />
          <Route path="cuentas-por-cobrar" element={<VistaCartera tipo="cobrar"><PaginaCartera tipo="cobrar" /></VistaCartera>} />
          <Route path="cuentas-por-cobrar/:id" element={<VistaCartera tipo="cobrar"><PaginaDetalleCartera tipo="cobrar" /></VistaCartera>} />
          <Route path="cuentas-por-pagar" element={<VistaCartera tipo="pagar"><PaginaCartera tipo="pagar" /></VistaCartera>} />
          <Route path="cuentas-por-pagar/registrar-factura" element={<VistaCartera tipo="pagar"><RutaConPermiso permiso={PERMISOS.cxpAdministrar}><RutaConPermiso permiso={PERMISOS.comprasConsultar}><PaginaFacturaProveedor /></RutaConPermiso></RutaConPermiso></VistaCartera>} />
          <Route path="cuentas-por-pagar/:id" element={<VistaCartera tipo="pagar"><PaginaDetalleCartera tipo="pagar" /></VistaCartera>} />
          <Route path="caja" element={<VistaCaja><PaginaCaja /></VistaCaja>} />
          <Route path="caja/cajas" element={<VistaCaja><PaginaCajas /></VistaCaja>} />
          <Route path="caja/sesiones" element={<VistaCaja><PaginaSesionesCaja /></VistaCaja>} />
          <Route path="caja/sesiones/:id" element={<VistaCaja><PaginaSesionCaja /></VistaCaja>} />
          <Route path="caja/abrir" element={<VistaCaja><RutaConPermiso permiso={PERMISOS.cajaAbrir}><PaginaAbrirCaja /></RutaConPermiso></VistaCaja>} />
          <Route path="sin-permiso" element={<Navigate to="/403" replace />} />
          <Route path="ventas" element={<RutaConPermiso permiso={PERMISOS.ventasConsultar}><CargaVentas><PaginaVentas /></CargaVentas></RutaConPermiso>} />
          <Route path="ventas/nueva" element={<RutaConPermiso permiso={PERMISOS.ventasAdministrar}><RutaConPermiso permiso={PERMISOS.posOperar}><CargaVentas><PaginaPos nueva /></CargaVentas></RutaConPermiso></RutaConPermiso>} />
          <Route path="ventas/:id" element={<RutaConPermiso permiso={PERMISOS.ventasConsultar}><CargaVentas><PaginaDetalleVenta /></CargaVentas></RutaConPermiso>} />
          <Route path="ventas/:id/editar" element={<RutaConPermiso permiso={PERMISOS.ventasAdministrar}><RutaConPermiso permiso={PERMISOS.ventasConsultar}><RutaConPermiso permiso={PERMISOS.posOperar}><CargaVentas><PaginaPos /></CargaVentas></RutaConPermiso></RutaConPermiso></RutaConPermiso>} />
          <Route path="pos" element={<RutaConPermiso permiso={PERMISOS.posOperar}><CargaVentas><PaginaPos /></CargaVentas></RutaConPermiso>} />
          <Route path="recetas" element={<RutaConPermiso permiso={PERMISOS.recetasConsultar}><PaginaRecetas /></RutaConPermiso>} />
          <Route path="recetas/nueva" element={<RutaConPermiso permiso={PERMISOS.recetasAdministrar}><PaginaFormularioReceta /></RutaConPermiso>} />
          <Route path="recetas/:id" element={<RutaConPermiso permiso={PERMISOS.recetasConsultar}><PaginaDetalleReceta /></RutaConPermiso>} />
          <Route path="recetas/:id/editar" element={<RutaConPermiso permiso={PERMISOS.recetasAdministrar}><PaginaFormularioReceta /></RutaConPermiso>} />
          <Route path="produccion" element={<RutaConPermiso permiso={PERMISOS.produccionConsultar}><PaginaProduccion /></RutaConPermiso>} />
          <Route path="produccion/nueva" element={<RutaConPermiso permiso={PERMISOS.produccionAdministrar}><PaginaFormularioProduccion /></RutaConPermiso>} />
          <Route path="produccion/:id" element={<RutaConPermiso permiso={PERMISOS.produccionConsultar}><PaginaDetalleProduccion /></RutaConPermiso>} />
          <Route path="produccion/:id/editar" element={<RutaConPermiso permiso={PERMISOS.produccionAdministrar}><PaginaFormularioProduccion /></RutaConPermiso>} />
          <Route path="produccion/:id/finalizar" element={<RutaConPermiso permiso={PERMISOS.produccionProducir}><PaginaFinalizarProduccion /></RutaConPermiso>} />

          <Route
            path="configuracion"
            element={
              <RutaConPermiso permiso={PERMISOS.configuracionAdministrar}>
                <Navigate to="/configuracion/estados" replace />
              </RutaConPermiso>
            }
          />
          <Route
            path="configuracion/estados"
            element={
              <RutaConPermiso permiso={PERMISOS.configuracionAdministrar}>
                <PaginaMantenimientoConfiguracion
                  key="estados"
                  tipo="estados"
                />
              </RutaConPermiso>
            }
          />
          <Route
            path="configuracion/clases-estados"
            element={
              <RutaConPermiso permiso={PERMISOS.configuracionAdministrar}>
                <PaginaMantenimientoConfiguracion
                  key="clases-estados"
                  tipo="clases-estados"
                />
              </RutaConPermiso>
            }
          />
          <Route
            path="configuracion/tipos"
            element={
              <RutaConPermiso permiso={PERMISOS.configuracionAdministrar}>
                <PaginaMantenimientoConfiguracion key="tipos" tipo="tipos" />
              </RutaConPermiso>
            }
          />
          <Route
            path="configuracion/clases-tipos"
            element={
              <RutaConPermiso permiso={PERMISOS.configuracionAdministrar}>
                <PaginaMantenimientoConfiguracion
                  key="clases-tipos"
                  tipo="clases-tipos"
                />
              </RutaConPermiso>
            }
          />
          <Route
            path="configuracion/modulos"
            element={
              <RutaConPermiso permiso={PERMISOS.configuracionAdministrar}>
                <PaginaMantenimientoConfiguracion
                  key="modulos"
                  tipo="modulos"
                />
              </RutaConPermiso>
            }
          />
          <Route
            path="configuracion/acciones"
            element={
              <RutaConPermiso permiso={PERMISOS.configuracionAdministrar}>
                <PaginaMantenimientoConfiguracion
                  key="acciones"
                  tipo="acciones"
                />
              </RutaConPermiso>
            }
          />
          <Route
            path="configuracion/niveles-acceso"
            element={
              <RutaConPermiso permiso={PERMISOS.configuracionAdministrar}>
                <PaginaMantenimientoConfiguracion
                  key="niveles-acceso"
                  tipo="niveles-acceso"
                />
              </RutaConPermiso>
            }
          />
          <Route
            path="configuracion/empresas"
            element={
              <RutaConPermiso permiso={PERMISOS.configuracionAdministrar}>
                <PaginaMantenimientoConfiguracion
                  key="empresas"
                  tipo="empresas"
                />
              </RutaConPermiso>
            }
          />
          <Route
            path="configuracion/sucursales"
            element={
              <RutaConPermiso permiso={PERMISOS.configuracionAdministrar}>
                <PaginaMantenimientoConfiguracion
                  key="sucursales"
                  tipo="sucursales"
                />
              </RutaConPermiso>
            }
          />

          <Route
            path="productos"
            element={
              <RutaConPermiso permiso={PERMISOS.productosConsultar}>
                <PaginaProductos />
              </RutaConPermiso>
            }
          />
          <Route
            path="productos/categorias"
            element={
              <RutaConPermiso permiso={PERMISOS.productosConsultar}>
                <PaginaCatalogo tipo="categorias" />
              </RutaConPermiso>
            }
          />
          <Route
            path="productos/marcas"
            element={
              <RutaConPermiso permiso={PERMISOS.productosConsultar}>
                <PaginaCatalogo tipo="marcas" />
              </RutaConPermiso>
            }
          />
          <Route
            path="productos/tipos"
            element={
              <RutaConPermiso permiso={PERMISOS.productosConsultar}>
                <PaginaCatalogo tipo="tipos" />
              </RutaConPermiso>
            }
          />
          <Route
            path="productos/unidades"
            element={
              <RutaConPermiso permiso={PERMISOS.productosConsultar}>
                <PaginaCatalogo tipo="unidades-medida" />
              </RutaConPermiso>
            }
          />
          <Route
            path="productos/presentaciones"
            element={
              <RutaConPermiso permiso={PERMISOS.productosConsultar}>
                <PaginaCatalogo tipo="presentaciones" />
              </RutaConPermiso>
            }
          />
          <Route
            path="productos/conversiones-unidad"
            element={
              <RutaConPermiso permiso={PERMISOS.productosConsultar}>
                <PaginaConversionesUnidad />
              </RutaConPermiso>
            }
          />

          <Route
            path="inventario"
            element={
              <RutaConPermiso permiso={PERMISOS.inventarioConsultar}>
                <Navigate to="/inventario/existencias" replace />
              </RutaConPermiso>
            }
          />
          <Route
            path="inventario/existencias"
            element={
              <RutaConPermiso permiso={PERMISOS.inventarioConsultar}>
                <PaginaExistencias />
              </RutaConPermiso>
            }
          />
          <Route
            path="inventario/ajustes"
            element={
              <RutaConPermiso permiso={PERMISOS.inventarioAdministrar}>
                <PaginaAjustesInventario />
              </RutaConPermiso>
            }
          />

          <Route
            path="compras"
            element={<RutaConPermiso permiso={PERMISOS.comprasConsultar}><PaginaCompras /></RutaConPermiso>}
          />
          <Route
            path="compras/nueva"
            element={<RutaConPermiso permiso={PERMISOS.comprasAdministrar}><PaginaFormularioCompra /></RutaConPermiso>}
          />
          <Route
            path="compras/:id"
            element={<RutaConPermiso permiso={PERMISOS.comprasConsultar}><PaginaDetalleCompra /></RutaConPermiso>}
          />
          <Route
            path="compras/:id/editar"
            element={<RutaConPermiso permiso={PERMISOS.comprasAdministrar}><PaginaFormularioCompra /></RutaConPermiso>}
          />
          <Route
            path="compras/:id/recepciones"
            element={<RutaConPermiso permiso={PERMISOS.comprasConsultar}><PaginaDetalleCompra enfocarRecepciones /></RutaConPermiso>}
          />
          <Route
            path="compras/:id/recibir"
            element={<RutaConPermiso permiso={PERMISOS.comprasRecibir}><PaginaRecepcionCompra /></RutaConPermiso>}
          />
          <Route
            path="proveedores"
            element={<RutaConPermiso permiso={PERMISOS.proveedoresConsultar}><PaginaProveedores /></RutaConPermiso>}
          />
          <Route
            path="proveedores/:id"
            element={<RutaConPermiso permiso={PERMISOS.proveedoresConsultar}><PaginaDetalleProveedor /></RutaConPermiso>}
          />
        </Route>
      </Route>
      <Route path="*" element={<PaginaNoEncontrada />} />
    </Routes>
  );
}
function InicioSegunPermisos() {
  const { sesion } = useAutenticacion();
  return soloOperacionPos(sesion?.permisos ?? []) ? <Navigate to="/pos" replace /> : <PaginaDashboard />;
}

