-- List active menus of an active module assigned to an active user.
USE `cxView`;

DROP PROCEDURE IF EXISTS `SP_TAS009_LISTAR_MENUS_USUARIO_MODULO`;
DELIMITER $$
CREATE PROCEDURE `SP_TAS009_LISTAR_MENUS_USUARIO_MODULO`(
  IN usuario varchar(20),
  IN modulo varchar(36)
)
READS SQL DATA
BEGIN
  SELECT
    menu.`IDMENU`,
    menu.`NOMMENU`,
    menu.`DESCMENU`
  FROM `TAS006` AS u
  INNER JOIN `TAS008` AS asignacion ON asignacion.`IDUSER` = u.`IDUSER`
    AND asignacion.`IDMOD` = TRIM(modulo)
    AND asignacion.`ESTMODUSU` = 'A' AND asignacion.`SACRG` = 1
  INNER JOIN `TAS007` AS module ON module.`IDMOD` = asignacion.`IDMOD`
    AND module.`ESTMOD` = 'A' AND module.`SACRG` = 1
  INNER JOIN `TAS009` AS menu ON menu.`IDMOD` = module.`IDMOD`
    AND menu.`ESTMENU` = 'A' AND menu.`SACRG` = 1
  WHERE u.`USUARIO` = TRIM(usuario)
    AND u.`ESTUSER` = 'A' AND u.`SACRG` = 1
  ORDER BY menu.`NOMMENU`;
END$$
DELIMITER ;
