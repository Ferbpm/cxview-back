-- Password reset for TAS006 users (SQL Server).
-- @clave NULL or empty resets to the default password 123456.
-- Any non-empty @clave must be a PBKDF2-SHA512 hash created by the backend.
USE [cxView];
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAS006_REINICIAR_CLAVE]
  @iduser varchar(36),
  @clave varbinary(128),
  @usuario varchar(20)
AS
BEGIN
  SET NOCOUNT ON;

  DECLARE @clave_defecto varbinary(128) = 0x70626b6466322d73686135313224323130303030244d66376f4136632b2b375a71765748737551303672444846654f4b3375534e7164635a534254446872496b3d2459352f4b62323652705a323770767159316e374f73446b7235782b666f343373476c514641666d727949673d;
  DECLARE @filasafectadas int;

  UPDATE [dbo].[TAS006]
  SET [CLAVE] = CASE WHEN @clave IS NULL OR DATALENGTH(@clave) = 0 THEN @clave_defecto ELSE @clave END,
      [FLGACTPASS] = CASE WHEN @clave IS NULL OR DATALENGTH(@clave) = 0 THEN 1 ELSE 0 END,
      [CULUSA] = NULLIF(LTRIM(RTRIM(@usuario)), ''),
      [FCHHRAACT] = SYSDATETIME()
  WHERE [IDUSER] = LTRIM(RTRIM(@iduser))
    AND [ESTUSER] = 'A'
    AND [SACRG] = 1;

  SET @filasafectadas = @@ROWCOUNT;

  SELECT [IDUSER], [USUARIO], [ESTUSER], [FLGACTPASS], [CULUSA], [FCHHRAACT],
         @filasafectadas AS [FILASAFECTADAS]
  FROM [dbo].[TAS006]
  WHERE [IDUSER] = LTRIM(RTRIM(@iduser))
    AND [ESTUSER] = 'A'
    AND [SACRG] = 1;
END;
GO
