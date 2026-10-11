-- Navigation submenus and fine permissions for Menu/Submenu maintenance (MySQL 8).
USE `cxView`;

SET @parent_menu = 'd6d3b9db-c95f-4d24-b5b7-ea15fd92a908';
SET @menus_submenu = 'a9fd8380-1005-4b11-9422-523100ee0001';
SET @submenus_submenu = 'a9fd8380-1005-4b11-9422-523100ee0002';
SET @user_id = (SELECT `IDUSER` FROM `TAS006` WHERE LOWER(`USUARIO`)='fpenam' AND `ESTUSER`='A' AND `SACRG`=1 LIMIT 1);

INSERT INTO `TAS011` (`IDSUBMENU`,`IDMENU`,`NOMSUBMNU`,`DESCSUBMNU`,`ESTSUBMNU`,`SACRG`,`CUSCRT`,`FCHHRACRT`)
SELECT x.idsubmenu,@parent_menu,x.nombre,x.descripcion,'A',1,'ADMIN',CURRENT_TIMESTAMP(6)
FROM (
  SELECT @menus_submenu AS idsubmenu,'MENÚS' AS nombre,'Mantenimiento de menús' AS descripcion
  UNION ALL SELECT @submenus_submenu,'SUBMENÚS','Mantenimiento de submenús'
) x
WHERE NOT EXISTS (SELECT 1 FROM `TAS011` s WHERE s.`IDSUBMENU`=x.idsubmenu);

UPDATE `TAS011`
SET `IDMENU`=@parent_menu,`ESTSUBMNU`='A',`SACRG`=1,`CULUSA`='ADMIN',`FCHHRAACT`=CURRENT_TIMESTAMP(6)
WHERE `IDSUBMENU` IN (@menus_submenu,@submenus_submenu);

UPDATE `TAS012`
SET `ESTSMUSER`='A',`SACRG`=1,`CULUSA`='ADMIN',`FCHHRAACT`=CURRENT_TIMESTAMP(6)
WHERE `IDUSER`=@user_id AND `IDSUBMENU` IN (@menus_submenu,@submenus_submenu);

INSERT INTO `TAS012` (`IDSMUSER`,`IDSUBMENU`,`IDUSER`,`ESTSMUSER`,`SACRG`,`CUSCRT`,`FCHHRACRT`)
SELECT UUID(),s.`IDSUBMENU`,@user_id,'A',1,'ADMIN',CURRENT_TIMESTAMP(6)
FROM `TAS011` s
WHERE @user_id IS NOT NULL AND s.`IDSUBMENU` IN (@menus_submenu,@submenus_submenu)
  AND NOT EXISTS (SELECT 1 FROM `TAS012` a WHERE a.`IDUSER`=@user_id AND a.`IDSUBMENU`=s.`IDSUBMENU`);

INSERT INTO `TAS015` (`IDOPCFOMR`,`IDSUBMENU`,`NOMOPC`,`DESCOPC`,`ESTOPC`,`SACRG`,`CUSCRT`,`FCHHRACRT`)
SELECT UUID(),x.idsubmenu,x.codigo,x.descripcion,'A',1,'ADMIN',CURRENT_TIMESTAMP(6)
FROM (
  SELECT @menus_submenu AS idsubmenu,'menus.filter' AS codigo,'Filtrar menús' AS descripcion
  UNION ALL SELECT @menus_submenu,'menus.create','Nuevo menú'
  UNION ALL SELECT @menus_submenu,'menus.update','Editar menú'
  UNION ALL SELECT @menus_submenu,'menus.delete','Dar de baja menú'
  UNION ALL SELECT @submenus_submenu,'submenus.filter','Filtrar submenús'
  UNION ALL SELECT @submenus_submenu,'submenus.create','Nuevo submenú'
  UNION ALL SELECT @submenus_submenu,'submenus.update','Editar submenú'
  UNION ALL SELECT @submenus_submenu,'submenus.delete','Dar de baja submenú'
) x
WHERE NOT EXISTS (SELECT 1 FROM `TAS015` o WHERE o.`NOMOPC`=x.codigo);

UPDATE `TAS015` o
JOIN (
  SELECT @menus_submenu AS idsubmenu,'menus.filter' AS codigo UNION ALL SELECT @menus_submenu,'menus.create' UNION ALL SELECT @menus_submenu,'menus.update' UNION ALL SELECT @menus_submenu,'menus.delete'
  UNION ALL SELECT @submenus_submenu,'submenus.filter' UNION ALL SELECT @submenus_submenu,'submenus.create' UNION ALL SELECT @submenus_submenu,'submenus.update' UNION ALL SELECT @submenus_submenu,'submenus.delete'
) x ON x.codigo=o.`NOMOPC`
SET o.`IDSUBMENU`=x.idsubmenu,o.`ESTOPC`='A',o.`SACRG`=1,o.`CULUSA`='ADMIN',o.`FCHHRAACT`=CURRENT_TIMESTAMP(6);

UPDATE `TAS016` a
JOIN `TAS015` o ON o.`IDOPCFOMR`=a.`IDOPCFOMR`
SET a.`ESTOPC`='A',a.`SACRG`=1,a.`CULUSA`='ADMIN',a.`FCHHRAACT`=CURRENT_TIMESTAMP(6)
WHERE a.`IDUSER`=@user_id AND o.`IDSUBMENU` IN (@menus_submenu,@submenus_submenu);

INSERT INTO `TAS016` (`IDOPCFORMUSU`,`IDOPCFOMR`,`IDUSER`,`ESTOPC`,`SACRG`,`CUSCRT`,`FCHHRACRT`)
SELECT UUID(),o.`IDOPCFOMR`,@user_id,'A',1,'ADMIN',CURRENT_TIMESTAMP(6)
FROM `TAS015` o
WHERE @user_id IS NOT NULL AND o.`IDSUBMENU` IN (@menus_submenu,@submenus_submenu)
  AND o.`ESTOPC`='A' AND o.`SACRG`=1
  AND NOT EXISTS (SELECT 1 FROM `TAS016` a WHERE a.`IDUSER`=@user_id AND a.`IDOPCFOMR`=o.`IDOPCFOMR`);
