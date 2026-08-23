CREATE TABLE auth."Providers"
(
    "ProviderId" smallint     NOT NULL,
    "Name"       varchar(50)  NOT NULL,
    "Scheme"     varchar(100) NOT NULL,
    "IsEnabled"  boolean      NOT NULL,
    "CreatedAt"  timestamp without time zone NOT NULL,

    CONSTRAINT "PK_auth_Providers" PRIMARY KEY ("ProviderId")
);

CREATE UNIQUE INDEX "UX_auth_Providers_Name"   ON auth."Providers" ("Name");
CREATE UNIQUE INDEX "UX_auth_Providers_Scheme" ON auth."Providers" ("Scheme");
