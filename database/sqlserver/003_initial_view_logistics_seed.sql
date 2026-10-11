-- Initial View Logistics configuration for SQL Server.
-- The test password is stored only as a salted PBKDF2-SHA512 hash.
USE [cxView];
GO

IF COL_LENGTH(N'dbo.TAS006', N'CLAVE') < 128
  ALTER TABLE [dbo].[TAS006] ALTER COLUMN [CLAVE] varbinary(128) NULL;
GO

IF COL_LENGTH(N'dbo.TAS023', N'IDEMPRESA') IS NULL
  ALTER TABLE [dbo].[TAS023] ADD [IDEMPRESA] varchar(36) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TAS023') AND name = N'IX_TAS023_IDEMPRESA')
  CREATE INDEX [IX_TAS023_IDEMPRESA] ON [dbo].[TAS023] ([IDEMPRESA]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.TAS023') AND name = N'FK_TAS023_IDEMPRESA')
  ALTER TABLE [dbo].[TAS023] ADD CONSTRAINT [FK_TAS023_IDEMPRESA]
    FOREIGN KEY ([IDEMPRESA]) REFERENCES [dbo].[TAS001] ([IDEMPRESA]);
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @idempresa varchar(36) = COALESCE(
  (SELECT TOP (1) [IDEMPRESA] FROM [dbo].[TAS001] WHERE [NOMEMPRESA] = 'View Logistics'),
  '6e0ba5b7-ce45-4aaf-a785-b049e5dfe6ec'
);
IF NOT EXISTS (SELECT 1 FROM [dbo].[TAS001] WHERE [NOMEMPRESA] = 'View Logistics')
  INSERT INTO [dbo].[TAS001] ([IDEMPRESA], [NOMEMPRESA], [ESTEMP], [SACRG], [CUSCRT])
  VALUES (@idempresa, 'View Logistics', 'A', 1, 'seed');

DECLARE @idapp varchar(36) = COALESCE(
  (SELECT TOP (1) [IDAPP] FROM [dbo].[TAS002] WHERE [IDEMPRESA] = @idempresa AND [NOMAPP] = 'Web View Logistics'),
  'c7fff4ee-f127-497e-ae0c-bcc6ad924acf'
);
IF NOT EXISTS (SELECT 1 FROM [dbo].[TAS002] WHERE [IDEMPRESA] = @idempresa AND [NOMAPP] = 'Web View Logistics')
  INSERT INTO [dbo].[TAS002] ([IDAPP], [IDEMPRESA], [NOMAPP], [DESCAPP], [ESTAPP], [SACRG], [CUSCRT])
  VALUES (@idapp, @idempresa, 'Web View Logistics', 'Aplicativo web de View Logistics', 'A', 1, 'seed');

DECLARE @iddominio varchar(36) = COALESCE(
  (SELECT TOP (1) [IDDOM] FROM [dbo].[TAS003] WHERE [IDEMPRESA] = @idempresa AND [NOMDOM] = 'viewlogistics'),
  '06e4cab1-3b14-4ab7-ab99-927f555af51c'
);
IF NOT EXISTS (SELECT 1 FROM [dbo].[TAS003] WHERE [IDEMPRESA] = @idempresa AND [NOMDOM] = 'viewlogistics')
  INSERT INTO [dbo].[TAS003] ([IDDOM], [IDEMPRESA], [NOMDOM], [DESCDOM], [ESTDOM], [SACRG], [CUSCRT])
  VALUES (@iddominio, @idempresa, 'viewlogistics', 'Dominio de View Logistics', 'A', 1, 'seed');

DECLARE @idarea varchar(36) = COALESCE(
  (SELECT TOP (1) [IDAREA] FROM [dbo].[TAS004] WHERE [IDEMPRESA] = @idempresa AND [NOMAREA] = 'Sistemas'),
  '1a24ebeb-6fda-418c-a6d6-dc173d028687'
);
IF NOT EXISTS (SELECT 1 FROM [dbo].[TAS004] WHERE [IDEMPRESA] = @idempresa AND [NOMAREA] = 'Sistemas')
  INSERT INTO [dbo].[TAS004] ([IDAREA], [IDEMPRESA], [NOMAREA], [DESCAREA], [ESTAREA], [SACRG], [CUSCRT])
  VALUES (@idarea, @idempresa, 'Sistemas', 'Área de sistemas', 'A', 1, 'seed');

DECLARE @idrol varchar(36) = COALESCE(
  (SELECT TOP (1) [IDROLEMP] FROM [dbo].[TAS005] WHERE [IDAREA] = @idarea AND [NOMROL] = 'Analista'),
  '6ab98dd5-11ae-444a-9832-db30a60dde7a'
);
IF NOT EXISTS (SELECT 1 FROM [dbo].[TAS005] WHERE [IDAREA] = @idarea AND [NOMROL] = 'Analista')
  INSERT INTO [dbo].[TAS005] ([IDROLEMP], [IDAREA], [NOMROL], [DESCROL], [ESTROL], [SACRG], [CUSCRT])
  VALUES (@idrol, @idarea, 'Analista', 'Analista del área de sistemas', 'A', 1, 'seed');

DECLARE @idsede varchar(36) = COALESCE(
  (SELECT TOP (1) [IDSEDE] FROM [dbo].[TAS023] WHERE [IDEMPRESA] = @idempresa AND [NOMSEDE] = 'Callao'),
  '99c8729a-4b05-4183-b0e4-02ef916e3784'
);
IF NOT EXISTS (SELECT 1 FROM [dbo].[TAS023] WHERE [IDEMPRESA] = @idempresa AND [NOMSEDE] = 'Callao')
  INSERT INTO [dbo].[TAS023] ([IDSEDE], [IDEMPRESA], [NOMSEDE], [NOMABRV], [ESTSEDE], [SACRG], [CUSCRT])
  VALUES (@idsede, @idempresa, 'Callao', 'CALLAO', 'A', 1, 'seed');

IF NOT EXISTS (SELECT 1 FROM [dbo].[TAS006] WHERE [USUARIO] = 'fpenam')
  INSERT INTO [dbo].[TAS006] ([IDUSER], [IDDOMINIO], [IDROLEMP], [USUARIO], [CLAVE], [NOMBRES], [APEPAT], [APEMAT], [EMAIL], [ESTUSER], [FLGACTPASS], [FLGMLTSEDE], [IDSEDEDEFAULT], [SACRG], [CUSCRT])
  VALUES ('df904178-ea36-4d82-9dfc-bb4ddaaba15c', @iddominio, @idrol, 'fpenam',
          0x70626b6466322d73686135313224323130303030244d66376f4136632b2b375a71765748737551303672444846654f4b3375534e7164635a534254446872496b3d2459352f4b62323652705a323770767159316e374f73446b7235782b666f343373476c514641666d727949673d,
          'Fernando', 'Peña', 'Mendoza', 'uxtecnologia@viewlc.com.pe', 'A', 1, 0, @idsede, 1, 'seed');

IF NOT EXISTS (SELECT 1 FROM [dbo].[TAS007] WHERE [IDAPP] = @idapp AND [NOMMOD] = 'Logística integrada')
  INSERT INTO [dbo].[TAS007] ([IDMOD], [IDAPP], [NOMMOD], [DESCMOD], [ESTMOD], [SACRG], [CUSCRT])
  VALUES ('9b9664c5-2eb6-4219-a60e-5c13f1b87d3d', @idapp, 'Logística integrada', 'Módulo de logística integrada', 'A', 1, 'seed');
IF NOT EXISTS (SELECT 1 FROM [dbo].[TAS007] WHERE [IDAPP] = @idapp AND [NOMMOD] = 'Deposito Temporal')
  INSERT INTO [dbo].[TAS007] ([IDMOD], [IDAPP], [NOMMOD], [DESCMOD], [ESTMOD], [SACRG], [CUSCRT])
  VALUES ('d19cc184-c9ba-423e-a734-d6bfba26bb1f', @idapp, 'Deposito Temporal', 'Módulo de depósito temporal', 'A', 1, 'seed');
IF NOT EXISTS (SELECT 1 FROM [dbo].[TAS007] WHERE [IDAPP] = @idapp AND [NOMMOD] = 'Seguridad')
  INSERT INTO [dbo].[TAS007] ([IDMOD], [IDAPP], [NOMMOD], [DESCMOD], [ESTMOD], [SACRG], [CUSCRT])
  VALUES ('5e5a2700-47e6-48c0-bf3a-9c53329a082f', @idapp, 'Seguridad', 'Módulo de seguridad', 'A', 1, 'seed');

COMMIT TRANSACTION;
GO
