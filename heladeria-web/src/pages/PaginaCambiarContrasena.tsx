import { useMemo, useState, type FormEvent } from 'react'
import { Check, Eye, EyeOff, KeyRound } from 'lucide-react'
import { Navigate, useNavigate } from 'react-router-dom'
import { ErrorApi } from '../api/clienteHttp'
import { useAutenticacion } from '../auth/useAutenticacion'
import { useToast } from '../components/toast/useToast'
import { Boton, Entrada, Tarjeta } from '../components/ui/Controles'

export function PaginaCambiarContrasena() {
  const [actual, setActual] = useState('')
  const [nueva, setNueva] = useState('')
  const [confirmacion, setConfirmacion] = useState('')
  const [visible, setVisible] = useState(false)
  const [enviando, setEnviando] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const { autenticado, cambiarContrasena } = useAutenticacion()
  const { mostrar } = useToast()
  const navegar = useNavigate()
  const reglas = useMemo(() => [
    { texto: 'Al menos 10 caracteres', valida: nueva.length >= 10 },
    { texto: 'Una letra mayúscula y una minúscula', valida: /[A-Z]/.test(nueva) && /[a-z]/.test(nueva) },
    { texto: 'Un número', valida: /\d/.test(nueva) },
    { texto: 'Un carácter especial', valida: /[^A-Za-z0-9]/.test(nueva) },
  ], [nueva])

  if (!autenticado) return <Navigate to="/login" replace />

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault()
    setError(null)
    if (!actual || reglas.some((regla) => !regla.valida)) {
      setError('Complete la contraseña actual y cumpla todos los requisitos.')
      return
    }
    if (nueva !== confirmacion) {
      setError('La confirmación no coincide con la nueva contraseña.')
      return
    }
    setEnviando(true)
    try {
      await cambiarContrasena({ contrasenaActual: actual, nuevaContrasena: nueva, confirmacion })
      mostrar({ tipo: 'exito', titulo: 'Contraseña actualizada', descripcion: 'Su sesión segura ha sido renovada.' })
      navegar('/dashboard', { replace: true })
    } catch (excepcion) {
      setError(excepcion instanceof ErrorApi ? excepcion.message : 'No fue posible cambiar la contraseña.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <main className="grid min-h-screen place-items-center bg-slate-100 px-5 py-10">
      <Tarjeta className="w-full max-w-lg overflow-hidden">
        <div className="border-b border-slate-200 px-6 py-6 sm:px-8">
          <span className="mb-4 grid h-12 w-12 place-items-center rounded-xl bg-amber-50 text-amber-700"><KeyRound className="h-6 w-6" /></span>
          <h1 className="text-2xl font-bold text-slate-950">Proteja su cuenta</h1>
          <p className="mt-2 text-sm leading-6 text-slate-500">Debe establecer una contraseña personal antes de acceder al sistema.</p>
        </div>
        <form className="space-y-5 px-6 py-6 sm:px-8" onSubmit={enviar}>
          <Entrada etiqueta="Contraseña actual" type="password" autoComplete="current-password" value={actual} onChange={(evento) => setActual(evento.target.value)} disabled={enviando} />
          <div className="relative">
            <Entrada etiqueta="Nueva contraseña" type={visible ? 'text' : 'password'} autoComplete="new-password" value={nueva} onChange={(evento) => setNueva(evento.target.value)} disabled={enviando} className="pr-12" />
            <button type="button" className="absolute right-2 top-[2.1rem] rounded-md p-2 text-slate-500 hover:bg-slate-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600" onClick={() => setVisible((valor) => !valor)} aria-label={visible ? 'Ocultar contraseñas' : 'Mostrar contraseñas'}>
              {visible ? <EyeOff className="h-5 w-5" /> : <Eye className="h-5 w-5" />}
            </button>
          </div>
          <Entrada etiqueta="Confirmar contraseña" type={visible ? 'text' : 'password'} autoComplete="new-password" value={confirmacion} onChange={(evento) => setConfirmacion(evento.target.value)} disabled={enviando} />
          <ul className="grid gap-2 rounded-lg bg-slate-50 p-4 text-xs text-slate-600 sm:grid-cols-2">
            {reglas.map((regla) => <li key={regla.texto} className={`flex items-center gap-2 ${regla.valida ? 'text-emerald-700' : ''}`}><Check className="h-3.5 w-3.5" />{regla.texto}</li>)}
          </ul>
          {error && <div className="rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700" role="alert">{error}</div>}
          <Boton type="submit" className="w-full" cargando={enviando}>Actualizar y continuar</Boton>
        </form>
      </Tarjeta>
    </main>
  )
}
