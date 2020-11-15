USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[sp_updatescan]    Script Date: 11/14/2020 5:04:42 PM ******/
SET ANSI_NULLS OFF
GO

SET QUOTED_IDENTIFIER OFF
GO


/* 
 ----------------------------------------------------
 S P  U P D A T E  S C A N
 ----------------------------------------------------

Updates the last scan date for a code.  If the scan code is not already
in the database, then it will add the scancode to the codes table.  If the 
code is already present, then it will just update the last inventorydate.
*/
CREATE PROCEDURE [dbo].[sp_updatescan]
@sScanCode varchar(14),
@sTitle varchar(64),
@dttmLastScan datetime
AS
declare @nCount int

select @nCount = count(*) FROM upc_Codes where ScanCode = @sScanCode

if (@nCount = 0)
	INSERT INTO upc_Codes
	 (ScanCode, DescriptionShort, FirstScanDate, LastScanDate) values (@sScanCode, @sTitle, @dttmLastScan, @dttmLastScan)
else
	UPDATE upc_Codes 
		SET  DescriptionShort = @sTitle, LastScanDate = @dttmLastScan WHERE ScanCode = @sScanCode

GO

