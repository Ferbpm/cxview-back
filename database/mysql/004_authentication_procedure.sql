-- Retrieve an active credential for application-side PBKDF2 verification.
USE `cxView`;

SET @ddl = IF(
  NOT EXISTS (
    SELECT 1 FROM information_schema.statistics
    WHERE table_schema = DATABASE() AND table_name = 'tas006'
      AND index_name = 'UX_TAS006_USUARIO'
  ),
  'CREATE UNIQUE INDEX `UX_TAS006_USUARIO` ON `TAS006` (`USUARIO`)',
  'SELECT 1'
);
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

DROP PROCEDURE IF EXISTS `SP_TAS006_OBTENER_AUTENTICABLE`;
DELIMITER $$
CREATE PROCEDURE `SP_TAS006_OBTENER_AUTENTICABLE`(IN usuario varchar(20))
READS SQL DATA
BEGIN
  SELECT
    u.`IDUSER`,
    u.`USUARIO`,
    CONVERT(u.`CLAVE` USING utf8mb4) AS `PASSWORD_HASH`,
    u.`NOMBRES`,
    u.`APEPAT`,
    u.`APEMAT`,
    u.`EMAIL`,
    u.`IDSEDEDEFAULT`
  FROM `TAS006` AS u
  WHERE u.`USUARIO` = TRIM(usuario)
    AND u.`ESTUSER` = 'A'
    AND u.`SACRG` = 1
  LIMIT 1;
END$$
DELIMITER ;
