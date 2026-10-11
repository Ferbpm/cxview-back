DELIMITER $$
DROP PROCEDURE IF EXISTS usp_security_session_renew$$
CREATE PROCEDURE usp_security_session_renew(IN p_session_id char(36),IN p_user_id char(36),IN p_now datetime(6),IN p_expires_at datetime(6))
BEGIN
  UPDATE security_sessions SET expires_at=p_expires_at
  WHERE id=p_session_id AND user_id=p_user_id AND is_revoked=0 AND expires_at>p_now;
  SELECT ROW_COUNT() AS rows_affected;
END$$
DELIMITER ;
