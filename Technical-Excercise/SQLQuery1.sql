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
