START TRANSACTION;

DROP TABLE `U2f`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20220121092546_RemoveU2F', '5.0.12');

COMMIT;
