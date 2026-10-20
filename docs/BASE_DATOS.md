# Base de datos e instalación limpia

## Alcance del maestro

`BaseDatos/Heladeria.sql` es el maestro de instalación limpia de **Heladeria**, SQL Server / Database First. Se conserva sin cambiar el esquema aprobado y se entrega idéntico en `artifacts/database/Heladeria.sql`.

Contiene CREATE DATABASE/USE, 128 tablas finales, PK/FK, índices, UNIQUE/CHECK/defaults y rowversion. Producción/recetas ya incorpora Fase 6 en CREATE TABLE; los bloques finales incorporan los ajustes 7–9. Fase 10 no necesita nuevas tablas. No existen triggers propios que deban agregarse. Las pruebas comparan las 128 tablas reales con EF y ejecutan los scripts en SQL Server LocalDB aislado.

Separación deliberada:

- **Estructura:** maestro SQL, sin INSERT de datos operativos ni catálogos de usuario. Sus INSERT en variables de tabla son metadata transitoria para validar DDL.
- **Datos estructurales:** Bootstrap y preparadores, que resuelven IDs reales, hash/contraseña, permisos ADMIN y marcadores en transacciones. Se mantienen ahí para no duplicar fuentes ni crear contraseñas en SQL.
- **Configuración del negocio:** empresa/sucursal reales, moneda base, cajas, productos/precios, impuestos/catálogos decididos por el propietario. No se inventan ni se extraen de validaciones.
- **Demostración:** fixtures de pruebas, usuario consulta opcional y reparación de costos de prueba. No forman parte de la entrega de BD.

El maestro **no es una migración ni es idempotente**: requiere que no exista una BD llamada Heladeria. No ejecutar sobre una instalación existente. Los bloques 7–9 son reparaciones transaccionales con comprobaciones de compatibilidad; ello NO hace transaccional/repetible todo el CREATE inicial. No ejecutar además todas las reparaciones sobre una instalación nueva: ya están incorporadas cuando corresponden.

## Instalación desde cero — operador autorizado

1. Elegir servidor/instancia de destino y comprobar certificado, identidad y ausencia de la BD. No usar la conexión de desarrollo por accidente. Separar credencial de instalación de la API.
2. El maestro crea la BD: no crear previamente otra vacía llamada Heladeria. Revisar que los directorios/configuración predeterminados de SQL Server sean adecuados (almacenamiento, collation, recuperación y backup son decisiones DBA).
3. Ejecutar con una herramienta que **aborte ante errores de batch**. Ejemplo para `sqlcmd` ODBC, en una consola administrativa con identidad Windows autorizada; sustituir el servidor:

```powershell
sqlcmd -S tcp:sql.heladeria.ejemplo.com,1433 -E -N -d master -b -V 16 -Q "IF DB_ID(N'Heladeria') IS NOT NULL THROW 51000, 'La BD ya existe; no ejecutar instalacion limpia.', 1;"
if ($LASTEXITCODE -ne 0) { throw 'Destino no vacío o conexión no válida.' }
sqlcmd -S tcp:sql.heladeria.ejemplo.com,1433 -E -N -d master -b -V 16 -i .\BaseDatos\Heladeria.sql
if ($LASTEXITCODE -ne 0) { throw 'Instalación incompleta. No arrancar API ni Bootstrap; revisar con DBA.' }
```

No añadir `-C` para saltar validación TLS ni contraseña en argumentos. Confirmar sintaxis de TLS para la variante instalada de sqlcmd. No usar SSMS “continuar tras errores” para aplicar el maestro indiscriminadamente. Si falla el CREATE inicial pueden existir objetos parciales: detenerse, inspeccionar esa instalación nueva y decidir recuperación con el DBA; no volver a ejecutar a ciegas ni borrar una BD existente automáticamente.

4. Resultado esperado: mensajes de esquema de fases 7, 8 y 9 preparado/validado; 128 tablas sin operaciones de negocio. Ejecutar consultas de lectura en la conexión elegida:

```sql
SELECT DB_NAME() AS BaseActual;
SELECT COUNT(*) AS Tablas FROM sys.tables;
SELECT name FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1;
SELECT name FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1;
```

5. Configurar externamente `ConnectionStrings__Heladeria`, opciones de seguridad y entorno Production para el CLI; comprobar destino sin imprimir la conexión/contraseña.

## Bootstrap y orden final

Usar la publicación `artifacts/bootstrap` en terminal interactiva, fuera del servidor web público. Nunca redirigir una contraseña, pasarla como argumento o guardarla en scripts/logs. No ejecutar contra una instalación administrativa existente.

```powershell
$env:DOTNET_ENVIRONMENT = 'Production'
# La conexión se suministra desde el proveedor seguro de la sesión; no está en el paquete.
dotnet .\artifacts\bootstrap\Heladeria.Bootstrap.dll
dotnet .\artifacts\bootstrap\Heladeria.Bootstrap.dll --preparar-fase3
dotnet .\artifacts\bootstrap\Heladeria.Bootstrap.dll --preparar-fase4
dotnet .\artifacts\bootstrap\Heladeria.Bootstrap.dll --preparar-fase5
dotnet .\artifacts\bootstrap\Heladeria.Bootstrap.dll --preparar-fase6
dotnet .\artifacts\bootstrap\Heladeria.Bootstrap.dll --preparar-fase7
dotnet .\artifacts\bootstrap\Heladeria.Bootstrap.dll --preparar-fase8
dotnet .\artifacts\bootstrap\Heladeria.Bootstrap.dll --preparar-fase9
dotnet .\artifacts\bootstrap\Heladeria.Bootstrap.dll --preparar-fase10
```

**Ejecutar/revisar cada línea individualmente**, no pegar la secuencia entera sin comprobar cada salida. Cada proceso solicita su confirmación (`CREAR`, luego `PREPARAR FASE N`). Sin argumentos se crea el usuario `admin`, correo `admin@heladeria.local`, rol ADMIN, y se pide contraseña oculta dos veces. La contraseña la decide el operador; no usar credenciales de prueba. En desarrollo equivale a `dotnet run --project Heladeria.Bootstrap` y sus opciones, usando el perfil Development. En producción usar el DLL publicado, no launchSettings.

| Paso | Datos creados / comportamiento al repetir |
|---|---|
| ADMIN | Estado/clases/tipos/nivel, SEGURIDAD, ADMIN/ADMINISTRAR y permiso, empresa, proveedor técnico PROV-INICIAL, sucursal, usuario, asignación principal, marcador. Rechaza si ya hay instalación/rol/admin o conflicto |
| 3 | CONFIGURACION.ADMINISTRAR y marcador; rechaza repetición o preparación parcial |
| 4 | Inventario/productos, permisos, catálogos/flujo y bodega/ubicación inicial; rechaza repetición o preparación parcial |
| 5 | Compras/proveedores: permisos, flujos, concepto, series/correlativos; compatible → no duplica |
| 6 | Producción/recetas: permisos, flujo, conceptos y series; compatible → no duplica |
| 7 | Ventas/POS: permisos, flujo, conceptos y series; compatible → no duplica |
| 8 | Caja: permisos, flujo, tipos, series, EFECTIVO/TARJETA; compatible → no duplica |
| 9 | CxC/CxP: permisos, flujos y conceptos; requiere preparación de Caja; compatible → no duplica |
| 10 | REPORTES.CONSULTAR y AUDITORIA.CONSULTAR para ADMIN; compatible → no duplica |

No se crea `--preparar-produccion`: envolver todo aparentando una transacción/idempotencia única sería incorrecto. Los preparadores aprobados poseen límites transaccionales propios y 3/4 son de ejecución única. Mantener la secuencia explícita evita cambiar esas garantías y permisos en un cierre. Cada paso es atómico, pero **la secuencia completa no es una sola transacción**. Si falla un paso, los anteriores exitosos permanecen; comprobar marcadores y reanudar únicamente el paso fallido tras resolver su causa. Nunca limpiar marcadores para forzar una repetición.

ADMIN tiene PasswordHasher registrado, SecurityStamp aleatorio, contraseña temporal y cambio obligatorio al primer login. PROV-INICIAL satisface la FK obligatoria de sucursal; es técnico, no un proveedor comercial ficticio. No eliminarlo ni usarlo para compras reales sin revisión. No ejecutar `--crear-usuario-consulta` en producción: es una utilidad de validación 403, no un usuario requerido.

La prueba `CierreTecnicoPruebas.InstalacionLimpiaYPreparadoresCompletos_SinDatosDemoNiOperaciones` ejecuta el maestro, todos los pasos, los rechazos de repetición y la idempotencia 5–10, con contextos separados como el CLI. Verifica ausencia de clientes, productos, monedas, cajas, ventas, compras, producción, pagos, cuentas, movimientos, Kardex y sesiones. Solo usa instancia/BD LocalDB GUID desechable.

## Configuración real posterior (antes de operar)

Cambiar la contraseña temporal e identificar empresa/sucursal con los datos del propietario mediante los mantenimientos existentes. Revisar los registros técnicos y la bodega/ubicación inicial, sin inactivar dependencias administrativas.

**Moneda base:** el esquema la contempla, pero ni el maestro ni los preparadores crean una moneda y no existe mantenimiento HTTP de Monedas en el alcance aprobado. Un DBA debe registrar, con autorización del propietario, la moneda real de cada empresa mediante procedimiento controlado/auditado. No copiar la fixture GTQ ni hardcodear IDs. Columnas reales obligatorias: EmpresaId, CodigoISO (3), NombreMoneda (50), Simbolo (5), EsMonedaBase, Decimales, EstadoId. Id es identity; FK a Empresa/Estados; UNIQUE EmpresaId+CodigoISO. La aplicación financiera actual exige moneda base operativa de dos decimales y no convierte FX en esos flujos. Resolver Empresa/Estado real y validar unicidad de moneda base antes de escribir. Este cierre no decide esos valores ni hace esa escritura.

Consulta de lectura para revisión:

```sql
SELECT e.Id AS EmpresaId,e.RazonSocial,m.Id AS MonedaId,m.CodigoISO,
       m.EsMonedaBase,m.Decimales,s.Nombre AS Estado
FROM dbo.Empresas e
LEFT JOIN dbo.Monedas m ON m.EmpresaId=e.Id
LEFT JOIN dbo.Estados s ON s.Id=m.EstadoId;
SELECT Accion,Fecha FROM dbo.Bitacoras
WHERE Entidad='Instalacion' ORDER BY Fecha,Id;
```

Crear cajas físicas reales mediante el mantenimiento existente solo si son necesarias; no abrir sesiones ni ingresar saldos inventados. Configurar productos, unidades, precios, impuestos y demás datos propios con sus APIs/mantenimientos aprobados. Una instalación vacía no equivale a una empresa configurada para vender.

## Backup y restauración

**Solo ejemplos para el DBA; no ejecutados por este cierre.** Usar rutas accesibles por la identidad del servicio SQL, espacio suficiente, archivos únicos, ACL, cifrado y copia fuera del servidor. No guardar backups en Git ni en el sitio web.

```sql
-- Backup extraordinario previo a despliegue: no altera la base diferencial habitual.
BACKUP DATABASE [Heladeria]
TO DISK = N'D:\RespaldosSQL\Heladeria_full_AAAAMMDD_HHMM.bak'
WITH COPY_ONLY, CHECKSUM, STATS=10;

RESTORE VERIFYONLY
FROM DISK = N'D:\RespaldosSQL\Heladeria_full_AAAAMMDD_HHMM.bak'
WITH CHECKSUM;
RESTORE FILELISTONLY
FROM DISK = N'D:\RespaldosSQL\Heladeria_full_AAAAMMDD_HHMM.bak';
```

Sustituir fecha/ruta; no usar INIT/FORMAT ni sobrescribir respaldos por comodidad. COPY_ONLY no sustituye el plan periódico full/diferencial/log. Definir recuperación SIMPLE/FULL y cadena de logs con DBA según RPO/RTO.

Restaurar en **OTRA instancia**, hacia una BD de ensayo cuyo nombre no exista y rutas nuevas. Tomar los nombres lógicos de FILELISTONLY; repetir MOVE para cada archivo. Ejemplo NO listo para ejecutar hasta sustituirlos:

```sql
IF DB_ID(N'Heladeria_EnsayoRestore') IS NOT NULL
    THROW 51000, 'La base de ensayo ya existe; no sobrescribir.', 1;
RESTORE DATABASE [Heladeria_EnsayoRestore]
FROM DISK = N'D:\RespaldosSQL\Heladeria_full_AAAAMMDD_HHMM.bak'
WITH MOVE N'<nombre_logico_datos>' TO N'D:\EnsayoSQL\Heladeria_EnsayoRestore.mdf',
     MOVE N'<nombre_logico_log>' TO N'D:\EnsayoSQL\Heladeria_EnsayoRestore_log.ldf',
     CHECKSUM, RECOVERY, STATS=10;
DBCC CHECKDB ([Heladeria_EnsayoRestore]) WITH NO_INFOMSGS;
```

No usar WITH REPLACE. VERIFYONLY no prueba integridad lógica ni sustituye una restauración real: validar CHECKDB, conteos, relaciones y consultas de lectura en el ensayo. [Limitaciones oficiales](https://learn.microsoft.com/es-es/sql/t-sql/statements/restore-statements-verifyonly-transact-sql?view=sql-server-ver17).

- [ ] Backup previo disponible, archivo verificado y copia externa protegida.
- [ ] Restauración ensayada en instancia aislada, tiempos medidos y responsable asignado.
- [ ] Certificados/claves necesarios para descifrar respaldados separadamente.
- [ ] RPO/RTO, retención y recuperación punto-en-tiempo acordados.
- [ ] Restauración productiva exige autorización explícita; puede perder datos posteriores al backup.

## Actualizaciones futuras y Database First

Los archivos bajo Reparaciones son historial de cambios específicos, no un instalador indiscriminado. La reparación `20260930_RepararCostosPruebaProducto1.sql` depende de una historia de pruebas y no debe aplicarse a producción ni al maestro. No borrar datos de validación automáticamente; ver DATOS_DE_PRUEBA.

Ante un cambio aprobado de esquema: diseñar script versionado con precondiciones, backup y ensayo sobre copia aislada; actualizar maestro y pruebas; ejecutar scaffold controlado; revisar el diff de entidades, DbContext, FKs, precisión, nulabilidad y RowVersion. No editar modelos generados como sustituto del DDL ni ejecutar migrations/EnsureCreated de EF en producción. Usar el tool local `dotnet-ef` fijado por `.config/dotnet-tools.json`, configuración externa y `--no-onconfiguring` para no incrustar conexiones. Conservar personalizaciones fuera del código generado. No se re-scaffoldeó en este cierre.
