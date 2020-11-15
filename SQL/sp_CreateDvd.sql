USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[sp_createdvd]    Script Date: 11/14/2020 5:02:56 PM ******/
SET ANSI_NULLS OFF
GO

SET QUOTED_IDENTIFIER ON
GO


/* 
 --------------------------------------
 S P  C R E A T E  D V D
 --------------------------------------

 Adds a new DVD to the DVD table.  Adds (or updates) the ScanCode entry in the Codes table
 If the scancode is already in the inventory, then dttmFirstScan is allowed to be NULL -- in which case
 the "First Scan" date is left unchanged.
*/
CREATE PROCEDURE [dbo].[sp_createdvd]
@sScanCode varchar(14),
@sTitle varchar(64),
@dttmFirstScan datetime,
@dttmLastScan datetime,
@sMediaType varchar(2)
AS
exec sp_createscan @sScanCode, @sTitle, @dttmFirstScan, @dttmLastScan
	
INSERT INTO upc_DVD
 (ScanCode, Title, MediaType) values (@sScanCode, @sTitle, @sMediaType)

GO

