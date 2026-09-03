# Bootstrap administrativo inicial

El bootstrap se implementa como una herramienta CLI separada. La API nunca lo ejecuta durante su arranque y la herramienta no acepta la contraseña mediante argumentos, configuración ni archivos.

## Ejecución

Desde la raíz de la solución (`C:\Pablo\UMG\Desarrollo web\Heladeria\Heladeria`):

```powershell
dotnet run --project .\Heladeria.Bootstrap\Heladeria.Bootstrap.csproj --configuration Release --launch-profile Bootstrap
```

La herramienta muestra el usuario `admin` y el correo `admin@heladeria.local`, exige escribir `CREAR` y después solicita la contraseña y su confirmación sin eco. `Ctrl+C` o `Escape` cancelan la operación.

El perfil `Bootstrap` usa el entorno `Development` y, por tanto, la conexión definida para desarrollo. En otro entorno debe configurarse `ConnectionStrings__Heladeria` mediante el proveedor de configuración autorizado.

## Garantías

- Ejecución exclusivamente explícita mediante el proyecto CLI.
- Transacción con aislamiento `Serializable`.
- Reintentos automáticos de conexión desactivados en esta herramienta para no interferir con la transacción manual; un fallo revierte y requiere volver a ejecutar el comando.
- Bloqueo exclusivo de SQL Server mediante `sys.sp_getapplock`, propiedad de la transacción.
- Rechazo si existe un usuario administrador/identidad inicial, el rol `ADMIN`, el marcador de instalación o una instalación de seguridad parcial.
- Validación previa del contrato de claves y relaciones de EF Core para todas las entidades usadas por el bootstrap.
- Los IDs `IDENTITY` son generados por SQL Server y leídos por EF Core.
- Los IDs obligatorios que no son `IDENTITY` se calculan dentro de la transacción después de adquirir el bloqueo; no se presuponen valores fijos.
- La sucursal referencia un proveedor técnico creado para la empresa inicial; nunca se presupone un `ProveedorId`.
- Cualquier excepción anterior al commit revierte toda la creación.
- La finalización se marca en `Bitacoras` con `Entidad = 'Instalacion'` y `Accion = 'BOOTSTRAP_ADMIN_COMPLETADO'`.
- No modifica el esquema ni `BaseDatos/Heladeria.sql`.

## Contraseña

La entrada se captura con `Console.ReadKey(intercept: true)` y no se muestra. Se exige mínimo de 10 caracteres, mayúscula, minúscula, número y carácter especial. La confirmación se compara en tiempo constante y los búferes auxiliares se limpian.

El servicio recibe el mismo `IPasswordHasher<Usuarios>` registrado por la infraestructura y ejecuta:

```csharp
usuario.PasswordHash = passwordHasher.HashPassword(usuario, contrasena);
```

Solo el hash se persiste. También se guarda un `SecurityStamp` aleatorio de 32 bytes; `ContraseniaTemporal = 1`, `UltimoCambioDeContrasenia = NULL` y `PasswordTemporal = NULL`. El primer login queda limitado a cambiar contraseña o cerrar sesión.

## Registros creados

Una ejecución exitosa crea exactamente 17 filas y muestra en consola los IDs reales asignados:

| Orden | Tabla | Clave/valor |
|---:|---|---|
| 1 | `ClaseEstados` | Siguiente ID disponible; `Seguridad`, activo. |
| 2 | `Estados` | Siguiente ID disponible; `Activo`, activo. |
| 3 | `Modulos` | `SEGURIDAD`, nombre `Seguridad`, activo. |
| 4 | `ClaseTipos` | Siguiente ID disponible; `Seguridad`. |
| 5 | `Tipos` | Siguiente ID disponible; `Rol administrador`. |
| 6 | `Tipos` | Siguiente ID disponible; `Inicio de sesión local`. |
| 7 | `NivelesDeAccesos` | Siguiente ID disponible; `Administrador`. |
| 8 | `Roles` | `ADMIN`, nombre `Administrador`, vencimiento 12 meses. |
| 9 | `AccionesSistema` | ID generado; `ADMINISTRAR`. |
| 10 | `Permisos` | ID generado; `SEGURIDAD.ADMINISTRAR`. |
| 11 | `RolesPermisos` | ID generado; `ADMIN` → `SEGURIDAD.ADMINISTRAR`, permitido. |
| 12 | `Empresas` | ID generado; `Heladeria Inicial`, activa. |
| 13 | `Proveedores` | ID generado; `PROV-INICIAL`, `Proveedor inicial`, asociado a la empresa y activo. |
| 14 | `Sucursales` | ID generado; `Sucursal Central`, activa y central, asociada al proveedor inicial. |
| 15 | `Usuarios` | ID generado; `admin`, `admin@heladeria.local`, temporal. |
| 16 | `UsuariosSucursales` | ID generado; asignación principal y activa. |
| 17 | `Bitacoras` | ID generado; marcador de instalación completada. |

## Verificación en SQL Server

La siguiente consulta no expone hashes ni otros secretos:

```sql
USE Heladeria;
GO

SELECT
    u.Id,
    u.UserName,
    u.Email,
    u.RoleId,
    e.Nombre AS Estado,
    u.ContraseniaTemporal,
    u.UltimoCambioDeContrasenia,
    u.PasswordTemporal,
    CASE WHEN LEN(u.PasswordHash) > 0 THEN 1 ELSE 0 END AS TienePasswordHash,
    s.Id AS SucursalId,
    s.Nombre AS Sucursal,
    p.CodigoProveedor,
    p.NombreProveedor,
    us.EsPrincipal
FROM Usuarios AS u
JOIN Estados AS e ON e.Id = u.EstadoId
JOIN UsuariosSucursales AS us ON us.UsuarioId = u.Id
JOIN Sucursales AS s ON s.Id = us.SucursalId
JOIN Proveedores AS p ON p.Id = s.ProveedorId
WHERE u.UserName = 'admin';

SELECT
    r.Id AS RolId,
    r.Nombre AS Rol,
    p.Codigo AS Permiso,
    rp.Permitido
FROM Roles AS r
JOIN RolesPermisos AS rp ON rp.RoleId = r.Id
JOIN Permisos AS p ON p.Id = rp.PermisoId
WHERE r.Id = 'ADMIN';

SELECT Id, Entidad, Accion, Fecha, UsuarioId
FROM Bitacoras
WHERE Entidad = 'Instalacion'
  AND Accion = 'BOOTSTRAP_ADMIN_COMPLETADO';
```

Se espera una fila en cada resultado, `ContraseniaTemporal = 1`, `PasswordTemporal = NULL`, `TienePasswordHash = 1`, `EsPrincipal = 1`, `CodigoProveedor = 'PROV-INICIAL'`, permiso habilitado y el marcador presente.

## Usuario limitado para probar autorización 403

La misma herramienta permite crear una sola vez el usuario `consulta`, sin duplicar mecanismos ni aceptar la contraseña mediante argumentos:

```powershell
dotnet run --project .\Heladeria.Bootstrap\Heladeria.Bootstrap.csproj --configuration Release --launch-profile Bootstrap -- --crear-usuario-consulta
```

La operación exige escribir `CREAR CONSULTA` y solicita la contraseña dos veces, sin mostrarla. Reutiliza del administrador existente el nivel de acceso, el tipo de rol, el tipo de inicio de sesión, el estado activo y la sucursal principal activa. Crea exclusivamente:

1. `Roles`: `CONSULTA / Consulta`.
2. `Usuarios`: `consulta / consulta@heladeria.local`.
3. `UsuariosSucursales`: asignación principal a la misma sucursal del administrador.

No crea ninguna fila en `RolesPermisos`. Todo se guarda en una transacción `Serializable`; si falta el entorno administrativo activo, si hay más de una referencia posible o si ya existe el rol/usuario, no se realiza ningún cambio.

La siguiente consulta verifica el resultado sin mostrar el hash ni ningún secreto:

```sql
SELECT
    u.Id,
    u.UserName,
    u.Email,
    u.RoleId,
    e.Nombre AS Estado,
    u.ContraseniaTemporal,
    u.UltimoCambioDeContrasenia,
    s.Id AS SucursalId,
    s.Nombre AS Sucursal,
    us.EsPrincipal
FROM Usuarios AS u
JOIN Estados AS e ON e.Id = u.EstadoId
JOIN UsuariosSucursales AS us ON us.UsuarioId = u.Id
JOIN Sucursales AS s ON s.Id = us.SucursalId
WHERE u.UserName = 'consulta';

SELECT COUNT(*) AS PermisosAsignados
FROM RolesPermisos
WHERE RoleId = 'CONSULTA';
```

Se espera una fila de usuario activo con `ContraseniaTemporal = 0`, fecha de cambio no nula y sucursal principal; `PermisosAsignados` debe ser `0`.

## Preparación de la Fase 3

Después de tener el administrador operativo, el permiso de configuración se prepara explícitamente y una sola vez:

```powershell
dotnet run --project .\Heladeria.Bootstrap\Heladeria.Bootstrap.csproj --configuration Release --launch-profile Bootstrap -- --preparar-fase3
```

La confirmación requerida es `PREPARAR FASE 3`. Esta modalidad no solicita contraseña; crea en una transacción el módulo `CONFIGURACION`, el permiso `CONFIGURACION.ADMINISTRAR`, su asignación permitida al rol `ADMIN` y el marcador `FASE3_CONFIGURADA`. No se ejecuta al iniciar la API. El detalle y la consulta de verificación están en `docs/fase-3-configuracion.md`.
