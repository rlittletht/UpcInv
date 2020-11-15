USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[sp_createcustomscan]    Script Date: 11/14/2020 5:02:38 PM ******/
SET ANSI_NULLS OFF
GO

SET QUOTED_IDENTIFIER ON
GO



/* 
 ------------------------------------------------------------------------
 S P  C R E A T E  C U S T O M  S C A N
 ------------------------------------------------------------------------

Caller must supply:
  ShortDescription (What will be printed on the label),
  LongDescription (Just informational)
  TypePattern (current '02%' for backup tapes, and '03%' for Books/DVD)
  ForcedCode is the code the caller is forcing us to use.  (can be null)
Caller must also supply an OUTPUT parameter to hold the new scancode

*/

CREATE PROCEDURE [dbo].[sp_createcustomscan]
  @sShortDesc varchar(50),
  @sLongDesc varchar(128),
  @sTypePattern varchar(14),
  @sForcedCode varchar(14),
  @sCustomScan varchar(14) OUTPUT
AS

if (IsNull(@sForcedCode, '_') = '_')
	exec sp_nextcustomscan @sTypePattern, @sCustomScan OUTPUT
else
	BEGIN
	set @sCustomScan = @sForcedCode
END

if (IsNull(@sCustomScan, '_') <> '_')
	INSERT INTO upc_CustomCodes 
		(ShortDesc, Code, LongDesc, New) 
	values 
		(@sShortDesc, @sCustomScan, @sLongDesc, 1)

GO

