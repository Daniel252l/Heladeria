# Validación opt-in. Crea exclusivamente una instancia LocalDB desechable y vacía.
# Nunca lee la conexión de la API, appsettings ni secretos; nunca ejecuta bootstrap.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$localDb = (Get-Command SqlLocalDB.exe -ErrorAction Stop).Source
$sqlCmd = (Get-Command sqlcmd.exe -ErrorAction Stop).Source
$instanciaPrueba = 'HeladeriaF7Test_' + [Guid]::NewGuid().ToString('N').Substring(0, 12)
$servidorPrueba = '(localdb)\' + $instanciaPrueba
$directorioBd = Split-Path $PSScriptRoot -Parent
$maestro = Get-Content -Raw -LiteralPath (Join-Path $directorioBd 'Heladeria.sql')
$reparacion = Get-Content -Raw -LiteralPath (Join-Path $directorioBd 'Reparaciones\20261007_Fase7_VentasPos.sql')
$marca = '-- Ampliaciones Database First de Fase 7, equivalentes al script de reparación.'
$posicion = $maestro.IndexOf($marca, [StringComparison]::Ordinal)
if ($posicion -lt 0) { throw 'No se encontró el límite del esquema anterior a Fase 7.' }
$anterior = $maestro.Substring(0, $posicion)
$basesCreadas = [System.Collections.Generic.List[string]]::new()
$instanciaCreada = $false
function Ejecutar-SqlPrueba([string] $base, [string] $sql) {
    $salida = $sql | & $sqlCmd -S $servidorPrueba -E -I -d $base -b -r 1 -l 30 -t 120 -f 65001 2>&1
    if ($LASTEXITCODE -ne 0) { throw ($salida -join [Environment]::NewLine) }
    $salida | Where-Object { $_ -match 'FASE 7|VALIDADO' } | Write-Output
}
function Crear-EsquemaPrueba([string] $base, [string] $script) {
    if ($base -notmatch '^HeladeriaF7_(Previa|Parcial|Maestro)$') { throw 'Nombre de BD de prueba no permitido.' }
    $aislado = $script.Replace('CREATE DATABASE Heladeria;', "CREATE DATABASE [$base];").Replace('USE Heladeria;', "USE [$base];")
    if ($aislado -match '(?im)^\s*(USE|CREATE DATABASE)\s+Heladeria\s*;') { throw 'Quedó una referencia a la BD real.' }
    $basesCreadas.Add($base)
    Ejecutar-SqlPrueba 'master' $aislado
}
try {
    & $localDb create $instanciaPrueba
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear instancia aislada.' }
    $instanciaCreada = $true
    & $localDb start $instanciaPrueba
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo iniciar instancia aislada.' }
    Write-Output "Instancia desechable: $servidorPrueba"

    Crear-EsquemaPrueba 'HeladeriaF7_Previa' $anterior
    Ejecutar-SqlPrueba 'HeladeriaF7_Previa' $reparacion
    Ejecutar-SqlPrueba 'HeladeriaF7_Previa' $reparacion
    Write-Output 'VALIDADO: esquema previo + actualización + segunda ejecución.'

    Crear-EsquemaPrueba 'HeladeriaF7_Parcial' $anterior
    Ejecutar-SqlPrueba 'HeladeriaF7_Parcial' @'
ALTER TABLE dbo.Ventas ADD BodegaId BIGINT NULL, VersionFila ROWVERSION NOT NULL;
ALTER TABLE dbo.VentasDetalle ADD FactorConversion DECIMAL(19,6) NULL, CantidadBase INT NULL;
'@
    $rechazoEsperado = $false
    try { Ejecutar-SqlPrueba 'HeladeriaF7_Parcial' $reparacion }
    catch {
        if ($_.Exception.Message -notmatch 'CantidadBase incompatible') { throw }
        $rechazoEsperado = $true
    }
    if (-not $rechazoEsperado) { throw 'El script aceptó una columna incompatible.' }
    Ejecutar-SqlPrueba 'HeladeriaF7_Parcial' @'
IF COL_LENGTH(N'dbo.Ventas', N'UbicacionId') IS NOT NULL THROW 51071, 'Falló el rollback del DDL.', 1;
ALTER TABLE dbo.VentasDetalle ALTER COLUMN CantidadBase DECIMAL(19,6) NULL;
PRINT N'VALIDADO: columna incompatible rechazada y rollback completo del DDL nuevo.';
'@
    Ejecutar-SqlPrueba 'HeladeriaF7_Parcial' $reparacion
    Ejecutar-SqlPrueba 'HeladeriaF7_Parcial' $reparacion
    Write-Output 'VALIDADO: ejecución parcial compatible + actualización + segunda ejecución.'

    Crear-EsquemaPrueba 'HeladeriaF7_Maestro' $maestro
    Ejecutar-SqlPrueba 'HeladeriaF7_Maestro' $reparacion
    foreach ($base in $basesCreadas) {
        Ejecutar-SqlPrueba $base @'
IF COL_LENGTH(N'dbo.Ventas', N'VersionFila') <> 8 THROW 51071, 'Falta rowversion.', 1;
IF COL_LENGTH(N'dbo.VentasDetalle', N'CantidadBase') IS NULL THROW 51071, 'Falta snapshot.', 1;
IF OBJECT_ID(N'dbo.VentaDetalleImpuestos', N'U') IS NULL THROW 51071, 'Falta tabla de impuestos por línea.', 1;
IF EXISTS(SELECT 1 FROM dbo.Ventas) THROW 51071, 'Se crearon datos de negocio indebidamente.', 1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) THROW 51071, 'FK no verificada.', 1;
PRINT N'VALIDADO: esquema íntegro, claves verificadas y ninguna venta creada.';
'@
    }
    Write-Output 'VALIDADO: maestro coherente con reparación. Ninguna conexión a la BD real ni bootstrap ejecutado.'
}
finally {
    if ($instanciaCreada) {
        foreach ($base in $basesCreadas) {
            # Destinos literales, permitidos arriba, en instancia que acaba de crear este proceso.
            Ejecutar-SqlPrueba 'master' "IF DB_ID(N'$base') IS NOT NULL BEGIN ALTER DATABASE [$base] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$base]; END;"
        }
        & $localDb stop $instanciaPrueba
        & $localDb delete $instanciaPrueba
        Write-Output 'Limpieza: bases e instancia desechables eliminadas; datos reales intactos.'
    }
}
