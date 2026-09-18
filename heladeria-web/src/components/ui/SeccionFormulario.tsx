export function SeccionFormulario({
  titulo,
  descripcion,
  children,
  icono,
  compacto = false,
}: {
  titulo: string;
  descripcion?: string;
  children: React.ReactNode;
  icono?: React.ReactNode;
  compacto?: boolean;
}) {
  return (
    <section className={`rounded-xl border border-slate-200 p-4 ${compacto ? "" : "sm:p-5"}`}>
      <header className={`${compacto ? "mb-3" : "mb-4"} flex items-center gap-3`}>
        {icono && (
          <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-teal-50 text-teal-700">
            {icono}
          </span>
        )}
        <div>
          <h3 className="text-sm font-bold text-slate-900">{titulo}</h3>
          {descripcion && (
            <p className="mt-0.5 text-xs leading-5 text-slate-500">
              {descripcion}
            </p>
          )}
        </div>
      </header>
      <div className={`grid min-w-0 ${compacto ? "gap-x-6 gap-y-3" : "gap-4"} [&>*]:min-w-0 sm:grid-cols-2`}>
        {children}
      </div>
    </section>
  );
}
