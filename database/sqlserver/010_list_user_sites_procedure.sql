-- List active sites assigned to an active user.
USE [cxView];
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAS024_LISTAR_SEDES_USUARIO]
  @usuario varchar(20)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    s.[IDSEDE],
    s.[NOMSEDE],
    s.[NOMABRV],
    s.[DIRSEDE],
    s.[TELFSEDE],
    CONVERT(bit, CASE WHEN s.[IDSEDE] = u.[IDSEDEDEFAULT] THEN 1 ELSE 0 END) AS [ESPREDETERMINADA]
  FROM [dbo].[TAS006] u
  JOIN [dbo].[TAS024] a ON a.[IDUSER] = u.[IDUSER]
    AND a.[ESTSEDEUSER] = 'A' AND a.[SACRG] = 1
  JOIN [dbo].[TAS023] s ON s.[IDSEDE] = a.[IDSEDE]
    AND s.[ESTSEDE] = 'A' AND s.[SACRG] = 1
  WHERE u.[USUARIO] = LTRIM(RTRIM(@usuario))
    AND u.[ESTUSER] = 'A' AND u.[SACRG] = 1
  ORDER BY [ESPREDETERMINADA] DESC, s.[NOMSEDE];
END;
GO
