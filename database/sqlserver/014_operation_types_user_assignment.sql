-- Expand operation type descriptions, seed the catalog, assign fpenam, and expose the user query.
USE [cxView];
GO

ALTER TABLE [dbo].[TAM069] ALTER COLUMN [TIPOOPEP] varchar(100) NOT NULL;
GO

UPDATE [dbo].[TAM069]
SET [TIPOOPEP] = 'Exportación', [ESTTIPOP] = 'A', [SACRG] = 1, [CULUSA] = 'FPENAM', [FCHHRAACT] = SYSDATETIME()
WHERE [IDTIPOPE] = 'dki4f081-edrf-cvft-vgyu-f7fea4ecxcsw';

UPDATE [dbo].[TAM069]
SET [TIPOOPEP] = 'Importación', [ESTTIPOP] = 'A', [SACRG] = 1, [CULUSA] = 'FPENAM', [FCHHRAACT] = SYSDATETIME()
WHERE [IDTIPOPE] = 'owlef081-deda-epor-loed-f7fea4ecjfur';

IF NOT EXISTS (SELECT 1 FROM [dbo].[TAM069] WHERE [IDTIPOPE] = '706caeb5-d0c4-44e7-b792-ae3777a33462')
  INSERT INTO [dbo].[TAM069] ([IDTIPOPE], [TIPOOPEP], [ESTTIPOP], [SACRG], [CUSCRT])
  VALUES ('706caeb5-d0c4-44e7-b792-ae3777a33462', 'Operación local', 'A', 1, 'FPENAM');
GO

INSERT INTO [dbo].[TAM070] ([IDTIPOPE], [IDUSER], [ESTTIPOPEJE], [SACRG], [CUSCRT])
SELECT o.[IDTIPOPE], u.[IDUSER], 'A', 1, 'FPENAM'
FROM [dbo].[TAM069] o
JOIN [dbo].[TAS006] u ON LOWER(u.[USUARIO]) = 'fpenam'
WHERE o.[IDTIPOPE] IN (
  'dki4f081-edrf-cvft-vgyu-f7fea4ecxcsw',
  'owlef081-deda-epor-loed-f7fea4ecjfur',
  '706caeb5-d0c4-44e7-b792-ae3777a33462'
)
AND NOT EXISTS (
  SELECT 1 FROM [dbo].[TAM070] a WHERE a.[IDTIPOPE] = o.[IDTIPOPE] AND a.[IDUSER] = u.[IDUSER]
);
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAM070_LISTAR_TIPOS_OPERACION_USUARIO]
  @usuario varchar(20)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    o.[IDTIPOPE],
    o.[TIPOOPEP]
  FROM [dbo].[TAS006] u
  JOIN [dbo].[TAM070] a ON a.[IDUSER] = u.[IDUSER]
    AND a.[ESTTIPOPEJE] = 'A' AND a.[SACRG] = 1
  JOIN [dbo].[TAM069] o ON o.[IDTIPOPE] = a.[IDTIPOPE]
    AND o.[ESTTIPOP] = 'A' AND o.[SACRG] = 1
  WHERE u.[USUARIO] = LTRIM(RTRIM(@usuario))
    AND u.[ESTUSER] = 'A' AND u.[SACRG] = 1
  ORDER BY o.[TIPOOPEP];
END;
GO
