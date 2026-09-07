import { ArrowRight, Boxes, Building2, Package, ShieldCheck, Warehouse } from 'lucide-react'
import { Link } from 'react-router-dom'
import { PERMISOS } from '../auth/permisos'
import { useAutenticacion } from '../auth/useAutenticacion'
import { Insignia, Tarjeta } from '../components/ui/Controles'
import { EncabezadoPagina } from '../components/ui/EncabezadoPagina'

export function PaginaDashboard() {
  const { usuario, sucursalActiva, tienePermiso } = useAutenticacion()
  const modulos = [
    {
      permiso: PERMISOS.productosConsultar,
      titulo: 'Catálogo de productos',
      descripcion: 'Organice categorías, marcas, tipos, unidades y presentaciones.',
      ruta: '/productos',
      icono: Package,
    },
    {
      permiso: PERMISOS.inventarioConsultar,
      titulo: 'Existencias',
      descripcion: 'Consulte disponibilidad, reservas y mínimos por sucursal.',
      ruta: '/inventario/existencias',
      icono: Boxes,
    },
    {
      permiso: PERMISOS.configuracionAdministrar,
      titulo: 'Configuración',
      descripcion: 'Acceda a la estructura organizativa y catálogos del sistema.',
      ruta: '/configuracion',
      icono: ShieldCheck,
    },
  ].filter((modulo) => tienePermiso(modulo.permiso))

  return (
    <div className="space-y-7">
      <EncabezadoPagina titulo={`Hola, ${usuario?.nombreUsuario ?? 'usuario'}`} descripcion="Este es el punto de entrada a la operación actual de Heladería." />
      <div className="grid gap-4 md:grid-cols-3">
        <Tarjeta className="p-5">
          <div className="flex items-start justify-between"><span className="rounded-lg bg-teal-50 p-2.5 text-teal-700"><Warehouse className="h-5 w-5" /></span><Insignia tono="positivo">Activa</Insignia></div>
          <p className="mt-5 text-xs font-semibold uppercase tracking-wider text-slate-400">Sucursal actual</p>
          <p className="mt-1 truncate text-lg font-bold text-slate-900">{sucursalActiva?.nombre}</p>
        </Tarjeta>
        <Tarjeta className="p-5">
          <span className="inline-flex rounded-lg bg-sky-50 p-2.5 text-sky-700"><Building2 className="h-5 w-5" /></span>
          <p className="mt-5 text-xs font-semibold uppercase tracking-wider text-slate-400">Empresa</p>
          <p className="mt-1 truncate text-lg font-bold text-slate-900">{sucursalActiva?.empresa}</p>
        </Tarjeta>
        <Tarjeta className="p-5">
          <span className="inline-flex rounded-lg bg-violet-50 p-2.5 text-violet-700"><ShieldCheck className="h-5 w-5" /></span>
          <p className="mt-5 text-xs font-semibold uppercase tracking-wider text-slate-400">Perfil de acceso</p>
          <p className="mt-1 truncate text-lg font-bold text-slate-900">{usuario?.rol}</p>
        </Tarjeta>
      </div>
      <section>
        <div className="mb-4"><h2 className="text-lg font-bold text-slate-900">Módulos disponibles</h2><p className="mt-1 text-sm text-slate-500">Accesos habilitados según sus permisos efectivos.</p></div>
        {modulos.length > 0 ? (
          <div className="grid gap-4 lg:grid-cols-3">
            {modulos.map((modulo) => {
              const Icono = modulo.icono
              return (
                <Link key={modulo.ruta} to={modulo.ruta} className="group rounded-xl border border-slate-200 bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:border-teal-300 hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600">
                  <span className="grid h-11 w-11 place-items-center rounded-lg bg-slate-100 text-slate-700 group-hover:bg-teal-50 group-hover:text-teal-700"><Icono className="h-5 w-5" /></span>
                  <h3 className="mt-5 font-bold text-slate-900">{modulo.titulo}</h3>
                  <p className="mt-2 min-h-10 text-sm leading-5 text-slate-500">{modulo.descripcion}</p>
                  <span className="mt-5 inline-flex items-center gap-2 text-sm font-semibold text-teal-700">Abrir módulo <ArrowRight className="h-4 w-4 transition-transform group-hover:translate-x-1" /></span>
                </Link>
              )
            })}
          </div>
        ) : <Tarjeta className="p-6 text-sm text-slate-500">No hay módulos operativos asignados a este usuario.</Tarjeta>}
      </section>
    </div>
  )
}
