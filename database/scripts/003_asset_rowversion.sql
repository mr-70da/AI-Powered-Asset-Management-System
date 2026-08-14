/* ============================================================================
   AI-Powered Asset Management System
   Script  : 003_asset_rowversion.sql
   Purpose : Adds the RowVersion optimistic-concurrency token to dbo.Assets
             (R3.5) for databases that were created from an older version of
             001_schema.sql that predated the column.

             It is idempotent - safe to run more than once. Databases created
             from the current 001_schema.sql already include the column, so
             the ALTER is simply skipped.

   How to run:
     sqlcmd -S .\SQLEXPRESS -d AssetManagement -i 003_asset_rowversion.sql
   ============================================================================ */

IF COL_LENGTH(N'dbo.Assets', N'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Assets ADD RowVersion rowversion;
    PRINT N'Added RowVersion to dbo.Assets.';
END
ELSE
BEGIN
    PRINT N'dbo.Assets.RowVersion already exists - skipping.';
END
GO

PRINT N'003_asset_rowversion.sql completed.';
GO
