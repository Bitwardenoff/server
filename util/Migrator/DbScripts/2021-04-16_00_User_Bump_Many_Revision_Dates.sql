-- Create sproc to bump the revision date of a batch of users
IF OBJECT_ID('[dbo].[User_BumpAccountRevisionDateByOrganizationUserIds]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[User_BumpAccountRevisionDateByOrganizationUserIds]
END
GO

CREATE PROCEDURE [dbo].[User_BumpAccountRevisionDateByOrganizationUserIds]
    @OrganizationUserIds [dbo].[GuidIdArray] READONLY
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        UserId
    INTO
        #UserIds
    FROM
        [dbo].[OrganizationUser] OU
        INNER JOIN
        @OrganizationUserIds OUIds on OUIds.Id = OU.Id
    WHERE
        OU.[Status] = 2
    -- Confirmed

    UPDATE
        U
    SET
        U.[AccountRevisionDate] = GETUTCDATE()
    FROM
        [dbo].[User] U
        Inner JOIN
        #UserIds ON U.[Id] = #UserIds.[UserId]
END
GO
