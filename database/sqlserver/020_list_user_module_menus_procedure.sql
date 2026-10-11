-- List active menus of an active module assigned to an active user.
USE [cxView];
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAS009_LISTAR_MENUS_USUARIO_MODULO]
  @usuario varchar(20),
  @modulo varchar(36)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    menu.[IDMENU],
    menu.[NOMMENU],
    menu.[DESCMENU]
  FROM [dbo].[TAS006] AS u
  INNER JOIN [dbo].[TAS008] AS asignacion ON asignacion.[IDUSER] = u.[IDUSER]
    AND asignacion.[IDMOD] = LTRIM(RTRIM(@modulo))
    AND asignacion.[ESTMODUSU] = 'A' AND asignacion.[SACRG] = 1
  INNER JOIN [dbo].[TAS007] AS module ON module.[IDMOD] = asignacion.[IDMOD]
    AND module.[ESTMOD] = 'A' AND module.[SACRG] = 1
  INNER JOIN [dbo].[TAS009] AS menu ON menu.[IDMOD] = module.[IDMOD]
    AND menu.[ESTMENU] = 'A' AND menu.[SACRG] = 1
  WHERE u.[USUARIO] = LTRIM(RTRIM(@usuario))
    AND u.[ESTUSER] = 'A' AND u.[SACRG] = 1
  ORDER BY menu.[NOMMENU];
END;
GO
