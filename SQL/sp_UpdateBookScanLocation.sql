USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[sp_updatebookscanlocation]    Script Date: 11/14/2020 5:04:09 PM ******/
SET ANSI_NULLS OFF
GO

SET QUOTED_IDENTIFIER OFF
GO


/* 
 ----------------------------------------------------
S P  U P D A T E  B O O K  S C A N  L O C A T I O N
 -----------------------------------------------------

Updates the last scan date for a Book.  If the scan code is not already
in the database, then it will add the scancode to the codes table.  If the 
code is already present, then it will just update the last inventorydate.
*/
CREATE PROCEDURE [dbo].[sp_updatebookscanlocation]
@sScanCode varchar(14),
@sTitle varchar(64),
@dttmLastScan datetime,
@sLocation varchar(32)
AS

exec sp_updatescan @sScanCode, @sTitle, @dttmLastScan

UPDATE upc_Books set Note=@sLocation WHERE ScanCode = @sScanCode

GO

