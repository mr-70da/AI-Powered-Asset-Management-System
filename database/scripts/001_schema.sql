/* ============================================================================
   AI-Powered Asset Management System
   Script  : 001_schema.sql
   Purpose : Creates every table in the database, in the correct order
             (tables that are referenced by a foreign key are created first).

   How to run:
     Open this file in SSMS / Azure Data Studio, connected to an EMPTY
     database, and hit Execute. It is safe to run more than once — every
     CREATE TABLE is wrapped in "IF NOT EXISTS ... BEGIN ... END", so if
     the table already exists it will just be skipped instead of erroring.

   Notes for myself (so I can explain these choices in the interview):
     - Money columns use DECIMAL(18,2) so we never lose precision the way
       FLOAT would (money should never be a floating point type).
     - Every table has an INT IDENTITY(1,1) primary key. Simple, fast,
       and easy to reason about — I didn't reach for GUIDs because I don't
       have a strong reason to (no offline/multi-database sync here).
      - I DID add a RowVersion column (rowversion type) to Assets for
        optimistic concurrency (R3.5, "two transfers happening at the
        same time"). SQL Server manages the value automatically - every
        INSERT/UPDATE bumps it - and EF Core uses it in the WHERE clause
        of UPDATE statements to detect when a client was editing a stale
        copy of the asset. The 003_asset_rowversion.sql script adds the
        same column idempotently to databases created before this one.
     - I did NOT add a filtered/partial unique index on SerialNumber.
       In SQL Server, a normal UNIQUE constraint only allows ONE row with
       a NULL value in that column — so if two assets both have no
       serial number, the second INSERT would fail. I'm aware of this
       limitation and I'm accepting it for now (documented in README)
       rather than using a more advanced "filtered index" I'd struggle
       to explain confidently.
   ============================================================================ */

-- ----------------------------------------------------------------------------
-- 1. Roles and users (login / identity)
-- ----------------------------------------------------------------------------
Create database AssetManagement;
GO
USE AssetManagement;
GO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Roles')
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL UNIQUE   -- 'Admin' or 'User'
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AppUsers')
BEGIN
    CREATE TABLE dbo.AppUsers
    (
        Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserName     NVARCHAR(50)  NOT NULL UNIQUE,
        DisplayName  NVARCHAR(100) NOT NULL,
        Email        NVARCHAR(150) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(500) NOT NULL,  -- never store plain text passwords
        RoleId       INT NOT NULL,
        IsDisabled   BIT NOT NULL DEFAULT (0),
        CreatedAtUtc DATETIME2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_AppUsers_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'RefreshTokens')
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AppUserId    INT NOT NULL,
        TokenHash    NVARCHAR(128) NOT NULL UNIQUE,
        ExpiresAtUtc DATETIME2(3) NOT NULL,
        RevokedAtUtc DATETIME2(3) NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_RefreshTokens_AppUsers FOREIGN KEY (AppUserId) REFERENCES dbo.AppUsers (Id)
    );
END
GO

-- ----------------------------------------------------------------------------
-- 2. Organisation / lookup data
-- ----------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Organisations')
BEGIN
    CREATE TABLE dbo.Organisations
    (
        Id   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Departments')
BEGIN
    CREATE TABLE dbo.Departments
    (
        Id             INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name           NVARCHAR(100) NOT NULL,
        OrganisationId INT NOT NULL,

        CONSTRAINT FK_Departments_Organisations FOREIGN KEY (OrganisationId) REFERENCES dbo.Organisations (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Locations')
BEGIN
    CREATE TABLE dbo.Locations
    (
        Id   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Employees')
BEGIN
    CREATE TABLE dbo.Employees
    (
        Id           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name         NVARCHAR(100) NOT NULL,
        Email        NVARCHAR(150) NULL UNIQUE,
        DepartmentId INT NULL,
        IsActive     BIT NOT NULL DEFAULT (1),

        CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Categories')
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AssetTypes')
BEGIN
    CREATE TABLE dbo.AssetTypes
    (
        Id   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL UNIQUE
    );
END
GO

-- ----------------------------------------------------------------------------
-- 3. Assets
-- ----------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Assets')
BEGIN
    CREATE TABLE dbo.Assets
    (
        Id                 INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AssetCode          NVARCHAR(50)  NOT NULL UNIQUE,   -- e.g. AST-0001
        AssetName          NVARCHAR(150) NOT NULL,
        Description        NVARCHAR(1000) NULL,

        CategoryId         INT NOT NULL,
        AssetTypeId        INT NOT NULL,

        Manufacturer       NVARCHAR(100) NOT NULL,
        Model              NVARCHAR(100) NOT NULL,
        SerialNumber       NVARCHAR(100) NULL,   -- see note at top of file re: uniqueness

        PurchaseDate       DATE NULL,
        PurchaseCost       DECIMAL(18,2) NULL,
        WarrantyExpiryDate DATE NULL,

        -- Kept as a plain string with a CHECK instead of a separate lookup
        -- table, because the requirement lists it as "e.g. Available,
        -- Assigned, ..." rather than a fully manageable entity like
        -- Category/AssetType/Department are.
        Status             NVARCHAR(30) NOT NULL
            CONSTRAINT CK_Assets_Status
            CHECK (Status IN (N'Available', N'Assigned', N'Under Maintenance', N'Retired')),

        DepartmentId       INT NULL,   -- NULL = asset not currently assigned to a department
        AssignedEmployeeId INT NULL,   -- NULL = asset not currently assigned to anyone
        LocationId         INT NULL,

        CreatedByUserId    INT NULL,
        CreatedAtUtc       DATETIME2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        ModifiedByUserId   INT NULL,
        ModifiedAtUtc      DATETIME2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),

        -- Optimistic-concurrency token (R3.5). SQL Server manages the value
        -- itself - the app never reads or writes it directly beyond sending
        -- it back on transfers so EF can use it in the UPDATE's WHERE clause.
        RowVersion         rowversion,

        CONSTRAINT CK_Assets_PurchaseCost_Positive CHECK (PurchaseCost IS NULL OR PurchaseCost >= 0),

        CONSTRAINT FK_Assets_Categories         FOREIGN KEY (CategoryId)         REFERENCES dbo.Categories (Id),
        CONSTRAINT FK_Assets_AssetTypes         FOREIGN KEY (AssetTypeId)        REFERENCES dbo.AssetTypes (Id),
        CONSTRAINT FK_Assets_Departments        FOREIGN KEY (DepartmentId)       REFERENCES dbo.Departments (Id),
        CONSTRAINT FK_Assets_Employees          FOREIGN KEY (AssignedEmployeeId) REFERENCES dbo.Employees (Id),
        CONSTRAINT FK_Assets_Locations          FOREIGN KEY (LocationId)         REFERENCES dbo.Locations (Id),
        CONSTRAINT FK_Assets_AppUsers_CreatedBy  FOREIGN KEY (CreatedByUserId)   REFERENCES dbo.AppUsers (Id),
        CONSTRAINT FK_Assets_AppUsers_ModifiedBy FOREIGN KEY (ModifiedByUserId)  REFERENCES dbo.AppUsers (Id)
    );
END
GO

-- A regular (non-filtered) unique index. Simple to reason about; the
-- trade-off is documented at the top of this file and in the README.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Assets_SerialNumber')
BEGIN
    CREATE UNIQUE INDEX UX_Assets_SerialNumber ON dbo.Assets (SerialNumber);
END
GO

-- Plain, ordinary indexes to speed up the list/filter screen (R2.2).
-- Nothing fancy here — one column each, so they're easy to reason about.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Assets_Status')
BEGIN
    CREATE INDEX IX_Assets_Status ON dbo.Assets (Status);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Assets_CategoryId')
BEGIN
    CREATE INDEX IX_Assets_CategoryId ON dbo.Assets (CategoryId);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Assets_AssetTypeId')
BEGIN
    CREATE INDEX IX_Assets_AssetTypeId ON dbo.Assets (AssetTypeId);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Assets_DepartmentId')
BEGIN
    CREATE INDEX IX_Assets_DepartmentId ON dbo.Assets (DepartmentId);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Assets_LocationId')
BEGIN
    CREATE INDEX IX_Assets_LocationId ON dbo.Assets (LocationId);
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Assets_AssignedEmployeeId')
BEGIN
    CREATE INDEX IX_Assets_AssignedEmployeeId ON dbo.Assets (AssignedEmployeeId);
END
GO

-- ----------------------------------------------------------------------------
-- 4. Asset transfers (history — we never edit or delete a row here)
-- ----------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AssetTransfers')
BEGIN
    CREATE TABLE dbo.AssetTransfers
    (
        Id                   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AssetId              INT NOT NULL,

        FromEmployeeId       INT NULL,
        ToEmployeeId         INT NULL,
        FromDepartmentId     INT NULL,
        ToDepartmentId       INT NULL,
        FromLocationId       INT NULL,
        ToLocationId         INT NULL,

        TransferDateUtc      DATETIME2(3) NOT NULL,
        Reason               NVARCHAR(500) NULL,
        TransferredByUserId  INT NOT NULL,

        CreatedAtUtc         DATETIME2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),

        -- NOTE: I decided NOT to add a database-level CHECK constraint for
        -- "a transfer must actually change something" (R3.4). The logic
        -- for that (comparing three nullable pairs of columns) needs
        -- ISNULL() tricks I'd rather not put in the schema when I can
        -- express the same rule far more clearly in a few lines of C#
        -- in the service layer, where it also gives me a chance to return
        -- a friendly error message instead of a raw SQL error. Same goes
        -- for "cannot transfer a retired asset" and "date can't be in
        -- the future" — those are enforced in the API, not here.

        CONSTRAINT FK_AssetTransfers_Assets            FOREIGN KEY (AssetId)          REFERENCES dbo.Assets (Id),
        CONSTRAINT FK_AssetTransfers_Employees_From     FOREIGN KEY (FromEmployeeId)   REFERENCES dbo.Employees (Id),
        CONSTRAINT FK_AssetTransfers_Employees_To       FOREIGN KEY (ToEmployeeId)     REFERENCES dbo.Employees (Id),
        CONSTRAINT FK_AssetTransfers_Departments_From   FOREIGN KEY (FromDepartmentId) REFERENCES dbo.Departments (Id),
        CONSTRAINT FK_AssetTransfers_Departments_To     FOREIGN KEY (ToDepartmentId)   REFERENCES dbo.Departments (Id),
        CONSTRAINT FK_AssetTransfers_Locations_From     FOREIGN KEY (FromLocationId)   REFERENCES dbo.Locations (Id),
        CONSTRAINT FK_AssetTransfers_Locations_To       FOREIGN KEY (ToLocationId)     REFERENCES dbo.Locations (Id),
        CONSTRAINT FK_AssetTransfers_AppUsers           FOREIGN KEY (TransferredByUserId) REFERENCES dbo.AppUsers (Id)
    );
END
GO

-- Lets us fetch one asset's history in date order quickly (R3.2).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AssetTransfers_AssetId_TransferDate')
BEGIN
    CREATE INDEX IX_AssetTransfers_AssetId_TransferDate ON dbo.AssetTransfers (AssetId, TransferDateUtc DESC);
END
GO

PRINT N'001_schema.sql completed.';
GO
