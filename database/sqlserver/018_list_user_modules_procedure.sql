USE [cxView];
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAS008_LISTAR_MODULOS_USUARIO]
  @usuario varchar(20)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    module.[IDMOD],
    app.[IDAPP],
    app.[NOMAPP],
    module.[NOMMOD],
    module.[DESCMOD]
  FROM [dbo].[TAS006] AS [user]
  INNER JOIN [dbo].[TAS008] AS assignment ON assignment.[IDUSER] = [user].[IDUSER]
    AND assignment.[ESTMODUSU] = 'A' AND assignment.[SACRG] = 1
  INNER JOIN [dbo].[TAS007] AS module ON module.[IDMOD] = assignment.[IDMOD]
    AND module.[ESTMOD] = 'A' AND module.[SACRG] = 1
  INNER JOIN [dbo].[TAS002] AS app ON app.[IDAPP] = module.[IDAPP]
    AND app.[ESTAPP] = 'A' AND app.[SACRG] = 1
  WHERE [user].[USUARIO] = LTRIM(RTRIM(@usuario))
    AND [user].[ESTUSER] = 'A' AND [user].[SACRG] = 1
  ORDER BY app.[NOMAPP], module.[NOMMOD];
END;
GO
