USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[sp_createscan]    Script Date: 11/14/2020 5:03:11 PM ******/
SET ANSI_NULLS OFF
GO

SET QUOTED_IDENTIFIER OFF
GO



/* 
 --------------------------------------
 S P  C R E A T E  S C A N
 --------------------------------------

 Adds a new scancode to the table.  Adds (or updates) the ScanCode entry in the Codes table
 If the scancode is already in the inventory, then dttmFirstScan is allowed to be NULL -- in which case
 the "First Scan" date is left unchanged.

 In all cases, the title is allowed to be changed with this call
*/
CREATE PROCEDURE [dbo].[sp_createscan]
@sScanCode varchar(14),
@sTitle varchar(64),
@dttmFirstScan datetime,
@dttmLastScan datetime
AS
declare @nCount int

select @nCount = count(*) FROM upc_Codes where ScanCode = @sScanCode

if (@nCount = 0)
	INSERT INTO upc_Codes
	 (ScanCode, DescriptionShort, FirstScanDate, LastScanDate) values (@sScanCode, @sTitle, @dttmFirstScan, @dttmLastScan)
else
	BEGIN
	/* get the first scan date from the existing code */
	declare @dttmCurrentFirst datetime
	declare @sCurrentTitle varchar(64)

	select @dttmCurrentFirst = FirstScanDate, @sCurrentTitle = DescriptionShort FROM upc_Codes WHERE ScanCode = @sScanCode
	set @dttmFirstScan = IsNull(@dttmFirstScan, @dttmCurrentFirst)
	set @sTitle = IsNull(@sTitle, @sCurrentTitle)
	UPDATE upc_Codes 
		SET  DescriptionShort = @sTitle, FirstScanDate = @dttmFirstScan, LastScanDate = @dttmLastScan WHERE ScanCode = @sScanCode
	END

GO

