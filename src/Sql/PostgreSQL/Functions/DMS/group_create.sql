BEGIN
    /*
    [7810 - Severity CRITICAL - PostgreSQL doesn't support the SET NOCOUNT. If need try another way to send message back to the client application.]
    SET NOCOUNT ON
    */
    INSERT INTO vault_dbo."Group" (id, organizationid, name, accessall, externalid, creationdate, revisiondate)
    VALUES (par_Id, par_OrganizationId, par_Name, aws_sqlserver_ext.tomsbit(par_AccessAll), par_ExternalId, par_CreationDate, par_RevisionDate);
END;
;
