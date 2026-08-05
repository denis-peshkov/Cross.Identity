ALTER TABLE `auth`.`PhoneVerifications`
    ADD CONSTRAINT `FK_auth_PhoneVerifications_User`
        FOREIGN KEY (`UserAccountId`)
            REFERENCES `auth`.`UsersAccounts` (`UserAccountId`)
            ON DELETE CASCADE;
