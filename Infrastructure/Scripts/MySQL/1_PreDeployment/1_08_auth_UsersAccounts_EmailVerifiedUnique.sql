DROP INDEX `UX_auth_UsersAccounts_Email` ON `auth`.`UsersAccounts`;

CREATE UNIQUE INDEX `UX_auth_UsersAccounts_Email`
    ON `auth`.`UsersAccounts` ((IF(`EmailVerified`, `Email`, NULL)));
