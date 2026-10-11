USE [cxView];
GO

IF COL_LENGTH('dbo.TAS015','NOMOPC') < 50
  ALTER TABLE dbo.TAS015 ALTER COLUMN NOMOPC varchar(50) NULL;
GO

DECLARE @controls TABLE (code varchar(50), description varchar(50));
INSERT INTO @controls VALUES
('company.update','Editar empresa'),('company.filter','Filtrar empresas'),
('users.create','Nuevo usuario'),('users.update','Editar usuario'),('users.delete','Dar de baja usuario'),('users.reset-password','Resetear clave'),('users.change-password','Cambiar contraseña'),('users.filter','Filtrar usuarios'),
('applications.create','Nueva aplicación'),('applications.update','Editar aplicación'),('applications.delete','Dar de baja aplicación'),('applications.filter','Filtrar aplicaciones'),
('domains.create','Nuevo dominio'),('domains.update','Editar dominio'),('domains.delete','Dar de baja dominio'),('domains.filter','Filtrar dominios'),
('areas.create','Nueva área'),('areas.update','Editar área'),('areas.delete','Dar de baja área'),('areas.filter','Filtrar áreas'),
('roles.create','Nuevo rol'),('roles.update','Editar rol'),('roles.delete','Dar de baja rol'),('roles.filter','Filtrar roles'),
('modules.create','Nuevo módulo'),('modules.update','Editar módulo'),('modules.delete','Dar de baja módulo'),('modules.filter','Filtrar módulos');

INSERT INTO dbo.TAS015 (IDOPCFOMR,NOMOPC,DESCOPC,ESTOPC,SACRG,CUSCRT,FCHHRACRT)
SELECT CONVERT(varchar(36),NEWID()),c.code,c.description,'A',1,'seed',SYSDATETIME()
FROM @controls c WHERE NOT EXISTS (SELECT 1 FROM dbo.TAS015 o WHERE o.NOMOPC=c.code);

UPDATE o SET IDSUBMENU=m.idsubmenu,ESTOPC='A',SACRG=1,CULUSA='seed',FCHHRAACT=SYSDATETIME()
FROM dbo.TAS015 o JOIN (VALUES
 ('company','8b1c2c05-5b3d-48b2-9e12-6e2621dcce6b'),
 ('applications','3dcbcc79-7efc-4969-a078-6827eb2886c6'),
 ('domains','c88ef4ad-caf3-4d42-91a4-3183d7cddabc'),
 ('areas','b0a64691-4a45-4b14-9708-1a32e4e37db6'),
 ('roles','8d954bea-c575-47e2-99b2-3c92bc840027'),
 ('users','e2a6ed92-5856-489e-b21d-fd3ceecb7fa9'),
 ('modules','386f85bf-e4a4-46cf-a4b2-c2af7528ea59')
) m(prefix,idsubmenu) ON o.NOMOPC LIKE m.prefix + '.%' COLLATE Latin1_General_100_CI_AI;
GO
