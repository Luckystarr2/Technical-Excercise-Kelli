SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID('dbo.Item','U') IS NULL
BEGIN
    CREATE TABLE dbo.Item (
        ItemID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Item PRIMARY KEY,
        Sku NVARCHAR(40) NOT NULL CONSTRAINT UQ_Item_Sku UNIQUE,
        Description NVARCHAR(200) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        Cost DECIMAL(18,2) NOT NULL,
        TaxCode NVARCHAR(10) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Inventory','U') IS NULL
BEGIN
    CREATE TABLE dbo.Inventory (
        ItemID INT NOT NULL CONSTRAINT PK_Inventory PRIMARY KEY
            CONSTRAINT FK_Inventory_Item FOREIGN KEY REFERENCES dbo.Item(ItemID),
        OnHandQty INT NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Customer','U') IS NULL
BEGIN
    CREATE TABLE dbo.Customer (
        CustomerID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customer PRIMARY KEY,
        AccountNo NVARCHAR(40) NOT NULL CONSTRAINT UQ_Customer_AccountNo UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        TaxExempt BIT NOT NULL CONSTRAINT DF_Customer_TaxExempt DEFAULT(0)
    );
END
GO

IF OBJECT_ID('dbo.TaxRate','U') IS NULL
BEGIN
    CREATE TABLE dbo.TaxRate (
        TaxCode NVARCHAR(10) NOT NULL CONSTRAINT PK_TaxRate PRIMARY KEY,
        RatePct DECIMAL(6,4) NOT NULL  -- e.g., 0.0825 = 8.25%
    );
END
GO

IF OBJECT_ID('dbo.Ticket','U') IS NULL
BEGIN
    CREATE TABLE dbo.Ticket (
        TicketID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Ticket PRIMARY KEY,
        CustomerID INT NULL CONSTRAINT FK_Ticket_Customer REFERENCES dbo.Customer(CustomerID),
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_Ticket_CreatedAt DEFAULT SYSUTCDATETIME(),
        Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Ticket_Subtotal DEFAULT(0),
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Ticket_TaxAmount DEFAULT(0),
        Total DECIMAL(18,2) NOT NULL CONSTRAINT DF_Ticket_Total DEFAULT(0)
    );
END
GO

IF OBJECT_ID('dbo.TicketLine','U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketLine (
        TicketLineID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TicketLine PRIMARY KEY,
        TicketID BIGINT NOT NULL CONSTRAINT FK_TicketLine_Ticket REFERENCES dbo.Ticket(TicketID),
        ItemID INT NOT NULL CONSTRAINT FK_TicketLine_Item REFERENCES dbo.Item(ItemID),
        Qty INT NOT NULL CHECK (Qty > 0),
        UnitPrice DECIMAL(18,2) NOT NULL,
        LineSubtotal DECIMAL(18,2) NOT NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TicketLine_Ticket_Item' AND object_id = OBJECT_ID('dbo.TicketLine'))
BEGIN
    CREATE UNIQUE INDEX IX_TicketLine_Ticket_Item ON dbo.TicketLine(TicketID, ItemID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Ticket_CreatedAt' AND object_id = OBJECT_ID('dbo.Ticket'))
BEGIN
    CREATE INDEX IX_Ticket_CreatedAt ON dbo.Ticket(CreatedAt);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TicketLine_ItemID' AND object_id = OBJECT_ID('dbo.TicketLine'))
BEGIN
    CREATE INDEX IX_TicketLine_ItemID ON dbo.TicketLine(ItemID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TicketLine_TicketID' AND object_id = OBJECT_ID('dbo.TicketLine'))
BEGIN
    CREATE INDEX IX_TicketLine_TicketID ON dbo.TicketLine(TicketID) INCLUDE (Qty, LineSubtotal, UnitPrice, ItemID);
END
GO

SET NOCOUNT ON;

;MERGE dbo.TaxRate AS T
USING (VALUES
    (N'TX',      CAST(0.0825 AS DECIMAL(6,4))),
    (N'NONTAX',  CAST(0.0000 AS DECIMAL(6,4)))
) AS S(TaxCode, RatePct)
ON T.TaxCode = S.TaxCode
WHEN MATCHED THEN
    UPDATE SET T.RatePct = S.RatePct
WHEN NOT MATCHED THEN
    INSERT (TaxCode, RatePct) VALUES (S.TaxCode, S.RatePct);
GO

;MERGE dbo.Item AS T
USING (VALUES
    (N'SKU-100', N'Widget Basic',       CAST( 9.99 AS DECIMAL(18,2)), CAST(4.00 AS DECIMAL(18,2)), N'TX'),
    (N'SKU-200', N'Widget Deluxe',      CAST(19.99 AS DECIMAL(18,2)), CAST(8.50 AS DECIMAL(18,2)), N'TX'),
    (N'SKU-300', N'Gift Card $25',      CAST(25.00 AS DECIMAL(18,2)), CAST(0.00 AS DECIMAL(18,2)), N'NONTAX'),
    (N'SKU-400', N'Service Plan 1yr',   CAST(29.99 AS DECIMAL(18,2)), CAST(0.00 AS DECIMAL(18,2)), N'NONTAX'),
    (N'SKU-500', N'Accessory Pack',     CAST(14.49 AS DECIMAL(18,2)), CAST(5.25 AS DECIMAL(18,2)), N'TX')
) AS S(Sku, [Description], Price, Cost, TaxCode)
ON T.Sku = S.Sku
WHEN MATCHED THEN UPDATE SET
    T.[Description] = S.[Description],
    T.Price = S.Price,
    T.Cost = S.Cost,
    T.TaxCode = S.TaxCode
WHEN NOT MATCHED THEN
    INSERT (Sku, [Description], Price, Cost, TaxCode)
    VALUES (S.Sku, S.[Description], S.Price, S.Cost, S.TaxCode);
GO

;MERGE dbo.Inventory AS T
USING (
    SELECT I.ItemID, S.OnHandQty
    FROM (VALUES
        (N'SKU-100', 50),
        (N'SKU-200', 35),
        (N'SKU-300', 100),
        (N'SKU-400', 20),
        (N'SKU-500', 60)
    ) AS S(Sku, OnHandQty)
    INNER JOIN dbo.Item AS I ON I.Sku = S.Sku
) AS S(ItemID, OnHandQty)
ON T.ItemID = S.ItemID
WHEN MATCHED THEN
    UPDATE SET T.OnHandQty = S.OnHandQty
WHEN NOT MATCHED THEN
    INSERT (ItemID, OnHandQty) VALUES (S.ItemID, S.OnHandQty);
GO

;MERGE dbo.Customer AS T
USING (VALUES
    (N'CUST1001', N'Acme Retail', 0),
    (N'CUST2001', N'Nonprofit Org', 1)
) AS S(AccountNo, [Name], TaxExempt)
ON T.AccountNo = S.AccountNo
WHEN MATCHED THEN
    UPDATE SET T.[Name] = S.[Name], T.TaxExempt = S.TaxExempt
WHEN NOT MATCHED THEN
    INSERT (AccountNo, [Name], TaxExempt)
    VALUES (S.AccountNo, S.[Name], S.TaxExempt);
GO

CREATE OR ALTER FUNCTION dbo.ufn_GetTaxAmount
(
    @TaxCode NVARCHAR(10),
    @Amount  DECIMAL(18,2)
)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @rate DECIMAL(6,4) = (SELECT RatePct FROM dbo.TaxRate WITH (NOLOCK) WHERE TaxCode = @TaxCode);
    IF @rate IS NULL SET @rate = 0;
    RETURN ROUND(@Amount * @rate, 2);
END
GO

IF TYPE_ID(N'dbo.TicketLineInput') IS NULL
BEGIN
    CREATE TYPE dbo.TicketLineInput AS TABLE
    (
        Sku NVARCHAR(40) NOT NULL,
        Qty INT NOT NULL CHECK (Qty > 0),
        OverridePrice DECIMAL(18,2) NULL
    );
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_CreateTicket
    @CustomerAccountNo NVARCHAR(40) = NULL,
    @Lines dbo.TicketLineInput READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM @Lines)
        THROW 50001, 'No lines were provided.', 1;

    IF EXISTS (
        SELECT 1
        FROM @Lines
        GROUP BY Sku
        HAVING COUNT(*) > 1
    )
        THROW 50004, 'Duplicate SKU detected in @Lines. Combine quantities per SKU.', 1;

    DECLARE @CustomerID INT = NULL,
            @CustomerTaxExempt BIT = 0;

    IF @CustomerAccountNo IS NOT NULL
    BEGIN
        SELECT 
            @CustomerID = C.CustomerID,
            @CustomerTaxExempt = C.TaxExempt
        FROM dbo.Customer AS C WITH (NOLOCK)
        WHERE C.AccountNo = @CustomerAccountNo;

        IF @CustomerID IS NULL
            THROW 50002, 'Customer account number not found.', 1;
    END

    IF EXISTS (
        SELECT L.Sku
        FROM @Lines AS L
        LEFT JOIN dbo.Item AS I WITH (NOLOCK) ON I.Sku = L.Sku
        WHERE I.ItemID IS NULL
    )
        THROW 50005, 'One or more SKUs do not exist.', 1;

    IF OBJECT_ID('tempdb..#LinesResolved') IS NOT NULL DROP TABLE #LinesResolved;

    CREATE TABLE #LinesResolved
    (
        ItemID INT NOT NULL,
        Qty INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        LineSubtotal DECIMAL(18,2) NOT NULL,
        TaxCode NVARCHAR(10) NOT NULL
    );

    INSERT #LinesResolved (ItemID, Qty, UnitPrice, LineSubtotal, TaxCode)
    SELECT 
        I.ItemID,
        L.Qty,
        ISNULL(L.OverridePrice, I.Price) AS UnitPrice,
        CAST(L.Qty * ISNULL(L.OverridePrice, I.Price) AS DECIMAL(18,2)) AS LineSubtotal,
        I.TaxCode
    FROM @Lines AS L
    JOIN dbo.Item AS I WITH (NOLOCK) ON I.Sku = L.Sku;

    IF EXISTS (
        SELECT 1
        FROM #LinesResolved AS R
        JOIN dbo.Inventory AS Inv WITH (UPDLOCK, HOLDLOCK)
            ON Inv.ItemID = R.ItemID
        WHERE Inv.OnHandQty < R.Qty
    )
        THROW 50003, 'Insufficient inventory for one or more items.', 1;

    DECLARE @Subtotal   DECIMAL(18,2) = (SELECT SUM(LineSubtotal) FROM #LinesResolved);
    DECLARE @TaxAmount  DECIMAL(18,2) =
        CASE WHEN @CustomerTaxExempt = 1 THEN 0
             ELSE (
                 SELECT SUM(dbo.ufn_GetTaxAmount(TaxCode, LineSubtotal))
                 FROM #LinesResolved
             )
        END;
    DECLARE @Total      DECIMAL(18,2) = @Subtotal + @TaxAmount;

    BEGIN TRAN;

    UPDATE Inv
    SET Inv.OnHandQty = Inv.OnHandQty - R.Qty
    FROM dbo.Inventory AS Inv WITH (UPDLOCK, HOLDLOCK)
    JOIN #LinesResolved AS R ON R.ItemID = Inv.ItemID;

    DECLARE @NewTicketID BIGINT;

    INSERT dbo.Ticket (CustomerID, Subtotal, TaxAmount, Total)
    VALUES (@CustomerID, @Subtotal, @TaxAmount, @Total);

    SET @NewTicketID = SCOPE_IDENTITY();

    INSERT dbo.TicketLine (TicketID, ItemID, Qty, UnitPrice, LineSubtotal)
    SELECT @NewTicketID, ItemID, Qty, UnitPrice, LineSubtotal
    FROM #LinesResolved;

    COMMIT TRAN;

    SELECT 
        TicketID   = @NewTicketID,
        Subtotal   = @Subtotal,
        TaxAmount  = @TaxAmount,
        Total      = @Total;
END
GO

IF OBJECT_ID('dbo.PayrollEligibility','U') IS NULL
BEGIN
    CREATE TABLE dbo.PayrollEligibility (
        EmployeeId NVARCHAR(20) PRIMARY KEY,
        FirstName NVARCHAR(100) NOT NULL,
        LastName NVARCHAR(100) NOT NULL,
        Eligible BIT NOT NULL,
        LimitPerPayPeriod DECIMAL(18,2) NOT NULL,
        UpdatedAtUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO
