CREATE PROCEDURE [dbo].[Collection_CreateOrUpdateAccessForMany]
	@OrganizationId UNIQUEIDENTIFIER,
	@CollectionIds AS [dbo].[GuidIdArray] READONLY,
    @Groups AS [dbo].[SelectionReadOnlyArray] READONLY,
    @Users AS [dbo].[SelectionReadOnlyArray] READONLY
AS
BEGIN
	SET NOCOUNT ON

	 -- Groups
	;WITH [NewCollectionGroups] AS (
		SELECT
			cId.[Id] AS [CollectionId],
			gu.[Id] AS [GroupId],
			gu.[ReadOnly],
			gu.[HidePasswords],
			gu.[Manage]
		FROM
			@Groups AS gu
		CROSS JOIN
			@CollectionIds cId
		INNER JOIN
			[dbo].[Group] g ON gu.[Id] = g.[Id]
		WHERE
			g.[OrganizationId] = @OrganizationId
	)
    MERGE
    	[dbo].[CollectionGroup] as [Target]
	USING
		[NewCollectionGroups] AS [Source]
	ON
		[Target].[CollectionId] = [Source].[CollectionId]
		AND [Target].[GroupId] = [Source].[GroupId]
	WHEN MATCHED AND EXISTS(
		SELECT [Source].[ReadOnly], [Source].[HidePasswords], [Source].[Manage]
		EXCEPT
		SELECT [Target].[ReadOnly], [Target].[HidePasswords], [Target].[Manage]
	) THEN UPDATE SET
		[Target].[ReadOnly] = [Source].[ReadOnly],
		[Target].[HidePasswords] = [Source].[HidePasswords],
		[Target].[Manage] = [Source].[Manage]
	WHEN NOT MATCHED BY TARGET
		THEN INSERT VALUES
		(
			[Source].[CollectionId],
			[Source].[GroupId],
			[Source].[ReadOnly],
			[Source].[HidePasswords],
			[Source].[Manage]
		);

	-- Users
	;WITH [NewCollectionUsers] AS (
		SELECT
			cId.[Id] AS [CollectionId],
			cu.[Id] AS [OrganizationUserId],
			cu.[ReadOnly],
			cu.[HidePasswords],
			cu.[Manage]
		FROM
			@Users AS cu
		CROSS JOIN
			@CollectionIds cId
		INNER JOIN
			[dbo].[OrganizationUser] u ON cu.[Id] = u.[Id]
		WHERE
			u.[OrganizationId] = @OrganizationId
	)
    MERGE
    	[dbo].[CollectionUser] as [Target]
	USING
		[NewCollectionUsers] AS [Source]
	ON
		[Target].[CollectionId] = [Source].[CollectionId]
		AND [Target].[OrganizationUserId] = [Source].[OrganizationUserId]
	WHEN MATCHED AND EXISTS(
		SELECT [Source].[ReadOnly], [Source].[HidePasswords], [Source].[Manage]
		EXCEPT
		SELECT [Target].[ReadOnly], [Target].[HidePasswords], [Target].[Manage]
	) THEN UPDATE SET
		[Target].[ReadOnly] = [Source].[ReadOnly],
		[Target].[HidePasswords] = [Source].[HidePasswords],
		[Target].[Manage] = [Source].[Manage]
	WHEN NOT MATCHED BY TARGET
		THEN INSERT VALUES
		(
			[Source].[CollectionId],
			[Source].[OrganizationUserId],
			[Source].[ReadOnly],
			[Source].[HidePasswords],
			[Source].[Manage]
		);
END
