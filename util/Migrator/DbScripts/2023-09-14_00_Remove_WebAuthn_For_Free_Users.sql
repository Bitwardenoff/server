-- Assumptions:
-- When a 2FA method is disabled, it is removed from the TwoFactorProviders array

-- Problem statement:
-- We have users who currently do not have any 2FA method, with the only one being
-- WebAuthn, which is effectively disabled by a server-side permission check for Premium status.
-- With WebAuthn being made free, we want to avoid these users suddenly being forced
-- to provide 2FA using a key that they haven't used in a long time, by deleting that key from their TwoFactorProviders.

declare @UsersWithoutPremium TABLE
(
    Id UNIQUEIDENTIFIER,
    TwoFactorProviders NVARCHAR(MAX)
);

declare @TwoFactorMethodsForUsersWithoutPremium TABLE
(
    Id UNIQUEIDENTIFIER,
    TwoFactorType INT
)

declare @UsersToAdjust TABLE
(
    Id UNIQUEIDENTIFIER
);

-- Insert users who don't have Premium
INSERT INTO @UsersWithoutPremium
SELECT u.Id, u.TwoFactorProviders
from [User] u
WHERE u.Premium = 0;

-- Filter out those users who get Premium from their org
DELETE FROM @UsersWithoutPremium
WHERE Id IN 
    (SELECT UserId 
    FROM [OrganizationUser] ou 
    INNER JOIN [Organization] o on o.Id = ou.OrganizationId WHERE o.Enabled = 1 AND o.UsersGetPremium = 1)

-- From users without Premium, get their enabled 2FA methods
INSERT INTO @TwoFactorMethodsForUsersWithoutPremium
SELECT  u.Id, 
        tfp1.[key] as TwoFactorType
FROM @UsersWithoutPremium u
CROSS APPLY OPENJSON(u.TwoFactorProviders) tfp1
CROSS APPLY OPENJSON(tfp1.[value]) WITH (
   [Enabled] BIT '$.Enabled'
) tfp2
WHERE [Enabled] = 'true' -- We only want enabled 2FA methods

insert into @UsersToAdjust
select t1.Id
from @TwoFactorMethodsForUsersWithoutPremium t1
where t1.TwoFactorType = 7
AND NOT EXISTS 
    (SELECT * 
    FROM @TwoFactorMethodsForUsersWithoutPremium t2 
    WHERE t2.Id = t1.Id AND t2.TwoFactorType <> 7 AND t2.TwoFactorType <> 4)

select *
from @UsersToAdjust

-- UPDATE [User]
-- SET TwoFactorProviders = NULL
-- FROM @UsersToAdjust ua
-- WHERE ua.Id = [User].Id