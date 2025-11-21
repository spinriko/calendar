using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pto.track.data.Migrations
{
    /// <inheritdoc />
    public partial class FixResourcesIdIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if Resources table Id column is already an IDENTITY column
            // If not, we need to recreate the table with IDENTITY
            migrationBuilder.Sql(@"
                -- Check if Id is already an IDENTITY column
                IF NOT EXISTS (
                    SELECT 1 
                    FROM sys.identity_columns 
                    WHERE object_id = OBJECT_ID('Resources') 
                    AND name = 'Id'
                )
                BEGIN
                    -- Create temporary table with IDENTITY
                    CREATE TABLE [Resources_Temp] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [Name] nvarchar(100) NOT NULL,
                        [Email] nvarchar(255) NULL,
                        [EmployeeNumber] nvarchar(50) NULL,
                        [Role] nvarchar(max) NOT NULL,
                        [IsApprover] bit NOT NULL,
                        [IsActive] bit NOT NULL,
                        [Department] nvarchar(100) NULL,
                        [ManagerId] int NULL,
                        [ActiveDirectoryId] nvarchar(255) NULL,
                        [LastSyncDate] datetime2 NULL,
                        [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        [ModifiedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [PK_Resources_Temp] PRIMARY KEY ([Id])
                    );

                    -- Copy data preserving existing Ids
                    SET IDENTITY_INSERT [Resources_Temp] ON;
                    INSERT INTO [Resources_Temp] ([Id], [Name], [Email], [EmployeeNumber], [Role], [IsApprover], [IsActive], [Department], [ManagerId], [ActiveDirectoryId], [LastSyncDate], [CreatedDate], [ModifiedDate])
                    SELECT [Id], [Name], [Email], [EmployeeNumber], [Role], [IsApprover], [IsActive], [Department], [ManagerId], [ActiveDirectoryId], [LastSyncDate], [CreatedDate], [ModifiedDate]
                    FROM [Resources];
                    SET IDENTITY_INSERT [Resources_Temp] OFF;

                    -- Drop foreign key constraints referencing Resources
                    DECLARE @sql NVARCHAR(MAX) = '';
                    SELECT @sql = @sql + 'ALTER TABLE [' + OBJECT_NAME(parent_object_id) + '] DROP CONSTRAINT [' + name + '];'
                    FROM sys.foreign_keys
                    WHERE referenced_object_id = OBJECT_ID('Resources');
                    EXEC sp_executesql @sql;

                    -- Drop old table
                    DROP TABLE [Resources];

                    -- Rename temp table
                    EXEC sp_rename 'Resources_Temp', 'Resources';
                    EXEC sp_rename 'PK_Resources_Temp', 'PK_Resources', 'OBJECT';

                    -- Recreate foreign keys
                    ALTER TABLE [AbsenceRequests] 
                        ADD CONSTRAINT [FK_AbsenceRequests_Resources_EmployeeId] 
                        FOREIGN KEY ([EmployeeId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE;

                    ALTER TABLE [AbsenceRequests] 
                        ADD CONSTRAINT [FK_AbsenceRequests_Resources_ApproverId] 
                        FOREIGN KEY ([ApproverId]) REFERENCES [Resources] ([Id]);

                    ALTER TABLE [Events] 
                        ADD CONSTRAINT [FK_Events_Resources_ResourceId] 
                        FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE;

                    ALTER TABLE [Resources]
                        ADD CONSTRAINT [FK_Resources_Resources_ManagerId]
                        FOREIGN KEY ([ManagerId]) REFERENCES [Resources] ([Id]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot revert IDENTITY to non-IDENTITY without data loss
            // This is a one-way migration for safety
        }
    }
}

