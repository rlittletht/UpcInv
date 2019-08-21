USE [thetasoft]
GO

/****** Object:  UserDefinedFunction [dbo].[ufn_upc_CalcIsbn13Check]    Script Date: 8/20/2019 3:16:02 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date, ,>
-- Description:	<Description, ,>
-- =============================================
ALTER FUNCTION [dbo].ufn_upc_CalcIsbn13Check
(
	@isbn nvarchar(32)
)
RETURNS nchar
AS
BEGIN
	if (LEN(@isbn) <> 12)
		BEGIN
			-- throw an error
			return CAST('input value must be 12 characters' as INT)
		END

	DECLARE @CheckDigit as INT;

WITH Tally (n) AS
(
    SELECT TOP (LEN(@isbn))
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL))
    -- 8,000 row tally table
    FROM (VALUES (0),(0),(0),(0),(0),(0),(0),(0)) a(n)
    CROSS JOIN (VALUES (0),(0),(0),(0),(0),(0),(0),(0),(0),(0)) b(n)
    CROSS JOIN (VALUES (0),(0),(0),(0),(0),(0),(0),(0),(0),(0)) c(n)
    CROSS JOIN (VALUES (0),(0),(0),(0),(0),(0),(0),(0),(0),(0)) d(n)
)
SELECT @CheckDigit = 
	(10 -
    SUM(CASE (n - 1)%2 
        WHEN 1 THEN 3 
        ELSE 1 END * SUBSTRING(@isbn, n, 1))
    % 10
    ) % 10 -- When check digit is 10 (remainder=0) use 0 as the check digit
FROM Tally;

	return CAST(@CheckDigit as nchar)
END

GO



