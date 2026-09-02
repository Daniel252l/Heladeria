# Revisión de arquitectura y esquema — Fase 1

## Resultado del análisis

El archivo `BaseDatos/Heladeria.sql` y la base local `VictusPabloE/Heladeria` contienen las mismas 127 tablas. La base inspeccionada presenta:

- 127 claves primarias; todas las tablas tienen PK.
- 481 claves foráneas.
- 133 restricciones `UNIQUE`.
- 113 restricciones `CHECK`.
- 182 índices adicionales a PK y `UNIQUE`.
- 2 restricciones `DEFAULT`.
- 21 columnas `VersionFila` de tipo `rowversion`.
- 0 eliminaciones o actualizaciones en cascada.
- 0 vistas, procedimientos almacenados o triggers.

El mapeo EF Core contiene 127 entidades y 127 `DbSet`. Las 21 columnas de concurrencia están configuradas con `IsRowVersion()`.

## Decisiones de esta fase

1. El modelo generado vive en Infraestructura porque representa persistencia y no debe contaminar el Dominio. Se desactivó el pluralizador inglés para conservar los nombres españoles exactos del esquema.
2. La configuración de EF Core está fuera del archivo generado, en `InyeccionDependencias.cs`, para conservarla al repetir el scaffold.
3. No se generaron migraciones ni se alteró la base.
4. La cadena de desarrollo utiliza configuración estándar; producción debe suministrarla mediante secretos o variables de entorno.
5. Se agregó una comprobación de salud que confirma acceso a SQL Server sin exponer la cadena ni detalles internos.
6. La prueba de integración valida conexión, cantidad de tablas y correspondencia con el modelo EF.

## Reglas deducidas del esquema

- `Estados`, `Tipos`, `FlujosEstados`, `EstadosFlujo` y `EstadoTransiciones` forman un sistema configurable de estados; los flujos no deben reemplazarse por enumeraciones arbitrarias.
- `Roles`, `Permisos`, `RolesPermisos`, `UsuariosSucursales`, sesiones e historial ya proporcionan la base de seguridad para la Fase 2.
- Empresa, sucursal y bodega aparecen de forma explícita en los agregados operativos. Los identificadores enviados por el cliente deberán validarse contra el contexto autorizado del usuario.
- La inexistencia de cascadas exige ordenar explícitamente las operaciones relacionadas y favorece anulaciones/reversiones en vez de borrados físicos.
- Al no existir triggers ni procedimientos, las transacciones y reglas multioperación deberán implementarse en los servicios de aplicación sobre EF Core.
- La escasez de valores `DEFAULT` implica que la aplicación deberá aportar de forma explícita la mayoría de los campos requeridos.
- Las columnas `VersionFila` deben conservarse como tokens de concurrencia en inventario, catálogos y correlativos.

## Límites respetados

No se implementaron login, autorización, productos, inventario, compras, producción, ventas, caja, impresión, FEL, delivery ni mermas. Esas capacidades quedan para sus fases autorizadas.
