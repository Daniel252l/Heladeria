/*
    Reparación CONTROLADA y EXCLUSIVA de la historia documentada de ProductoId = 1.

    Secuencia admitida (ninguna otra):
      1. Detalle/Kardex 1: ENTRADA 3 L × 1000 = 3000 ML a Q20/L, mal guardado como Q20/ML.
      2. Detalle/Kardex 2: SALIDA 2 vasos × 250 = 500 ML, valorizada con el costo incorrecto Q20/ML.
      3. Detalle/Kardex 3: ENTRADA de COMPRAS 3 L × 1000 = 3000 ML a Q0.025/ML, ya correcta.

    Uso seguro:
      1. Mantenga @ConfirmarCommit = 0 y ejecute para inspeccionar ANTES/DESPUÉS.
         La ejecución de inspección termina siempre en ROLLBACK.
      2. Verifique todos los resultados.
      3. Solo si corresponden exactamente a la historia conocida, cambie el valor a 1
         y reejecute para confirmar.

    No elimina historial, no modifica cantidades, movimientos, documentos, recepciones,
    estados, relaciones ni estructura. No forma parte del bootstrap ni se ejecuta desde la API.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

DECLARE @ConfirmarCommit bit = 0;
DECLARE @ProductoId bigint = 1;
DECLARE @CostoBaseAjuste decimal(19,6) = 0.020000;
DECLARE @CostoBaseRecepcion decimal(19,6) = 0.025000;
DECLARE @CostoPromedioFinal decimal(19,6) = 0.022727;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @EmpresaId int;
    SELECT @EmpresaId = p.EmpresaId
    FROM dbo.Productos AS p WITH (UPDLOCK, HOLDLOCK)
    WHERE p.Id = @ProductoId;

    IF @EmpresaId IS NULL
        THROW 51000, N'No existe ProductoId = 1. No se realizó ningún cambio.', 1;

    IF (SELECT COUNT_BIG(*) FROM dbo.Productos WHERE Id = @ProductoId) <> 1
        THROW 51001, N'ProductoId = 1 no identifica un producto único. No se realizó ningún cambio.', 1;

    PRINT N'ANTES: historia completa del ProductoId = 1, todavía dentro de la transacción.';

    SELECT m.Id AS MovimientoId, m.ModuloOrigen, m.DocumentoOrigenId,
           m.SerieDocumento, m.NumeroDocumento, m.FechaMovimiento,
           d.Id AS MovimientoDetalleId, d.ProductoId, d.ProductoPresentacionId,
           d.Cantidad, d.FactorConversion, d.CantidadBase,
           d.CostoUnitario, d.CostoTotal, d.Observaciones,
           k.Id AS KardexId, k.TipoAfectacion, k.Cantidad AS CantidadKardex,
           k.CostoUnitario AS CostoUnitarioKardex, k.CostoTotal AS CostoTotalKardex,
           k.SaldoCantidad, k.SaldoCosto, k.BodegaId, k.UbicacionId
    FROM dbo.MovimientoDetalles AS d
    INNER JOIN dbo.Movimientos AS m
        ON m.EmpresaId = d.EmpresaId AND m.Id = d.MovimientoId
    LEFT JOIN dbo.Kardex AS k
        ON k.EmpresaId = d.EmpresaId AND k.MovimientoDetalleId = d.Id
    WHERE d.EmpresaId = @EmpresaId AND d.ProductoId = @ProductoId
    ORDER BY d.Id;

    SELECT e.Id, e.EmpresaId, e.ProductoId, e.BodegaId, e.UbicacionId,
           e.Cantidad, e.CantidadReservada, e.CostoPromedio, e.UltimoMovimiento
    FROM dbo.Existencias AS e
    WHERE e.EmpresaId = @EmpresaId AND e.ProductoId = @ProductoId;

    SELECT cp.Id, cp.EmpresaId, cp.ProductoId, cp.CostoActual,
           cp.FechaCalculo, cp.MetodoValuacionId
    FROM dbo.CostosProductos AS cp
    WHERE cp.EmpresaId = @EmpresaId AND cp.ProductoId = @ProductoId;

    SELECT h.Id, h.EmpresaId, h.ProductoId, h.CostoAnterior, h.CostoNuevo,
           h.FechaCambio, h.OrigenCambio, h.DocumentoOrigenId, h.UsuarioId
    FROM dbo.HistoricoCostosProductos AS h
    WHERE h.EmpresaId = @EmpresaId AND h.ProductoId = @ProductoId
    ORDER BY h.Id;

    /* Validación cerrada de los tres detalles y ninguna fila adicional. */
    IF (SELECT COUNT_BIG(*)
        FROM dbo.MovimientoDetalles
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId) <> 3
        THROW 51002, N'ProductoId = 1 no tiene exactamente tres MovimientoDetalles. No se realizó ningún cambio.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.MovimientoDetalles
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id NOT IN (1, 2, 3)
    )
        THROW 51003, N'Existen MovimientoDetalles distintos de 1, 2 y 3 para ProductoId = 1.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.MovimientoDetalles AS d
        INNER JOIN dbo.Movimientos AS m
            ON m.EmpresaId = d.EmpresaId AND m.Id = d.MovimientoId
        WHERE d.EmpresaId = @EmpresaId AND d.ProductoId = @ProductoId
          AND d.Id = 1 AND d.MovimientoId = 1
          AND d.ProductoPresentacionId = 1
          AND d.Cantidad = 3.000000 AND d.FactorConversion = 1000.000000
          AND d.CantidadBase = 3000.000000 AND d.CostoUnitario = 20.000000
          AND d.CostoTotal = 60000.000000
          AND d.Observaciones = 'Entrada inicial de 3 litros para prueba de multimedida'
          AND m.Id = 1 AND m.ModuloOrigen = 'AJUSTE_MANUAL'
    )
        THROW 51004, N'El MovimientoDetalle 1 no coincide exactamente con la entrada manual documentada.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.MovimientoDetalles AS d
        INNER JOIN dbo.Movimientos AS m
            ON m.EmpresaId = d.EmpresaId AND m.Id = d.MovimientoId
        WHERE d.EmpresaId = @EmpresaId AND d.ProductoId = @ProductoId
          AND d.Id = 2 AND d.MovimientoId = 2
          AND d.ProductoPresentacionId = 2
          AND d.Cantidad = 2.000000 AND d.FactorConversion = 250.000000
          AND d.CantidadBase = 500.000000 AND d.CostoUnitario = 20.000000
          AND d.CostoTotal = 10000.000000
          AND d.Observaciones = 'Salida de 2 vasos para prueba de multimedida'
          AND m.Id = 2 AND m.ModuloOrigen = 'AJUSTE_MANUAL'
    )
        THROW 51005, N'El MovimientoDetalle 2 no coincide exactamente con la salida manual documentada.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.MovimientoDetalles AS d
        INNER JOIN dbo.Movimientos AS m
            ON m.EmpresaId = d.EmpresaId AND m.Id = d.MovimientoId
        WHERE d.EmpresaId = @EmpresaId AND d.ProductoId = @ProductoId
          AND d.Id = 3 AND d.MovimientoId = 3
          AND d.ProductoPresentacionId = 1
          AND d.Cantidad = 3.000000 AND d.FactorConversion = 1000.000000
          AND d.CantidadBase = 3000.000000 AND d.CostoUnitario = @CostoBaseRecepcion
          AND d.CostoTotal = 75.000000
          AND d.Observaciones = 'Recepción parcial de 3 litros de Helado Chocolate'
          AND m.Id = 3 AND m.ModuloOrigen = 'COMPRAS'
          AND m.DocumentoOrigenId IS NOT NULL
    )
        THROW 51006, N'El MovimientoDetalle 3 no coincide exactamente con la recepción documentada.', 1;

    /* Validación cerrada de los tres Kardex y sus valores incorrectos conocidos. */
    IF (SELECT COUNT_BIG(*)
        FROM dbo.Kardex
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId) <> 3
        THROW 51007, N'ProductoId = 1 no tiene exactamente tres registros Kardex.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Kardex
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id NOT IN (1, 2, 3)
    )
        THROW 51008, N'Existen registros Kardex distintos de 1, 2 y 3 para ProductoId = 1.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Kardex
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 1 AND MovimientoDetalleId = 1 AND TipoAfectacion = 'ENTRADA'
          AND Cantidad = 3000.000000 AND CostoUnitario = 20.000000
          AND CostoTotal = 60000.000000
          AND SaldoCantidad = 3000.000000 AND SaldoCosto = 60000.000000
    )
        THROW 51009, N'Kardex 1 no coincide exactamente con el estado documentado.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Kardex
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 2 AND MovimientoDetalleId = 2 AND TipoAfectacion = 'SALIDA'
          AND Cantidad = 500.000000 AND CostoUnitario = 20.000000
          AND CostoTotal = 10000.000000
          AND SaldoCantidad = 2500.000000 AND SaldoCosto = 50000.000000
    )
        THROW 51010, N'Kardex 2 no coincide exactamente con el estado documentado.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Kardex
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 3 AND MovimientoDetalleId = 3 AND TipoAfectacion = 'ENTRADA'
          AND Cantidad = 3000.000000 AND CostoUnitario = @CostoBaseRecepcion
          AND CostoTotal = 75.000000
          AND SaldoCantidad = 5500.000000 AND SaldoCosto = 50074.997500
    )
        THROW 51011, N'Kardex 3 no coincide exactamente con el estado documentado.', 1;

    IF (SELECT COUNT_BIG(DISTINCT CONCAT(BodegaId, ':', UbicacionId))
        FROM dbo.Kardex
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId) <> 1
        THROW 51012, N'Los tres Kardex no pertenecen a una única bodega/ubicación.', 1;

    /* Existencia y costo vigente deben seguir exactamente en el estado previo conocido. */
    IF (SELECT COUNT_BIG(*) FROM dbo.Existencias
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId) <> 1
        THROW 51013, N'No existe una única existencia para ProductoId = 1.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Existencias
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Cantidad = 5500.000000 AND CostoPromedio = 9.104545
    )
        THROW 51014, N'La existencia no está en 5500 ML a costo promedio 9.104545.', 1;

    IF (SELECT COUNT_BIG(*) FROM dbo.CostosProductos
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId) <> 1
        THROW 51015, N'No existe un único CostosProductos para ProductoId = 1.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.CostosProductos
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND CostoActual = 9.104545
    )
        THROW 51016, N'CostosProductos no está en el valor previo esperado 9.104545.', 1;

    /* Los dos históricos deben existir sin filas adicionales; no se alteran sus trazas. */
    IF (SELECT COUNT_BIG(*) FROM dbo.HistoricoCostosProductos
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId) <> 2
        THROW 51017, N'ProductoId = 1 no tiene exactamente dos históricos de costo.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.HistoricoCostosProductos
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 1 AND CostoAnterior IS NULL AND CostoNuevo = 20.000000
          AND OrigenCambio = 'AJUSTE_INVENTARIO' AND DocumentoOrigenId IS NULL
    )
        THROW 51018, N'El histórico de costo 1 no coincide con el ajuste documentado.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.HistoricoCostosProductos AS h
        INNER JOIN dbo.Movimientos AS m
            ON m.EmpresaId = h.EmpresaId AND m.Id = 3
        WHERE h.EmpresaId = @EmpresaId AND h.ProductoId = @ProductoId
          AND h.Id = 2 AND h.CostoAnterior = 20.000000 AND h.CostoNuevo = 9.104545
          AND h.OrigenCambio = 'RECEPCION_COMPRA'
          AND h.DocumentoOrigenId = m.DocumentoOrigenId
    )
        THROW 51019, N'El histórico de costo 2 no coincide con la recepción documentada.', 1;

    /*
       Reparación: solo columnas de costo. Las columnas calculadas CostoTotal se
       actualizan automáticamente por SQL Server al cambiar CostoUnitario.
    */
    UPDATE dbo.MovimientoDetalles
    SET CostoUnitario = @CostoBaseAjuste
    WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId AND Id IN (1, 2);

    IF @@ROWCOUNT <> 2
        THROW 51020, N'No se actualizaron exactamente los MovimientoDetalles 1 y 2.', 1;

    /*
       SaldoCosto conserva la valoración acumulada exacta: 60 - 10 + 75 = 125.
       CostoPromedio debe almacenarse a seis decimales (0.022727); por ello
       5500 × 0.022727 = 124.998500 no se usa para degradar el saldo exacto.
    */
    UPDATE dbo.Kardex
    SET CostoUnitario = @CostoBaseAjuste,
        SaldoCosto = CASE Id
            WHEN 1 THEN 60.000000
            WHEN 2 THEN 50.000000
        END
    WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId AND Id IN (1, 2);

    IF @@ROWCOUNT <> 2
        THROW 51021, N'No se actualizaron exactamente los Kardex 1 y 2.', 1;

    UPDATE dbo.Kardex
    SET SaldoCosto = 125.000000
    WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
      AND Id = 3 AND CostoUnitario = @CostoBaseRecepcion;

    IF @@ROWCOUNT <> 1
        THROW 51031, N'No se actualizó exactamente el saldo valorizado del Kardex 3.', 1;

    UPDATE dbo.Existencias
    SET CostoPromedio = @CostoPromedioFinal
    WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId;

    IF @@ROWCOUNT <> 1
        THROW 51022, N'No se actualizó exactamente una existencia.', 1;

    UPDATE dbo.CostosProductos
    SET CostoActual = @CostoPromedioFinal
    WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId;

    IF @@ROWCOUNT <> 1
        THROW 51023, N'No se actualizó exactamente un costo vigente.', 1;

    UPDATE dbo.HistoricoCostosProductos
    SET CostoAnterior = NULL,
        CostoNuevo = @CostoBaseAjuste
    WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId AND Id = 1;

    IF @@ROWCOUNT <> 1
        THROW 51024, N'No se actualizó exactamente el histórico 1.', 1;

    UPDATE dbo.HistoricoCostosProductos
    SET CostoAnterior = @CostoBaseAjuste,
        CostoNuevo = @CostoPromedioFinal
    WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId AND Id = 2;

    IF @@ROWCOUNT <> 1
        THROW 51025, N'No se actualizó exactamente el histórico 2.', 1;

    /* Verificación completa del resultado antes de permitir COMMIT. */
    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.MovimientoDetalles
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 1 AND CostoUnitario = 0.020000 AND CostoTotal = 60.000000
    ) OR NOT EXISTS
    (
        SELECT 1 FROM dbo.MovimientoDetalles
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 2 AND CostoUnitario = 0.020000 AND CostoTotal = 10.000000
    ) OR NOT EXISTS
    (
        SELECT 1 FROM dbo.MovimientoDetalles
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 3 AND CostoUnitario = 0.025000 AND CostoTotal = 75.000000
    )
        THROW 51026, N'Los costos finales de MovimientoDetalles no coinciden con 60, 10 y 75.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Kardex
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 1 AND CostoUnitario = 0.020000 AND CostoTotal = 60.000000
          AND SaldoCantidad = 3000.000000 AND SaldoCosto = 60.000000
    ) OR NOT EXISTS
    (
        SELECT 1 FROM dbo.Kardex
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 2 AND CostoUnitario = 0.020000 AND CostoTotal = 10.000000
          AND SaldoCantidad = 2500.000000 AND SaldoCosto = 50.000000
    ) OR NOT EXISTS
    (
        SELECT 1 FROM dbo.Kardex
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 3 AND CostoUnitario = 0.025000 AND CostoTotal = 75.000000
          AND SaldoCantidad = 5500.000000 AND SaldoCosto = 125.000000
    )
        THROW 51027, N'Los tres Kardex no quedaron en los saldos valorizados 60, 50 y 125.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.Existencias
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Cantidad = 5500.000000 AND CostoPromedio = @CostoPromedioFinal
    )
        THROW 51028, N'La existencia final no quedó en 5500 ML a costo 0.022727.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.CostosProductos
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND CostoActual = @CostoPromedioFinal
    )
        THROW 51029, N'CostosProductos no quedó en 0.022727.', 1;

    IF NOT EXISTS
    (
        SELECT 1 FROM dbo.HistoricoCostosProductos
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 1 AND CostoAnterior IS NULL AND CostoNuevo = 0.020000
          AND OrigenCambio = 'AJUSTE_INVENTARIO'
    ) OR NOT EXISTS
    (
        SELECT 1 FROM dbo.HistoricoCostosProductos
        WHERE EmpresaId = @EmpresaId AND ProductoId = @ProductoId
          AND Id = 2 AND CostoAnterior = 0.020000 AND CostoNuevo = @CostoPromedioFinal
          AND OrigenCambio = 'RECEPCION_COMPRA'
    )
        THROW 51030, N'Los históricos no quedaron en 0.020000 y 0.022727.', 1;

    PRINT N'DESPUÉS: resultado reparado, todavía dentro de la transacción.';

    SELECT m.Id AS MovimientoId, m.ModuloOrigen, m.DocumentoOrigenId,
           d.Id AS MovimientoDetalleId, d.ProductoPresentacionId,
           d.Cantidad, d.FactorConversion, d.CantidadBase,
           d.CostoUnitario, d.CostoTotal, d.Observaciones,
           k.Id AS KardexId, k.TipoAfectacion, k.Cantidad AS CantidadKardex,
           k.CostoUnitario AS CostoUnitarioKardex, k.CostoTotal AS CostoTotalKardex,
           k.SaldoCantidad, k.SaldoCosto
    FROM dbo.MovimientoDetalles AS d
    INNER JOIN dbo.Movimientos AS m
        ON m.EmpresaId = d.EmpresaId AND m.Id = d.MovimientoId
    INNER JOIN dbo.Kardex AS k
        ON k.EmpresaId = d.EmpresaId AND k.MovimientoDetalleId = d.Id
    WHERE d.EmpresaId = @EmpresaId AND d.ProductoId = @ProductoId
    ORDER BY d.Id;

    SELECT e.Id, e.Cantidad, e.CantidadReservada, e.CostoPromedio,
           cp.Id AS CostosProductosId, cp.CostoActual
    FROM dbo.Existencias AS e
    CROSS JOIN dbo.CostosProductos AS cp
    WHERE e.EmpresaId = @EmpresaId AND e.ProductoId = @ProductoId
      AND cp.EmpresaId = @EmpresaId AND cp.ProductoId = @ProductoId;

    SELECT h.Id, h.CostoAnterior, h.CostoNuevo, h.FechaCambio,
           h.OrigenCambio, h.DocumentoOrigenId, h.UsuarioId
    FROM dbo.HistoricoCostosProductos AS h
    WHERE h.EmpresaId = @EmpresaId AND h.ProductoId = @ProductoId
    ORDER BY h.Id;

    IF @ConfirmarCommit = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT N'COMMIT realizado: se reparó exclusivamente la historia validada de ProductoId = 1.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT N'ROLLBACK de inspección realizado. Revise ANTES/DESPUÉS; cambie @ConfirmarCommit a 1 solo para aplicar.';
    END;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    THROW;
END CATCH;
