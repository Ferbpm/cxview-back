-- Assign the Callao site to user fpenam.
USE [cxView];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

UPDATE a
SET a.[ESTSEDEUSER] = 'A',
    a.[SACRG] = 1,
    a.[CULUSA] = 'seed',
    a.[FCHHRAACT] = SYSDATETIME()
FROM [dbo].[TAS024] a
JOIN [dbo].[TAS006] u ON u.[IDUSER] = a.[IDUSER] AND u.[USUARIO] = 'fpenam'
JOIN [dbo].[TAS023] s ON s.[IDSEDE] = a.[IDSEDE] AND s.[NOMSEDE] = 'Callao';

INSERT INTO [dbo].[TAS024] ([IDSEDEUSER], [IDSEDE], [IDUSER], [ESTSEDEUSER], [SACRG], [CUSCRT], [FCHHRACRT])
SELECT
  'f9095b5c-2009-4bd3-a998-17c7220d3964',
  s.[IDSEDE],
  u.[IDUSER],
  'A', 1, 'seed', SYSDATETIME()
FROM [dbo].[TAS006] u
JOIN [dbo].[TAS023] s ON s.[NOMSEDE] = 'Callao' AND s.[SACRG] = 1
WHERE u.[USUARIO] = 'fpenam' AND u.[SACRG] = 1
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[TAS024] a
    WHERE a.[IDUSER] = u.[IDUSER] AND a.[IDSEDE] = s.[IDSEDE]
  );

COMMIT TRANSACTION;
GO
