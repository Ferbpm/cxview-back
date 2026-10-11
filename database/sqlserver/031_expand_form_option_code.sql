USE [cxView];
GO
IF COL_LENGTH('dbo.TAS015','NOMOPC') < 50
  ALTER TABLE dbo.TAS015 ALTER COLUMN NOMOPC varchar(50) NULL;
GO
CREATE OR ALTER PROCEDURE dbo.SP_TAS015_CREAR_OPCION
  @idsubmenu varchar(36)=NULL,@idsmsn varchar(36)=NULL,@nomopc varchar(50),@descopc varchar(50)=NULL,
  @color varchar(20)=NULL,@icono varchar(30)=NULL,@usuario varchar(20)
AS BEGIN SET NOCOUNT ON; DECLARE @id varchar(36)=CONVERT(varchar(36),NEWID());
  IF NULLIF(LTRIM(RTRIM(@idsubmenu)),'') IS NULL AND NULLIF(LTRIM(RTRIM(@idsmsn)),'') IS NULL THROW 50001,'Debe indicar un submenú o submenú segundo nivel.',1;
  INSERT dbo.TAS015(IDOPCFOMR,IDSUBMENU,IDSMSN,NOMOPC,DESCOPC,COLOR,ICONO,ESTOPC,SACRG,CUSCRT,FCHHRACRT) VALUES(@id,NULLIF(LTRIM(RTRIM(@idsubmenu)),''),NULLIF(LTRIM(RTRIM(@idsmsn)),''),LTRIM(RTRIM(@nomopc)),NULLIF(LTRIM(RTRIM(@descopc)),''),NULLIF(LTRIM(RTRIM(@color)),''),NULLIF(LTRIM(RTRIM(@icono)),''),'A',1,NULLIF(LTRIM(RTRIM(@usuario)),''),SYSDATETIME());
  EXEC dbo.SP_TAS015_CONSULTAR_OPCIONES @idopcfomr=@id;
END;
GO
CREATE OR ALTER PROCEDURE dbo.SP_TAS015_ACTUALIZAR_OPCION
  @idopcfomr varchar(36),@idsubmenu varchar(36)=NULL,@idsmsn varchar(36)=NULL,@nomopc varchar(50),@descopc varchar(50)=NULL,
  @color varchar(20)=NULL,@icono varchar(30)=NULL,@estado varchar(1)='A',@usuario varchar(20)
AS BEGIN SET NOCOUNT ON;
  UPDATE dbo.TAS015 SET IDSUBMENU=NULLIF(LTRIM(RTRIM(@idsubmenu)),''),IDSMSN=NULLIF(LTRIM(RTRIM(@idsmsn)),''),NOMOPC=LTRIM(RTRIM(@nomopc)),DESCOPC=NULLIF(LTRIM(RTRIM(@descopc)),''),COLOR=NULLIF(LTRIM(RTRIM(@color)),''),ICONO=NULLIF(LTRIM(RTRIM(@icono)),''),ESTOPC=UPPER(LTRIM(RTRIM(@estado))),CULUSA=NULLIF(LTRIM(RTRIM(@usuario)),''),FCHHRAACT=SYSDATETIME() WHERE IDOPCFOMR=LTRIM(RTRIM(@idopcfomr)) AND SACRG=1;
  EXEC dbo.SP_TAS015_CONSULTAR_OPCIONES @idopcfomr=@idopcfomr;
END;
GO
