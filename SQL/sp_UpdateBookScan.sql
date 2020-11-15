USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[sp_updatebookscan]    Script Date: 11/14/2020 5:03:53 PM ******/
SET ANSI_NULLS OFF
GO

SET QUOTED_IDENTIFIER OFF
GO


/* 
 ----------------------------------------------------
S P  U P D A T E  B O O K  S C A N 
 -----------------------------------------------------

Updates the last scan date for a Book.  If the scan code is not already
in the database, then it will add the scancode to the codes table.  If the 
code is already present, then it will just update the last inventorydate.
*/
CREATE PROCEDURE [dbo].[sp_updatebookscan]
@sScanCode varchar(14),
@guid uniqueidentifier,
@dttmLastScan datetime
AS
declare @sTitle as varchar(64)

select @sTitle = Title from upc_Books where ID = @guid

exec sp_updatescan @sScanCode, @sTitle, @dttmLastScan

UPDATE upc_Books set ScanCode = @sScanCode WHERE ID = @guid

GO

