﻿IF OBJECT_ID('[dbo].[User_BumpAccountRevisionDateByProviderUserId]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[User_BumpAccountRevisionDateByProviderUserId]
END
GO

CREATE PROCEDURE [dbo].[User_BumpAccountRevisionDateByProviderUserId]
    @ProviderUserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        U
    SET
        U.[AccountRevisionDate] = GETUTCDATE()
    FROM
        [dbo].[User] U
    INNER JOIN
        [dbo].[ProviderUser] PU ON PU.[UserId] = U.[Id]
    WHERE
        PU.[Id] = @ProviderUserId
        AND PU.[Status] = 2 -- Confirmed
END
GO


IF OBJECT_ID('[dbo].[User_BumpAccountRevisionDateByProviderId]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[User_BumpAccountRevisionDateByProviderId]
END
GO

CREATE PROCEDURE [dbo].[User_BumpAccountRevisionDateByProviderId]
@ProviderId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        U
    SET
        U.[AccountRevisionDate] = GETUTCDATE()
    FROM
        [dbo].[User] U
            INNER JOIN
        [dbo].[ProviderUser] PU ON PU.[UserId] = U.[Id]
    WHERE
            PU.[ProviderId] = @ProviderId
      AND PU.[Status] = 2 -- Confirmed
END
GO

IF OBJECT_ID('[dbo].[Organization_ReadByProviderId]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Organization_ReadByProviderId]
END
GO

CREATE PROCEDURE [dbo].[Organization_ReadByProviderId]
    @ProviderId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        O.*
    FROM
        [dbo].[OrganizationView] O
    INNER JOIN
        [dbo].[ProviderOrganization] PO ON O.[Id] = PO.[OrganizationId]
    WHERE
        PO.[ProviderId] = @ProviderId
END
GO

IF OBJECT_ID('[dbo].[Provider]') IS NULL
BEGIN
    CREATE TABLE [dbo].[Provider] (
        [Id]                UNIQUEIDENTIFIER NOT NULL,
        [Name]              NVARCHAR (50)    NOT NULL,
        [BusinessName]      NVARCHAR (50)    NULL,
        [BusinessAddress1]  NVARCHAR (50)    NULL,
        [BusinessAddress2]  NVARCHAR (50)    NULL,
        [BusinessAddress3]  NVARCHAR (50)    NULL,
        [BusinessCountry]   VARCHAR (2)      NULL,
        [BusinessTaxNumber] NVARCHAR (30)    NULL,
        [BillingEmail]      NVARCHAR (256)   NOT NULL,
        [Status]            TINYINT          NOT NULL,
        [Enabled]           BIT              NOT NULL,
        [CreationDate]      DATETIME2 (7)    NOT NULL,
        [RevisionDate]      DATETIME2 (7)    NOT NULL,
        CONSTRAINT [PK_Provider] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

ALTER TABLE [dbo].[Provider] ALTER COLUMN [Name] NVARCHAR (50) NULL;
GO

ALTER TABLE [dbo].[Provider] ALTER COLUMN [BillingEmail] NVARCHAR (256) NULL;
GO

IF EXISTS(SELECT * FROM sys.views WHERE [Name] = 'ProviderView')
BEGIN
    DROP VIEW [dbo].[ProviderView];
END
GO

CREATE VIEW [dbo].[ProviderView]
AS
SELECT
    *
FROM
    [dbo].[Provider]
GO

IF OBJECT_ID('[dbo].[Provider_Create]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Provider_Create]
END
GO

CREATE PROCEDURE [dbo].[Provider_Create]
    @Id UNIQUEIDENTIFIER,
    @Name NVARCHAR(50),
    @BusinessName NVARCHAR(50),
    @BusinessAddress1 NVARCHAR(50),
    @BusinessAddress2 NVARCHAR(50),
    @BusinessAddress3 NVARCHAR(50),
    @BusinessCountry VARCHAR(2),
    @BusinessTaxNumber NVARCHAR(30),
    @BillingEmail NVARCHAR(256),
    @Status TINYINT,
    @Enabled BIT,
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO [dbo].[Provider]
    (
        [Id],
        [Name],
        [BusinessName],
        [BusinessAddress1],
        [BusinessAddress2],
        [BusinessAddress3],
        [BusinessCountry],
        [BusinessTaxNumber],
        [BillingEmail],
        [Status],
        [Enabled],
        [CreationDate],
        [RevisionDate]
    )
    VALUES
    (
        @Id,
        @Name,
        @BusinessName,
        @BusinessAddress1,
        @BusinessAddress2,
        @BusinessAddress3,
        @BusinessCountry,
        @BusinessTaxNumber,
        @BillingEmail,
        @Status,
        @Enabled,
        @CreationDate,
        @RevisionDate
    )
END
GO

IF OBJECT_ID('[dbo].[Provider_Update]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Provider_Update]
END
GO

CREATE PROCEDURE [dbo].[Provider_Update]
    @Id UNIQUEIDENTIFIER,
    @Name NVARCHAR(50),
    @BusinessName NVARCHAR(50),
    @BusinessAddress1 NVARCHAR(50),
    @BusinessAddress2 NVARCHAR(50),
    @BusinessAddress3 NVARCHAR(50),
    @BusinessCountry VARCHAR(2),
    @BusinessTaxNumber NVARCHAR(30),
    @BillingEmail NVARCHAR(256),
    @Status TINYINT,
    @Enabled BIT,
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        [dbo].[Provider]
    SET
        [Name] = @Name,
        [BusinessName] = @BusinessName,
        [BusinessAddress1] = @BusinessAddress1,
        [BusinessAddress2] = @BusinessAddress2,
        [BusinessAddress3] = @BusinessAddress3,
        [BusinessCountry] = @BusinessCountry,
        [BusinessTaxNumber] = @BusinessTaxNumber,
        [BillingEmail] = @BillingEmail,
        [Status] = @Status,
        [Enabled] = @Enabled,
        [CreationDate] = @CreationDate,
        [RevisionDate] = @RevisionDate
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[Provider_DeleteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Provider_DeleteById]
END
GO

CREATE PROCEDURE [dbo].[Provider_DeleteById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    EXEC [dbo].[User_BumpAccountRevisionDateByProviderId] @Id

    BEGIN TRANSACTION Provider_DeleteById

        DELETE
        FROM
            [dbo].[ProviderUser]
        WHERE
            [ProviderId] = @Id

        DELETE
        FROM
            [dbo].[Provider]
        WHERE
            [Id] = @Id

    COMMIT TRANSACTION Provider_DeleteById
END
GO

IF OBJECT_ID('[dbo].[Provider_ReadById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[Provider_ReadById]
END
GO

CREATE PROCEDURE [dbo].[Provider_ReadById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[ProviderView]
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[ProviderUser]') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProviderUser] (
        [Id]           UNIQUEIDENTIFIER    NOT NULL,
        [ProviderId]   UNIQUEIDENTIFIER    NOT NULL,
        [UserId]       UNIQUEIDENTIFIER    NULL,
        [Email]        NVARCHAR (256)      NULL,
        [Key]          VARCHAR (MAX)       NULL,
        [Status]       TINYINT             NOT NULL,
        [Type]         TINYINT             NOT NULL,
        [Permissions]  NVARCHAR (MAX)      NULL,
        [CreationDate] DATETIME2 (7)       NOT NULL,
        [RevisionDate] DATETIME2 (7)       NOT NULL,
        CONSTRAINT [PK_ProviderUser] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ProviderUser_Provider] FOREIGN KEY ([ProviderId]) REFERENCES [dbo].[Provider] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProviderUser_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([Id])
    );
END
GO

IF EXISTS(SELECT * FROM sys.views WHERE [Name] = 'ProviderUserView')
BEGIN
    DROP VIEW [dbo].[ProviderUserView];
END
GO

CREATE VIEW [dbo].[ProviderUserView]
AS
SELECT
    *
FROM
    [dbo].[ProviderUser]
GO

IF OBJECT_ID('[dbo].[ProviderUser_Create]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderUser_Create]
END
GO

CREATE PROCEDURE [dbo].[ProviderUser_Create]
    @Id UNIQUEIDENTIFIER,
    @ProviderId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @Email NVARCHAR(256),
    @Key VARCHAR(MAX),
    @Status TINYINT,
    @Type TINYINT,
    @Permissions NVARCHAR(MAX),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO [dbo].[ProviderUser]
    (
        [Id],
        [ProviderId],
        [UserId],
        [Email],
        [Key],
        [Status],
        [Type],
        [Permissions],
        [CreationDate],
        [RevisionDate]
    )
    VALUES
    (
        @Id,
        @ProviderId,
        @UserId,
        @Email,
        @Key,
        @Status,
        @Type,
        @Permissions,
        @CreationDate,
        @RevisionDate
    )
END
GO

IF OBJECT_ID('[dbo].[ProviderUser_Update]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderUser_Update]
END
GO

CREATE PROCEDURE [dbo].[ProviderUser_Update]
    @Id UNIQUEIDENTIFIER,
    @ProviderId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @Email NVARCHAR(256),
    @Key VARCHAR(MAX),
    @Status TINYINT,
    @Type TINYINT,
    @Permissions NVARCHAR(MAX),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        [dbo].[ProviderUser]
    SET
        [ProviderId] = @ProviderId,
        [UserId] = @UserId,
        [Email] = @Email,
        [Key] = @Key,
        [Status] = @Status,
        [Type] = @Type,
        [Permissions] = @Permissions,
        [CreationDate] = @CreationDate,
        [RevisionDate] = @RevisionDate
    WHERE
        [Id] = @Id

    EXEC [dbo].[User_BumpAccountRevisionDate] @UserId
END
GO

IF OBJECT_ID('[dbo].[ProviderUser_DeleteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderUser_DeleteById]
END
GO

CREATE PROCEDURE [dbo].[ProviderUser_DeleteById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    EXEC [dbo].[User_BumpAccountRevisionDateByProviderUserId] @Id

    BEGIN TRANSACTION ProviderUser_DeleteById

        DECLARE @ProviderId UNIQUEIDENTIFIER
        DECLARE @UserId UNIQUEIDENTIFIER

        SELECT
            @ProviderId = [ProviderId],
            @UserId = [UserId]
        FROM
            [dbo].[ProviderUser]
        WHERE
            [Id] = @Id

        DELETE
        FROM
            [dbo].[ProviderUser]
        WHERE
            [Id] = @Id

    COMMIT TRANSACTION ProviderUser_DeleteById
END
GO

IF OBJECT_ID('[dbo].[ProviderUser_ReadById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderUser_ReadById]
END
GO

CREATE PROCEDURE [dbo].[ProviderUser_ReadById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[ProviderUserView]
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[ProviderUser_ReadByProviderId]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderUser_ReadByProviderId]
END
GO

CREATE PROCEDURE [dbo].[ProviderUser_ReadByProviderId]
    @ProviderId UNIQUEIDENTIFIER,
    @Type TINYINT
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[ProviderUserView]
    WHERE
        [ProviderId] = @ProviderId
        AND [Type] = COALESCE(@Type, [Type])
END
GO

IF OBJECT_ID('[dbo].[ProviderUser_ReadByUserId]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderUser_ReadByUserId]
END
GO

CREATE PROCEDURE [dbo].[ProviderUser_ReadByUserId]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[ProviderUserView]
    WHERE
        [UserId] = @UserId
END
GO

IF OBJECT_ID('[dbo].[ProviderOrganization]') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProviderOrganization] (
        [Id]             UNIQUEIDENTIFIER    NOT NULL,
        [ProviderId]     UNIQUEIDENTIFIER    NOT NULL,
        [OrganizationId] UNIQUEIDENTIFIER    NULL,
        [Key]            VARCHAR (MAX)       NULL,
        [Settings]       NVARCHAR(MAX)       NULL,
        [CreationDate]   DATETIME2 (7)       NOT NULL,
        [RevisionDate]   DATETIME2 (7)       NOT NULL,
        CONSTRAINT [PK_ProviderOrganization] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ProviderOrganization_Provider] FOREIGN KEY ([ProviderId]) REFERENCES [dbo].[Provider] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProviderOrganization_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[Organization] ([Id])
    );
END
GO

IF EXISTS(SELECT * FROM sys.views WHERE [Name] = 'ProviderOrganizationView')
BEGIN
    DROP VIEW [dbo].[ProviderOrganizationView];
END
GO

CREATE VIEW [dbo].[ProviderOrganizationView]
AS
SELECT
    *
FROM
    [dbo].[ProviderOrganization]
GO

IF OBJECT_ID('[dbo].[ProviderOrganization_Create]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderOrganization_Create]
END
GO

CREATE PROCEDURE [dbo].[ProviderOrganization_Create]
    @Id UNIQUEIDENTIFIER,
    @ProviderId UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Key VARCHAR(MAX),
    @Settings NVARCHAR(MAX),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO [dbo].[ProviderOrganization]
    (
        [Id],
        [ProviderId],
        [OrganizationId],
        [Key],
        [Settings],
        [CreationDate],
        [RevisionDate]
    )
    VALUES
    (
        @Id,
        @ProviderId,
        @OrganizationId,
        @Key,
        @Settings,
        @CreationDate,
        @RevisionDate
    )
END
GO

IF OBJECT_ID('[dbo].[ProviderOrganization_Update]') IS NOT NULL
    BEGIN
        DROP PROCEDURE [dbo].[ProviderOrganization_Update]
    END
GO

CREATE PROCEDURE [dbo].[ProviderOrganization_Update]
    @Id UNIQUEIDENTIFIER,
    @ProviderId UNIQUEIDENTIFIER,
    @OrganizationId UNIQUEIDENTIFIER,
    @Key VARCHAR(MAX),
    @Settings NVARCHAR(MAX),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        [dbo].[ProviderOrganization]
    SET
        [ProviderId] = @ProviderId,
        [OrganizationId] = @OrganizationId,
        [Key] = @Key,
        [Settings] = @Settings,
        [CreationDate] = @CreationDate,
        [RevisionDate] = @RevisionDate
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[ProviderOrganization_DeleteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderOrganization_DeleteById]
END
GO

CREATE PROCEDURE [dbo].[ProviderOrganization_DeleteById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    BEGIN TRANSACTION ProviderOrganization_DeleteById

        DECLARE @ProviderId UNIQUEIDENTIFIER
        DECLARE @OrganizationId UNIQUEIDENTIFIER

        SELECT
            @ProviderId = [ProviderId],
            @OrganizationId = [OrganizationId]
        FROM
            [dbo].[ProviderOrganization]
        WHERE
            [Id] = @Id

        DELETE
        FROM
            [dbo].[ProviderOrganization]
        WHERE
            [Id] = @Id

    COMMIT TRANSACTION ProviderOrganization_DeleteById
END
GO


IF OBJECT_ID('[dbo].[ProviderOrganization_ReadById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderOrganization_ReadById]
END
GO

CREATE PROCEDURE [dbo].[ProviderOrganization_ReadById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[ProviderOrganizationView]
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[ProviderOrganizationProviderUser]') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProviderOrganizationProviderUser] (
        [Id]                     UNIQUEIDENTIFIER    NOT NULL,
        [ProviderOrganizationId] UNIQUEIDENTIFIER    NOT NULL,
        [ProviderUserId]         UNIQUEIDENTIFIER    NULL,
        [Type]                   TINYINT             NOT NULL,
        [Permissions]            NVARCHAR (MAX)      NULL,
        [CreationDate]           DATETIME2 (7)       NOT NULL,
        [RevisionDate]           DATETIME2 (7)       NOT NULL,
        CONSTRAINT [PK_ProviderOrganizationProviderUser] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ProviderOrganizationProviderUser_Provider] FOREIGN KEY ([ProviderOrganizationId]) REFERENCES [dbo].[ProviderOrganization] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProviderOrganizationProviderUser_User] FOREIGN KEY ([ProviderUserId]) REFERENCES [dbo].[ProviderUser] ([Id])
    );
END
GO

IF OBJECT_ID('[dbo].[ProviderOrganizationProviderUser_Create]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderOrganizationProviderUser_Create]
END
GO

CREATE PROCEDURE [dbo].[ProviderOrganizationProviderUser_Create]
    @Id UNIQUEIDENTIFIER,
    @ProviderOrganizationId UNIQUEIDENTIFIER,
    @ProviderUserId UNIQUEIDENTIFIER,
    @Type TINYINT,
    @Permissions NVARCHAR(MAX),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO [dbo].[ProviderOrganizationProviderUser]
    (
        [Id],
        [ProviderOrganizationId],
        [ProviderUserId],
        [Type],
        [Permissions],
        [CreationDate],
        [RevisionDate]
    )
    VALUES
    (
        @Id,
        @ProviderOrganizationId,
        @ProviderUserId,
        @Type,
        @Permissions,
        @CreationDate,
        @RevisionDate
    )
END
GO

IF OBJECT_ID('[dbo].[ProviderOrganizationProviderUser_DeleteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderOrganizationProviderUser_DeleteById]
END
GO

CREATE PROCEDURE [dbo].[ProviderOrganizationProviderUser_DeleteById]
@Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    BEGIN TRANSACTION POPU_DeleteById

        DECLARE @ProviderUserId UNIQUEIDENTIFIER

        SELECT
            @ProviderUserId = [ProviderUserId]
        FROM
            [dbo].[ProviderOrganizationProviderUser]
        WHERE
            [Id] = @Id

        DELETE
        FROM
            [dbo].[ProviderOrganizationProviderUser]
        WHERE
            [Id] = @Id

        EXEC [dbo].[User_BumpAccountRevisionDateByProviderUserId] @ProviderUserId

    COMMIT TRANSACTION POPU_DeleteById
END
GO

IF OBJECT_ID('[dbo].[ProviderOrganizationProviderUser_ReadById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderOrganizationProviderUser_ReadById]
END
GO

CREATE PROCEDURE [dbo].[ProviderOrganizationProviderUser_ReadById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[ProviderOrganizationProviderUser]
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[ProviderOrganizationProviderUser_Update]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[ProviderOrganizationProviderUser_Update]
END
GO

CREATE PROCEDURE [dbo].[ProviderOrganizationProviderUser_Update]
    @Id UNIQUEIDENTIFIER,
    @ProviderOrganizationId UNIQUEIDENTIFIER,
    @ProviderUserId UNIQUEIDENTIFIER,
    @Type TINYINT,
    @Permissions NVARCHAR(MAX),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        [dbo].[ProviderOrganizationProviderUser]
    SET
        [ProviderOrganizationId] = @ProviderOrganizationId,
        [ProviderUserId] = @ProviderUserId,
        [Type] = @Type,
        [Permissions] = @Permissions,
        [CreationDate] = @CreationDate,
        [RevisionDate] = @RevisionDate
    WHERE
        [Id] = @Id

    EXEC [dbo].[User_BumpAccountRevisionDateByProviderUserId] @ProviderUserId
END
GO

IF OBJECT_ID('[dbo].[ProviderUser_ReadCountByProviderIdEmail]') IS NOT NULL
    BEGIN
        DROP PROCEDURE [dbo].[ProviderUser_ReadCountByProviderIdEmail]
    END
GO

CREATE PROCEDURE [dbo].[ProviderUser_ReadCountByProviderIdEmail]
    @ProviderId UNIQUEIDENTIFIER,
    @Email NVARCHAR(256),
    @OnlyUsers BIT
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        COUNT(1)
    FROM
        [dbo].[ProviderUser] OU
            LEFT JOIN
        [dbo].[User] U ON OU.[UserId] = U.[Id]
    WHERE
            OU.[ProviderId] = @ProviderId
      AND (
            (@OnlyUsers = 0 AND @Email IN (OU.[Email], U.[Email]))
            OR (@OnlyUsers = 1 AND U.[Email] = @Email)
        )
END
GO

IF OBJECT_ID('[dbo].[ProviderUser_ReadByIds]') IS NOT NULL
    BEGIN
        DROP PROCEDURE [dbo].[ProviderUser_ReadByIds]
    END
GO

CREATE PROCEDURE [dbo].[ProviderUser_ReadByIds]
@Ids AS [dbo].[GuidIdArray] READONLY
AS
BEGIN
    SET NOCOUNT ON

    IF (SELECT COUNT(1) FROM @Ids) < 1
        BEGIN
            RETURN(-1)
        END

    SELECT
        *
    FROM
        [dbo].[ProviderUserView]
    WHERE
        [Id] IN (SELECT [Id] FROM @Ids)
END
GO


IF OBJECT_ID('[dbo].[User_BumpAccountRevisionDateByProviderUserIds]') IS NOT NULL
    BEGIN
        DROP PROCEDURE [dbo].[User_BumpAccountRevisionDateByProviderUserIds]
    END
GO

CREATE PROCEDURE [dbo].[User_BumpAccountRevisionDateByProviderUserIds]
@ProviderUserIds [dbo].[GuidIdArray] READONLY
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        U
    SET
        U.[AccountRevisionDate] = GETUTCDATE()
    FROM
        @ProviderUserIds OUIDs
            INNER JOIN
        [dbo].[ProviderUser] PU ON OUIDs.Id = PU.Id AND PU.[Status] = 2 -- Confirmed
            INNER JOIN
        [dbo].[User] U ON PU.UserId = U.Id
END
GO

IF OBJECT_ID('[dbo].[ProviderUser_DeleteByIds]') IS NOT NULL
    BEGIN
        DROP PROCEDURE [dbo].[ProviderUser_DeleteByIds]
    END
GO

CREATE PROCEDURE [dbo].[ProviderUser_DeleteByIds]
@Ids [dbo].[GuidIdArray] READONLY
AS
BEGIN
    SET NOCOUNT ON

    EXEC [dbo].[User_BumpAccountRevisionDateByProviderUserIds] @Ids

    DECLARE @UserAndProviderIds [dbo].[TwoGuidIdArray]

    INSERT INTO @UserAndProviderIds
    (Id1, Id2)
    SELECT
        UserId,
        ProviderId
    FROM
        [dbo].[ProviderUser] PU
            INNER JOIN
        @Ids PUIds ON PUIds.Id = PU.Id
    WHERE
        UserId IS NOT NULL AND
        ProviderId IS NOT NULL

    DECLARE @BatchSize INT = 100

    -- Delete ProviderUsers
    WHILE @BatchSize > 0
        BEGIN
            BEGIN TRANSACTION ProviderUser_DeleteMany_PUs

                DELETE TOP(@BatchSize) OU
                FROM
                    [dbo].[ProviderUser] PU
                        INNER JOIN
                    @Ids I ON I.Id = PU.Id

                SET @BatchSize = @@ROWCOUNT

            COMMIT TRANSACTION ProviderUser_DeleteMany_PUs
        END
END
GO

IF OBJECT_ID('[dbo].[Provider_Search]') IS NOT NULL
    BEGIN
        DROP PROCEDURE [dbo].[Provider_Search]
    END
GO

CREATE PROCEDURE [dbo].[Provider_Search]
    @Name NVARCHAR(50),
    @UserEmail NVARCHAR(256),
    @Skip INT = 0,
    @Take INT = 25
    WITH RECOMPILE
AS
BEGIN
    SET NOCOUNT ON
    DECLARE @NameLikeSearch NVARCHAR(55) = '%' + @Name + '%'

    IF @UserEmail IS NOT NULL
        BEGIN
            SELECT
                O.*
            FROM
                [dbo].[ProviderView] O
                    INNER JOIN
                [dbo].[ProviderUser] OU ON O.[Id] = OU.[ProviderId]
                    INNER JOIN
                [dbo].[User] U ON U.[Id] = OU.[UserId]
            WHERE
                (@Name IS NULL OR O.[Name] LIKE @NameLikeSearch)
              AND U.[Email] = COALESCE(@UserEmail, U.[Email])
            ORDER BY O.[CreationDate] DESC
            OFFSET @Skip ROWS
                FETCH NEXT @Take ROWS ONLY
        END
    ELSE
        BEGIN
            SELECT
                O.*
            FROM
                [dbo].[ProviderView] O
            WHERE
                (@Name IS NULL OR O.[Name] LIKE @NameLikeSearch)
            ORDER BY O.[CreationDate] DESC
            OFFSET @Skip ROWS
                FETCH NEXT @Take ROWS ONLY
        END
END
GO
