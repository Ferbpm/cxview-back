-- Navigation submenus and fine permissions for Menu/Submenu maintenance (SQL Server).
USE [cxView];
GO
DECLARE @parentMenu varchar(36)='d6d3b9db-c95f-4d24-b5b7-ea15fd92a908';
DECLARE @menusSubmenu varchar(36)='a9fd8380-1005-4b11-9422-523100ee0001';
DECLARE @submenusSubmenu varchar(36)='a9fd8380-1005-4b11-9422-523100ee0002';
DECLARE @userId varchar(36)=(SELECT TOP(1) IDUSER FROM dbo.TAS006 WHERE LOWER(USUARIO)='fpenam' AND ESTUSER='A' AND SACRG=1);
IF @userId IS NULL THROW 50037,'No se encontró el usuario activo fpenam.',1;

DECLARE @submenus TABLE (id varchar(36),nombre varchar(50),descripcion varchar(100));
INSERT @submenus VALUES (@menusSubmenu,'MENÚS','Mantenimiento de menús'),(@submenusSubmenu,'SUBMENÚS','Mantenimiento de submenús');
INSERT dbo.TAS011(IDSUBMENU,IDMENU,NOMSUBMNU,DESCSUBMNU,ESTSUBMNU,SACRG,CUSCRT,FCHHRACRT)
SELECT s.id,@parentMenu,s.nombre,s.descripcion,'A',1,'ADMIN',SYSDATETIME() FROM @submenus s
WHERE NOT EXISTS(SELECT 1 FROM dbo.TAS011 t WHERE t.IDSUBMENU=s.id);
UPDATE dbo.TAS011 SET IDMENU=@parentMenu,ESTSUBMNU='A',SACRG=1,CULUSA='ADMIN',FCHHRAACT=SYSDATETIME() WHERE IDSUBMENU IN (@menusSubmenu,@submenusSubmenu);

UPDATE dbo.TAS012 SET ESTSMUSER='A',SACRG=1,CULUSA='ADMIN',FCHHRAACT=SYSDATETIME() WHERE IDUSER=@userId AND IDSUBMENU IN (@menusSubmenu,@submenusSubmenu);
INSERT dbo.TAS012(IDSMUSER,IDSUBMENU,IDUSER,ESTSMUSER,SACRG,CUSCRT,FCHHRACRT)
SELECT CONVERT(varchar(36),NEWID()),s.IDSUBMENU,@userId,'A',1,'ADMIN',SYSDATETIME() FROM dbo.TAS011 s
WHERE s.IDSUBMENU IN (@menusSubmenu,@submenusSubmenu) AND NOT EXISTS(SELECT 1 FROM dbo.TAS012 a WHERE a.IDUSER=@userId AND a.IDSUBMENU=s.IDSUBMENU);

DECLARE @controls TABLE (idsubmenu varchar(36),codigo varchar(50),descripcion varchar(50));
INSERT @controls VALUES
(@menusSubmenu,'menus.filter','Filtrar menús'),(@menusSubmenu,'menus.create','Nuevo menú'),(@menusSubmenu,'menus.update','Editar menú'),(@menusSubmenu,'menus.delete','Dar de baja menú'),
(@submenusSubmenu,'submenus.filter','Filtrar submenús'),(@submenusSubmenu,'submenus.create','Nuevo submenú'),(@submenusSubmenu,'submenus.update','Editar submenú'),(@submenusSubmenu,'submenus.delete','Dar de baja submenú');
INSERT dbo.TAS015(IDOPCFOMR,IDSUBMENU,NOMOPC,DESCOPC,ESTOPC,SACRG,CUSCRT,FCHHRACRT)
SELECT CONVERT(varchar(36),NEWID()),c.idsubmenu,c.codigo,c.descripcion,'A',1,'ADMIN',SYSDATETIME() FROM @controls c
WHERE NOT EXISTS(SELECT 1 FROM dbo.TAS015 o WHERE o.NOMOPC=c.codigo);
UPDATE o SET IDSUBMENU=c.idsubmenu,ESTOPC='A',SACRG=1,CULUSA='ADMIN',FCHHRAACT=SYSDATETIME() FROM dbo.TAS015 o JOIN @controls c ON c.codigo=o.NOMOPC;

UPDATE a SET ESTOPC='A',SACRG=1,CULUSA='ADMIN',FCHHRAACT=SYSDATETIME() FROM dbo.TAS016 a JOIN dbo.TAS015 o ON o.IDOPCFOMR=a.IDOPCFOMR WHERE a.IDUSER=@userId AND o.IDSUBMENU IN (@menusSubmenu,@submenusSubmenu);
INSERT dbo.TAS016(IDOPCFORMUSU,IDOPCFOMR,IDUSER,ESTOPC,SACRG,CUSCRT,FCHHRACRT)
SELECT CONVERT(varchar(36),NEWID()),o.IDOPCFOMR,@userId,'A',1,'ADMIN',SYSDATETIME() FROM dbo.TAS015 o
WHERE o.IDSUBMENU IN (@menusSubmenu,@submenusSubmenu) AND o.ESTOPC='A' AND o.SACRG=1
  AND NOT EXISTS(SELECT 1 FROM dbo.TAS016 a WHERE a.IDUSER=@userId AND a.IDOPCFOMR=o.IDOPCFOMR);
GO
