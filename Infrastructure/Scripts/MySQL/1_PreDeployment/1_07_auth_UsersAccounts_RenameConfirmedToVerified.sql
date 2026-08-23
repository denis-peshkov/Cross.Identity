SET @sql := (
    SELECT IF(
        COUNT(*) > 0,
        'ALTER TABLE `auth`.`UsersAccounts` CHANGE COLUMN `EmailConfirmed` `EmailVerified` TINYINT(1) NOT NULL',
        'SELECT 1'
    )
    FROM information_schema.columns
    WHERE table_schema = 'auth'
      AND table_name = 'UsersAccounts'
      AND column_name = 'EmailConfirmed'
      AND NOT EXISTS (
          SELECT 1 FROM information_schema.columns c2
          WHERE c2.table_schema = 'auth'
            AND c2.table_name = 'UsersAccounts'
            AND c2.column_name = 'EmailVerified'
      )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) > 0,
        'ALTER TABLE `auth`.`UsersAccounts` CHANGE COLUMN `PhoneNumberConfirmed` `PhoneNumberVerified` TINYINT(1) NOT NULL',
        'SELECT 1'
    )
    FROM information_schema.columns
    WHERE table_schema = 'auth'
      AND table_name = 'UsersAccounts'
      AND column_name = 'PhoneNumberConfirmed'
      AND NOT EXISTS (
          SELECT 1 FROM information_schema.columns c2
          WHERE c2.table_schema = 'auth'
            AND c2.table_name = 'UsersAccounts'
            AND c2.column_name = 'PhoneNumberVerified'
      )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
