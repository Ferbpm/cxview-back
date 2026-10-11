-- Empresa submenu under the Maestros menu of the Security module.
USE [cxView];
GO

UPDATE [dbo].[TAS011]
SET [DESCSUBMNU] = 'EMPRESA', [ESTSUBMNU] = 'A', [SACRG] = 1, [CULUSA] = 'ADMIN', [FCHHRAACT] = SYSDATETIME()
WHERE [IDMENU] = 'd6d3b9db-c95f-4d24-b5b7-ea15fd92a908'
  AND [NOMSUBMNU] = 'EMPRESA'
  AND ([DESCSUBMNU] <> 'EMPRESA' OR [ESTSUBMNU] <> 'A' OR [SACRG] <> 1);
GO

IF NOT EXISTS (
  SELECT 1 FROM [dbo].[TAS011]
  WHERE [IDMENU] = 'd6d3b9db-c95f-4d24-b5b7-ea15fd92a908' AND [NOMSUBMNU] = 'EMPRESA'
)
  INSERT INTO [dbo].[TAS011] ([IDSUBMENU], [IDMENU], [NOMSUBMNU], [DESCSUBMNU], [ESTSUBMNU], [SACRG], [CUSCRT], [FCHHRACRT], [CULUSA], [FCHHRAACT])
  VALUES ('8b1c2c05-5b3d-48b2-9e12-6e2621dcce6b', 'd6d3b9db-c95f-4d24-b5b7-ea15fd92a908', 'EMPRESA', 'EMPRESA', 'A', 1, 'ADMIN', SYSDATETIME(), NULL, NULL);
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAS011_LISTAR_SUBMENUS_USUARIO_MENU]
  @usuario varchar(20),
  @menu varchar(36)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    submenu.[IDSUBMENU],
    submenu.[NOMSUBMNU],
    submenu.[DESCSUBMNU]
  FROM [dbo].[TAS006] AS u
  INNER JOIN [dbo].[TAS008] AS asignacion ON asignacion.[IDUSER] = u.[IDUSER]
    AND asignacion.[ESTMODUSU] = 'A' AND asignacion.[SACRG] = 1
  INNER JOIN [dbo].[TAS007] AS module ON module.[IDMOD] = asignacion.[IDMOD]
    AND module.[ESTMOD] = 'A' AND module.[SACRG] = 1
  INNER JOIN [dbo].[TAS009] AS parent_menu ON parent_menu.[IDMOD] = module.[IDMOD]
    AND parent_menu.[IDMENU] = LTRIM(RTRIM(@menu))
    AND parent_menu.[ESTMENU] = 'A' AND parent_menu.[SACRG] = 1
  INNER JOIN [dbo].[TAS011] AS submenu ON submenu.[IDMENU] = parent_menu.[IDMENU]
    AND submenu.[ESTSUBMNU] = 'A' AND submenu.[SACRG] = 1
  WHERE u.[USUARIO] = LTRIM(RTRIM(@usuario))
    AND u.[ESTUSER] = 'A' AND u.[SACRG] = 1
  ORDER BY submenu.[NOMSUBMNU];
END;
GO
