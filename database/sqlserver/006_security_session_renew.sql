/* Renueva una sesión activa sin crear otra ni repetir OTP. */
CREATE OR ALTER PROCEDURE usp_security_session_renew
  @p_session_id uniqueidentifier,
  @p_user_id uniqueidentifier,
  @p_now datetimeoffset,
  @p_expires_at datetimeoffset
AS
BEGIN
  SET NOCOUNT ON;
  UPDATE security_sessions SET expires_at=@p_expires_at
  WHERE id=@p_session_id AND user_id=@p_user_id AND is_revoked=0 AND expires_at>@p_now;
  SELECT CONVERT(int,@@ROWCOUNT) AS rows_affected;
END;
GO
