/* Fase 7 — Ventas/POS. No ejecuta bootstrap ni crea datos comerciales.
   Ejecutar en ventana de mantenimiento, tras respaldo, sobre la BD seleccionada.
   No rellena snapshots históricos: lo desconocido permanece NULL.
   DDL y consumo de columnas se compilan separadamente dentro de una transacción. */
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
    IF OBJECT_ID(N'dbo.Ventas', N'U') IS NULL OR OBJECT_ID(N'dbo.VentasDetalle', N'U') IS NULL
       OR OBJECT_ID(N'dbo.VentaImpuestos', N'U') IS NULL OR OBJECT_ID(N'dbo.Impuestos', N'U') IS NULL
        THROW 51070, 'Falta el esquema base de ventas. No se realizaron cambios.', 1;
    DECLARE @res int;
    EXEC @res = sys.sp_getapplock @Resource = N'Heladeria.Schema.Fase7',
        @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
    IF @res < 0 THROW 51070, 'No se pudo reservar la actualización de esquema.', 1;

    IF COL_LENGTH(N'dbo.Ventas', N'BodegaId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD BodegaId BIGINT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'BodegaId' AND (TYPE_NAME(system_type_id) <> N'bigint' OR max_length <> 8 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.BodegaId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'UbicacionId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD UbicacionId BIGINT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'UbicacionId' AND (TYPE_NAME(system_type_id) <> N'bigint' OR max_length <> 8 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.UbicacionId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'ClaveOperacion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD ClaveOperacion UNIQUEIDENTIFIER NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'ClaveOperacion' AND (TYPE_NAME(system_type_id) <> N'uniqueidentifier' OR max_length <> 16 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.ClaveOperacion incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'FechaModificacion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD FechaModificacion DATETIME2(3) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'FechaModificacion' AND (TYPE_NAME(system_type_id) <> N'datetime2' OR scale <> 3 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.FechaModificacion incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'ModificadoPorUsuarioId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD ModificadoPorUsuarioId INT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'ModificadoPorUsuarioId' AND (TYPE_NAME(system_type_id) <> N'int' OR max_length <> 4 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.ModificadoPorUsuarioId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'FechaCompletada') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD FechaCompletada DATETIME2(3) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'FechaCompletada' AND (TYPE_NAME(system_type_id) <> N'datetime2' OR scale <> 3 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.FechaCompletada incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'CompletadoPorUsuarioId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD CompletadoPorUsuarioId INT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'CompletadoPorUsuarioId' AND (TYPE_NAME(system_type_id) <> N'int' OR max_length <> 4 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.CompletadoPorUsuarioId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'FechaAnulacion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD FechaAnulacion DATETIME2(3) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'FechaAnulacion' AND (TYPE_NAME(system_type_id) <> N'datetime2' OR scale <> 3 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.FechaAnulacion incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'AnuladoPorUsuarioId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD AnuladoPorUsuarioId INT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'AnuladoPorUsuarioId' AND (TYPE_NAME(system_type_id) <> N'int' OR max_length <> 4 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.AnuladoPorUsuarioId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'MotivoAnulacion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD MotivoAnulacion VARCHAR(500) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'MotivoAnulacion' AND (TYPE_NAME(system_type_id) <> N'varchar' OR max_length <> 500 OR is_nullable <> 1))
        THROW 51070, 'Columna Ventas.MotivoAnulacion incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.Ventas', N'VersionFila') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas ADD VersionFila ROWVERSION NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'VersionFila' AND (TYPE_NAME(system_type_id) <> N'timestamp' OR max_length <> 8 OR is_nullable <> 0))
        THROW 51070, 'Columna Ventas.VersionFila incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'FactorConversion') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD FactorConversion DECIMAL(19,6) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'FactorConversion' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 19 OR scale <> 6 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.FactorConversion incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'CantidadBase') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD CantidadBase DECIMAL(19,6) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'CantidadBase' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 19 OR scale <> 6 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.CantidadBase incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'UnidadMedidaId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD UnidadMedidaId INT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'UnidadMedidaId' AND (TYPE_NAME(system_type_id) <> N'int' OR max_length <> 4 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.UnidadMedidaId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'UnidadBaseId') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD UnidadBaseId INT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'UnidadBaseId' AND (TYPE_NAME(system_type_id) <> N'int' OR max_length <> 4 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.UnidadBaseId incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'ManejaExistencia') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD ManejaExistencia BIT NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'ManejaExistencia' AND (TYPE_NAME(system_type_id) <> N'bit' OR max_length <> 1 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.ManejaExistencia incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'SubtotalNeto') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD SubtotalNeto DECIMAL(12,2) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'SubtotalNeto' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 12 OR scale <> 2 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.SubtotalNeto incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'DescuentoPrecioCatalogo') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD DescuentoPrecioCatalogo DECIMAL(12,2) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'DescuentoPrecioCatalogo' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 12 OR scale <> 2 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.DescuentoPrecioCatalogo incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'TotalLinea') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD TotalLinea DECIMAL(12,2) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'TotalLinea' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 12 OR scale <> 2 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.TotalLinea incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'CostoTotal') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD CostoTotal DECIMAL(19,6) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'CostoTotal' AND (TYPE_NAME(system_type_id) <> N'decimal' OR max_length <> 9 OR precision <> 19 OR scale <> 6 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.CostoTotal incompatible; revisar manualmente.', 1;
    IF COL_LENGTH(N'dbo.VentasDetalle', N'FuentePrecio') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle ADD FuentePrecio VARCHAR(30) NULL;';
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.VentasDetalle') AND name=N'FuentePrecio' AND (TYPE_NAME(system_type_id) <> N'varchar' OR max_length <> 30 OR is_nullable <> 1))
        THROW 51070, 'Columna VentasDetalle.FuentePrecio incompatible; revisar manualmente.', 1;
    IF OBJECT_ID(N'dbo.FK_Ventas_BodegaId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT FK_Ventas_BodegaId_Fase7 FOREIGN KEY (BodegaId) REFERENCES dbo.Bodegas(Id);';
    IF OBJECT_ID(N'dbo.FK_Ventas_UbicacionId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT FK_Ventas_UbicacionId_Fase7 FOREIGN KEY (UbicacionId) REFERENCES dbo.Ubicaciones(Id);';
    IF OBJECT_ID(N'dbo.FK_Ventas_ModificadoPorUsuarioId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT FK_Ventas_ModificadoPorUsuarioId_Fase7 FOREIGN KEY (ModificadoPorUsuarioId) REFERENCES dbo.Usuarios(Id);';
    IF OBJECT_ID(N'dbo.FK_Ventas_CompletadoPorUsuarioId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT FK_Ventas_CompletadoPorUsuarioId_Fase7 FOREIGN KEY (CompletadoPorUsuarioId) REFERENCES dbo.Usuarios(Id);';
    IF OBJECT_ID(N'dbo.FK_Ventas_AnuladoPorUsuarioId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT FK_Ventas_AnuladoPorUsuarioId_Fase7 FOREIGN KEY (AnuladoPorUsuarioId) REFERENCES dbo.Usuarios(Id);';
    IF OBJECT_ID(N'dbo.FK_VentasDetalle_UnidadMedidaId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle WITH CHECK ADD CONSTRAINT FK_VentasDetalle_UnidadMedidaId_Fase7 FOREIGN KEY (UnidadMedidaId) REFERENCES dbo.UnidadesMedida(Id);';
    IF OBJECT_ID(N'dbo.FK_VentasDetalle_UnidadBaseId_Fase7', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle WITH CHECK ADD CONSTRAINT FK_VentasDetalle_UnidadBaseId_Fase7 FOREIGN KEY (UnidadBaseId) REFERENCES dbo.UnidadesMedida(Id);';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'UX_Ventas_ClaveOperacion')
        EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UX_Ventas_ClaveOperacion ON dbo.Ventas(EmpresaId, ClaveOperacion) WHERE ClaveOperacion IS NOT NULL;';
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Ventas') AND name=N'UX_Ventas_ClaveOperacion' AND (is_unique=0 OR is_disabled=1 OR has_filter=0))
        THROW 51070, 'Indice de idempotencia incompatible.', 1;

    IF OBJECT_ID(N'dbo.VentaDetalleImpuestos', N'U') IS NULL
        EXEC sys.sp_executesql N'CREATE TABLE dbo.VentaDetalleImpuestos(
            Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VentaDetalleImpuestos PRIMARY KEY,
            VentaDetalleId BIGINT NOT NULL CONSTRAINT FK_VentaDetalleImpuestos_Detalle REFERENCES dbo.VentasDetalle(Id),
            ImpuestoId INT NOT NULL CONSTRAINT FK_VentaDetalleImpuestos_Impuesto REFERENCES dbo.Impuestos(Id),
            Codigo VARCHAR(20) NOT NULL,
            Nombre VARCHAR(100) NOT NULL,
            Porcentaje DECIMAL(9,6) NOT NULL CONSTRAINT CK_VentaDetalleImpuestos_Porcentaje CHECK(Porcentaje BETWEEN 0 AND 100),
            IncluidoEnPrecio BIT NOT NULL,
            BaseImponible DECIMAL(12,2) NOT NULL CONSTRAINT CK_VentaDetalleImpuestos_Base CHECK(BaseImponible>=0),
            Monto DECIMAL(12,2) NOT NULL CONSTRAINT CK_VentaDetalleImpuestos_Monto CHECK(Monto>=0),
            CONSTRAINT UQ_VentaDetalleImpuestos UNIQUE(VentaDetalleId, ImpuestoId)
        );';
    IF OBJECT_ID(N'dbo.CK_VentasDetalle_Snapshot_Fase7', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentasDetalle WITH CHECK ADD CONSTRAINT CK_VentasDetalle_Snapshot_Fase7 CHECK ((FactorConversion IS NULL OR FactorConversion>0) AND (CantidadBase IS NULL OR CantidadBase>0) AND (CostoTotal IS NULL OR CostoTotal>=0) AND (SubtotalNeto IS NULL OR SubtotalNeto>=0) AND (TotalLinea IS NULL OR TotalLinea>=0) AND (DescuentoPrecioCatalogo IS NULL OR DescuentoPrecioCatalogo>=0));';
    IF OBJECT_ID(N'dbo.CK_Ventas_Almacen_Fase7', N'C') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.Ventas WITH CHECK ADD CONSTRAINT CK_Ventas_Almacen_Fase7 CHECK ((BodegaId IS NULL AND UbicacionId IS NULL) OR (BodegaId IS NOT NULL AND UbicacionId IS NOT NULL));';

 
    -- Un objeto existente solo se reutiliza si su definición mantiene las garantías de esta fase.
    DECLARE @columnasIncompatibles nvarchar(2048);
    SELECT @columnasIncompatibles = STRING_AGG(CONVERT(nvarchar(max), CONCAT(esperado.nombre, N': tipo=', TYPE_NAME(c.system_type_id), N', longitud=',c.max_length,N', nullable=',c.is_nullable,N', identidad=',c.is_identity,N', precisión=',c.precision,N', escala=',c.scale)), N'; ') FROM (VALUES
          (N'Id', N'bigint', 8, 0, 0, 1),
          (N'VentaDetalleId', N'bigint', 8, 0, 0, 0),
          (N'ImpuestoId', N'int', 4, 0, 0, 0),
          (N'Codigo', N'varchar', 20, 0, 0, 0),
          (N'Nombre', N'varchar', 100, 0, 0, 0),
          (N'Porcentaje', N'decimal', 5, 9, 6, 0),
          (N'IncluidoEnPrecio', N'bit', 1, 0, 0, 0),
          (N'BaseImponible', N'decimal', 9, 12, 2, 0),
          (N'Monto', N'decimal', 9, 12, 2, 0)
        ) AS esperado(nombre, tipo, longitud, precisionEsperada, escala, identidad)
        LEFT JOIN sys.columns c ON c.object_id=OBJECT_ID(N'dbo.VentaDetalleImpuestos') AND c.name=esperado.nombre
        WHERE c.column_id IS NULL OR TYPE_NAME(c.system_type_id)<>esperado.tipo
          OR c.max_length<>esperado.longitud OR c.is_nullable<>0 OR c.is_identity<>esperado.identidad
          OR (esperado.tipo=N'decimal' AND (c.precision<>esperado.precisionEsperada OR c.scale<>esperado.escala))
    ;
    IF @columnasIncompatibles IS NOT NULL
    BEGIN
        SET @columnasIncompatibles = N'VentaDetalleImpuestos incompatible: ' + @columnasIncompatibles;
        THROW 51070, @columnasIncompatibles, 1;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.VentaDetalleImpuestos') AND name=N'UQ_VentaDetalleImpuestos')
        EXEC sys.sp_executesql N'CREATE UNIQUE INDEX UQ_VentaDetalleImpuestos ON dbo.VentaDetalleImpuestos(VentaDetalleId, ImpuestoId);';
    IF OBJECT_ID(N'dbo.FK_VentaDetalleImpuestos_Detalle', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentaDetalleImpuestos WITH CHECK ADD CONSTRAINT FK_VentaDetalleImpuestos_Detalle FOREIGN KEY(VentaDetalleId) REFERENCES dbo.VentasDetalle(Id);';
    IF OBJECT_ID(N'dbo.FK_VentaDetalleImpuestos_Impuesto', N'F') IS NULL
        EXEC sys.sp_executesql N'ALTER TABLE dbo.VentaDetalleImpuestos WITH CHECK ADD CONSTRAINT FK_VentaDetalleImpuestos_Impuesto FOREIGN KEY(ImpuestoId) REFERENCES dbo.Impuestos(Id);';

    IF EXISTS (
        SELECT 1 FROM (VALUES
          (N'FK_Ventas_BodegaId_Fase7', N'Ventas', N'BodegaId', N'Bodegas'),
          (N'FK_Ventas_UbicacionId_Fase7', N'Ventas', N'UbicacionId', N'Ubicaciones'),
          (N'FK_Ventas_ModificadoPorUsuarioId_Fase7', N'Ventas', N'ModificadoPorUsuarioId', N'Usuarios'),
          (N'FK_Ventas_CompletadoPorUsuarioId_Fase7', N'Ventas', N'CompletadoPorUsuarioId', N'Usuarios'),
          (N'FK_Ventas_AnuladoPorUsuarioId_Fase7', N'Ventas', N'AnuladoPorUsuarioId', N'Usuarios'),
          (N'FK_VentasDetalle_UnidadMedidaId_Fase7', N'VentasDetalle', N'UnidadMedidaId', N'UnidadesMedida'),
          (N'FK_VentasDetalle_UnidadBaseId_Fase7', N'VentasDetalle', N'UnidadBaseId', N'UnidadesMedida'),
          (N'FK_VentaDetalleImpuestos_Detalle', N'VentaDetalleImpuestos', N'VentaDetalleId', N'VentasDetalle'),
          (N'FK_VentaDetalleImpuestos_Impuesto', N'VentaDetalleImpuestos', N'ImpuestoId', N'Impuestos')
        ) esperado(nombre, tabla, columna, destino)
        LEFT JOIN sys.foreign_keys fk ON fk.object_id=OBJECT_ID(N'dbo.'+esperado.nombre)
        LEFT JOIN sys.foreign_key_columns fc ON fc.constraint_object_id=fk.object_id AND fc.constraint_column_id=1
        WHERE fk.object_id IS NULL OR fk.parent_object_id<>OBJECT_ID(N'dbo.'+esperado.tabla)
          OR fk.referenced_object_id<>OBJECT_ID(N'dbo.'+esperado.destino)
          OR COL_NAME(fc.parent_object_id,fc.parent_column_id)<>esperado.columna
          OR COL_NAME(fc.referenced_object_id,fc.referenced_column_id)<>N'Id'
          OR fk.is_disabled=1 OR fk.is_not_trusted=1 OR fk.delete_referential_action<>0 OR fk.update_referential_action<>0
          OR (SELECT COUNT(*) FROM sys.foreign_key_columns f WHERE f.constraint_object_id=fk.object_id)<>1
    ) THROW 51070, 'Clave foránea Fase 7 incompatible o no verificada.', 1;

    IF EXISTS (
        SELECT 1 FROM (VALUES
          (N'Ventas',N'UX_Ventas_ClaveOperacion',N'EmpresaId',N'ClaveOperacion',1),
          (N'VentaDetalleImpuestos',N'UQ_VentaDetalleImpuestos',N'VentaDetalleId',N'ImpuestoId',0)
        ) esperado(tabla, indice, primera, segunda, filtrado)
        LEFT JOIN sys.indexes i ON i.object_id=OBJECT_ID(N'dbo.'+esperado.tabla) AND i.name=esperado.indice
        WHERE i.index_id IS NULL OR i.is_unique<>1 OR i.is_disabled<>0 OR i.has_filter<>esperado.filtrado
          OR INDEX_COL(N'dbo.'+esperado.tabla,i.index_id,1)<>esperado.primera
          OR INDEX_COL(N'dbo.'+esperado.tabla,i.index_id,2)<>esperado.segunda
          OR (SELECT COUNT(*) FROM sys.index_columns c WHERE c.object_id=i.object_id AND c.index_id=i.index_id AND c.key_ordinal>0)<>2
          OR (esperado.filtrado=1 AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(UPPER(i.filter_definition),N'[',N''),N']',N''),N'(',N''),N')',N''),N' ',N'')<>N'CLAVEOPERACIONISNOTNULL')
    ) THROW 51070, 'Índice Fase 7 existente con claves o filtro incompatible.', 1;

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name IN (
        N'CK_VentasDetalle_Snapshot_Fase7',N'CK_Ventas_Almacen_Fase7',
        N'CK_VentaDetalleImpuestos_Porcentaje',N'CK_VentaDetalleImpuestos_Base',N'CK_VentaDetalleImpuestos_Monto')
        AND (is_disabled=1 OR is_not_trusted=1))
        THROW 51070, 'Restricción Fase 7 deshabilitada o no verificada.', 1;

    COMMIT TRANSACTION;
    PRINT N'FASE 7: esquema de ventas preparado. No se crearon ventas ni catalogos. Ejecute --preparar-fase7 explicitamente despues de revisar la configuracion.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
