using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pto.track.data.Migrations
{
    /// <inheritdoc />
    public partial class FixEventColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix Events table columns by recreating them
            // Step 1: Drop primary key
            migrationBuilder.Sql("ALTER TABLE [Events] DROP CONSTRAINT [PK_Events];");

            // Step 2: Add temporary columns
            migrationBuilder.Sql(@"
                ALTER TABLE [Events] ADD [StartNew] datetime2 NULL;
                ALTER TABLE [Events] ADD [EndNew] datetime2 NULL;
                ALTER TABLE [Events] ADD [TextNew] nvarchar(200) NULL;
                ALTER TABLE [Events] ADD [ColorNew] nvarchar(50) NULL;
            ");

            // Step 3: Copy data
            migrationBuilder.Sql(@"
                UPDATE [Events] 
                SET 
                    [StartNew] = CONVERT(datetime2, CONVERT(varchar(max), [Start])),
                    [EndNew] = CONVERT(datetime2, CONVERT(varchar(max), [End])),
                    [TextNew] = CONVERT(nvarchar(200), [Text]),
                    [ColorNew] = CONVERT(nvarchar(50), [Color]);
            ");

            // Step 4: Drop old columns
            migrationBuilder.Sql(@"
                ALTER TABLE [Events] DROP COLUMN [Start];
                ALTER TABLE [Events] DROP COLUMN [End];
                ALTER TABLE [Events] DROP COLUMN [Text];
                ALTER TABLE [Events] DROP COLUMN [Color];
            ");

            // Step 5: Rename columns
            migrationBuilder.Sql("EXEC sp_rename 'Events.StartNew', 'Start', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename 'Events.EndNew', 'End', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename 'Events.TextNew', 'Text', 'COLUMN';");
            migrationBuilder.Sql("EXEC sp_rename 'Events.ColorNew', 'Color', 'COLUMN';");

            // Step 6: Make columns NOT NULL
            migrationBuilder.Sql(@"
                ALTER TABLE [Events] ALTER COLUMN [Start] datetime2 NOT NULL;
                ALTER TABLE [Events] ALTER COLUMN [End] datetime2 NOT NULL;
            ");

            // Step 7: Recreate primary key
            migrationBuilder.Sql("ALTER TABLE [Events] ADD CONSTRAINT [PK_Events] PRIMARY KEY ([Id]);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Events table columns
            migrationBuilder.Sql(@"
                ALTER TABLE [Events] DROP CONSTRAINT [PK_Events];
                
                ALTER TABLE [Events] ALTER COLUMN [Start] TEXT NOT NULL;
                ALTER TABLE [Events] ALTER COLUMN [End] TEXT NOT NULL;
                ALTER TABLE [Events] ALTER COLUMN [Text] TEXT NULL;
                ALTER TABLE [Events] ALTER COLUMN [Color] TEXT NULL;
                
                ALTER TABLE [Events] ADD CONSTRAINT [PK_Events] PRIMARY KEY ([Id]);
            ");

            // Revert Resources table columns
            migrationBuilder.Sql(@"
                ALTER TABLE [Resources] DROP CONSTRAINT [PK_Resources];
                
                ALTER TABLE [Resources] ALTER COLUMN [Name] TEXT NOT NULL;
                
                ALTER TABLE [Resources] ADD CONSTRAINT [PK_Resources] PRIMARY KEY ([Id]);
            ");
        }
    }
}
