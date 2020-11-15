USE [thetasoft]
GO

/****** Object:  StoredProcedure [dbo].[sp_drinkwine]    Script Date: 11/14/2020 4:47:04 PM ******/
SET ANSI_NULLS OFF
GO

SET QUOTED_IDENTIFIER OFF
GO



/* 
 ----------------------------------------------------
S P  D R I N K  W I N E
 -----------------------------------------------------

Sets the Drink date and adds drinking notes for this wine
(When these notes are uploaded to CellarTracker, then the wine
will be marked as sync'd with cellartracker)

*/
CREATE  PROCEDURE [dbo].[sp_drinkwine]
@sScanCode varchar(14),
@sWine varchar(255),
@sVintage varchar(32),
@sNotes varchar(255),
@dttmDrink datetime
AS

declare @nCount int

select @nCount = count(*) FROM upc_Wines where ScanCode = @sScanCode

if (@nCount = 0)
BEGIN
	INSERT INTO upc_Wines
	(ScanCode, Wine, Vintage, Notes, Consumed, Locale, Country, Region, SubRegion, Appelation, Producer, Type, Color, Category, Varietal, Designation, Vineyard) 
		values 
	(@sScanCode, @sWine, @sVintage, @sNotes, @dttmDrink, '', '', '', '', '', '', '', '', '', '', '', '')

	INSERT INTO upc_Codes
	(ScanCode, DescriptionShort, FirstScanDate, LastScanDate) 
 		values 
	(@sScanCode, @sWine, @dttmDrink, @dttmDrink)
END
else
-- check if the wine already has notes

declare @notesT as varchar(255)

	select @notesT = Notes FROM upc_Wines where ScanCode = @sScanCode

	IF (LEN(@notesT) > 0)
	BEGIN
		IF (LEN(@sNotes) > 0)
			set @sNotes = CONCAT(@sNotes, ' (was ', @notesT, ')')
		ELSE
			set @sNotes = @notesT
	END

	UPDATE upc_Wines SET Notes=@sNotes, Consumed=@dttmDrink WHERE ScanCode=@sScanCode
GO

