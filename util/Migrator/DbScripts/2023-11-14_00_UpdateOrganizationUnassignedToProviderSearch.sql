/*
    This procedure is used by the Bitwarden Admin Panel to retrieve the
    Organizations a Reseller Provider is capable of adding as a client.

    Currently, the procedure is only surfacing Organizations with the most
    current Enterprise or Teams plans, but we actually need to surface any
    Enterprise or Teams plan regardless of the version as all of them are
    applicable to Resellers.
*/ 

-- Drop existing SPROC
IF OBJECT_ID('[dbo].[Organization_UnassignedToProviderSearch]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Organization_UnassignedToProviderSearch]
END
GO

CREATE PROCEDURE [dbo].[Organization_UnassignedToProviderSearch]
    @Name NVARCHAR(50),
    @OwnerEmail NVARCHAR(256),
    @Skip INT = 0,
    @Take INT = 25
WITH RECOMPILE
AS
BEGIN
    SET NOCOUNT ON
    DECLARE @NameLikeSearch NVARCHAR(55) = '%' + @Name + '%'
    DECLARE @OwnerLikeSearch NVARCHAR(55) = @OwnerEmail + '%'

    IF @OwnerEmail IS NOT NULL
    BEGIN
        SELECT
            O.*
        FROM
            [dbo].[OrganizationView] O
            INNER JOIN
                [dbo].[OrganizationUser] OU ON O.[Id] = OU.[OrganizationId]
            INNER JOIN
                [dbo].[User] U ON U.[Id] = OU.[UserId]
        WHERE
            ((O.[PlanType] >= 2 AND O.[PlanType] <= 5) OR (O.[PlanType] >= 8 AND O.[PlanType] <= 15)) -- All 'Teams' and 'Enterprise' organizations
            AND NOT EXISTS (SELECT * FROM [dbo].[ProviderOrganizationView] PO WHERE PO.[OrganizationId] = O.[Id])
            AND (@Name IS NULL OR O.[Name] LIKE @NameLikeSearch)
            AND (U.[Email] LIKE @OwnerLikeSearch)
        ORDER BY O.[CreationDate] DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY
    END
    ELSE
    BEGIN
        SELECT
            O.*
        FROM
            [dbo].[OrganizationView] O
        WHERE
            ((O.[PlanType] >= 2 AND O.[PlanType] <= 5) OR (O.[PlanType] >= 8 AND O.[PlanType] <= 15)) -- All 'Teams' and 'Enterprise' organizations
            AND NOT EXISTS (SELECT * FROM [dbo].[ProviderOrganizationView] PO WHERE PO.[OrganizationId] = O.[Id])
            AND (@Name IS NULL OR O.[Name] LIKE @NameLikeSearch)
        ORDER BY O.[CreationDate] DESC
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY
    END
END
GO