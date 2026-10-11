-- Initial View Logistics configuration for MySQL 8.
-- The test password is stored only as a salted PBKDF2-SHA512 hash.
USE `cxView`;

SET @ddl = IF(
  EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE() AND table_name = 'tas006'
      AND column_name = 'CLAVE' AND character_maximum_length < 128
  ),
  'ALTER TABLE `TAS006` MODIFY COLUMN `CLAVE` varbinary(128) NULL',
  'SELECT 1'
);
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @ddl = IF(
  NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = DATABASE() AND table_name = 'tas023' AND column_name = 'IDEMPRESA'
  ),
  'ALTER TABLE `TAS023` ADD COLUMN `IDEMPRESA` varchar(36) NULL AFTER `IDSEDE`',
  'SELECT 1'
);
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @ddl = IF(
  NOT EXISTS (
    SELECT 1 FROM information_schema.statistics
    WHERE table_schema = DATABASE() AND table_name = 'tas023' AND index_name = 'IX_TAS023_IDEMPRESA'
  ),
  'CREATE INDEX `IX_TAS023_IDEMPRESA` ON `TAS023` (`IDEMPRESA`)',
  'SELECT 1'
);
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @ddl = IF(
  NOT EXISTS (
    SELECT 1 FROM information_schema.referential_constraints
    WHERE constraint_schema = DATABASE() AND table_name = 'tas023'
      AND constraint_name = 'FK_TAS023_IDEMPRESA'
  ),
  'ALTER TABLE `TAS023` ADD CONSTRAINT `FK_TAS023_IDEMPRESA` FOREIGN KEY (`IDEMPRESA`) REFERENCES `TAS001` (`IDEMPRESA`)',
  'SELECT 1'
);
PREPARE stmt FROM @ddl;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

START TRANSACTION;

SET @idempresa = COALESCE(
  (SELECT `IDEMPRESA` FROM `TAS001` WHERE `NOMEMPRESA` = 'View Logistics' LIMIT 1),
  '6e0ba5b7-ce45-4aaf-a785-b049e5dfe6ec'
);
INSERT INTO `TAS001` (`IDEMPRESA`, `NOMEMPRESA`, `ESTEMP`, `SACRG`, `CUSCRT`)
SELECT @idempresa, 'View Logistics', 'A', 1, 'seed'
WHERE NOT EXISTS (SELECT 1 FROM `TAS001` WHERE `NOMEMPRESA` = 'View Logistics');

SET @idapp = COALESCE(
  (SELECT `IDAPP` FROM `TAS002` WHERE `IDEMPRESA` = @idempresa AND `NOMAPP` = 'Web View Logistics' LIMIT 1),
  'c7fff4ee-f127-497e-ae0c-bcc6ad924acf'
);
INSERT INTO `TAS002` (`IDAPP`, `IDEMPRESA`, `NOMAPP`, `DESCAPP`, `ESTAPP`, `SACRG`, `CUSCRT`)
SELECT @idapp, @idempresa, 'Web View Logistics', 'Aplicativo web de View Logistics', 'A', 1, 'seed'
WHERE NOT EXISTS (SELECT 1 FROM `TAS002` WHERE `IDEMPRESA` = @idempresa AND `NOMAPP` = 'Web View Logistics');

SET @iddominio = COALESCE(
  (SELECT `IDDOM` FROM `TAS003` WHERE `IDEMPRESA` = @idempresa AND `NOMDOM` = 'viewlogistics' LIMIT 1),
  '06e4cab1-3b14-4ab7-ab99-927f555af51c'
);
INSERT INTO `TAS003` (`IDDOM`, `IDEMPRESA`, `NOMDOM`, `DESCDOM`, `ESTDOM`, `SACRG`, `CUSCRT`)
SELECT @iddominio, @idempresa, 'viewlogistics', 'Dominio de View Logistics', 'A', 1, 'seed'
WHERE NOT EXISTS (SELECT 1 FROM `TAS003` WHERE `IDEMPRESA` = @idempresa AND `NOMDOM` = 'viewlogistics');

SET @idarea = COALESCE(
  (SELECT `IDAREA` FROM `TAS004` WHERE `IDEMPRESA` = @idempresa AND `NOMAREA` = 'Sistemas' LIMIT 1),
  '1a24ebeb-6fda-418c-a6d6-dc173d028687'
);
INSERT INTO `TAS004` (`IDAREA`, `IDEMPRESA`, `NOMAREA`, `DESCAREA`, `ESTAREA`, `SACRG`, `CUSCRT`)
SELECT @idarea, @idempresa, 'Sistemas', 'Área de sistemas', 'A', 1, 'seed'
WHERE NOT EXISTS (SELECT 1 FROM `TAS004` WHERE `IDEMPRESA` = @idempresa AND `NOMAREA` = 'Sistemas');

SET @idrol = COALESCE(
  (SELECT `IDROLEMP` FROM `TAS005` WHERE `IDAREA` = @idarea AND `NOMROL` = 'Analista' LIMIT 1),
  '6ab98dd5-11ae-444a-9832-db30a60dde7a'
);
INSERT INTO `TAS005` (`IDROLEMP`, `IDAREA`, `NOMROL`, `DESCROL`, `ESTROL`, `SACRG`, `CUSCRT`)
SELECT @idrol, @idarea, 'Analista', 'Analista del área de sistemas', 'A', 1, 'seed'
WHERE NOT EXISTS (SELECT 1 FROM `TAS005` WHERE `IDAREA` = @idarea AND `NOMROL` = 'Analista');

SET @idsede = COALESCE(
  (SELECT `IDSEDE` FROM `TAS023` WHERE `IDEMPRESA` = @idempresa AND `NOMSEDE` = 'Callao' LIMIT 1),
  '99c8729a-4b05-4183-b0e4-02ef916e3784'
);
INSERT INTO `TAS023` (`IDSEDE`, `IDEMPRESA`, `NOMSEDE`, `NOMABRV`, `ESTSEDE`, `SACRG`, `CUSCRT`)
SELECT @idsede, @idempresa, 'Callao', 'CALLAO', 'A', 1, 'seed'
WHERE NOT EXISTS (SELECT 1 FROM `TAS023` WHERE `IDEMPRESA` = @idempresa AND `NOMSEDE` = 'Callao');

INSERT INTO `TAS006` (`IDUSER`, `IDDOMINIO`, `IDROLEMP`, `USUARIO`, `CLAVE`, `NOMBRES`, `APEPAT`, `APEMAT`, `EMAIL`, `ESTUSER`, `FLGACTPASS`, `FLGMLTSEDE`, `IDSEDEDEFAULT`, `SACRG`, `CUSCRT`)
SELECT 'df904178-ea36-4d82-9dfc-bb4ddaaba15c', @iddominio, @idrol, 'fpenam',
       UNHEX('70626b6466322d73686135313224323130303030244d66376f4136632b2b375a71765748737551303672444846654f4b3375534e7164635a534254446872496b3d2459352f4b62323652705a323770767159316e374f73446b7235782b666f343373476c514641666d727949673d'),
       'Fernando', 'Peña', 'Mendoza', 'uxtecnologia@viewlc.com.pe', 'A', 1, 0, @idsede, 1, 'seed'
WHERE NOT EXISTS (SELECT 1 FROM `TAS006` WHERE `USUARIO` = 'fpenam');

INSERT INTO `TAS007` (`IDMOD`, `IDAPP`, `NOMMOD`, `DESCMOD`, `ESTMOD`, `SACRG`, `CUSCRT`)
SELECT '9b9664c5-2eb6-4219-a60e-5c13f1b87d3d', @idapp, 'Logística integrada', 'Módulo de logística integrada', 'A', 1, 'seed'
WHERE NOT EXISTS (SELECT 1 FROM `TAS007` WHERE `IDAPP` = @idapp AND `NOMMOD` = 'Logística integrada');
INSERT INTO `TAS007` (`IDMOD`, `IDAPP`, `NOMMOD`, `DESCMOD`, `ESTMOD`, `SACRG`, `CUSCRT`)
SELECT 'd19cc184-c9ba-423e-a734-d6bfba26bb1f', @idapp, 'Deposito Temporal', 'Módulo de depósito temporal', 'A', 1, 'seed'
WHERE NOT EXISTS (SELECT 1 FROM `TAS007` WHERE `IDAPP` = @idapp AND `NOMMOD` = 'Deposito Temporal');
INSERT INTO `TAS007` (`IDMOD`, `IDAPP`, `NOMMOD`, `DESCMOD`, `ESTMOD`, `SACRG`, `CUSCRT`)
SELECT '5e5a2700-47e6-48c0-bf3a-9c53329a082f', @idapp, 'Seguridad', 'Módulo de seguridad', 'A', 1, 'seed'
WHERE NOT EXISTS (SELECT 1 FROM `TAS007` WHERE `IDAPP` = @idapp AND `NOMMOD` = 'Seguridad');

COMMIT;
