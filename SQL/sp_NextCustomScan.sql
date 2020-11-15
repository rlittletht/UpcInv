USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[sp_nextcustomscan]    Script Date: 11/14/2020 5:03:32 PM ******/
SET ANSI_NULLS OFF
GO

SET QUOTED_IDENTIFIER ON
GO


/* 
 ------------------------------------------------------------------------
 S P  N E X T  C U S T O M  S C A N
 ------------------------------------------------------------------------

Given a type pattern (currently '02%' for backup tapes, and '03%' for book/dvd/other), 
this procedure will figure out the next custom scancode to use.  It always pads with a single
leading zero before returning.

Caller must supply an OUTPUT parameter to receive the new scancode.

*/
CREATE PROCEDURE [dbo].[sp_nextcustomscan] 
@sPattern varchar(14),
@sNewCode varchar(14) OUTPUT
 AS
declare @sMaxCode as varchar(14)

select @sMaxCode = Max(Code) FROM dbo.upc_CustomCodes WHERE Code Like @sPattern
set @sMaxCode = @sMaxCode + 1
set @sNewCode = '0' + @sMaxCode

GO


