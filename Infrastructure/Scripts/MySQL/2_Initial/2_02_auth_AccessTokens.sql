ALTER TABLE `auth`.`AccessTokens`
    ADD CONSTRAINT `FK_auth_AccessTokens_User`
        FOREIGN KEY (`UserId`)
            REFERENCES `auth`.`UsersAccounts` (`UserAccountId`)
            ON DELETE CASCADE;
