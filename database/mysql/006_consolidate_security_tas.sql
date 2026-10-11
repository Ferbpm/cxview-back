-- Consolidate the Identity backend on TAS tables and retire security_*.
USE `cxView`;

CREATE TABLE IF NOT EXISTS `TAS025` (
  `IDUSER` varchar(36) NOT NULL,
  `IDPERFIL` varchar(36) NOT NULL,
  `SACRG` boolean NOT NULL DEFAULT 1,
  `FCHHRACRT` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  CONSTRAINT `PK_TAS025` PRIMARY KEY (`IDUSER`, `IDPERFIL`),
  CONSTRAINT `FK_TAS025_IDUSER` FOREIGN KEY (`IDUSER`) REFERENCES `TAS006` (`IDUSER`),
  CONSTRAINT `FK_TAS025_IDPERFIL` FOREIGN KEY (`IDPERFIL`) REFERENCES `TAS017` (`IDPERFIL`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `TAS026` (
  `IDSESION` varchar(36) NOT NULL,
  `IDUSER` varchar(36) NOT NULL,
  `FCHHRAEXP` datetime(6) NOT NULL,
  `FLGREVO` boolean NOT NULL DEFAULT 0,
  `FCHHRACRT` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  CONSTRAINT `PK_TAS026` PRIMARY KEY (`IDSESION`),
  KEY `IX_TAS026_USUARIO_ACTIVO` (`IDUSER`, `FLGREVO`, `FCHHRAEXP`),
  CONSTRAINT `FK_TAS026_IDUSER` FOREIGN KEY (`IDUSER`) REFERENCES `TAS006` (`IDUSER`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `TAS027` (
  `IDDESAFIO` varchar(36) NOT NULL,
  `IDUSER` varchar(36) NOT NULL,
  `OTPHASH` varchar(128) NOT NULL,
  `FCHHRAEXP` datetime(6) NOT NULL,
  `FLGUSADO` boolean NOT NULL DEFAULT 0,
  `FCHHRACRT` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `FCHHRAUSO` datetime(6) NULL,
  CONSTRAINT `PK_TAS027` PRIMARY KEY (`IDDESAFIO`),
  KEY `IX_TAS027_EXPIRACION` (`FCHHRAEXP`, `FLGUSADO`),
  CONSTRAINT `FK_TAS027_IDUSER` FOREIGN KEY (`IDUSER`) REFERENCES `TAS006` (`IDUSER`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

DROP PROCEDURE IF EXISTS `migrate_legacy_security_permissions`;
DELIMITER $$
CREATE PROCEDURE `migrate_legacy_security_permissions`()
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'security_permissions') THEN
    INSERT INTO `TAS015` (`IDOPCFOMR`, `NOMOPC`, `DESCOPC`, `ESTOPC`, `SACRG`, `CUSCRT`, `FCHHRACRT`)
    SELECT p.id, LEFT(p.code,20), LEFT(p.name,50), IF(p.is_active=1,'A','I'), 1, 'migration', CURRENT_TIMESTAMP(6)
    FROM `security_permissions` p
    WHERE NOT EXISTS (SELECT 1 FROM `TAS015` t WHERE t.`IDOPCFOMR`=p.id OR t.`NOMOPC`=LEFT(p.code,20));
  END IF;
END$$
DELIMITER ;
CALL `migrate_legacy_security_permissions`();
DROP PROCEDURE `migrate_legacy_security_permissions`;

DROP TABLE IF EXISTS `security_otp_challenges`;
DROP TABLE IF EXISTS `security_sessions`;
DROP TABLE IF EXISTS `security_profile_permissions`;
DROP TABLE IF EXISTS `security_user_profiles`;
DROP TABLE IF EXISTS `security_permissions`;
DROP TABLE IF EXISTS `security_profiles`;
DROP TABLE IF EXISTS `security_users`;

DROP PROCEDURE IF EXISTS `usp_security_user_get_by_username`;
DROP PROCEDURE IF EXISTS `usp_security_user_get_by_id`;
DROP PROCEDURE IF EXISTS `usp_security_permission_codes_by_user`;
DROP PROCEDURE IF EXISTS `usp_security_session_create`;
DROP PROCEDURE IF EXISTS `usp_security_session_is_active`;
DROP PROCEDURE IF EXISTS `usp_security_session_revoke`;
DROP PROCEDURE IF EXISTS `usp_security_otp_create`;
DROP PROCEDURE IF EXISTS `usp_security_otp_get`;
DROP PROCEDURE IF EXISTS `usp_security_otp_consume`;
DROP PROCEDURE IF EXISTS `usp_security_users_list`;
DROP PROCEDURE IF EXISTS `usp_security_user_create`;
DROP PROCEDURE IF EXISTS `usp_security_user_update`;
DROP PROCEDURE IF EXISTS `usp_security_user_deactivate`;
DROP PROCEDURE IF EXISTS `usp_security_profiles_list`;
DROP PROCEDURE IF EXISTS `usp_security_profile_create`;
DROP PROCEDURE IF EXISTS `usp_security_profile_update`;
DROP PROCEDURE IF EXISTS `usp_security_permissions_list`;
DROP PROCEDURE IF EXISTS `usp_security_user_profile_assign`;
DROP PROCEDURE IF EXISTS `usp_security_profile_permissions_replace`;

DELIMITER $$
CREATE PROCEDURE `usp_security_user_get_by_id`(IN p_user_id varchar(36))
BEGIN SELECT `IDUSER` Id,`USUARIO` UserName,`EMAIL` Email,CONVERT(`CLAVE` USING utf8mb4) PasswordHash,(`ESTUSER`='A' AND `SACRG`=1) IsActive FROM `TAS006` WHERE `IDUSER`=p_user_id AND `SACRG`=1; END$$
CREATE PROCEDURE `usp_security_permission_codes_by_user`(IN p_user_id varchar(36))
BEGIN SELECT DISTINCT o.`NOMOPC` FROM `TAS015` o JOIN `TAS016` uo ON uo.`IDOPCFOMR`=o.`IDOPCFOMR` WHERE uo.`IDUSER`=p_user_id AND uo.`ESTOPC`='A' AND uo.`SACRG`=1 AND o.`ESTOPC`='A' AND o.`SACRG`=1 UNION SELECT DISTINCT o.`NOMOPC` FROM `TAS025` up JOIN `TAS022` po ON po.`IDPERFIL`=up.`IDPERFIL` JOIN `TAS015` o ON o.`IDOPCFOMR`=po.`IDOPCFOMR` WHERE up.`IDUSER`=p_user_id AND up.`SACRG`=1 AND po.`ESTPRFLMNU`='A' AND po.`SACRG`=1 AND o.`ESTOPC`='A' AND o.`SACRG`=1; END$$
CREATE PROCEDURE `usp_security_session_create`(IN p_id varchar(36),IN p_user_id varchar(36),IN p_expires_at datetime(6),IN p_created_at datetime(6))
BEGIN INSERT INTO `TAS026`(`IDSESION`,`IDUSER`,`FCHHRAEXP`,`FLGREVO`,`FCHHRACRT`) VALUES(p_id,p_user_id,p_expires_at,0,p_created_at); END$$
CREATE PROCEDURE `usp_security_session_is_active`(IN p_session_id varchar(36),IN p_user_id varchar(36),IN p_now datetime(6))
BEGIN SELECT IF(EXISTS(SELECT 1 FROM `TAS026` WHERE `IDSESION`=p_session_id AND `IDUSER`=p_user_id AND `FLGREVO`=0 AND `FCHHRAEXP`>p_now),1,0); END$$
CREATE PROCEDURE `usp_security_session_revoke`(IN p_session_id varchar(36),IN p_user_id varchar(36))
BEGIN UPDATE `TAS026` SET `FLGREVO`=1 WHERE `IDSESION`=p_session_id AND `IDUSER`=p_user_id; END$$
CREATE PROCEDURE `usp_security_otp_create`(IN p_id varchar(36),IN p_user_id varchar(36),IN p_otp_hash varchar(128),IN p_expires_at datetime(6),IN p_created_at datetime(6))
BEGIN INSERT INTO `TAS027`(`IDDESAFIO`,`IDUSER`,`OTPHASH`,`FCHHRAEXP`,`FLGUSADO`,`FCHHRACRT`) VALUES(p_id,p_user_id,p_otp_hash,p_expires_at,0,p_created_at); END$$
CREATE PROCEDURE `usp_security_otp_get`(IN p_challenge_id varchar(36))
BEGIN SELECT `IDDESAFIO` Id,`IDUSER` UserId,`OTPHASH` OtpHash,`FCHHRAEXP` ExpiresAt,`FLGUSADO` IsConsumed FROM `TAS027` WHERE `IDDESAFIO`=p_challenge_id; END$$
CREATE PROCEDURE `usp_security_otp_consume`(IN p_challenge_id varchar(36),IN p_now datetime(6))
BEGIN UPDATE `TAS027` SET `FLGUSADO`=1,`FCHHRAUSO`=p_now WHERE `IDDESAFIO`=p_challenge_id AND `FLGUSADO`=0 AND `FCHHRAEXP`>p_now; SELECT ROW_COUNT(); END$$
CREATE PROCEDURE `usp_security_users_list`()
BEGIN SELECT `IDUSER` Id,`USUARIO` UserName,`EMAIL` Email,(`ESTUSER`='A' AND `SACRG`=1) IsActive FROM `TAS006` WHERE `SACRG`=1 ORDER BY `USUARIO`; END$$
CREATE PROCEDURE `usp_security_user_create`(IN p_id varchar(36),IN p_user_name varchar(20),IN p_email varchar(50),IN p_password_hash varchar(128),IN p_created_at datetime(6))
BEGIN INSERT INTO `TAS006`(`IDUSER`,`USUARIO`,`EMAIL`,`CLAVE`,`ESTUSER`,`SACRG`,`FCHHRACRT`) VALUES(p_id,p_user_name,p_email,CONVERT(p_password_hash USING binary),'A',1,p_created_at); END$$
CREATE PROCEDURE `usp_security_user_update`(IN p_id varchar(36),IN p_email varchar(50),IN p_is_active boolean,IN p_updated_at datetime(6))
BEGIN UPDATE `TAS006` SET `EMAIL`=p_email,`ESTUSER`=IF(p_is_active=1,'A','I'),`FCHHRAACT`=p_updated_at WHERE `IDUSER`=p_id AND `SACRG`=1; SELECT ROW_COUNT(); END$$
CREATE PROCEDURE `usp_security_user_deactivate`(IN p_id varchar(36),IN p_updated_at datetime(6))
BEGIN UPDATE `TAS006` SET `ESTUSER`='I',`SACRG`=0,`FCHHRAACT`=p_updated_at WHERE `IDUSER`=p_id AND `SACRG`=1; SELECT ROW_COUNT(); END$$
CREATE PROCEDURE `usp_security_profiles_list`()
BEGIN SELECT `IDPERFIL` Id,`NOMPERFIL` Code,`DESCPERFIL` Name,(`ESTPERFIL`='A' AND `SACRG`=1) IsActive FROM `TAS017` WHERE `SACRG`=1 ORDER BY `DESCPERFIL`; END$$
CREATE PROCEDURE `usp_security_profile_create`(IN p_id varchar(36),IN p_code varchar(20),IN p_name varchar(100),IN p_created_at datetime(6))
BEGIN INSERT INTO `TAS017`(`IDPERFIL`,`NOMPERFIL`,`DESCPERFIL`,`ESTPERFIL`,`SACRG`,`FCHHRACRT`) VALUES(p_id,p_code,p_name,'A',1,p_created_at); END$$
CREATE PROCEDURE `usp_security_profile_update`(IN p_id varchar(36),IN p_name varchar(100),IN p_is_active boolean,IN p_updated_at datetime(6))
BEGIN UPDATE `TAS017` SET `DESCPERFIL`=p_name,`ESTPERFIL`=IF(p_is_active=1,'A','I'),`FCHHRAACT`=p_updated_at WHERE `IDPERFIL`=p_id AND `SACRG`=1; SELECT ROW_COUNT(); END$$
CREATE PROCEDURE `usp_security_permissions_list`()
BEGIN SELECT `IDOPCFOMR` Id,`NOMOPC` Code,`DESCOPC` Name FROM `TAS015` WHERE `ESTOPC`='A' AND `SACRG`=1 ORDER BY `NOMOPC`; END$$
CREATE PROCEDURE `usp_security_user_profile_assign`(IN p_user_id varchar(36),IN p_profile_id varchar(36))
BEGIN START TRANSACTION; DELETE FROM `TAS025` WHERE `IDUSER`=p_user_id; INSERT INTO `TAS025`(`IDUSER`,`IDPERFIL`) VALUES(p_user_id,p_profile_id); COMMIT; END$$
CREATE PROCEDURE `usp_security_profile_permissions_replace`(IN p_profile_id varchar(36),IN p_permission_ids_json json)
BEGIN START TRANSACTION; DELETE FROM `TAS022` WHERE `IDPERFIL`=p_profile_id; INSERT INTO `TAS022`(`IDPRFLOPC`,`IDPERFIL`,`IDOPCFOMR`,`ESTPRFLMNU`,`SACRG`,`FCHHRACRT`) SELECT UUID(),p_profile_id,j.permission_id,'A',1,CURRENT_TIMESTAMP(6) FROM JSON_TABLE(p_permission_ids_json,'$[*]' COLUMNS(permission_id varchar(36) PATH '$')) j JOIN `TAS015` o ON o.`IDOPCFOMR`=j.permission_id; COMMIT; END$$
DELIMITER ;

