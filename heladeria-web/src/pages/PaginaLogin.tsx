import { useState, type FormEvent } from 'react'
import { Eye, EyeOff, IceCreamBowl, LockKeyhole, ShieldCheck } from 'lucide-react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ErrorApi } from '../api/clienteHttp'
import { useAutenticacion } from '../auth/useAutenticacion'
import { Boton, Entrada } from '../components/ui/Controles'

export function PaginaLogin() {
  const [identificador, setIdentificador] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [mostrarContrasena, setMostrarContrasena] = useState(false)
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const { autenticado, cargando, sesion, iniciarSesion } = useAutenticacion()
  const navegar = useNavigate()
  const ubicacion = useLocation()

  if (!cargando && autenticado) {
    return <Navigate to={sesion?.requiereCambioContrasena ? '/cambiar-contrasena' : '/dashboard'} replace />
  }

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault()
    setError(null)
    if (!identificador.trim() || !contrasena) {
      setError('Ingrese su usuario o correo y la contraseña.')
      return
    }
    setEnviando(true)
    try {
      const requiereCambio = await iniciarSesion({
        identificador: identificador.trim(),
        contrasena,
      })
      const destino = (ubicacion.state as { desde?: string } | null)?.desde
      navegar(requiereCambio ? '/cambiar-contrasena' : destino || '/dashboard', { replace: true })
    } catch (excepcion) {
      setError(excepcion instanceof ErrorApi && excepcion.codigo === 'SESION_ACTIVA'
        ? 'Este usuario ya tiene una sesión activa. Cierre la sesión anterior antes de volver a ingresar.'
        : excepcion instanceof ErrorApi
          ? excepcion.message
          : 'No fue posible iniciar sesión. Intente nuevamente.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <main className="min-h-screen bg-slate-100 lg:grid lg:grid-cols-[minmax(0,1.05fr)_minmax(28rem,0.95fr)]">
      <section className="relative hidden overflow-hidden bg-[#17202f] p-12 text-white lg:flex lg:flex-col lg:justify-between">
        <div className="absolute -right-28 top-24 h-72 w-72 rounded-full border border-white/10" />
        <div className="absolute -right-10 top-40 h-72 w-72 rounded-full border border-white/10" />
        <div className="relative flex items-center gap-3">
          <span className="grid h-11 w-11 place-items-center rounded-xl bg-teal-600"><IceCreamBowl className="h-6 w-6" /></span>
          <div><p className="text-lg font-bold">Heladería</p><p className="text-xs tracking-wide text-slate-400">GESTIÓN OPERATIVA</p></div>
        </div>
        <div className="relative max-w-xl">
          <p className="mb-5 text-sm font-semibold uppercase tracking-[0.18em] text-teal-300">Control claro. Operación segura.</p>
          <h1 className="text-4xl font-bold leading-tight xl:text-5xl">Toda la operación de tu heladería, en un solo lugar.</h1>
          <p className="mt-6 max-w-lg text-base leading-7 text-slate-300">Administra catálogos, productos e inventario con control por sucursal y trazabilidad de cada acción.</p>
        </div>
        <div className="relative flex items-center gap-3 text-sm text-slate-400"><ShieldCheck className="h-5 w-5 text-teal-400" /> Sesiones protegidas y acceso basado en permisos</div>
      </section>

      <section className="flex min-h-screen items-center justify-center px-5 py-10 sm:px-10">
        <div className="w-full max-w-md">
          <div className="mb-9 flex items-center gap-3 lg:hidden">
            <span className="grid h-10 w-10 place-items-center rounded-xl bg-[#17202f] text-white"><IceCreamBowl className="h-5 w-5" /></span>
            <span className="font-bold text-slate-900">Heladería</span>
          </div>
          <div className="mb-8">
            <span className="mb-4 grid h-12 w-12 place-items-center rounded-xl bg-teal-50 text-teal-700"><LockKeyhole className="h-6 w-6" /></span>
            <h2 className="text-3xl font-bold tracking-tight text-slate-950">Bienvenido</h2>
            <p className="mt-2 text-slate-500">Ingrese sus credenciales para continuar.</p>
          </div>
          <form className="space-y-5" onSubmit={enviar} noValidate>
            <Entrada etiqueta="Usuario o correo" name="identificador" autoComplete="username" value={identificador} onChange={(evento) => setIdentificador(evento.target.value)} placeholder="admin o admin@heladeria.local" disabled={enviando} />
            <div className="relative">
              <Entrada etiqueta="Contraseña" name="contrasena" type={mostrarContrasena ? 'text' : 'password'} autoComplete="current-password" value={contrasena} onChange={(evento) => setContrasena(evento.target.value)} placeholder="Ingrese su contraseña" disabled={enviando} className="pr-12" />
              <button type="button" className="absolute right-2 top-[2.1rem] rounded-md p-2 text-slate-500 hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600" onClick={() => setMostrarContrasena((actual) => !actual)} aria-label={mostrarContrasena ? 'Ocultar contraseña' : 'Mostrar contraseña'}>
                {mostrarContrasena ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
              </button>
            </div>
            {error && <div className="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700" role="alert">{error}</div>}
            <Boton type="submit" className="w-full" cargando={enviando}>Iniciar sesión</Boton>
          </form>
          <p className="mt-8 text-center text-xs leading-5 text-slate-400">El acceso queda registrado para fines de seguridad y auditoría.</p>
        </div>
      </section>
    </main>
  )
}
