ALTER TABLE `auth`.`EmailVerifications`
    ADD CONSTRAINT `FK_auth_EmailVerifications_User`
        FOREIGN KEY (`UserAccountId`)
            REFERENCES `auth`.`UsersAccounts` (`UserAccountId`)
            ON DELETE CASCADE;
