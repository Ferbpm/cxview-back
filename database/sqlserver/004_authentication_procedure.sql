-- Retrieve an active credential for application-side PBKDF2 verification.
USE [cxView];
GO

IF NOT EXISTS (
  SELECT 1 FROM sys.indexes
  WHERE object_id = OBJECT_ID(N'dbo.TAS006') AND name = N'UX_TAS006_USUARIO'
)
  CREATE UNIQUE INDEX [UX_TAS006_USUARIO] ON [dbo].[TAS006] ([USUARIO]);
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAS006_OBTENER_AUTENTICABLE]
  @usuario varchar(20)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT TOP (1)
    [IDUSER],
    [USUARIO],
    CONVERT(varchar(128), [CLAVE]) AS [PASSWORD_HASH],
    [NOMBRES],
    [APEPAT],
    [APEMAT],
    [EMAIL],
    [IDSEDEDEFAULT]
  FROM [dbo].[TAS006]
  WHERE [USUARIO] = LTRIM(RTRIM(@usuario))
    AND [ESTUSER] = 'A'
    AND [SACRG] = 1;
END;
GO
