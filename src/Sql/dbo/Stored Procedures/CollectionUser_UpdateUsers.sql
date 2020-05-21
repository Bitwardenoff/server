﻿CREATE PROCEDURE [dbo].[CollectionUser_UpdateUsers]
    @CollectionId UNIQUEIDENTIFIER,
    @Users AS [dbo].[SelectionReadOnlyArray] READONLY
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @OrgId UNIQUEIDENTIFIER = (
        SELECT TOP 1
            [OrganizationId]
        FROM
            [dbo].[Collection]
        WHERE
            [Id] = @CollectionId
    )

    -- Update
    UPDATE
        [Target]
    SET
        [Target].[ReadOnly] = [Source].[ReadOnly]
    FROM
        [dbo].[CollectionUser] [Target]
    INNER JOIN
        @Users [Source] ON [Source].[Id] = [Target].[OrganizationUserId]
    WHERE
        [Target].[CollectionId] = @CollectionId
        AND [Target].[ReadOnly] != [Source].[ReadOnly]

    -- Insert
    INSERT INTO
        [dbo].[CollectionUser]
    SELECT
        @CollectionId,
        [Source].[Id],
        [Source].[ReadOnly]
    FROM
        @Users [Source]
    INNER JOIN
        [dbo].[OrganizationUser] OU ON [Source].[Id] = OU.[Id] AND OU.[OrganizationId] = @OrgId
    WHERE
        NOT EXISTS (
            SELECT
                1
            FROM
                [dbo].[CollectionUser]
            WHERE
                [CollectionId] = @CollectionId
                AND [OrganizationUserId] = [Source].[Id]
        )
    
    -- Delete
    DELETE
        CU
    FROM
        [dbo].[CollectionUser] CU
    WHERE
        CU.[CollectionId] = @CollectionId
        AND NOT EXISTS (
            SELECT
                1
            FROM
                @Users
            WHERE
                [Id] = CU.[OrganizationUserId]
        )

    EXEC [dbo].[User_BumpAccountRevisionDateByCollectionId] @CollectionId, @OrgId
END