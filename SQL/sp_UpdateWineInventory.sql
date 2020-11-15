USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[sp_updatewineinventory]    Script Date: 11/14/2020 4:47:04 PM ******/
SET ANSI_NULLS OFF
GO

SET QUOTED_IDENTIFIER OFF
GO



/*  
Updates the bin and last scan date 
*/
CREATE  PROCEDURE [dbo].[sp_updatewineinventory]
@sScanCode varchar(14),
@sWine varchar(255),
@sBinCode varchar(32),
@dttmLastScan datetime
AS

exec sp_updatescan @sScanCode, @sWine, @dttmLastScan
UPDATE upc_Wines SET Bin=@sBinCode WHERE ScanCode=@sScanCode

GO

