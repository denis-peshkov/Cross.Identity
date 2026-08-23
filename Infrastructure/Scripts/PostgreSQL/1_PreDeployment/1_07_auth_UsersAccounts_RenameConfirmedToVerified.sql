DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'auth' AND table_name = 'UsersAccounts' AND column_name = 'EmailConfirmed'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'auth' AND table_name = 'UsersAccounts' AND column_name = 'EmailVerified'
    ) THEN
        ALTER TABLE auth."UsersAccounts" RENAME COLUMN "EmailConfirmed" TO "EmailVerified";
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'auth' AND table_name = 'UsersAccounts' AND column_name = 'PhoneNumberConfirmed'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'auth' AND table_name = 'UsersAccounts' AND column_name = 'PhoneNumberVerified'
    ) THEN
        ALTER TABLE auth."UsersAccounts" RENAME COLUMN "PhoneNumberConfirmed" TO "PhoneNumberVerified";
    END IF;
END $$;
