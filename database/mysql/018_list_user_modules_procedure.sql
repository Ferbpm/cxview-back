USE `cxView`;

DROP PROCEDURE IF EXISTS `SP_TAS008_LISTAR_MODULOS_USUARIO`;
DELIMITER $$
CREATE PROCEDURE `SP_TAS008_LISTAR_MODULOS_USUARIO`(IN usuario varchar(20))
READS SQL DATA
BEGIN
  SELECT
    module.`IDMOD`,
    app.`IDAPP`,
    app.`NOMAPP`,
    module.`NOMMOD`,
    module.`DESCMOD`
  FROM `TAS006` AS `user`
  INNER JOIN `TAS008` AS assignment ON assignment.`IDUSER` = `user`.`IDUSER`
    AND assignment.`ESTMODUSU` = 'A' AND assignment.`SACRG` = 1
  INNER JOIN `TAS007` AS module ON module.`IDMOD` = assignment.`IDMOD`
    AND module.`ESTMOD` = 'A' AND module.`SACRG` = 1
  INNER JOIN `TAS002` AS app ON app.`IDAPP` = module.`IDAPP`
    AND app.`ESTAPP` = 'A' AND app.`SACRG` = 1
  WHERE `user`.`USUARIO` = TRIM(usuario)
    AND `user`.`ESTUSER` = 'A' AND `user`.`SACRG` = 1
  ORDER BY app.`NOMAPP`, module.`NOMMOD`;
END$$
DELIMITER ;
