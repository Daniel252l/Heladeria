import { clases } from "../../utils/clases";

export function SwitchCampo({
  etiqueta,
  descripcion,
  activo,
  cambiar,
  className,
  sinTarjeta = false,
  compacto = false,
}: {
  etiqueta: string;
  descripcion?: string;
  activo: boolean;
  cambiar: (activo: boolean) => void;
  className?: string;
  sinTarjeta?: boolean;
  compacto?: boolean;
}) {
  return (
    <label
      className={clases(
        "flex cursor-pointer items-start gap-2.5 rounded-lg bg-white transition",
        !sinTarjeta && "border border-slate-200 hover:border-slate-300",
        sinTarjeta ? "py-1" : compacto ? "p-2.5" : "p-3.5",
        className,
      )}
    >
      <input
        type="checkbox"
        role="switch"
        className="peer sr-only"
        checked={activo}
        onChange={(evento) => cambiar(evento.target.checked)}
      />
      <span aria-hidden="true" className="relative mt-0.5 h-5 w-9 shrink-0 rounded-full bg-slate-300 transition-colors peer-checked:bg-teal-700 peer-focus-visible:ring-2 peer-focus-visible:ring-teal-600 peer-focus-visible:ring-offset-2 after:absolute after:left-0.5 after:top-0.5 after:h-4 after:w-4 after:rounded-full after:bg-white after:shadow-sm after:transition-transform peer-checked:after:translate-x-4" />
      <span className="min-w-0">
        <span className={clases("block font-semibold text-slate-800", compacto ? "text-xs leading-5" : "text-sm")}>
          {etiqueta}
        </span>
        {descripcion && (
          <span className="mt-0.5 block text-xs leading-4 text-slate-500">
            {descripcion}
          </span>
        )}
      </span>
    </label>
  );
}
