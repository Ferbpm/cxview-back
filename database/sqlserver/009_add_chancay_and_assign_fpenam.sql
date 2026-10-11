-- Create the Chancay site and assign it to fpenam.
USE [cxView];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @idempresa varchar(36) = (
  SELECT TOP (1) d.[IDEMPRESA]
  FROM [dbo].[TAS006] u
  JOIN [dbo].[TAS003] d ON d.[IDDOM] = u.[IDDOMINIO]
  WHERE u.[USUARIO] = 'fpenam' AND u.[SACRG] = 1
);
DECLARE @idsede varchar(36) = COALESCE(
  (SELECT TOP (1) s.[IDSEDE] FROM [dbo].[TAS023] s WHERE s.[IDEMPRESA] = @idempresa AND s.[NOMSEDE] = 'Chancay'),
  'c921472a-709c-46ab-80a1-88d4478f5e18'
);

UPDATE [dbo].[TAS023]
SET [ESTSEDE] = 'A', [SACRG] = 1, [CULUSA] = 'seed', [FCHHRAACT] = SYSDATETIME()
WHERE [IDSEDE] = @idsede;

INSERT INTO [dbo].[TAS023] ([IDSEDE], [IDEMPRESA], [NOMSEDE], [NOMABRV], [ESTSEDE], [SACRG], [CUSCRT], [FCHHRACRT])
SELECT @idsede, @idempresa, 'Chancay', 'CHANCAY', 'A', 1, 'seed', SYSDATETIME()
WHERE @idempresa IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[TAS023] WHERE [IDSEDE] = @idsede);

DECLARE @iduser varchar(36) = (SELECT TOP (1) [IDUSER] FROM [dbo].[TAS006] WHERE [USUARIO] = 'fpenam' AND [SACRG] = 1);

UPDATE [dbo].[TAS024]
SET [ESTSEDEUSER] = 'A', [SACRG] = 1, [CULUSA] = 'seed', [FCHHRAACT] = SYSDATETIME()
WHERE [IDUSER] = @iduser AND [IDSEDE] = @idsede;

INSERT INTO [dbo].[TAS024] ([IDSEDEUSER], [IDSEDE], [IDUSER], [ESTSEDEUSER], [SACRG], [CUSCRT], [FCHHRACRT])
SELECT 'ba5b15fe-b71f-47c9-aa37-d75468cafe10', @idsede, @iduser, 'A', 1, 'seed', SYSDATETIME()
WHERE @iduser IS NOT NULL
  AND EXISTS (SELECT 1 FROM [dbo].[TAS023] WHERE [IDSEDE] = @idsede)
  AND NOT EXISTS (SELECT 1 FROM [dbo].[TAS024] WHERE [IDUSER] = @iduser AND [IDSEDE] = @idsede);

COMMIT TRANSACTION;
GO

