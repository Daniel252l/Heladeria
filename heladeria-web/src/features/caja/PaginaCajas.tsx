import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Wallet, Plus, Search } from 'lucide-react'
import { cajaApi } from '../../api/caja'
import { PERMISOS } from '../../auth/permisos'
import { useAutenticacion } from '../../auth/useAutenticacion'
import { Boton, Entrada } from '../../components/ui/Controles'
import { EncabezadoPagina } from '../../components/ui/EncabezadoPagina'
import { EstadoCargando, EstadoVacio } from '../../components/ui/Estados'
import { Paginacion } from '../../components/ui/Paginacion'
import type { Paginado } from '../../types/api'
import type { Caja } from '../../types/caja'
import { useOperacionCaja, useRecursoCaja } from './useDatosCaja'
import { ErrorCaja, EstadoCaja, ModalCaja, NavegacionCaja, SeccionCaja, TablaCaja } from './ComponentesCaja'
import { enlaceCaja } from './utilidadesCaja'

export function PaginaCajas() {
  const { usuario, sucursalActiva } = useAutenticacion()
  return <ListadoCajas key={`${usuario?.id}:${sucursalActiva?.id}`} />
}
function ListadoCajas() {
  const { tienePermiso, sucursalActiva } = useAutenticacion()
  const [busqueda, setBusqueda] = useState(''); const [texto, setTexto] = useState(''); const [pagina, setPagina] = useState(1)
  const [crear, setCrear] = useState(false)
  const datos = useRecursoCaja<Paginado<Caja>>(`/api/caja/cajas?${new URLSearchParams({ busqueda, pagina: String(pagina), tamanioPagina: '10' })}`)
  return <div className="caja-vista space-y-6"><EncabezadoPagina titulo="Cajas" descripcion="Puntos de cobro de la sucursal activa." icono={<Wallet />} acciones={tienePermiso(PERMISOS.cajaOperar) && <Boton onClick={() => setCrear(true)}><Plus /> Registrar caja</Boton>} />
    <NavegacionCaja /><form className="flex flex-wrap items-end gap-3" onSubmit={(e) => { e.preventDefault(); setBusqueda(texto.trim()); setPagina(1) }}><Entrada etiqueta="Buscar caja" value={texto} onChange={(e) => setTexto(e.target.value)} maxLength={150} /><Boton type="submit"><Search /> Buscar</Boton></form>
    {datos.cargando ? <EstadoCargando /> : datos.error ? <ErrorCaja error={datos.error} recargar={datos.recargar} /> : datos.datos && <SeccionCaja titulo="Cajas de la sucursal">
      {!datos.datos.elementos.length ? <EstadoVacio titulo="No hay cajas registradas" descripcion="Registre una caja con permiso de operación para comenzar." /> : <TablaCaja etiqueta="Listado de cajas" columnas={['Código', 'Nombre', 'Estado', 'Sucursal', 'Sesión abierta', 'Acciones']}>
        {datos.datos.elementos.map((c) => <tr key={c.id}><td>{c.codigo}</td><td>{c.nombre}</td><td><EstadoCaja estado={c.estado} /></td><td>{sucursalActiva?.nombre}</td><td>{c.sesionAbiertaId ? `Sesión #${c.sesionAbiertaId}` : 'Sin sesión abierta'}</td><td>{c.sesionAbiertaId ? <Link className={enlaceCaja} to={`/caja/sesiones/${c.sesionAbiertaId}`}>Ver sesión</Link> : tienePermiso(PERMISOS.cajaAbrir) ? <Link className={enlaceCaja} to={`/caja/abrir?cajaId=${c.id}`}>Abrir caja</Link> : '—'}</td></tr>)}
      </TablaCaja>}<Paginacion {...datos.datos} cambiar={setPagina} entidad="caja" /></SeccionCaja>}
    {crear && tienePermiso(PERMISOS.cajaOperar) && <CrearCaja cerrar={() => setCrear(false)} creada={() => { setCrear(false); datos.recargar() }} />}
  </div>
}
function CrearCaja({ cerrar, creada }: { cerrar: () => void; creada: () => void }) {
  const [codigo, setCodigo] = useState(''); const [nombre, setNombre] = useState(''); const op = useOperacionCaja()
  async function guardar(e: FormEvent) { e.preventDefault(); if (!codigo.trim() || !nombre.trim()) return; if (await op.ejecutar(() => cajaApi.crear(codigo, nombre))) creada() }
  return <ModalCaja titulo="Registrar caja" cerrar={() => { if (!op.procesando) cerrar() }}><form className="space-y-5" onSubmit={(e) => void guardar(e)}>
    <fieldset disabled={op.procesando} className="grid gap-4 sm:grid-cols-2"><Entrada etiqueta="Código" required maxLength={20} pattern="[A-Za-z0-9_-]+" value={codigo} onChange={(e) => setCodigo(e.target.value)} /><Entrada etiqueta="Nombre" required maxLength={100} value={nombre} onChange={(e) => setNombre(e.target.value)} /></fieldset>
    {op.error && <ErrorCaja error={op.error} />}<div className="flex flex-wrap justify-end gap-3"><Boton variante="secundario" type="button" disabled={op.procesando} onClick={cerrar}>Cancelar</Boton><Boton type="submit" cargando={op.procesando}>Guardar caja</Boton></div>
  </form></ModalCaja>
}
