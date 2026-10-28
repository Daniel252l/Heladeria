# Datos demostrativos opcionales de Heladeria

Preparado el 28/10/2026. **No se ejecutó sobre VictusPabloE / Heladeria ni sobre otra BD real.** El propietario decide cuándo cargarlo. Este conjunto es independiente de la instalación estructural y del Ajuste D funcional.

## Objetivo y estrategia

Poblar una demostración con catálogo, compras, inventario, POS, ventas, caja, cuentas y reportes coherentes. Se eligió **Heladeria.Bootstrap --cargar-datos-demo**, reutilizando los servicios existentes. El archivo requerido `BaseDatos/Demo/Heladeria_DatosDemo.sql` contiene instrucciones explícitas, no INSERTs: ejecutarlo solo imprime que no se cargó nada.

Las operaciones tienen invariantes que un conjunto de INSERTs no reproduce con seguridad: estados/transiciones, rowversion, UUID y huellas, correlativos, costos promedio, detalle de movimientos/Kardex, pago aplicado/recibido, conciliación de turnos y aplicaciones financieras. El cargador pasa por esos servicios; no mantiene una segunda implementación de esas reglas.

**No se modifica Heladeria.sql**, ningún script de reparación, contratos API, frontend, lógica funcional, permisos ni seguridad.

## Inspección del esquema real

Se revisó el maestro completo con sus bloques incrementales hasta Ajuste D, entidades EF y servicios escritores/lectores.

| Área | Estructura real y restricciones utilizadas |
|---|---|
| Empresa/sucursal/usuario | Empresas y Sucursales se reutilizan desde UsuariosSucursales principal activa de un ADMIN activo. La empresa/sucursal destino debe ser inequívoca. Usuario solo como FK/actor de las operaciones; no se actualiza. Sucursales ya tiene proveedor obligatorio y no se crea otra sucursal. |
| Estados/tipos | Estados/ClaseEstados, Tipos/ClaseTipos y catálogos/estados de flujo existentes. Se identifican por claves naturales y se exige preparación previa. No se insertan ni modifican en el demo. |
| Catálogo | Categorias, TiposProductos, UnidadesMedida, Presentaciones y Productos. Códigos/nombres únicos, estados, empresa, FKs de unidades y auditoría se validan por los servicios. |
| Presentaciones | ProductoPresentaciones vincula empresa/producto/presentación; factor positivo decimal(19,6). Los factores pertenecen al producto, no al nombre de la presentación. |
| Composición | CombosDetalle: cantidad decimal, presentación opcional y FKs del Ajuste D. Solo componentes terminales, sin anidación. |
| Inventario | Existencias, Movimientos, MovimientoDetalles y Kardex; no existen módulos paralelos llamados Inventarios/MovimientosInventario para esta descarga. Los saldos y el Kardex nacen de recepciones, ventas y anulaciones. |
| Mínimos | ParametrosStockProductos: producto/bodega/ubicación, mínimo/máximo/reorden y usuario/fecha. Solo se añaden parámetros para los 30 productos terminales nuevos; no se insertan saldos manuales. |
| Terceros | Clientes y Proveedores tienen empresa/estado; no necesitan Persona/Tercero ni contactos separados para ser válidos. NIT, teléfono y contactos son opcionales y se omiten. |
| Compras | OrdenesCompra/OrdenesCompraDetalle → RecepcionesCompra/RecepcionCompraDetalles. Estados reales: CONFIRMADA, RECIBIDA, PARCIALMENTE_RECIBIDA. |
| CxP | FacturasProveedores → CuentasPorPagar → AplicacionesCuentasPorPagar → Pagos/PagoDetalles. Solo se factura una orden explícitamente a crédito, totalmente recibida y sin factura previa. |
| Ventas/CxC | Ventas/VentasDetalle, incluidos costo y composición históricos. El cierre a crédito genera CuentasPorCobrar; los abonos generan AplicacionesCuentasPorCobrar y pagos reales de demostración. |
| Caja | Cajas, TurnosCaja, MovimientosCaja, ArqueosCaja. Apertura/arqueo/cierre y saldo se crean con servicios, conservando el índice de una sesión abierta por caja. |
| Moneda | Se reutiliza GTQ base activa con dos decimales. Si no existe y no hay otra moneda base, se crea GTQ. Si la configuración existente es incompatible se aborta, no se sustituye. |
| Documentos | SeriesDocumentos/CorrelativosDocumentos existentes de OC/RC/VT/VS/VA/CA/CC/MC. Los servicios reservan números bajo bloqueo; nunca se reinician ni se inventan IDs/documentos de venta. |
| Auditoría | Bitacoras generada por operaciones funcionales, con Equipo = HELADERIA-DEMO-V1. Sin auditoría falsa de autenticación, JWT o sesiones. |

No se usa IDENTITY_INSERT ni un Id fijo de empresa, sucursal, producto, proveedor, moneda o usuario. Se conservan los IDs reales devueltos por los servicios.

## Qué crea

| Conjunto | Cantidad / contenido |
|---|---|
| Productos | **35**: 10 helados, 4 bebidas, 5 especiales compuestos, 8 empaques/consumibles y 8 insumos. **20 vendibles**, 30 terminales inventariables. |
| Categorías | Seis grupos: Helados de la Casa, Bebidas frías, Postres de la Casa, Especialidades, Toppings e insumos, Empaques y consumibles. Reutiliza una categoría homónima activa o crea una nueva. |
| Unidades | ml, g y unidad, reutilizadas si son compatibles; no se cambia una unidad preexistente. |
| Presentaciones | Seis formatos de helado por sabor, más litro para leche y dos siropes: **63 relaciones** ProductoPresentaciones. |
| Proveedores | **5**, todos ficticios, con 30 días y límite Q15,000. |
| Clientes | **12**: seis consumidores sin crédito configurado y seis negocios ficticios con límite Q2,000 y 15 días. |
| Compras | **10**: nueve totalmente recibidas y una parcialmente recibida. Seis a crédito y cuatro de contado. |
| Ventas | **38**: 36 completadas y dos cortesías sin cobro completadas/anuladas conforme al flujo permitido. |
| Formas/modalidad | Entre las 36 completadas: **17 efectivo, 10 tarjeta, 3 mixtas y 6 crédito**. |
| Caja | Una caja nueva, código DEMO-CAJA, con **14 turnos cerrados**, aperturas, cobros, abonos/pagos, dos movimientos manuales ficticios, arqueos sin diferencias y cierres. |
| CxC | **6**: dos pagadas, una parcial y tres pendientes. |
| CxP | **6** facturas/cuentas: tres pagadas, una parcial y dos pendientes, con vencimientos vigentes/vencidos según la fecha relativa. |
| Existencias | 30 productos con stock/costo real derivados de operaciones. Tres ejemplos bajo mínimo: limón, té frío y cerezas. |
| Auditoría | Eventos funcionales del servicio y una marca final CARGA_DEMO_V1. |

Helados: Chocolate de la Casa, Vainilla Bourbon, Fresa del Huerto, Cookies & Cream, Pistacho Selecto, Café de Altura, Mango Tropical, Limón Fresco, Caramelo Dorado y Chicle Azul.

Proveedores: Distribuidora Frío Norte, Lácteos del Valle, Bebidas Centroamericanas, Empaques del Pacífico y Dulces y Complementos GT. Son nombres para la demostración, sin afirmar identidad comercial real.

Clientes ficticios: Familias Nube, Brisa, Aurora, Canelo, Almendra y Acacia; Cafetería Luna Azul, Café Jardín de Papel, Eventos Horizonte, Oficinas Río Claro, Repostería Nube Dulce y Club Mirador. Correos `@heladeria-demo.invalid`, dominio reservado no operativo. No se usan NIT, teléfonos ni datos personales reales.

### Presentaciones, precios y costos

| Formato de helado | Factor a ml | Precio GTQ |
|---|---:|---:|
| Bola | 60 | 8.00 |
| Vaso pequeño | 120 | 12.00 |
| Vaso mediano | 180 | 18.00 |
| Vaso grande | 250 | 24.00 |
| Medio litro | 500 | 45.00 |
| Litro | 1000 | 80.00 |

Se compra el litro a Q50 (pistacho Q55): costo base Q0.05/ml o Q0.055/ml. Los formatos comerciales mantienen márgenes positivos razonables; el precio del litro se ajusta al costo, no se utiliza el ejemplo Q60 si erosionaría el margen. Bebidas Q6–Q10; especiales Q22–Q32. El precio comercial permanece independiente del costo.

Stock de compra inicial: helados 8,000–18,000 ml, bebidas 24–70 unidades, vasos/copa 120–220, cucharas 400, servilletas 800, pajillas 300, tapas 200 e insumos en sus unidades reales. Compras posteriores reponen chocolate/Coca-Cola y parcialmente vasos/servilletas. La existencia final es la inicial más recepciones menos consumos más reversión, **no un saldo impuesto al terminar**.

### Composiciones

- **Vaca Loca de la Casa:** dos BOLA de vainilla, Coca-Cola lata, vaso 16 oz, cuchara y servilleta.
- **Banana Split Tropical:** una BOLA chocolate/vainilla/fresa, banana, 20 g crema, 10 ml sirope chocolate, cereza, copa, cuchara y servilleta.
- **Sundae Chocolate Intenso:** dos BOLA chocolate, 15 ml sirope, 10 g chispas, vaso 8 oz, cuchara y servilleta.
- **Milkshake Vainilla Bourbon:** 180 ml vainilla, 150 ml leche, vaso 16 oz, tapa y pajilla.
- **Brownie Tibio con Helado:** brownie, una BOLA vainilla, 10 ml sirope, copa, cuchara y servilleta.

Cantidades por una unidad base del padre, conforme al Ajuste D. Empaques/insumos pueden no ser vendibles. No se crea composición anidada ni recetas de producción.

### Historial y reportes

Fechas relativas: desde hoy local menos 13 días hasta hoy, según la zona empresarial resuelta por TiempoEmpresarial. Los servicios reciben un TimeProvider privado de la carga; el reloj de la API no se cambia. Timestamps UTC y fechas civiles se convierten mediante la zona IANA configurada, sin restar seis horas manualmente. Ninguna operación del día se fecha después del instante de inicio de carga.

Se generan dos ventas completadas por día anterior y diez completadas hoy; una anulación pasada y una hoy. El diario actual tiene diez completadas, dos de ellas a crédito, y una anulada. Ocho compras iniciales abastecen todo el catálogo; dos reposiciones se distribuyen en días posteriores.

Los reportes usan las operaciones existentes: ventas por día/producto, costo/margen, entradas/salidas y valorización, proveedores, formas de pago, turnos y cartera. Kardex no se fabrica por separado. Ticket conserva el producto comercial y los consumos internos quedan en inventario.

Las anulaciones son **cortesías al 100% sin pago ni CxC**. No se eluden las restricciones para anular ventas pagadas. Los pedidos de contado no producen una factura/CxP ficticia: se mantienen las posibilidades reales del flujo aprobado.

## Qué conserva y qué no crea

- No crea ni modifica usuarios, contraseñas, hashes, roles, permisos, políticas, JWT, tokens, sesiones de autenticación, SMTP o secretos.
- No ejecuta bootstrap administrativo ni preparadores estructurales durante la carga demo.
- Reutiliza la empresa/sucursal principal del administrador y una ubicación activa existente. No presupone Id 4/8/1.
- **No utiliza ni modifica una sesión de caja preexistente**, incluida una eventual sesión #8. Todos los movimientos demo usan la caja nueva DEMO-CAJA.
- No crea un turno abierto persistente: los 14 turnos demo quedan cerrados, incluido el de hoy. Para probar manualmente nuevas ventas deberá abrirse explícitamente un turno.
- No crea datos de producción, lotes/series, impuestos, promociones, imágenes remotas ni permisos nuevos.
- No cambia registros operativos anteriores. **Sí avanzan los correlativos compartidos**, mediante sus servicios, para reservar documentos únicos.
- Al pertenecer a la misma empresa/sucursal, las nuevas operaciones **sí aparecen en sus reportes agregados**. No existe una vista financiera que los excluya automáticamente; use una BD copia si necesita mantener reportes reales libres de demostraciones.

## Transacción, repetición y fallos

Toda la carga usa una transacción SQL Server SERIALIZABLE. Un bloqueo exclusivo de carga impide carreras entre dos cargadores; se toma además la reserva empresarial de inventario utilizada por los servicios.

Los servicios actuales administran sus propias transacciones. Para reutilizarlos sin modificar su código, el **DbContext exclusivo del comando demo** registra TransaccionesDemo mediante el contrato público IDbContextTransactionManager/IRelationalTransactionManager de EF:

- Los comienzos locales participan en la transacción raíz.
- Commit/Dispose locales no confirman ni cierran la raíz.
- Un rollback local marca toda la carga como fallida; no permite confirmarla.
- Solo el coordinador demo confirma después de validar existencias, cuentas y turnos.
- Cualquier excepción/cancelación revierte datos, auditoría y correlativos. Los identity pueden conservar huecos tras rollback, comportamiento normal de SQL Server; no se reseñan ni se reinician.

Este adaptador **no se registra en la API ni en bootstrap estructural**. No se cambian servicios de Ventas/Compras/Caja/Cartera/Inventario. Una actualización futura de EF requiere repetir las pruebas transaccionales del comando.

Idempotencia por empresa: se comprueba Bitacoras.Accion = CARGA_DEMO_V1 y los códigos reservados de productos/terceros/caja. Si existen, se rechaza con «Los datos demo ya fueron cargados…», sin hacer UPSERT ni sobrescribir. La marca se guarda en la misma transacción, al final. Después de rollback completo se puede reintentar. Si otra carga conserva el bloqueo más de diez segundos, la segunda se rechaza con un mensaje controlado.

## Cómo cargar manualmente

**Esta sección no autoriza ni ejecuta la carga automáticamente.** Verifique que la conexión apunta a la BD de desarrollo deseada. Es recomendable respaldarla y detener temporalmente operaciones de la API para evitar competir por bloqueos.

Desde la carpeta que contiene `Heladeria.sln`, PowerShell:

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet run --project Heladeria.Bootstrap -- --cargar-datos-demo
```

Usa la configuración existente de Bootstrap: appsettings del ambiente, User Secrets de desarrollo y/o `ConnectionStrings__Heladeria`. No escriba contraseñas ni connection strings en argumentos o archivos versionados.

El comando muestra ambiente, servidor y base, nunca contraseña. El argumento es una orden explícita de carga; **no solicita una segunda confirmación interactiva en Development**. Al terminar informa empresa/sucursal, cantidades y período, únicamente después del commit.

Fuera de Development se rechaza antes de abrir conexión. Para una copia de demostración en otro ambiente, la habilitación adicional explícita es:

```powershell
dotnet run --project Heladeria.Bootstrap -- --cargar-datos-demo --confirmar-demo
```

La bandera no vuelve segura una BD de producción ni distingue su contenido. No usar en producción operativa. No combinar con argumentos de preparación ni pasar IDs o secretos.

Precondiciones: maestro/esquema hasta Ajuste D, bootstrap administrativo y preparaciones estructurales correspondientes ya ejecutadas por el propietario; administrador principal activo, bodega/ubicación activa, series/estados/formas de pago válidos. Si faltan, el cargador aborta: no los instala silenciosamente ni concede permisos.

## Identificación y restablecimiento

- SKU y códigos propios comienzan con `DEMO-`; nombres visibles comerciales.
- Caja: `DEMO-CAJA`; facturas ficticias: serie DEMO y DEMO-FACT-001…006.
- Descripciones y auditoría funcional: HELADERIA-DEMO-V1.
- Bitácora final: CARGA_DEMO_V1, entidad DatosDemo, versión V1.
- Documentos internos OC/VT/MC etc. conservan el formato real y se identifican por sus relaciones con productos/terceros/caja demo, no por números identity fijos.

Para reiniciar una demostración, usar **una BD nueva o restaurar una copia previa conocida**, instalar el maestro limpio y realizar el bootstrap estructural de forma explícita antes de cargar nuevamente. No se recomienda DELETE manual masivo, borrar la marca, reseed de identities ni reiniciar correlativos: rompería trazabilidad y claves financieras. No se incluye un purgador.

## Validación y límites

Pruebas específicas en instancias LocalDB nuevas con nombres aleatorios; no leen conexiones reales:
instalar Heladeria.sql → bootstrap/preparadores estructurales → ejecutar cargador → comprobar invariantes.

Cobertura: protección de ambientes antes de conectar, catálogo/composición, presentación/conversión, documentos únicos, stock/costo/Kardex, caja conciliada, CxC/CxP, reporte diario y fechas al inicio/final del día, preservación de usuarios/roles/permisos y sesión abierta anterior, segunda carga, cargas concurrentes, fallos tras compras y tras todas las operaciones. Para rollback se comparan huellas SHA-256 del contenido de **todas las tablas**, incluidos correlativos y auditoría, sin imprimir secretos.

Resultados finales, 28/10/2026:

| Validación | Resultado |
|---|---|
| dotnet restore | Correcto, proyectos actualizados. |
| dotnet build --configuration Release | **0 errores / 0 advertencias**. |
| dotnet test --configuration Release | **722 aprobadas / 0 fallidas / 0 omitidas**, 4 min 9 s; 710 anteriores + 12 casos demo nuevos. |
| npm run lint | Correcto, sin errores. |
| npm test | **608 aprobadas / 0 fallidas / 0 omitidas**, 32 archivos, 105.85 s. |
| npm run build | TypeScript y Vite correctos. |
| LocalDB | Carga limpia y con sesión previa, rollback en dos etapas, repetición y concurrencia aprobados. |

Evidencias locales: `Heladeria.Pruebas/TestResults/datos-demo-completa.trx` y `heladeria-web/test-results/datos-demo-regresion.json`. No se eliminó ni debilitó ninguna prueba anterior. Se corrigieron dos errores de los datos/fixtures nuevos durante el desarrollo: normalizar el JSON de tablas vacías en la utilidad de huellas y evitar repetir agua en una línea adicional de la misma venta demo.

Los 12 casos nuevos comprenden cinco combinaciones de ambiente, dos instalaciones integrales (inicio del día y tarde con sesión existente), dos fallos tardíos con reintento, una BD sin bootstrap, un rechazo Production antes de conectar y dos cargas concurrentes. Los cambios de mínimos de stock también quedan cubiertos en la suite final.

Comparación SHA-256 contra los 562 archivos de origen capturados al inicio: ocho creados y tres modificados, ningún archivo eliminado. Maestro, reparaciones, servicios funcionales, contratos y frontend permanecen iguales. Las salidas locales de build/test no son una publicación de artifacts.

Límites operativos:

- Una sola empresa/sucursal principal administrativa inequívoca. Múltiples destinos principales distintos requieren resolver la ambigüedad antes de cargar.
- Colisiones de nombres/códigos o catálogos incompatibles abortan. No renombra ni reutiliza un producto operativo anterior para completar el demo.
- Solo GTQ base y las formas existentes EFECTIVO/TARJETA; no cambia políticas monetarias.
- Catálogos compatibles pueden reutilizarse; el número de **nuevas filas de catálogo** depende de lo que ya exista. Los 35 productos y los documentos demo siempre son nuevos.
- Sin producción demo ni imágenes: no se añadieron reglas, archivos frontend ni dependencias para esos fines.
- No se publican artifacts ni se modifica el esquema maestro. La carga de desarrollo permanece pendiente de decisión del propietario.

## Archivos

Creados:

- `BaseDatos/Demo/Heladeria_DatosDemo.sql`
- `Heladeria.Bootstrap/Demo/ComandoDatosDemo.cs`
- `Heladeria.Bootstrap/Demo/CargadorDatosDemo.cs`
- `Heladeria.Bootstrap/Demo/CargadorDatosDemo.Catalogo.cs`
- `Heladeria.Bootstrap/Demo/CargadorDatosDemo.Operaciones.cs`
- `Heladeria.Bootstrap/Demo/TransaccionesDemo.cs`
- `Heladeria.Pruebas/Demo/DatosDemoPruebas.cs`
- `docs/DATOS_DEMO.md`

Modificados:

- `Heladeria.Bootstrap/Program.cs`: rama explícita del comando opcional.
- `Heladeria.Pruebas/Heladeria.Pruebas.csproj`: referencia al proyecto Bootstrap para probar el cargador real.
- `docs/AJUSTES_FINALES.md`: solo referencia a esta guía.
