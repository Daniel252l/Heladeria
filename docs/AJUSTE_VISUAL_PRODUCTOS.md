# Ajuste visual — Productos y componentes compartidos

Alcance: solo interfaz existente. Sin cambios en contratos, autenticación, permisos, SQL Server, bootstrap o módulos funcionales. Login, header y sidebar no se rediseñan.

## Cambios

- `PaginaProductos.tsx`: filtros desktop de tres columnas y tablet de dos; búsqueda con bloque de icono; tarjetas separadas de filtros/listado; tabla al ancho disponible con mínimo de72rem para scroll en pantallas pequeñas, conservando las11 columnas; nombre/SKU y miniatura real o icono de respaldo; check del estado operativo. No se inventan fotos, exportaciones, moneda ni DELETE.
- Formulario único Nuevo/Editar de896px máximo y90vh: campos compactos, clasificación integrada con información general, secciones de inventario/comercial, switches comerciales en cuatro/dos/una columnas. Todos los campos reales permanecen. Header y footer fuera del contenido desplazable; Guardar se asocia mediante el atributo `form` al formulario original.
- `SelectorEntidad.tsx`: un control visible, icono configurable, divisor opcional, placeholder de formulario y variante compacta. Búsqueda/paginación/backend siguen iguales (30 resultados, Enter para búsqueda y carga adicional). Escape del popover no cierra el modal contenedor. Listbox etiquetado y vinculado al botón.
- `Modal.tsx`: footer opcional y títulos con IDs únicos. Ventana y controles mantienen los límites de altura y scroll.
- `PanelFiltros.tsx`, `EncabezadoPagina.tsx`, `SeccionFormulario.tsx`, `SwitchCampo.tsx`, `Paginacion.tsx`: ajustes reutilizables de espaciado, jerarquía, densidad y accesibilidad. No se crean capas nuevas de arquitectura.

La paleta existente `BotonAccionTabla`/`GrupoAccionesTabla` se conserva y se comprueba con pruebas: azul ver, ámbar editar, verde activar, rojo desactivar/eliminar. Cada acción es solo-icono con aria-label/title/focus. No se agregan acciones inexistentes. Estos componentes ya están aplicados en Productos, Presentaciones de producto, Conversiones, los cinco catálogos de productos y los nueve mantenimientos de Configuración. Existencias reutiliza paginación/badges; no se inventan acciones de escritura.

## Archivos de este ajuste

Rutas relativas a `heladeria-web`:

- `src/pages/PaginaProductos.tsx`
- `src/components/ui/SelectorEntidad.tsx`
- `src/components/ui/Modal.tsx`
- `src/components/ui/PanelFiltros.tsx`
- `src/components/ui/EncabezadoPagina.tsx`
- `src/components/ui/SeccionFormulario.tsx`
- `src/components/ui/SwitchCampo.tsx`
- `src/components/ui/Paginacion.tsx`
- `src/features/productos/PaginaProductos.test.tsx`
- `src/components/ui/SelectorEntidad.test.tsx` (nuevo)
- `src/components/ui/ComponentesVisuales.test.tsx` (nuevo)
- `vite.config.ts`

Además, este informe está en `docs/AJUSTE_VISUAL_PRODUCTOS.md`. No se duplican componentes compartidos: se refinan los existentes. `MiniaturaProducto` es un pequeño helper local de la página, con respaldo ante imagen ausente/fallida, no una nueva capa funcional.

## Validación

- `npm run lint`: correcto.
- `npm run build`: correcto.
- `npm test`: 56/56, ocho archivos. Dieciocho pruebas nuevas cubren filtro conjunto/limpieza/colapso, tamaño de página, footer asociado al formulario, campos/switches, Escape del selector dentro del modal, conflicto409 con preservación de datos, búsqueda remota/páginas/selección opcional y paleta/accesibilidad/IDs de modal.
- Dos ejecuciones con concurrencia automática produjeron timeouts de5segundos en una prueba existente de inventario (y, en la primera, en una prueba nueva de producto). No se eliminaron comprobaciones ni se ampliaron timeouts. Con un worker toda la suite pasa. `vite.config.ts` limita por ello a uno los workers de pruebas, conservando el aislamiento y permitiendo usar el comando habitual `npm test`.
- `dotnet build --configuration Release`:0advertencias,0errores.
- `dotnet test --configuration Release --no-build`:104/104,0omitidas,0fallidas.

## Límite de la comprobación visual

Se inspeccionaron las dos imágenes proporcionadas y el código. Se preparó una vista temporal con datos simulados para verificar en navegador sin afectar datos reales, pero computer-use devolvió timeout de conexión de la webview tanto con apertura oculta/visible como con la alternativa documentada. No se pudo capturar ni verificar la comparación lado a lado, ni comprobar breakpoints en navegador. Las clases responsive están implementadas, pero su aprobación visual sigue pendiente. La vista temporal y el servidor fueron retirados.

## Revisión manual pendiente

1. Abrir Productos en el frontend habitual, con sidebar expandido y colapsado.
2. Comparar header, filtros (tres columnas desktop), tabla/badges/acciones y paginación con la primera imagen.
3. Abrir Nuevo y Editar; comparar secciones y switches con la segunda. El modelo real requiere más campos que la imagen: se conservan y se desplazan dentro del modal.
4. Comprobar que Guardar/Cancelar permanecen visibles mientras se desplaza el contenido.
5. Abrir selectores, buscar con Enter, cargar más y probar Escape. Confirmar permisos de consulta/administración y el conflicto409 sin pérdida de cambios.
6. Comprobar tablet/móvil: dos/una columnas, sin desbordamiento de formulario y con scroll horizontal limitado a las tablas.

No avanzar funcionalmente a otra fase hasta la revisión del usuario.
