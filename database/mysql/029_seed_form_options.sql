USE `cxView`;

ALTER TABLE `TAS015` MODIFY `NOMOPC` varchar(50) NULL;

INSERT INTO `TAS015` (`IDOPCFOMR`,`NOMOPC`,`DESCOPC`,`ESTOPC`,`SACRG`,`CUSCRT`,`FCHHRACRT`)
SELECT UUID(),x.code,x.description,'A',1,'seed',CURRENT_TIMESTAMP(6) FROM (
 SELECT 'company.update' code,'Editar empresa' description UNION ALL SELECT 'company.filter','Filtrar empresas' UNION ALL
 SELECT 'users.create','Nuevo usuario' UNION ALL SELECT 'users.update','Editar usuario' UNION ALL SELECT 'users.delete','Dar de baja usuario' UNION ALL SELECT 'users.reset-password','Resetear clave' UNION ALL SELECT 'users.change-password','Cambiar contraseña' UNION ALL SELECT 'users.filter','Filtrar usuarios' UNION ALL
 SELECT 'applications.create','Nueva aplicación' UNION ALL SELECT 'applications.update','Editar aplicación' UNION ALL SELECT 'applications.delete','Dar de baja aplicación' UNION ALL SELECT 'applications.filter','Filtrar aplicaciones' UNION ALL
 SELECT 'domains.create','Nuevo dominio' UNION ALL SELECT 'domains.update','Editar dominio' UNION ALL SELECT 'domains.delete','Dar de baja dominio' UNION ALL SELECT 'domains.filter','Filtrar dominios' UNION ALL
 SELECT 'areas.create','Nueva área' UNION ALL SELECT 'areas.update','Editar área' UNION ALL SELECT 'areas.delete','Dar de baja área' UNION ALL SELECT 'areas.filter','Filtrar áreas' UNION ALL
 SELECT 'roles.create','Nuevo rol' UNION ALL SELECT 'roles.update','Editar rol' UNION ALL SELECT 'roles.delete','Dar de baja rol' UNION ALL SELECT 'roles.filter','Filtrar roles' UNION ALL
 SELECT 'modules.create','Nuevo módulo' UNION ALL SELECT 'modules.update','Editar módulo' UNION ALL SELECT 'modules.delete','Dar de baja módulo' UNION ALL SELECT 'modules.filter','Filtrar módulos'
) x WHERE NOT EXISTS(SELECT 1 FROM `TAS015` o WHERE o.`NOMOPC`=x.code);

UPDATE `TAS015` o JOIN (
 SELECT 'company' prefix,'8b1c2c05-5b3d-48b2-9e12-6e2621dcce6b' idsubmenu UNION ALL
 SELECT 'applications','3dcbcc79-7efc-4969-a078-6827eb2886c6' UNION ALL SELECT 'domains','c88ef4ad-caf3-4d42-91a4-3183d7cddabc' UNION ALL
 SELECT 'areas','b0a64691-4a45-4b14-9708-1a32e4e37db6' UNION ALL SELECT 'roles','8d954bea-c575-47e2-99b2-3c92bc840027' UNION ALL
 SELECT 'users','e2a6ed92-5856-489e-b21d-fd3ceecb7fa9' UNION ALL SELECT 'modules','386f85bf-e4a4-46cf-a4b2-c2af7528ea59'
) m ON o.`NOMOPC` LIKE CONCAT(m.prefix,'.%') SET o.`IDSUBMENU`=m.idsubmenu,o.`ESTOPC`='A',o.`SACRG`=1,o.`CULUSA`='seed',o.`FCHHRAACT`=CURRENT_TIMESTAMP(6);
