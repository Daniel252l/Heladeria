import type { Categoria } from "../../types/productos";

export interface FilaCategoriaArbol {
  categoria: Categoria;
  profundidad: number;
  tieneHijos: boolean;
  esUltimoHermano: boolean;
}

const comparadorCategorias = new Intl.Collator("es", {
  sensitivity: "base",
  numeric: true,
});

function normalizar(texto: string) {
  return texto
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim()
    .toLocaleLowerCase("es");
}

export function idsCategoriasPadre(categorias: Categoria[]) {
  const idsConHijos = new Set<number>();
  for (const categoria of categorias) {
    if (categoria.categoriaPadreId !== null)
      idsConHijos.add(categoria.categoriaPadreId);
  }
  return idsConHijos;
}

export function construirFilasCategorias(
  categorias: Categoria[],
  expandidas: ReadonlySet<number>,
  busqueda: string,
  estadoFiltroId: string,
): FilaCategoriaArbol[] {
  const porId = new Map(categorias.map((categoria) => [categoria.id, categoria]));
  const hijosPorPadre = new Map<number | null, Categoria[]>();
  const padreReal = new Map<number, number | null>();

  for (const categoria of categorias) {
    const padre =
      categoria.categoriaPadreId !== null &&
      categoria.categoriaPadreId !== categoria.id &&
      porId.has(categoria.categoriaPadreId)
        ? categoria.categoriaPadreId
        : null;
    padreReal.set(categoria.id, padre);
    const hijos = hijosPorPadre.get(padre) ?? [];
    hijos.push(categoria);
    hijosPorPadre.set(padre, hijos);
  }

  for (const hijos of hijosPorPadre.values())
    hijos.sort((a, b) => comparadorCategorias.compare(a.nombre, b.nombre));

  const texto = normalizar(busqueda);
  const estadoId = estadoFiltroId ? Number(estadoFiltroId) : null;
  const filtroActivo = Boolean(texto || estadoId);
  const incluidas = new Set<number>();

  const agregarAncestros = (categoria: Categoria) => {
    const visitados = new Set<number>();
    let actual: Categoria | undefined = categoria;
    while (actual && !visitados.has(actual.id)) {
      visitados.add(actual.id);
      incluidas.add(actual.id);
      const padreId = padreReal.get(actual.id);
      actual = padreId === null || padreId === undefined ? undefined : porId.get(padreId);
    }
  };

  const coincideEstado = (categoria: Categoria) =>
    estadoId === null || categoria.estadoId === estadoId;
  const coincideTexto = (categoria: Categoria) =>
    !texto ||
    normalizar(categoria.nombre).includes(texto) ||
    normalizar(categoria.codigo).includes(texto);

  if (filtroActivo) {
    const coincidencias = categorias.filter(
      (categoria) => coincideTexto(categoria) && coincideEstado(categoria),
    );
    for (const coincidencia of coincidencias) {
      agregarAncestros(coincidencia);

      // Si coincide un padre, se conserva su rama completa; el filtro de estado
      // continúa aplicándose a sus descendientes y se mantienen sus conectores.
      if (texto && coincideTexto(coincidencia)) {
        const pendientes = [...(hijosPorPadre.get(coincidencia.id) ?? [])];
        while (pendientes.length) {
          const descendiente = pendientes.shift()!;
          if (coincideEstado(descendiente)) agregarAncestros(descendiente);
          pendientes.push(...(hijosPorPadre.get(descendiente.id) ?? []));
        }
      }
    }
  } else {
    for (const categoria of categorias) incluidas.add(categoria.id);
  }

  const filas: FilaCategoriaArbol[] = [];
  const recorridas = new Set<number>();
  const conectadasConRaiz = new Set<number>();
  const marcarConexion = (categoria: Categoria) => {
    if (conectadasConRaiz.has(categoria.id)) return;
    conectadasConRaiz.add(categoria.id);
    for (const hijo of hijosPorPadre.get(categoria.id) ?? [])
      marcarConexion(hijo);
  };
  const recorrer = (categoria: Categoria, profundidad: number, esUltimo: boolean) => {
    if (recorridas.has(categoria.id) || !incluidas.has(categoria.id)) return;
    recorridas.add(categoria.id);
    const hijosVisibles = (hijosPorPadre.get(categoria.id) ?? []).filter((hijo) =>
      incluidas.has(hijo.id),
    );
    filas.push({
      categoria,
      profundidad,
      tieneHijos: hijosVisibles.length > 0,
      esUltimoHermano: esUltimo,
    });
    if (!filtroActivo && !expandidas.has(categoria.id)) return;
    hijosVisibles.forEach((hijo, indice) =>
      recorrer(hijo, profundidad + 1, indice === hijosVisibles.length - 1),
    );
  };

  const raices = hijosPorPadre.get(null) ?? [];
  for (const raiz of raices) marcarConexion(raiz);
  raices.forEach((raiz, indice) => recorrer(raiz, 0, indice === raices.length - 1));

  // Una referencia circular o un padre corrupto no debe ocultar el registro.
  const restantes = categorias
    .filter(
      (categoria) =>
        incluidas.has(categoria.id) && !conectadasConRaiz.has(categoria.id),
    )
    .sort((a, b) => comparadorCategorias.compare(a.nombre, b.nombre));
  restantes.forEach((categoria, indice) =>
    recorrer(categoria, 0, indice === restantes.length - 1),
  );

  return filas;
}
