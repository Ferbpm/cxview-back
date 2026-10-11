-- Empresa submenu under the Maestros menu of the Security module.
USE `cxView`;

UPDATE `TAS011`
SET `DESCSUBMNU` = 'EMPRESA', `ESTSUBMNU` = 'A', `SACRG` = 1, `CULUSA` = 'ADMIN', `FCHHRAACT` = CURRENT_TIMESTAMP(6)
WHERE `IDMENU` = 'd6d3b9db-c95f-4d24-b5b7-ea15fd92a908'
  AND `NOMSUBMNU` = 'EMPRESA'
  AND (`DESCSUBMNU` <> 'EMPRESA' OR `ESTSUBMNU` <> 'A' OR `SACRG` <> 1);

INSERT INTO `TAS011` (`IDSUBMENU`, `IDMENU`, `NOMSUBMNU`, `DESCSUBMNU`, `ESTSUBMNU`, `SACRG`, `CUSCRT`, `FCHHRACRT`, `CULUSA`, `FCHHRAACT`)
SELECT '8b1c2c05-5b3d-48b2-9e12-6e2621dcce6b', 'd6d3b9db-c95f-4d24-b5b7-ea15fd92a908', 'EMPRESA', 'EMPRESA', 'A', 1, 'ADMIN', CURRENT_TIMESTAMP(6), NULL, NULL
WHERE NOT EXISTS (
  SELECT 1 FROM `TAS011`
  WHERE `IDMENU` = 'd6d3b9db-c95f-4d24-b5b7-ea15fd92a908' AND `NOMSUBMNU` = 'EMPRESA'
);

DROP PROCEDURE IF EXISTS `SP_TAS011_LISTAR_SUBMENUS_USUARIO_MENU`;
DELIMITER $$
CREATE PROCEDURE `SP_TAS011_LISTAR_SUBMENUS_USUARIO_MENU`(
  IN usuario varchar(20),
  IN menu varchar(36)
)
READS SQL DATA
BEGIN
  SELECT
    submenu.`IDSUBMENU`,
    submenu.`NOMSUBMNU`,
    submenu.`DESCSUBMNU`
  FROM `TAS006` AS u
  INNER JOIN `TAS008` AS asignacion ON asignacion.`IDUSER` = u.`IDUSER`
    AND asignacion.`ESTMODUSU` = 'A' AND asignacion.`SACRG` = 1
  INNER JOIN `TAS007` AS module ON module.`IDMOD` = asignacion.`IDMOD`
    AND module.`ESTMOD` = 'A' AND module.`SACRG` = 1
  INNER JOIN `TAS009` AS parent_menu ON parent_menu.`IDMOD` = module.`IDMOD`
    AND parent_menu.`IDMENU` = TRIM(menu)
    AND parent_menu.`ESTMENU` = 'A' AND parent_menu.`SACRG` = 1
  INNER JOIN `TAS011` AS submenu ON submenu.`IDMENU` = parent_menu.`IDMENU`
    AND submenu.`ESTSUBMNU` = 'A' AND submenu.`SACRG` = 1
  WHERE u.`USUARIO` = TRIM(usuario)
    AND u.`ESTUSER` = 'A' AND u.`SACRG` = 1
  ORDER BY submenu.`NOMSUBMNU`;
END$$
DELIMITER ;
