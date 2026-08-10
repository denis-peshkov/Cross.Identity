ALTER TABLE `auth`.`RefreshTokens`
    ADD CONSTRAINT `FK_auth_RefreshTokens_UserAccount`
        FOREIGN KEY (`UserAccountId`)
            REFERENCES `auth`.`UsersAccounts` (`UserAccountId`)
            ON DELETE CASCADE;
