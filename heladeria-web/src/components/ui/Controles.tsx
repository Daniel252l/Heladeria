import type {
  ButtonHTMLAttributes,
  InputHTMLAttributes,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from "react";
import { LoaderCircle } from "lucide-react";
import { clases } from "../../utils/clases";

type VarianteBoton = "primario" | "secundario" | "sutil" | "peligro";

interface PropiedadesBoton extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: VarianteBoton;
  cargando?: boolean;
}

export function Boton({
  variante = "primario",
  cargando = false,
  className,
  children,
  disabled,
  ...propiedades
}: PropiedadesBoton) {
  const variantes: Record<VarianteBoton, string> = {
    primario: "bg-teal-700 text-white hover:bg-teal-800 shadow-sm",
    secundario:
      "border border-slate-300 bg-white text-slate-700 hover:bg-slate-50",
    sutil: "text-slate-600 hover:bg-slate-100 hover:text-slate-900",
    peligro: "bg-rose-600 text-white hover:bg-rose-700 shadow-sm",
  };
  return (
    <button
      className={clases(
        "inline-flex min-h-10 items-center justify-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-55",
        variantes[variante],
        className,
      )}
      disabled={disabled || cargando}
      {...propiedades}
    >
      {cargando && (
        <LoaderCircle className="h-4 w-4 animate-spin" aria-hidden="true" />
      )}
      {children}
    </button>
  );
}

interface CampoBase {
  etiqueta: string;
  error?: string;
  ayuda?: string;
}

export function Entrada({
  etiqueta,
  error,
  ayuda,
  className,
  id,
  ...propiedades
}: CampoBase & InputHTMLAttributes<HTMLInputElement>) {
  const identificador = id ?? propiedades.name;
  return (
    <label
      className="block min-w-0 text-sm font-medium text-slate-700"
      htmlFor={identificador}
    >
      <span>{etiqueta}</span>
      <input
        id={identificador}
        className={clases(
          "mt-1.5 block min-h-11 min-w-0 w-full rounded-lg border bg-white px-3 py-2.5 text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100",
          error ? "border-rose-400" : "border-slate-300",
          className,
        )}
        aria-invalid={Boolean(error)}
        {...propiedades}
      />
      {error ? (
        <span className="mt-1 block text-xs text-rose-600">{error}</span>
      ) : ayuda ? (
        <span className="mt-1 block text-xs text-slate-500">{ayuda}</span>
      ) : null}
    </label>
  );
}

export function Seleccion({
  etiqueta,
  error,
  ayuda,
  className,
  id,
  children,
  ...propiedades
}: CampoBase & SelectHTMLAttributes<HTMLSelectElement>) {
  const identificador = id ?? propiedades.name;
  return (
    <label
      className="block min-w-0 text-sm font-medium text-slate-700"
      htmlFor={identificador}
    >
      <span>{etiqueta}</span>
      <select
        id={identificador}
        className={clases(
          "mt-1.5 block min-h-11 min-w-0 w-full rounded-lg border bg-white px-3 py-2.5 text-slate-900 outline-none transition focus:border-teal-600 focus:ring-2 focus:ring-teal-100",
          error ? "border-rose-400" : "border-slate-300",
          className,
        )}
        aria-invalid={Boolean(error)}
        {...propiedades}
      >
        {children}
      </select>
      {error ? (
        <span className="mt-1 block text-xs text-rose-600">{error}</span>
      ) : ayuda ? (
        <span className="mt-1 block text-xs text-slate-500">{ayuda}</span>
      ) : null}
    </label>
  );
}

export function AreaTexto({
  etiqueta,
  error,
  ayuda,
  className,
  id,
  ...propiedades
}: CampoBase & TextareaHTMLAttributes<HTMLTextAreaElement>) {
  const identificador = id ?? propiedades.name;
  return (
    <label
      className="block min-w-0 text-sm font-medium text-slate-700"
      htmlFor={identificador}
    >
      <span>{etiqueta}</span>
      <textarea
        id={identificador}
        className={clases(
          "mt-1.5 block min-w-0 w-full rounded-lg border bg-white px-3 py-2.5 text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-teal-600 focus:ring-2 focus:ring-teal-100",
          error ? "border-rose-400" : "border-slate-300",
          className,
        )}
        aria-invalid={Boolean(error)}
        {...propiedades}
      />
      {error ? (
        <span className="mt-1 block text-xs text-rose-600">{error}</span>
      ) : ayuda ? (
        <span className="mt-1 block text-xs text-slate-500">{ayuda}</span>
      ) : null}
    </label>
  );
}

export function Tarjeta({
  className,
  children,
}: {
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <section
      className={clases(
        "rounded-xl border border-slate-200 bg-white shadow-sm",
        className,
      )}
    >
      {children}
    </section>
  );
}

export function Insignia({
  tono = "neutro",
  children,
}: {
  tono?: "positivo" | "advertencia" | "negativo" | "neutro" | "info";
  children: React.ReactNode;
}) {
  const tonos = {
    positivo: "bg-emerald-50 text-emerald-700 ring-emerald-600/20",
    advertencia: "bg-amber-50 text-amber-700 ring-amber-600/20",
    negativo: "bg-rose-50 text-rose-700 ring-rose-600/20",
    info: "bg-sky-50 text-sky-700 ring-sky-600/20",
    neutro: "bg-slate-100 text-slate-700 ring-slate-500/20",
  };
  return (
    <span
      className={clases(
        "inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset",
        tonos[tono],
      )}
    >
      {children}
    </span>
  );
}
