-- Opciones de la vista Empresa y asignación a fpenam (MySQL 8).
USE `cxView`;

SET @company_submenu = '8b1c2c05-5b3d-48b2-9e12-6e2621dcce6b';
SET @user_id = (
  SELECT `IDUSER` FROM `TAS006`
  WHERE LOWER(`USUARIO`) = 'fpenam' AND `ESTUSER` = 'A' AND `SACRG` = 1
  LIMIT 1
);

INSERT INTO `TAS015` (`IDOPCFOMR`, `IDSUBMENU`, `NOMOPC`, `DESCOPC`, `ESTOPC`, `SACRG`, `CUSCRT`, `FCHHRACRT`)
SELECT UUID(), @company_submenu, x.code, x.description, 'A', 1, 'ADMIN', CURRENT_TIMESTAMP(6)
FROM (
  SELECT 'company.create' AS code, 'Nueva empresa' AS description
  UNION ALL SELECT 'company.update', 'Editar empresa'
  UNION ALL SELECT 'company.delete', 'Dar de baja empresa'
  UNION ALL SELECT 'company.filter', 'Filtrar empresas'
) AS x
WHERE NOT EXISTS (
  SELECT 1 FROM `TAS015` o
  WHERE o.`IDSUBMENU` = @company_submenu AND o.`NOMOPC` = x.code
);

UPDATE `TAS015`
SET `ESTOPC` = 'A', `SACRG` = 1, `CULUSA` = 'ADMIN', `FCHHRAACT` = CURRENT_TIMESTAMP(6)
WHERE `IDSUBMENU` = @company_submenu
  AND `NOMOPC` IN ('company.create', 'company.update', 'company.delete', 'company.filter');

UPDATE `TAS016` a
JOIN `TAS015` o ON o.`IDOPCFOMR` = a.`IDOPCFOMR`
SET a.`ESTOPC` = 'A', a.`SACRG` = 1, a.`CULUSA` = 'ADMIN', a.`FCHHRAACT` = CURRENT_TIMESTAMP(6)
WHERE a.`IDUSER` = @user_id
  AND o.`IDSUBMENU` = @company_submenu
  AND o.`ESTOPC` = 'A'
  AND o.`SACRG` = 1;

INSERT INTO `TAS016` (`IDOPCFORMUSU`, `IDOPCFOMR`, `IDUSER`, `ESTOPC`, `SACRG`, `CUSCRT`, `FCHHRACRT`)
SELECT UUID(), o.`IDOPCFOMR`, @user_id, 'A', 1, 'ADMIN', CURRENT_TIMESTAMP(6)
FROM `TAS015` o
WHERE @user_id IS NOT NULL
  AND o.`IDSUBMENU` = @company_submenu
  AND o.`ESTOPC` = 'A'
  AND o.`SACRG` = 1
  AND NOT EXISTS (
    SELECT 1 FROM `TAS016` a
    WHERE a.`IDUSER` = @user_id AND a.`IDOPCFOMR` = o.`IDOPCFOMR`
  );
