DROP INDEX `UX_auth_UsersAccounts_Phone` ON `auth`.`UsersAccounts`;

CREATE UNIQUE INDEX `UX_auth_UsersAccounts_Phone`
    ON `auth`.`UsersAccounts` ((IF(`PhoneNumberVerified`, `PhoneNumber`, NULL)));
