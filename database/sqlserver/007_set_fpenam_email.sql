USE [cxView];
GO
UPDATE dbo.TAS006
SET EMAIL='uxtecnologia@viewlc.com.pe', CULUSA='migration', FCHHRAACT=SYSUTCDATETIME()
WHERE USUARIO='fpenam';
GO
