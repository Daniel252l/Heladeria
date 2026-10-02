# Fase 6 — Producción y recetas

## Diagnóstico y decisión de modelo

El esquema base ya contenía `Recetas`, `RecetaDetalles`, `OrdenesProduccion`,
`OrdenProduccionDetalles`, `ConsumosProduccion` y `ProduccionTerminada`; por eso
Fase 6 no crea tablas paralelas. Esas tablas no conservaban el factor ni la
cantidad base que justifican una conversión histórica, tampoco tenían
`rowversion` en receta/orden y una orden podía tener más de una fila en
`ProduccionTerminada`.

`BaseDatos/Reparaciones/20261002_Fase6_ProduccionRecetas.sql` amplía las tablas
existentes de forma transaccional:

- guarda presentación opcional, factor y cantidad/rendimiento base en receta y orden;
- agrega `Observacion` al ingrediente;
- agrega `VersionFila` a receta y orden;
- asegura una sola receta vigente por producto y una sola finalización por orden;
- reconstruye snapshots históricos solo cuando la unidad base o una única
  presentación hacen inequívoca la conversión; ante ambigüedad ejecuta `THROW`
  y revierte todo.

El script no crea permisos, catálogos ni datos de negocio y la aplicación no lo
ejecuta automáticamente. `BaseDatos/Heladeria.sql` contiene el mismo modelo para
una instalación nueva. El modelo EF Database First quedó sincronizado con esas
columnas y restricciones.

## Aplicación controlada

1. Detener escrituras de la API y hacer respaldo.
2. Ejecutar manualmente en SQL Server:

   ```text
   BaseDatos/Reparaciones/20261002_Fase6_ProduccionRecetas.sql
   ```

3. Iniciar la preparación explícita:

   ```powershell
   dotnet run --configuration Release --project Heladeria.Bootstrap -- --preparar-fase6
   ```

4. Escribir exactamente `PREPARAR FASE 6`.

El preparador requiere Fase 4 operativa, toma bloqueo transaccional, es
idempotente y no reinicia correlativos. Crea o valida únicamente:

- módulos `PRODUCCION` y `RECETAS`;
- permisos `PRODUCCION.CONSULTAR`, `PRODUCCION.ADMINISTRAR`,
  `PRODUCCION.PRODUCIR`, `RECETAS.CONSULTAR` y `RECETAS.ADMINISTRAR`, asignados
  solo a `ADMIN`;
- flujo `ORDENES_PRODUCCION`: `BORRADOR -> CONFIRMADA -> FINALIZADA`, más
  anulación desde borrador o confirmada;
- conceptos automáticos `PRODUCCION-CONSUMO` y `PRODUCCION-ENTRADA`;
- series/correlativos `OP`, `PC` y `PT`;
- marcador de bitácora `FASE6_CONFIGURADA`.

No crea recetas, órdenes, productos, presentaciones ni existencias.

## Contrato funcional

Una receta vigente es única por empresa/producto. Una receta todavía no usada
se corrige en la misma versión. Cuando ya existe una orden que la referencia,
la versión queda histórica y se crea la siguiente; la versión histórica es
inmutable. Se rechazan ingredientes duplicados, productos propios y ciclos
indirectos entre recetas. Toda cantidad conserva unidad solicitada,
presentación, factor y cantidad base.

Una orden se crea en borrador y copia los ingredientes escalados de la receta.
Confirmar no afecta inventario. Finalizar, dentro de una sola transacción
`Serializable`, hace una validación previa de todo el stock y después:

1. registra una salida por cada ingrediente mediante el motor de inventario;
2. registra `ConsumosProduccion` al costo promedio base real;
3. suma solamente materia prima;
4. registra la entrada base del producto terminado con ese costo unitario;
5. deja que el motor calcule el nuevo promedio, Kardex, `CostosProductos` e
   `HistoricoCostosProductos`;
6. crea una única `ProduccionTerminada` y cierra la orden.

El mismo bloqueo empresarial `Heladeria.Inventario.{EmpresaId}` que usan ajustes
y compras coordina producción. Los productos se ordenan por Id antes de afectar
stock. `RowVersion`, la clave UUID de finalización, el estado final y el índice
único de salida impiden una aplicación doble. Esta fase admite únicamente el
alcance ya soportado por el motor: productos sin lote/serie/vencimiento y con
valoración `PROMEDIO`.

## Endpoints

- `GET /api/recetas`
- `GET /api/recetas/{id}`
- `GET /api/recetas/productos/{productoId}/presentaciones`
- `POST /api/recetas`
- `PUT /api/recetas/{id}`
- `GET /api/produccion/referencias`
- `GET /api/produccion`
- `GET /api/produccion/{id}`
- `POST /api/produccion`
- `PUT /api/produccion/{id}`
- `POST /api/produccion/{id}/confirmar`
- `POST /api/produccion/{id}/finalizar`
- `POST /api/produccion/{id}/anular`

Los endpoints aplican JWT, permiso específico y alcance empresa/sucursal. Las
escrituras que cambian receta u orden reciben `VersionFila` en Base64. Finalizar
también exige `ClaveOperacion` UUID y la ubicación de la bodega de la orden.

## Verificación SQL posterior a la preparación

```sql
SELECT p.Codigo, m.Id AS Modulo, a.Codigo AS Accion, rp.Permitido
FROM Permisos p
JOIN Modulos m ON m.Id = p.ModuloId
JOIN AccionesSistema a ON a.Id = p.AccionId
JOIN RolesPermisos rp ON rp.PermisoId = p.Id AND rp.RoleId = 'ADMIN'
WHERE p.Codigo LIKE 'PRODUCCION.%' OR p.Codigo LIKE 'RECETAS.%';

SELECT f.Codigo AS Flujo, e.Codigo, e.EsInicial, e.EsFinal, e.PermiteEdicion
FROM EstadosFlujo e
JOIN FlujosEstados f ON f.Id = e.FlujoEstadoId
WHERE f.Codigo = 'ORDENES_PRODUCCION'
ORDER BY e.Orden;

SELECT Codigo, TipoOperacion, RequiereReferencia, AfectaCosto, EsAutomatico
FROM ConceptosMovimientos
WHERE Codigo IN ('PRODUCCION-CONSUMO', 'PRODUCCION-ENTRADA');

SELECT sd.Serie, sd.Prefijo, sd.Longitud, cd.UltimoNumero
FROM SeriesDocumentos sd
JOIN CorrelativosDocumentos cd ON cd.SerieDocumentoId = sd.Id
WHERE sd.Serie IN ('OP', 'PC', 'PT');
```

Estas consultas solo verifican infraestructura. No se deben insertar datos de
prueba automáticamente.
