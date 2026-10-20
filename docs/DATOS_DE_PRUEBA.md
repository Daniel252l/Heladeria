# Datos de validación frente a producción

No se consultó ni modificó la BD real durante el cierre técnico. No se presume que un ID, nombre o importe citado en pruebas siga representando el mismo registro.

## Identificación

- `Heladeria.Pruebas`: datos sintéticos y claves de fixtures para contextos InMemory/SQLite o instancias LocalDB privadas. No publicar el proyecto de pruebas ni appsettings.Testing.json.
- `heladeria-web/src/test`, archivos `.test.*` y scripts visuales: respuestas API simuladas y capturas. No son semillas productivas; no publicar scripts, test-results ni datos fuente.
- `--crear-usuario-consulta`: utilidad explícita para probar 403. No es parte de instalación productiva ni se ejecuta en este cierre.
- `BaseDatos/Reparaciones/20260930_RepararCostosPruebaProducto1.sql`: reparación específica de una historia de prueba, no migración general; queda fuera del artefacto de BD.
- La BD utilizada para validación manual podría contener clientes/proveedores de prueba, órdenes, ventas, recetas, producciones, existencias/Kardex/costos, pagos, CxC/CxP, sesiones de Caja, accesos y bitácoras. Su identificación requiere revisión del propietario, documentos de origen, usuarios/fechas y relaciones, no filtrar únicamente nombres o IDs.
- ADMIN, PROV-INICIAL, empresa/sucursal iniciales, bodegas y marcadores son estructurales; no tratarlos como basura por su nombre técnico.

## Decisión recomendada

Para una instalación nueva, preferir maestro + bootstrap + configuración real revisada, sin copiar la BD de validación. No copiar usuarios, hashes, refresh tokens, sesiones ni datos personales desde fixtures.

Si se requiere conservar/reutilizar una BD existente: inventariar con consultas de solo lectura, respaldo y restauración de ensayo, clasificación por propietario y análisis de dependencias financieras. Las relaciones entre pagos, cuentas, Caja, ventas, compras, inventario, costos y auditoría impiden un borrado superficial seguro.

No se proporciona DELETE masivo ni se autoriza limpiar auditoría o reiniciar correlativos. Cualquier limpieza futura debe ser una operación independiente, explícita, revisada y recuperable. Aplicar políticas de retención y privacidad a backups/logs; un ambiente de ensayo restaurado también puede contener datos personales reales.
