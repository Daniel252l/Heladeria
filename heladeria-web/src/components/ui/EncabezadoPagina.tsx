export function EncabezadoPagina({
  titulo,
  descripcion,
  acciones,
  icono,
}: {
  titulo: string;
  descripcion: string;
  acciones?: React.ReactNode;
  icono?: React.ReactNode;
}) {
  return (
    <header className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-center gap-4">
        {icono && (
          <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-xl bg-teal-50 text-teal-700 sm:h-18 sm:w-18">
            {icono}
          </span>
        )}
        <div>
          <h1 className={`text-2xl font-bold tracking-tight text-slate-950 ${icono ? "sm:text-4xl" : "sm:text-3xl"}`}>
            {titulo}
          </h1>
          <p className={`mt-1 max-w-3xl text-sm leading-6 text-slate-500 ${icono ? "sm:text-base" : ""}`}>
            {descripcion}
          </p>
        </div>
      </div>
      {acciones && <div className="shrink-0">{acciones}</div>}
    </header>
  );
}
