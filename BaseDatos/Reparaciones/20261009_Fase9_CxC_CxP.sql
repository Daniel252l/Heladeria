-- FASE 9 CXC CXP. Ejecutar explícitamente en la base seleccionada, después de Fase 8.
-- No crea datos de negocio ni concilia históricos. Revisar y respaldar antes de aplicar.
-- Sin GO interno: todo DDL y validación pertenecen a UNA transacción.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @lock int;
    EXEC @lock=sys.sp_getapplock @Resource=N'Heladeria.Esquema.Fase9',
        @LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
    IF @lock<0 THROW 51090, 'No se obtuvo la reserva del esquema de Caja.', 1;
    IF EXISTS(SELECT 1 FROM (VALUES(N'CuentasPorCobrar'),(N'CuentasPorPagar'),(N'FacturasProveedores'),
        (N'AplicacionesCuentasPorCobrar'),(N'AplicacionesCuentasPorPagar'),(N'Pagos'),(N'OrdenesCompra'),(N'Ventas')) t(nombre)
        WHERE OBJECT_ID(N'dbo.'+t.nombre,N'U') IS NULL)
        OR COL_LENGTH(N'dbo.Pagos',N'ClaveOperacion') IS NULL
        THROW 51090, 'Se requiere el esquema existente y Fase 8. No se crean tablas equivalentes.', 1;

    DECLARE @columnas TABLE(tabla sysname,columna sysname,declaracion nvarchar(100),tipo sysname,
        longitud smallint,precision tinyint,escala tinyint,nulo bit);
    INSERT @columnas VALUES
        (N'Ventas',N'FechaVencimientoCredito',N'date',N'date',3,10,0,1),
        (N'OrdenesCompra',N'EsCredito',N'bit',N'bit',1,1,0,1),
        (N'OrdenesCompra',N'VersionFila',N'rowversion',N'timestamp',8,0,0,0),
        (N'Pagos',N'HuellaSolicitud',N'varbinary(32)',N'varbinary',32,0,0,1),
        (N'Pagos',N'Observacion',N'nvarchar(500)',N'nvarchar',1000,0,0,1),
        (N'FacturasProveedores',N'ClaveOperacion',N'uniqueidentifier',N'uniqueidentifier',16,0,0,1),
        (N'FacturasProveedores',N'HuellaSolicitud',N'varbinary(32)',N'varbinary',32,0,0,1),
        (N'CuentasPorCobrar',N'SucursalId',N'bigint',N'bigint',8,19,0,1),
        (N'CuentasPorCobrar',N'MonedaId',N'int',N'int',4,10,0,1),
        (N'CuentasPorCobrar',N'UsuarioCreacionId',N'int',N'int',4,10,0,1),
        (N'CuentasPorCobrar',N'FechaCreacion',N'datetime2(3)',N'datetime2',7,23,3,1),
        (N'CuentasPorCobrar',N'UsuarioModificacionId',N'int',N'int',4,10,0,1),
        (N'CuentasPorCobrar',N'FechaModificacion',N'datetime2(3)',N'datetime2',7,23,3,1),
        (N'CuentasPorCobrar',N'VersionFila',N'rowversion',N'timestamp',8,0,0,0),
        (N'CuentasPorPagar',N'SucursalId',N'bigint',N'bigint',8,19,0,1),
        (N'CuentasPorPagar',N'MonedaId',N'int',N'int',4,10,0,1),
        (N'CuentasPorPagar',N'UsuarioCreacionId',N'int',N'int',4,10,0,1),
        (N'CuentasPorPagar',N'FechaCreacion',N'datetime2(3)',N'datetime2',7,23,3,1),
        (N'CuentasPorPagar',N'UsuarioModificacionId',N'int',N'int',4,10,0,1),
        (N'CuentasPorPagar',N'FechaModificacion',N'datetime2(3)',N'datetime2',7,23,3,1),
        (N'CuentasPorPagar',N'VersionFila',N'rowversion',N'timestamp',8,0,0,0);
    DECLARE @tabla sysname,@columna sysname,@declaracion nvarchar(100),@nulo bit,@sql nvarchar(max);
    DECLARE columnas CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,columna,declaracion,nulo FROM @columnas;
    OPEN columnas;
    FETCH NEXT FROM columnas INTO @tabla,@columna,@declaracion,@nulo;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF COL_LENGTH(N'dbo.'+@tabla,@columna) IS NULL
        BEGIN
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' ADD '+QUOTENAME(@columna)+N' '+@declaracion+
                CASE WHEN @nulo=1 THEN N' NULL;' ELSE N' NOT NULL;' END;
            EXEC sys.sp_executesql @sql;
        END;
        FETCH NEXT FROM columnas INTO @tabla,@columna,@declaracion,@nulo;
    END;
    CLOSE columnas; DEALLOCATE columnas;
    IF EXISTS(SELECT 1 FROM @columnas e JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.'+e.tabla) AND c.name=e.columna
        WHERE TYPE_NAME(c.system_type_id)<>e.tipo OR c.max_length<>e.longitud OR c.precision<>e.precision
        OR c.scale<>e.escala OR c.is_nullable<>e.nulo OR c.is_computed=1 OR c.is_identity=1
        OR c.default_object_id<>0)
        THROW 51090, 'Columna Fase 9 preexistente incompatible. No se cambiarán tipos ni datos automáticamente.', 1;

    DECLARE @foraneas TABLE(tabla sysname,columna sysname,destino sysname,nombre sysname);
    INSERT @foraneas VALUES
        (N'CuentasPorCobrar',N'SucursalId',N'Sucursales',N'FK_CuentasPorCobrar_Sucursales_SucursalId'),
        (N'CuentasPorCobrar',N'MonedaId',N'Monedas',N'FK_CuentasPorCobrar_Monedas_MonedaId'),
        (N'CuentasPorCobrar',N'UsuarioCreacionId',N'Usuarios',N'FK_CuentasPorCobrar_Usuarios_UsuarioCreacionId'),
        (N'CuentasPorCobrar',N'UsuarioModificacionId',N'Usuarios',N'FK_CuentasPorCobrar_Usuarios_UsuarioModificacionId'),
        (N'CuentasPorPagar',N'SucursalId',N'Sucursales',N'FK_CuentasPorPagar_Sucursales_SucursalId'),
        (N'CuentasPorPagar',N'MonedaId',N'Monedas',N'FK_CuentasPorPagar_Monedas_MonedaId'),
        (N'CuentasPorPagar',N'UsuarioCreacionId',N'Usuarios',N'FK_CuentasPorPagar_Usuarios_UsuarioCreacionId'),
        (N'CuentasPorPagar',N'UsuarioModificacionId',N'Usuarios',N'FK_CuentasPorPagar_Usuarios_UsuarioModificacionId');
    DECLARE @destino sysname,@nombre sysname;
    DECLARE foraneas CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,columna,destino,nombre FROM @foraneas;
    OPEN foraneas;
    FETCH NEXT FROM foraneas INTO @tabla,@columna,@destino,@nombre;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF OBJECT_ID(N'dbo.'+@nombre,N'F') IS NULL
        BEGIN
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' WITH CHECK ADD CONSTRAINT '+QUOTENAME(@nombre)+
                N' FOREIGN KEY('+QUOTENAME(@columna)+N') REFERENCES dbo.'+QUOTENAME(@destino)+N'(Id);';
            EXEC sys.sp_executesql @sql;
        END;
        IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys k JOIN sys.foreign_key_columns c ON c.constraint_object_id=k.object_id
            WHERE k.name=@nombre AND k.parent_object_id=OBJECT_ID(N'dbo.'+@tabla)
            AND k.referenced_object_id=OBJECT_ID(N'dbo.'+@destino) AND k.is_disabled=0 AND k.is_not_trusted=0
            AND k.delete_referential_action=0 AND k.update_referential_action=0
            AND COL_NAME(c.parent_object_id,c.parent_column_id)=@columna AND COL_NAME(c.referenced_object_id,c.referenced_column_id)=N'Id'
            AND (SELECT COUNT(*) FROM sys.foreign_key_columns f WHERE f.constraint_object_id=k.object_id)=1)
            THROW 51090, 'Clave foránea Fase 9 incompatible, deshabilitada o no verificada.', 1;
        FETCH NEXT FROM foraneas INTO @tabla,@columna,@destino,@nombre;
    END;
    CLOSE foraneas; DEALLOCATE foraneas;

    DECLARE @checks TABLE(tabla sysname,nombre sysname,definicion nvarchar(max));
    INSERT @checks VALUES
        (N'Pagos',N'CK_Pagos_Tercero',N'(VentaId IS NULL AND ClaveOperacion IS NULL AND ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL))) OR (VentaId IS NOT NULL AND ClaveOperacion IS NOT NULL AND ProveedorId IS NULL AND TurnoCajaId IS NOT NULL AND SucursalId IS NOT NULL) OR (VentaId IS NULL AND ClaveOperacion IS NOT NULL AND HuellaSolicitud IS NOT NULL AND TurnoCajaId IS NOT NULL AND SucursalId IS NOT NULL AND ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL)))'),
        (N'MovimientosCaja',N'CK_MovimientosCaja_Snapshot_Fase8',N'(CodigoConcepto IS NULL AND EsEfectivo IS NULL AND NumeroDocumento IS NULL AND PagoDetalleId IS NULL) OR (CodigoConcepto IS NOT NULL AND EsEfectivo IS NOT NULL AND NumeroDocumento IS NOT NULL AND FormaPagoId IS NOT NULL AND (((CodigoConcepto=''COBRO_VENTA'' OR CodigoConcepto=''COBRO_CUENTA_POR_COBRAR'' OR (CodigoConcepto=''PAGO_CUENTA_POR_PAGAR'' AND EsEfectivo=1)) AND PagoDetalleId IS NOT NULL) OR ((CodigoConcepto=''APERTURA_CAJA'' OR CodigoConcepto=''INGRESO_CAJA'' OR CodigoConcepto=''EGRESO_CAJA'') AND PagoDetalleId IS NULL AND EsEfectivo=1)))'),
        (N'Pagos',N'CK_Pagos_Huella_Fase9',N'HuellaSolicitud IS NULL OR (DATALENGTH(HuellaSolicitud)=32 AND ClaveOperacion IS NOT NULL AND VentaId IS NULL)'),
        (N'FacturasProveedores',N'CK_FacturasProveedores_Operacion_Fase9',N'(ClaveOperacion IS NULL AND HuellaSolicitud IS NULL) OR (ClaveOperacion IS NOT NULL AND HuellaSolicitud IS NOT NULL AND DATALENGTH(HuellaSolicitud)=32 AND OrdenCompraId IS NOT NULL AND FechaVencimiento IS NOT NULL AND FechaVencimiento>=FechaDocumento AND Total>0)'),
        (N'CuentasPorCobrar',N'CK_CuentasPorCobrar_Metadata_Fase9',N'(FechaCreacion IS NULL AND SucursalId IS NULL AND MonedaId IS NULL AND UsuarioCreacionId IS NULL AND UsuarioModificacionId IS NULL AND FechaModificacion IS NULL) OR (FechaCreacion IS NOT NULL AND SucursalId IS NOT NULL AND MonedaId IS NOT NULL AND UsuarioCreacionId IS NOT NULL AND MontoOriginal>0 AND SaldoPendiente>=0 AND SaldoPendiente<=MontoOriginal AND FechaVencimiento>=FechaEmision AND ((UsuarioModificacionId IS NULL AND FechaModificacion IS NULL) OR (UsuarioModificacionId IS NOT NULL AND FechaModificacion IS NOT NULL)))'),
        (N'CuentasPorPagar',N'CK_CuentasPorPagar_Metadata_Fase9',N'(FechaCreacion IS NULL AND SucursalId IS NULL AND MonedaId IS NULL AND UsuarioCreacionId IS NULL AND UsuarioModificacionId IS NULL AND FechaModificacion IS NULL) OR (FechaCreacion IS NOT NULL AND SucursalId IS NOT NULL AND MonedaId IS NOT NULL AND UsuarioCreacionId IS NOT NULL AND MontoOriginal>0 AND SaldoPendiente>=0 AND SaldoPendiente<=MontoOriginal AND FechaVencimiento>=FechaEmision AND ((UsuarioModificacionId IS NULL AND FechaModificacion IS NULL) OR (UsuarioModificacionId IS NOT NULL AND FechaModificacion IS NOT NULL)))');
    DECLARE @esperada nvarchar(max),@def nvarchar(max),@normal nvarchar(max),@normalEsperada nvarchar(max),@canon nvarchar(max);
    DECLARE restricciones CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,nombre,definicion FROM @checks;
    OPEN restricciones;
    FETCH NEXT FROM restricciones INTO @tabla,@nombre,@esperada;
    WHILE @@FETCH_STATUS=0
    BEGIN
        -- Compilar la expresión esperada en tabla temporal vacía permite comparar su forma canónica.
        -- Conserva paréntesis lógicos: quitarlos podría ocultar un cambio de precedencia AND/OR.
        SET @sql=N'SELECT TOP (0) * INTO #F9Check FROM dbo.'+QUOTENAME(@tabla)+N';
            ALTER TABLE #F9Check ADD CHECK ('+@esperada+N');
            SELECT @salida=definition FROM tempdb.sys.check_constraints
                WHERE parent_object_id=OBJECT_ID(N''tempdb..#F9Check'');';
        EXEC sys.sp_executesql @sql,N'@salida nvarchar(max) OUTPUT',@salida=@canon OUTPUT;
        SET @normalEsperada=REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@canon),N' ',N''),CHAR(9),N''),CHAR(13),N''),CHAR(10),N'');
        SET @def=NULL;
        SELECT @def=definition FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre;
        SET @normal=REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@def),N' ',N''),CHAR(9),N''),CHAR(13),N''),CHAR(10),N'');
        -- Solo se permite ampliar las dos expresiones exactas aprobadas en Fase 8.
        IF @nombre IN (N'CK_Pagos_Tercero',N'CK_MovimientosCaja_Snapshot_Fase8') AND @def IS NOT NULL AND @normal<>@normalEsperada
        BEGIN
            DECLARE @anterior nvarchar(max)=CASE WHEN @nombre=N'CK_Pagos_Tercero' THEN N'(VentaId IS NULL AND ClaveOperacion IS NULL AND ((ClienteId IS NOT NULL AND ProveedorId IS NULL) OR (ClienteId IS NULL AND ProveedorId IS NOT NULL))) OR (VentaId IS NOT NULL AND ClaveOperacion IS NOT NULL AND ProveedorId IS NULL AND TurnoCajaId IS NOT NULL AND SucursalId IS NOT NULL)' ELSE N'(CodigoConcepto IS NULL AND EsEfectivo IS NULL AND NumeroDocumento IS NULL AND PagoDetalleId IS NULL) OR (CodigoConcepto IS NOT NULL AND EsEfectivo IS NOT NULL AND NumeroDocumento IS NOT NULL AND FormaPagoId IS NOT NULL AND ((CodigoConcepto=''COBRO_VENTA'' AND PagoDetalleId IS NOT NULL) OR ((CodigoConcepto=''APERTURA_CAJA'' OR CodigoConcepto=''INGRESO_CAJA'' OR CodigoConcepto=''EGRESO_CAJA'') AND PagoDetalleId IS NULL AND EsEfectivo=1)))' END;
            SET @sql=N'SELECT TOP(0) * INTO #F9CheckAnterior FROM dbo.'+QUOTENAME(@tabla)+N';
                ALTER TABLE #F9CheckAnterior ADD CHECK ('+@anterior+N');
                SELECT @salida=definition FROM tempdb.sys.check_constraints WHERE parent_object_id=OBJECT_ID(N''tempdb..#F9CheckAnterior'');';
            EXEC sys.sp_executesql @sql,N'@salida nvarchar(max) OUTPUT',@salida=@canon OUTPUT;
            IF @normal<>REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@canon),N' ',N''),CHAR(9),N''),CHAR(13),N''),CHAR(10),N'')
                THROW 51090, 'Restricción financiera personalizada; no se reemplazará sin revisión.', 1;
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' DROP CONSTRAINT '+QUOTENAME(@nombre)+N';';
            EXEC sys.sp_executesql @sql;
            SET @def=NULL;
        END;
        IF @def IS NULL
        BEGIN
            SET @sql=N'ALTER TABLE dbo.'+QUOTENAME(@tabla)+N' WITH CHECK ADD CONSTRAINT '+QUOTENAME(@nombre)+N' CHECK ('+@esperada+N');';
            EXEC sys.sp_executesql @sql;
        END
        ELSE IF @normal<>@normalEsperada
        BEGIN
            DECLARE @mensaje nvarchar(2048)=N'Restricción Fase 9 preexistente incompatible: '+@nombre;
            THROW 51090, @mensaje, 1;
        END;
        IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.'+@tabla)
            AND name=@nombre AND (is_disabled=1 OR is_not_trusted=1))
            THROW 51090, 'Restricción Fase 9 deshabilitada o no verificada.', 1;
        FETCH NEXT FROM restricciones INTO @tabla,@nombre,@esperada;
    END;
    CLOSE restricciones; DEALLOCATE restricciones;

    DECLARE @indices TABLE(tabla sysname,nombre sysname,claves nvarchar(100),filtro nvarchar(100));
    INSERT @indices VALUES
        (N'FacturasProveedores',N'UX_FacturasProveedores_ClaveOperacion',N'EmpresaId,ClaveOperacion',N'[ClaveOperacion] IS NOT NULL'),
        (N'FacturasProveedores',N'UX_FacturasProveedores_OrdenFase9',N'OrdenCompraId',N'[ClaveOperacion] IS NOT NULL AND [OrdenCompraId] IS NOT NULL');
    DECLARE @claves nvarchar(100),@filtro nvarchar(100),@reales nvarchar(max);
    DECLARE indices CURSOR LOCAL FAST_FORWARD FOR SELECT tabla,nombre,claves,filtro FROM @indices;
    OPEN indices;
    FETCH NEXT FROM indices INTO @tabla,@nombre,@claves,@filtro;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre)
        BEGIN
            SET @sql=N'CREATE UNIQUE INDEX '+QUOTENAME(@nombre)+N' ON dbo.'+QUOTENAME(@tabla)+N'('+@claves+N') WHERE '+@filtro+N';';
            EXEC sys.sp_executesql @sql;
        END;
        SET @def=@filtro; SET @normalEsperada=REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@def),N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N''),CHAR(13),N''),CHAR(10),N'');
        SELECT @def=filter_definition FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre;
        SET @normal=REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(@def),N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N''),CHAR(13),N''),CHAR(10),N'');
        SELECT @reales=STRING_AGG(CONVERT(nvarchar(max),c.name),N',') WITHIN GROUP(ORDER BY ic.key_ordinal)
        FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0
        JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
        WHERE i.object_id=OBJECT_ID(N'dbo.'+@tabla) AND i.name=@nombre;
        IF @reales IS NULL OR @reales<>@claves OR @normal IS NULL OR @normal<>@normalEsperada
            OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@tabla) AND name=@nombre
                AND is_unique=1 AND is_disabled=0 AND has_filter=1 AND ignore_dup_key=0)
            THROW 51090, 'Índice Fase 9 incompatible. No se reemplazará silenciosamente.', 1;
        FETCH NEXT FROM indices INTO @tabla,@nombre,@claves,@filtro;
    END;
    CLOSE indices; DEALLOCATE indices;
    COMMIT TRANSACTION;
    PRINT N'FASE 9: esquema CxC/CxP validado. Sin datos operativos. Revise y ejecute --preparar-fase9 explícitamente.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
