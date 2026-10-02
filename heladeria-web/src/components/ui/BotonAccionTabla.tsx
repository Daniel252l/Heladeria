import type { ButtonHTMLAttributes } from "react";
import { Ban, Check, CheckCircle2, Eye, PackageCheck, Pencil, Power, Trash2 } from "lucide-react";
import { clases } from "../../utils/clases";

type TipoAccion = "ver" | "editar" | "activar" | "desactivar" | "eliminar" | "confirmar" | "recibir" | "anular";

const configuracion = {
  ver: {
    Icono: Eye,
    titulo: "Ver detalle",
    clases: "border-blue-200 bg-blue-50 text-blue-600 hover:bg-blue-100",
  },
  editar: {
    Icono: Pencil,
    titulo: "Editar",
    clases: "border-amber-200 bg-amber-50 text-amber-700 hover:bg-amber-100",
  },
  activar: {
    Icono: Check,
    titulo: "Activar",
    clases:
      "border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100",
  },
  desactivar: {
    Icono: Power,
    titulo: "Desactivar",
    clases: "border-red-200 bg-red-50 text-red-600 hover:bg-red-100",
  },
  eliminar: {
    Icono: Trash2,
    titulo: "Eliminar",
    clases: "border-red-200 bg-red-50 text-red-600 hover:bg-red-100",
  },
  confirmar: {
    Icono: CheckCircle2,
    titulo: "Confirmar",
    clases: "border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100",
  },
  recibir: {
    Icono: PackageCheck,
    titulo: "Recibir productos",
    clases: "border-teal-200 bg-teal-50 text-teal-700 hover:bg-teal-100",
  },
  anular: {
    Icono: Ban,
    titulo: "Anular",
    clases: "border-red-200 bg-red-50 text-red-600 hover:bg-red-100",
  },
} satisfies Record<
  TipoAccion,
  { Icono: typeof Eye; titulo: string; clases: string }
>;

export function BotonAccionTabla({
  tipo,
  etiqueta,
  className,
  ...propiedades
}: {
  tipo: TipoAccion;
  etiqueta?: string;
} & Omit<ButtonHTMLAttributes<HTMLButtonElement>, "children">) {
  const { Icono, titulo, clases: color } = configuracion[tipo];
  const nombre = etiqueta ?? titulo;
  return (
    <button
      type="button"
      className={clases(
        "inline-flex h-9 w-9 items-center justify-center rounded-lg border transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-600 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50",
        color,
        className,
      )}
      aria-label={nombre}
      title={titulo}
      {...propiedades}
    >
      <Icono className="h-4 w-4" aria-hidden="true" />
    </button>
  );
}

export function GrupoAccionesTabla({
  children,
}: {
  children: React.ReactNode;
}) {
  return <div className="flex items-center justify-end gap-2">{children}</div>;
}
