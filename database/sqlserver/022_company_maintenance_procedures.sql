-- Company maintenance procedures.
USE [cxView];
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAS001_CREAR_EMPRESA]
  @empresa varchar(100),
  @ruc varchar(11),
  @usuario varchar(20)
AS
BEGIN
  SET NOCOUNT ON;

  IF NULLIF(LTRIM(RTRIM(@empresa)), '') IS NULL
    THROW 50001, 'El nombre de la empresa es obligatorio.', 1;

  DECLARE @idempresa_generada varchar(36) = CONVERT(varchar(36), NEWID());

  INSERT INTO [dbo].[TAS001] ([IDEMPRESA], [NOMEMPRESA], [NRORUC], [ESTEMP], [SACRG], [CUSCRT], [FCHHRACRT])
  VALUES (@idempresa_generada, LTRIM(RTRIM(@empresa)), NULLIF(LTRIM(RTRIM(@ruc)), ''), 'A', 1, NULLIF(LTRIM(RTRIM(@usuario)), ''), SYSDATETIME());

  SELECT [IDEMPRESA], [NOMEMPRESA], [NRORUC], [ESTEMP], [SACRG], [CUSCRT], [FCHHRACRT], [CULUSA], [FCHHRAACT]
  FROM [dbo].[TAS001]
  WHERE [IDEMPRESA] = @idempresa_generada;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAS001_ACTUALIZAR_EMPRESA]
  @idempresa varchar(36),
  @empresa varchar(100),
  @ruc varchar(11),
  @estado varchar(1),
  @usuario varchar(20)
AS
BEGIN
  SET NOCOUNT ON;

  IF NULLIF(LTRIM(RTRIM(@empresa)), '') IS NULL
    THROW 50002, 'El nombre de la empresa es obligatorio.', 1;

  IF UPPER(LTRIM(RTRIM(@estado))) NOT IN ('A', 'I')
    THROW 50003, 'El estado debe ser A o I.', 1;

  UPDATE [dbo].[TAS001]
  SET [NOMEMPRESA] = LTRIM(RTRIM(@empresa)),
      [NRORUC] = NULLIF(LTRIM(RTRIM(@ruc)), ''),
      [ESTEMP] = UPPER(LTRIM(RTRIM(@estado))),
      [CULUSA] = NULLIF(LTRIM(RTRIM(@usuario)), ''),
      [FCHHRAACT] = SYSDATETIME()
  WHERE [IDEMPRESA] = LTRIM(RTRIM(@idempresa)) AND [SACRG] = 1;

  SELECT [IDEMPRESA], [NOMEMPRESA], [NRORUC], [ESTEMP], [SACRG], [CUSCRT], [FCHHRACRT], [CULUSA], [FCHHRAACT]
  FROM [dbo].[TAS001]
  WHERE [IDEMPRESA] = LTRIM(RTRIM(@idempresa)) AND [SACRG] = 1;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAS001_DAR_BAJA_EMPRESA]
  @idempresa varchar(36),
  @usuario varchar(20)
AS
BEGIN
  SET NOCOUNT ON;

  UPDATE [dbo].[TAS001]
  SET [ESTEMP] = 'I',
      [SACRG] = 0,
      [CULUSA] = NULLIF(LTRIM(RTRIM(@usuario)), ''),
      [FCHHRAACT] = SYSDATETIME()
  WHERE [IDEMPRESA] = LTRIM(RTRIM(@idempresa)) AND [SACRG] = 1;

  SELECT @@ROWCOUNT AS [FILASAFECTADAS];
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[SP_TAS001_CONSULTAR_EMPRESAS]
  @idempresa varchar(36),
  @empresa varchar(100),
  @ruc varchar(11),
  @estado varchar(1)
AS
BEGIN
  SET NOCOUNT ON;

  SELECT
    [IDEMPRESA], [NOMEMPRESA], [NRORUC], [ESTEMP], [SACRG],
    [CUSCRT], [FCHHRACRT], [CULUSA], [FCHHRAACT]
  FROM [dbo].[TAS001]
  WHERE [SACRG] = 1
    AND (NULLIF(LTRIM(RTRIM(@idempresa)), '') IS NULL OR [IDEMPRESA] = LTRIM(RTRIM(@idempresa)))
    AND (NULLIF(LTRIM(RTRIM(@empresa)), '') IS NULL OR [NOMEMPRESA] LIKE '%' + LTRIM(RTRIM(@empresa)) + '%')
    AND (NULLIF(LTRIM(RTRIM(@ruc)), '') IS NULL OR [NRORUC] = LTRIM(RTRIM(@ruc)))
    AND (NULLIF(LTRIM(RTRIM(@estado)), '') IS NULL OR [ESTEMP] = UPPER(LTRIM(RTRIM(@estado))))
  ORDER BY [NOMEMPRESA];
END;
GO
