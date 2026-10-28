-- AJUSTE D COMPOSICION VENTA. Ejecutar explícitamente en la base seleccionada y con respaldo.
-- No crea productos, catálogos ni ventas. No convierte recetas de producción.
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF @@TRANCOUNT <> 0 THROW 51110, 'Ejecute Ajuste D fuera de otra transacción.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.CombosDetalle', N'U') IS NULL OR OBJECT_ID(N'dbo.VentasDetalle', N'U') IS NULL
        OR OBJECT_ID(N'dbo.MovimientoDetalles', N'U') IS NULL
        THROW 51110, 'Falta el esquema existente de Productos/Ventas/Inventario.', 1;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.CombosDetalle') AND name=N'Cantidad'
        AND NOT (is_nullable=0 AND (system_type_id=56 OR system_type_id=106 AND precision=19 AND scale=6)))
        THROW 51110, 'CombosDetalle.Cantidad tiene un tipo incompatible.', 1;
    DECLARE @defaultCantidad sysname, @defCantidad nvarchar(max), @ddlCantidad nvarchar(max);
    SELECT @defaultCantidad=d.name,@defCantidad=d.definition FROM sys.default_constraints d
        JOIN sys.columns c ON c.object_id=d.parent_object_id AND c.column_id=d.parent_column_id
        WHERE d.parent_object_id=OBJECT_ID(N'dbo.CombosDetalle') AND c.name=N'Cantidad';
    IF @defCantidad IS NOT NULL AND REPLACE(REPLACE(REPLACE(@defCantidad,N'(',N''),N')',N''),N' ',N'')<>N'1'
        THROW 51110, 'Default de Cantidad incompatible; no se reemplazará silenciosamente.', 1;
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.CombosDetalle') AND name=N'Cantidad' AND system_type_id=56)
    BEGIN
        IF @defaultCantidad IS NOT NULL
        BEGIN
            SET @ddlCantidad=N'ALTER TABLE dbo.CombosDetalle DROP CONSTRAINT '+QUOTENAME(@defaultCantidad);
            EXEC sys.sp_executesql @ddlCantidad;
        END;
        ALTER TABLE dbo.CombosDetalle ALTER COLUMN Cantidad decimal(19,6) NOT NULL;
        SET @ddlCantidad=N'ALTER TABLE dbo.CombosDetalle ADD CONSTRAINT '+QUOTENAME(COALESCE(@defaultCantidad,N'DF_CombosDetalle_Cantidad'))+N' DEFAULT(1) FOR Cantidad';
        EXEC sys.sp_executesql @ddlCantidad;
    END;

    IF COL_LENGTH(N'dbo.CombosDetalle', N'ProductoPresentacionId') IS NULL
        ALTER TABLE dbo.CombosDetalle ADD ProductoPresentacionId bigint NULL;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'EsCompuesto') IS NULL
        ALTER TABLE dbo.VentasDetalle ADD EsCompuesto bit NULL;
    IF COL_LENGTH(N'dbo.MovimientoDetalles', N'VentaDetalleId') IS NULL
        ALTER TABLE dbo.MovimientoDetalles ADD VentaDetalleId bigint NULL;
    IF COL_LENGTH(N'dbo.MovimientoDetalles', N'UnidadBaseId') IS NULL
        ALTER TABLE dbo.MovimientoDetalles ADD UnidadBaseId int NULL;

    IF EXISTS (SELECT 1 FROM (VALUES (N'CombosDetalle',N'ProductoPresentacionId',127),
        (N'VentasDetalle',N'EsCompuesto',104),(N'MovimientoDetalles',N'VentaDetalleId',127),
        (N'MovimientoDetalles',N'UnidadBaseId',56)) esperado(tabla,columna,tipo)
        LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+esperado.tabla) AND c.name=esperado.columna
        WHERE c.column_id IS NULL OR c.system_type_id<>esperado.tipo OR c.is_nullable<>1 OR c.is_computed<>0)
        THROW 51110, 'Una columna existente del Ajuste D es incompatible.', 1;

    -- Batch compilado después de ALTER. Nunca se referencia estáticamente una columna recién añadida.
    EXEC sys.sp_executesql N'
        IF EXISTS (SELECT 1 FROM dbo.CombosDetalle WHERE Cantidad<=0 OR ComboProductoId=ProductoIncluidoId)
            THROW 51110, ''Composición histórica inválida: cantidad o autorreferencia. No se inventarán valores.'', 1;
        IF EXISTS (SELECT 1 FROM dbo.CombosDetalle GROUP BY EmpresaId,ComboProductoId,ProductoIncluidoId,ProductoPresentacionId HAVING COUNT(*)>1)
            THROW 51110, ''Composición histórica duplicada; concilie antes de aplicar Ajuste D.'', 1;
        IF EXISTS (SELECT 1 FROM dbo.CombosDetalle a JOIN dbo.CombosDetalle b ON a.EmpresaId=b.EmpresaId AND a.ProductoIncluidoId=b.ComboProductoId)
            THROW 51110, ''Existen composiciones anidadas/ciclos; Ajuste D admite solo componentes terminales.'', 1;
        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N''dbo.CombosDetalle'') AND name=N''CK_CombosDetalle_Composicion'')
            ALTER TABLE dbo.CombosDetalle WITH CHECK ADD CONSTRAINT CK_CombosDetalle_Composicion CHECK(Cantidad>0 AND ComboProductoId<>ProductoIncluidoId);
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N''dbo.CombosDetalle'') AND name=N''FK_CombosDetalle_Presentacion'')
            ALTER TABLE dbo.CombosDetalle WITH CHECK ADD CONSTRAINT FK_CombosDetalle_Presentacion
                FOREIGN KEY(EmpresaId,ProductoIncluidoId,ProductoPresentacionId) REFERENCES dbo.ProductoPresentaciones(EmpresaId,ProductoId,Id);
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N''dbo.MovimientoDetalles'') AND name=N''FK_MovimientoDetalles_VentaDetalle'')
            ALTER TABLE dbo.MovimientoDetalles WITH CHECK ADD CONSTRAINT FK_MovimientoDetalles_VentaDetalle FOREIGN KEY(VentaDetalleId) REFERENCES dbo.VentasDetalle(Id);
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N''dbo.MovimientoDetalles'') AND name=N''FK_MovimientoDetalles_UnidadBase'')
            ALTER TABLE dbo.MovimientoDetalles WITH CHECK ADD CONSTRAINT FK_MovimientoDetalles_UnidadBase FOREIGN KEY(EmpresaId,UnidadBaseId) REFERENCES dbo.UnidadesMedida(EmpresaId,Id);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N''dbo.CombosDetalle'') AND name=N''UQ_CombosDetalle_Componente'')
            CREATE UNIQUE INDEX UQ_CombosDetalle_Componente ON dbo.CombosDetalle(EmpresaId,ComboProductoId,ProductoIncluidoId,ProductoPresentacionId);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N''dbo.MovimientoDetalles'') AND name=N''IX_MovimientoDetalles_VentaDetalle'')
            CREATE INDEX IX_MovimientoDetalles_VentaDetalle ON dbo.MovimientoDetalles(VentaDetalleId);
    ';

    DECLARE @tabla sysname, @nombre sysname, @claves nvarchar(200), @unico bit, @reales nvarchar(max);
    DECLARE indicesD CURSOR LOCAL FAST_FORWARD FOR SELECT * FROM (VALUES
        (N'CombosDetalle',N'UQ_CombosDetalle_Componente',N'EmpresaId,ComboProductoId,ProductoIncluidoId,ProductoPresentacionId',CONVERT(bit,1)),
        (N'MovimientoDetalles',N'IX_MovimientoDetalles_VentaDetalle',N'VentaDetalleId',CONVERT(bit,0))) v(tabla,nombre,claves,unico);
    OPEN indicesD; FETCH NEXT FROM indicesD INTO @tabla,@nombre,@claves,@unico;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SELECT @reales=STRING_AGG(CONVERT(nvarchar(max),c.name),N',') WITHIN GROUP(ORDER BY ic.key_ordinal)
        FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0
        JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        WHERE i.object_id=OBJECT_ID(N'dbo.'+@tabla) AND i.name=@nombre;
        IF @reales IS NULL OR @reales<>@claves OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla)
            AND name=@nombre AND is_unique=@unico AND has_filter=0 AND is_disabled=0 AND ignore_dup_key=0)
            THROW 51110, 'Índice Ajuste D incompatible; no se reemplazará silenciosamente.', 1;
        FETCH NEXT FROM indicesD INTO @tabla,@nombre,@claves,@unico;
    END;
    CLOSE indicesD; DEALLOCATE indicesD;
    DECLARE @referenciada sysname, @relaciones nvarchar(300);
    DECLARE clavesD CURSOR LOCAL FAST_FORWARD FOR SELECT * FROM (VALUES
        (N'CombosDetalle',N'FK_CombosDetalle_Presentacion',N'ProductoPresentaciones',N'EmpresaId=EmpresaId,ProductoIncluidoId=ProductoId,ProductoPresentacionId=Id'),
        (N'MovimientoDetalles',N'FK_MovimientoDetalles_VentaDetalle',N'VentasDetalle',N'VentaDetalleId=Id'),
        (N'MovimientoDetalles',N'FK_MovimientoDetalles_UnidadBase',N'UnidadesMedida',N'EmpresaId=EmpresaId,UnidadBaseId=Id')) v(tabla,nombre,referenciada,relaciones);
    OPEN clavesD; FETCH NEXT FROM clavesD INTO @tabla,@nombre,@referenciada,@relaciones;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SELECT @reales=STRING_AGG(CONVERT(nvarchar(max),p.name+N'='+r.name),N',') WITHIN GROUP(ORDER BY fc.constraint_column_id)
        FROM sys.foreign_keys f JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=f.object_id
        JOIN sys.columns p ON p.object_id=fc.parent_object_id AND p.column_id=fc.parent_column_id
        JOIN sys.columns r ON r.object_id=fc.referenced_object_id AND r.column_id=fc.referenced_column_id
        WHERE f.parent_object_id=OBJECT_ID(N'dbo.'+@tabla) AND f.name=@nombre AND f.referenced_object_id=OBJECT_ID(N'dbo.'+@referenciada);
        IF @reales IS NULL OR @reales<>@relaciones THROW 51110, 'Clave foránea Ajuste D incompatible.', 1;
        FETCH NEXT FROM clavesD INTO @tabla,@nombre,@referenciada,@relaciones;
    END;
    CLOSE clavesD; DEALLOCATE clavesD;
    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name IN(N'FK_CombosDetalle_Presentacion',N'FK_MovimientoDetalles_VentaDetalle',N'FK_MovimientoDetalles_UnidadBase')
        AND (is_disabled=1 OR is_not_trusted=1 OR delete_referential_action<>0 OR update_referential_action<>0))
        THROW 51110, 'Restricción Ajuste D deshabilitada o incompatible.', 1;
    DECLARE @check nvarchar(max)=(SELECT definition FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.CombosDetalle') AND name=N'CK_CombosDetalle_Composicion' AND is_disabled=0 AND is_not_trusted=0);
    SET @check=REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@check,N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N'');
    IF @check IS NULL OR @check<>N'Cantidad>0ANDComboProductoId<>ProductoIncluidoId'
        THROW 51110, 'Restricción de composición incompatible.', 1;
    COMMIT TRANSACTION;
    PRINT N'AJUSTE D: composición de venta y trazabilidad listas. Sin datos demo ni cambios financieros.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
