/*
  HELADERIA — DATOS DEMOSTRATIVOS OPCIONALES, V1

  Este archivo NO inserta filas y NO forma parte del maestro Heladeria.sql.
  La carga completa se implementa en Heladeria.Bootstrap/Demo para reutilizar
  los servicios aprobados de Productos, Compras, Ventas, Caja y Cartera.
  Insertar directamente ventas, pagos, saldos o Kardex omitiría invariantes.

  Desde la carpeta que contiene Heladeria.sln, en PowerShell:

    $env:DOTNET_ENVIRONMENT = 'Development'
    dotnet run --project Heladeria.Bootstrap -- --cargar-datos-demo

  La conexión se obtiene de la configuración existente / User Secrets /
  ConnectionStrings__Heladeria. Nunca pasar credenciales como argumentos.
  Fuera de Development se rechaza por defecto; --confirmar-demo expresa una
  autorización adicional. No usar sobre producción operativa.

  Precondiciones: esquema hasta Ajuste D y preparación estructural existente.
  El cargador NO instala esquema, NO prepara permisos, NO altera usuarios.
  Una transacción SERIALIZABLE engloba catálogo, documentos y operaciones.
  Cualquier fallo revierte toda la carga, incluida la marca CARGA_DEMO_V1.
  Repetir una carga confirmada se rechaza; no se duplica ni sobrescribe.

  35 productos, 5 composiciones, 5 proveedores, 12 clientes, 10 compras,
  38 ventas, 14 turnos CERRADOS en una caja propia, 6 CxC y 6 CxP.
  Fechas relativas a la ejecución en la zona empresarial; timestamps UTC.
  Usuarios/roles/permisos/turnos preexistentes se conservan.
  Los correlativos existentes avanzan mediante los servicios, nunca se reinician.

  No ejecutar contra VictusPabloE / Heladeria sin decisión manual del propietario.
  Pruebas de implementación exclusivamente en LocalDB aislado.
  Instrucciones y limitaciones: docs/DATOS_DEMO.md.
*/
SET NOCOUNT ON;
PRINT N'No se cargaron datos. Este SQL es la entrada documental del cargador opcional.';
PRINT N'Use: dotnet run --project Heladeria.Bootstrap -- --cargar-datos-demo';
PRINT N'Consulte docs/DATOS_DEMO.md y verifique manualmente ambiente/conexión antes de cargar.';
