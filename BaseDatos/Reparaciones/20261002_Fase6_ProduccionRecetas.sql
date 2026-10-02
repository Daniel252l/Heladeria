/*
    Fase 6 - Producción y recetas
    --------------------------------
    Amplía las tablas de producción que ya existen. No crea datos de negocio,
    permisos ni catálogos; estos últimos se preparan con --preparar-fase6.

    El script es transaccional e idempotente para una instalación compatible.
    No debe ejecutarse mientras la API está procesando escrituras.
*/
SET XACT_ABORT ON;
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Recetas', N'U') IS NULL
       OR OBJECT_ID(N'dbo.RecetaDetalles', N'U') IS NULL
       OR OBJECT_ID(N'dbo.OrdenesProduccion', N'U') IS NULL
       OR OBJECT_ID(N'dbo.OrdenProduccionDetalles', N'U') IS NULL
       OR OBJECT_ID(N'dbo.ProduccionTerminada', N'U') IS NULL
        THROW 51060, 'El esquema base de producción no está instalado.', 1;

    /*
       Cada DDL y cada sentencia que consume una columna nueva se compila en su
       propia ejecución. Esto evita que SQL Server intente resolver las columnas
       antes de que los ALTER TABLE condicionales hayan terminado.
       sp_executesql participa en la misma transacción: no introduce commits.
    */
    IF COL_LENGTH(N'dbo.Recetas', N'ProductoPresentacionRendimientoId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Recetas ADD ProductoPresentacionRendimientoId BIGINT NULL;';
    IF COL_LENGTH(N'dbo.Recetas', N'FactorConversion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Recetas ADD FactorConversion DECIMAL(19,6) NULL;';
    IF COL_LENGTH(N'dbo.Recetas', N'RendimientoBase') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Recetas ADD RendimientoBase DECIMAL(19,6) NULL;';

    IF COL_LENGTH(N'dbo.RecetaDetalles', N'ProductoPresentacionId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.RecetaDetalles ADD ProductoPresentacionId BIGINT NULL;';
    IF COL_LENGTH(N'dbo.RecetaDetalles', N'FactorConversion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.RecetaDetalles ADD FactorConversion DECIMAL(19,6) NULL;';
    IF COL_LENGTH(N'dbo.RecetaDetalles', N'CantidadBase') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.RecetaDetalles ADD CantidadBase DECIMAL(19,6) NULL;';
    IF COL_LENGTH(N'dbo.RecetaDetalles', N'Observacion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.RecetaDetalles ADD Observacion VARCHAR(300) NULL;';

    IF COL_LENGTH(N'dbo.OrdenesProduccion', N'ProductoPresentacionId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenesProduccion ADD ProductoPresentacionId BIGINT NULL;';
    IF COL_LENGTH(N'dbo.OrdenesProduccion', N'FactorConversion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenesProduccion ADD FactorConversion DECIMAL(19,6) NULL;';
    IF COL_LENGTH(N'dbo.OrdenesProduccion', N'CantidadPlanificadaBase') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenesProduccion ADD CantidadPlanificadaBase DECIMAL(19,6) NULL;';

    IF COL_LENGTH(N'dbo.OrdenProduccionDetalles', N'ProductoPresentacionId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenProduccionDetalles ADD ProductoPresentacionId BIGINT NULL;';
    IF COL_LENGTH(N'dbo.OrdenProduccionDetalles', N'FactorConversion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenProduccionDetalles ADD FactorConversion DECIMAL(19,6) NULL;';
    IF COL_LENGTH(N'dbo.OrdenProduccionDetalles', N'CantidadPlanificadaBase') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenProduccionDetalles ADD CantidadPlanificadaBase DECIMAL(19,6) NULL;';

    /*
       Los registros históricos sin snapshot solo se pueden completar cuando la
       unidad era la base o existe una única presentación inequívoca del producto.
       Ante ambigüedad se aborta todo y se exige conciliación manual.
    */
    EXEC sys.sp_executesql N'
        IF EXISTS (
            SELECT 1
            FROM dbo.Recetas r
            JOIN dbo.Productos p ON p.Id = r.ProductoTerminadoId
            WHERE r.UnidadRendimientoId <> p.UnidadBaseId
              AND (r.ProductoPresentacionRendimientoId IS NULL
                   OR r.FactorConversion IS NULL
                   OR r.RendimientoBase IS NULL)
              AND (SELECT COUNT_BIG(*) FROM dbo.ProductoPresentaciones pp
                   WHERE pp.ProductoId = r.ProductoTerminadoId
                     AND pp.UnidadMedidaId = r.UnidadRendimientoId) <> 1)
            THROW 51061, N''Hay recetas históricas cuya conversión de rendimiento es ambigua; concilie antes de aplicar Fase 6.'', 1;

        UPDATE r
           SET ProductoPresentacionRendimientoId = CASE
                   WHEN r.UnidadRendimientoId = p.UnidadBaseId THEN NULL
                   ELSE COALESCE(r.ProductoPresentacionRendimientoId, pp.Id) END,
               FactorConversion = COALESCE(r.FactorConversion,
                   CASE WHEN r.UnidadRendimientoId = p.UnidadBaseId THEN 1 ELSE pp.FactorConversion END),
               RendimientoBase = COALESCE(r.RendimientoBase,
                   r.Rendimiento * COALESCE(r.FactorConversion,
                       CASE WHEN r.UnidadRendimientoId = p.UnidadBaseId THEN 1 ELSE pp.FactorConversion END))
        FROM dbo.Recetas r
        JOIN dbo.Productos p ON p.Id = r.ProductoTerminadoId
        OUTER APPLY (
            SELECT TOP (1) x.Id, x.FactorConversion
            FROM dbo.ProductoPresentaciones x
            WHERE x.ProductoId = r.ProductoTerminadoId
              AND x.UnidadMedidaId = r.UnidadRendimientoId
            ORDER BY x.Id) pp
        WHERE r.FactorConversion IS NULL
           OR r.RendimientoBase IS NULL
           OR (r.UnidadRendimientoId <> p.UnidadBaseId
               AND r.ProductoPresentacionRendimientoId IS NULL);

        IF EXISTS (
            SELECT 1
            FROM dbo.RecetaDetalles d
            JOIN dbo.Productos p ON p.Id = d.ProductoInsumoId
            WHERE d.UnidadMedidaId <> p.UnidadBaseId
              AND (d.ProductoPresentacionId IS NULL
                   OR d.FactorConversion IS NULL
                   OR d.CantidadBase IS NULL)
              AND (SELECT COUNT_BIG(*) FROM dbo.ProductoPresentaciones pp
                   WHERE pp.ProductoId = d.ProductoInsumoId
                     AND pp.UnidadMedidaId = d.UnidadMedidaId) <> 1)
            THROW 51062, N''Hay ingredientes históricos cuya conversión es ambigua; concilie antes de aplicar Fase 6.'', 1;

        UPDATE d
           SET ProductoPresentacionId = CASE
                   WHEN d.UnidadMedidaId = p.UnidadBaseId THEN NULL
                   ELSE COALESCE(d.ProductoPresentacionId, pp.Id) END,
               FactorConversion = COALESCE(d.FactorConversion,
                   CASE WHEN d.UnidadMedidaId = p.UnidadBaseId THEN 1 ELSE pp.FactorConversion END),
               CantidadBase = COALESCE(d.CantidadBase,
                   d.CantidadRequerida * COALESCE(d.FactorConversion,
                       CASE WHEN d.UnidadMedidaId = p.UnidadBaseId THEN 1 ELSE pp.FactorConversion END))
        FROM dbo.RecetaDetalles d
        JOIN dbo.Productos p ON p.Id = d.ProductoInsumoId
        OUTER APPLY (
            SELECT TOP (1) x.Id, x.FactorConversion
            FROM dbo.ProductoPresentaciones x
            WHERE x.ProductoId = d.ProductoInsumoId
              AND x.UnidadMedidaId = d.UnidadMedidaId
            ORDER BY x.Id) pp
        WHERE d.FactorConversion IS NULL
           OR d.CantidadBase IS NULL
           OR (d.UnidadMedidaId <> p.UnidadBaseId AND d.ProductoPresentacionId IS NULL);

        UPDATE o
           SET ProductoPresentacionId = COALESCE(o.ProductoPresentacionId, r.ProductoPresentacionRendimientoId),
               FactorConversion = COALESCE(o.FactorConversion, r.FactorConversion),
               CantidadPlanificadaBase = COALESCE(o.CantidadPlanificadaBase,
                   o.CantidadPlanificada * COALESCE(o.FactorConversion, r.FactorConversion))
        FROM dbo.OrdenesProduccion o
        JOIN dbo.Recetas r ON r.Id = o.RecetaId
        WHERE o.FactorConversion IS NULL
           OR o.CantidadPlanificadaBase IS NULL
           OR (o.ProductoPresentacionId IS NULL
               AND r.ProductoPresentacionRendimientoId IS NOT NULL);

        IF EXISTS (
            SELECT 1
            FROM dbo.OrdenProduccionDetalles d
            JOIN dbo.Productos p ON p.Id = d.ProductoInsumoId
            WHERE d.UnidadMedidaId <> p.UnidadBaseId
              AND (d.ProductoPresentacionId IS NULL
                   OR d.FactorConversion IS NULL
                   OR d.CantidadPlanificadaBase IS NULL)
              AND (SELECT COUNT_BIG(*) FROM dbo.ProductoPresentaciones pp
                   WHERE pp.ProductoId = d.ProductoInsumoId
                     AND pp.UnidadMedidaId = d.UnidadMedidaId) <> 1)
            THROW 51063, N''Hay detalles de orden históricos cuya conversión es ambigua; concilie antes de aplicar Fase 6.'', 1;

        UPDATE d
           SET ProductoPresentacionId = CASE
                   WHEN d.UnidadMedidaId = p.UnidadBaseId THEN NULL
                   ELSE COALESCE(d.ProductoPresentacionId, pp.Id) END,
               FactorConversion = COALESCE(d.FactorConversion,
                   CASE WHEN d.UnidadMedidaId = p.UnidadBaseId THEN 1 ELSE pp.FactorConversion END),
               CantidadPlanificadaBase = COALESCE(d.CantidadPlanificadaBase,
                   d.CantidadPlanificada * COALESCE(d.FactorConversion,
                       CASE WHEN d.UnidadMedidaId = p.UnidadBaseId THEN 1 ELSE pp.FactorConversion END))
        FROM dbo.OrdenProduccionDetalles d
        JOIN dbo.Productos p ON p.Id = d.ProductoInsumoId
        OUTER APPLY (
            SELECT TOP (1) x.Id, x.FactorConversion
            FROM dbo.ProductoPresentaciones x
            WHERE x.ProductoId = d.ProductoInsumoId
              AND x.UnidadMedidaId = d.UnidadMedidaId
            ORDER BY x.Id) pp
        WHERE d.FactorConversion IS NULL
           OR d.CantidadPlanificadaBase IS NULL
           OR (d.UnidadMedidaId <> p.UnidadBaseId AND d.ProductoPresentacionId IS NULL);

        IF EXISTS (SELECT 1 FROM dbo.Recetas WHERE FactorConversion IS NULL OR RendimientoBase IS NULL
            OR FactorConversion <= 0 OR RendimientoBase <= 0)
           OR EXISTS (SELECT 1 FROM dbo.RecetaDetalles WHERE FactorConversion IS NULL OR CantidadBase IS NULL
            OR FactorConversion <= 0 OR CantidadBase <= 0)
           OR EXISTS (SELECT 1 FROM dbo.OrdenesProduccion WHERE FactorConversion IS NULL OR CantidadPlanificadaBase IS NULL
            OR FactorConversion <= 0 OR CantidadPlanificadaBase <= 0)
           OR EXISTS (SELECT 1 FROM dbo.OrdenProduccionDetalles WHERE FactorConversion IS NULL OR CantidadPlanificadaBase IS NULL
            OR FactorConversion <= 0 OR CantidadPlanificadaBase <= 0)
            THROW 51064, N''No fue posible construir snapshots válidos para todos los registros históricos.'', 1;
    ';

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Recetas')
        AND name = N'FactorConversion' AND is_nullable = 1)
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Recetas ALTER COLUMN FactorConversion DECIMAL(19,6) NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Recetas')
        AND name = N'RendimientoBase' AND is_nullable = 1)
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Recetas ALTER COLUMN RendimientoBase DECIMAL(19,6) NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RecetaDetalles')
        AND name = N'FactorConversion' AND is_nullable = 1)
        EXEC sys.sp_executesql N'ALTER TABLE dbo.RecetaDetalles ALTER COLUMN FactorConversion DECIMAL(19,6) NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.RecetaDetalles')
        AND name = N'CantidadBase' AND is_nullable = 1)
        EXEC sys.sp_executesql N'ALTER TABLE dbo.RecetaDetalles ALTER COLUMN CantidadBase DECIMAL(19,6) NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OrdenesProduccion')
        AND name = N'FactorConversion' AND is_nullable = 1)
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenesProduccion ALTER COLUMN FactorConversion DECIMAL(19,6) NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OrdenesProduccion')
        AND name = N'CantidadPlanificadaBase' AND is_nullable = 1)
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenesProduccion ALTER COLUMN CantidadPlanificadaBase DECIMAL(19,6) NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OrdenProduccionDetalles')
        AND name = N'FactorConversion' AND is_nullable = 1)
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenProduccionDetalles ALTER COLUMN FactorConversion DECIMAL(19,6) NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OrdenProduccionDetalles')
        AND name = N'CantidadPlanificadaBase' AND is_nullable = 1)
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenProduccionDetalles ALTER COLUMN CantidadPlanificadaBase DECIMAL(19,6) NOT NULL;';

    IF COL_LENGTH(N'dbo.Recetas', N'VersionFila') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Recetas ADD VersionFila ROWVERSION NOT NULL;';
    IF COL_LENGTH(N'dbo.OrdenesProduccion', N'VersionFila') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenesProduccion ADD VersionFila ROWVERSION NOT NULL;';

    IF OBJECT_ID(N'dbo.FK_Recetas_ProductoPresentacionRendimiento', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Recetas WITH CHECK ADD CONSTRAINT FK_Recetas_ProductoPresentacionRendimiento
            FOREIGN KEY (ProductoPresentacionRendimientoId) REFERENCES dbo.ProductoPresentaciones(Id);';
    IF OBJECT_ID(N'dbo.FK_RecetaDetalles_ProductoPresentacion', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.RecetaDetalles WITH CHECK ADD CONSTRAINT FK_RecetaDetalles_ProductoPresentacion
            FOREIGN KEY (ProductoPresentacionId) REFERENCES dbo.ProductoPresentaciones(Id);';
    IF OBJECT_ID(N'dbo.FK_OrdenesProduccion_ProductoPresentacion', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenesProduccion WITH CHECK ADD CONSTRAINT FK_OrdenesProduccion_ProductoPresentacion
            FOREIGN KEY (ProductoPresentacionId) REFERENCES dbo.ProductoPresentaciones(Id);';
    IF OBJECT_ID(N'dbo.FK_OrdenProduccionDetalles_ProductoPresentacion', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenProduccionDetalles WITH CHECK ADD CONSTRAINT FK_OrdenProduccionDetalles_ProductoPresentacion
            FOREIGN KEY (ProductoPresentacionId) REFERENCES dbo.ProductoPresentaciones(Id);';

    IF OBJECT_ID(N'dbo.CK_Recetas_FactorConversion', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Recetas WITH CHECK ADD CONSTRAINT CK_Recetas_FactorConversion CHECK (FactorConversion > 0);';
    IF OBJECT_ID(N'dbo.CK_Recetas_RendimientoBase', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Recetas WITH CHECK ADD CONSTRAINT CK_Recetas_RendimientoBase CHECK (RendimientoBase > 0);';
    IF OBJECT_ID(N'dbo.CK_RecetaDetalles_FactorConversion', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.RecetaDetalles WITH CHECK ADD CONSTRAINT CK_RecetaDetalles_FactorConversion CHECK (FactorConversion > 0);';
    IF OBJECT_ID(N'dbo.CK_RecetaDetalles_CantidadBase', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.RecetaDetalles WITH CHECK ADD CONSTRAINT CK_RecetaDetalles_CantidadBase CHECK (CantidadBase > 0);';
    IF OBJECT_ID(N'dbo.CK_OrdenesProduccion_FactorConversion', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenesProduccion WITH CHECK ADD CONSTRAINT CK_OrdenesProduccion_FactorConversion CHECK (FactorConversion > 0);';
    IF OBJECT_ID(N'dbo.CK_OrdenesProduccion_CantidadPlanificadaBase', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenesProduccion WITH CHECK ADD CONSTRAINT CK_OrdenesProduccion_CantidadPlanificadaBase CHECK (CantidadPlanificadaBase > 0);';
    IF OBJECT_ID(N'dbo.CK_OrdenProduccionDetalles_FactorConversion', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenProduccionDetalles WITH CHECK ADD CONSTRAINT CK_OrdenProduccionDetalles_FactorConversion CHECK (FactorConversion > 0);';
    IF OBJECT_ID(N'dbo.CK_OrdenProduccionDetalles_CantidadBase', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.OrdenProduccionDetalles WITH CHECK ADD CONSTRAINT CK_OrdenProduccionDetalles_CantidadBase CHECK (CantidadPlanificadaBase > 0);';

    IF EXISTS (
        SELECT 1 FROM dbo.Recetas
        WHERE VigenteHasta IS NULL
        GROUP BY EmpresaId, ProductoTerminadoId
        HAVING COUNT_BIG(*) > 1)
        THROW 51065, 'Existe más de una receta vigente para un producto; concilie las vigencias antes de aplicar Fase 6.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Recetas')
        AND name = N'UQ_Recetas_Empresa_Producto_Vigente')
        CREATE UNIQUE INDEX UQ_Recetas_Empresa_Producto_Vigente
            ON dbo.Recetas(EmpresaId, ProductoTerminadoId) WHERE VigenteHasta IS NULL;

    IF EXISTS (
        SELECT 1 FROM dbo.ProduccionTerminada
        GROUP BY OrdenProduccionId HAVING COUNT_BIG(*) > 1)
        THROW 51066, 'Hay órdenes con más de una producción terminada; concilie antes de aplicar la restricción idempotente.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ProduccionTerminada')
        AND name = N'UQ_ProduccionTerminada_Orden')
        CREATE UNIQUE INDEX UQ_ProduccionTerminada_Orden ON dbo.ProduccionTerminada(OrdenProduccionId);

    COMMIT TRANSACTION;
    PRINT N'FASE6_PRODUCCION_RECETAS_APLICADA';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
