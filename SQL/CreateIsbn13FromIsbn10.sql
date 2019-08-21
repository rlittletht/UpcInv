USE [thetasoft]
GO

/****** Object:  UserDefinedFunction [dbo].[ufn_upc_CreateIsbn13FromIsbn10]    Script Date: 8/20/2019 3:16:02 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
ALTER FUNCTION [dbo].ufn_upc_CreateIsbn13FromIsbn10
(
	@isbn nvarchar(32)
)
RETURNS NVARCHAR(13)
AS
BEGIN
	if (LEN(@isbn) <> 10 AND LEN(@isbn) <> 9)
		BEGIN
			-- throw an error
			-- return CAST('input value must be 10 characters' as INT)
    	   RETURN '';
		END

	IF (PATINDEX('%[^0-9]%', SUBSTRING(@isbn, 1, 9)) <> 0)
		BEGIN
			-- this isn't just numbers... (we don't check the check digit since that is OK to be non-numeric...and code below will handle that)
			RETURN '';
		END

	-- first verify that the isbn10 is value
	DECLARE @Check10DigitActual as NCHAR;
	DECLARE @Check10DigitExpected as NCHAR;

	select @Check10DigitExpected = [dbo].ufn_upc_CalcIsbn10Check(SUBSTRING(@isbn, 1, 9));
    IF (LEN(@isbn) = 9)
		BEGIN
			select @Check10DigitActual = @Check10DigitExpected;
		END
	ELSE
		BEGIN
			select @Check10DigitActual = SUBSTRING(@isbn, 10, 1);
		END

	if (@Check10DigitActual <> @Check10DigitExpected)
		BEGIN
			-- return CAST('actual check digit(' + CAST(@Check10DigitActual as VARCHAR) + ') should be (' + CAST(@Check10DigitExpected as VARCHAR) + ')' as INT)
    	   RETURN '';
		END

	DECLARE @Isbn13 as NVARCHAR(13);

	select @Isbn13 = '978' + SUBSTRING(@isbn, 1, 9);
	select @Isbn13 = @Isbn13 + [dbo].ufn_upc_CalcIsbn13Check(@Isbn13);

	return @Isbn13;
END

GO



