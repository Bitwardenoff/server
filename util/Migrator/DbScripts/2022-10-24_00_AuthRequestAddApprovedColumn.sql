--Add Column
IF COL_LENGTH('[dbo].[AuthRequest]', 'Approved') IS NULL
    BEGIN
        ALTER TABLE
            [dbo].[AuthRequest]
        ADD
            [Approved] BIT NULL
    END
GO

--Drop existing SPROC
IF OBJECT_ID('[dbo].[AuthRequest_Update]') IS NOT NULL
    BEGIN
        DROP PROCEDURE [dbo].[AuthRequest_Update]
    END
GO

--Create SPROC with new column
CREATE PROCEDURE [dbo].[AuthRequest_Update]
    @Id UNIQUEIDENTIFIER OUTPUT,
    @UserId UNIQUEIDENTIFIER,
    @Type SMALLINT, 
    @RequestDeviceIdentifier NVARCHAR(50),
    @RequestDeviceType SMALLINT,
    @RequestIpAddress VARCHAR(50),
    @RequestFingerprint VARCHAR(MAX),
    @ResponseDeviceId UNIQUEIDENTIFIER,
    @AccessCode VARCHAR(25),
    @PublicKey VARCHAR(MAX),
    @Key VARCHAR(MAX),
    @MasterPasswordHash VARCHAR(MAX),
    @Approved BIT,
    @CreationDate DATETIME2 (7),
    @ResponseDate DATETIME2 (7),
    @AuthenticationDate DATETIME2 (7)    
AS
BEGIN
    SET NOCOUNT ON

UPDATE
    [dbo].[AuthRequest]
SET
    [UserId] = @UserId,
    [Type] = @Type,
    [RequestDeviceIdentifier] = @RequestDeviceIdentifier,
    [RequestDeviceType] = @RequestDeviceType,
    [RequestIpAddress] = @RequestIpAddress,
    [RequestFingerprint] = @RequestFingerprint,
    [ResponseDeviceId] = @ResponseDeviceId,
    [AccessCode] = @AccessCode,
    [PublicKey] = @PublicKey,
    [Key] = @Key,
    [MasterPasswordHash] = @MasterPasswordHash,
    [Approved] = @Approved,
    [CreationDate] = @CreationDate,
    [ResponseDate] = @ResponseDate,
    [AuthenticationDate] = @AuthenticationDate
WHERE
    [Id] = @Id
END
GO