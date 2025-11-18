using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pto.track.data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeIdToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraints first
            migrationBuilder.Sql("ALTER TABLE [AbsenceRequests] DROP CONSTRAINT IF EXISTS [FK_AbsenceRequests_Resources_ApproverId]");
            migrationBuilder.Sql("ALTER TABLE [AbsenceRequests] DROP CONSTRAINT IF EXISTS [FK_AbsenceRequests_Resources_EmployeeId]");

            // Drop and recreate Events table with GUID Id
            migrationBuilder.Sql("DROP TABLE IF EXISTS [Events]");
            migrationBuilder.Sql(@"
                CREATE TABLE [Events] (
                    [Id] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
                    [Start] datetime2(7) NOT NULL,
                    [End] datetime2(7) NOT NULL,
                    [Text] nvarchar(200) NULL,
                    [Color] nvarchar(50) NULL,
                    [ResourceId] int NOT NULL,
                    CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Events_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE
                )");
            migrationBuilder.Sql("CREATE INDEX [IX_Events_ResourceId] ON [Events] ([ResourceId])");

            // Drop and recreate AbsenceRequests table with GUID Id
            migrationBuilder.Sql("DROP TABLE IF EXISTS [AbsenceRequests]");
            migrationBuilder.Sql(@"
                CREATE TABLE [AbsenceRequests] (
                    [Id] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
                    [Start] datetime2(7) NOT NULL,
                    [End] datetime2(7) NOT NULL,
                    [Reason] nvarchar(500) NOT NULL,
                    [EmployeeId] int NOT NULL,
                    [Status] int NOT NULL,
                    [RequestedDate] datetime2(7) NOT NULL,
                    [ApproverId] int NULL,
                    [ApprovedDate] datetime2(7) NULL,
                    [ApprovalComments] nvarchar(1000) NULL,
                    CONSTRAINT [PK_AbsenceRequests] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_AbsenceRequests_Resources_ApproverId] FOREIGN KEY ([ApproverId]) REFERENCES [Resources] ([Id]),
                    CONSTRAINT [FK_AbsenceRequests_Resources_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE
                )");
            migrationBuilder.Sql("CREATE INDEX [IX_AbsenceRequests_ApproverId] ON [AbsenceRequests] ([ApproverId])");
            migrationBuilder.Sql("CREATE INDEX [IX_AbsenceRequests_EmployeeId] ON [AbsenceRequests] ([EmployeeId])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraints first
            migrationBuilder.Sql("ALTER TABLE [AbsenceRequests] DROP CONSTRAINT IF EXISTS [FK_AbsenceRequests_Resources_ApproverId]");
            migrationBuilder.Sql("ALTER TABLE [AbsenceRequests] DROP CONSTRAINT IF EXISTS [FK_AbsenceRequests_Resources_EmployeeId]");

            // Drop and recreate Events table with int identity Id
            migrationBuilder.Sql("DROP TABLE IF EXISTS [Events]");
            migrationBuilder.Sql(@"
                CREATE TABLE [Events] (
                    [Id] int IDENTITY(1,1) NOT NULL,
                    [Start] datetime2(7) NOT NULL,
                    [End] datetime2(7) NOT NULL,
                    [Text] nvarchar(200) NULL,
                    [Color] nvarchar(50) NULL,
                    [ResourceId] int NOT NULL,
                    CONSTRAINT [PK_Events] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Events_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE
                )");
            migrationBuilder.Sql("CREATE INDEX [IX_Events_ResourceId] ON [Events] ([ResourceId])");

            // Drop and recreate AbsenceRequests table with int identity Id
            migrationBuilder.Sql("DROP TABLE IF EXISTS [AbsenceRequests]");
            migrationBuilder.Sql(@"
                CREATE TABLE [AbsenceRequests] (
                    [Id] int IDENTITY(1,1) NOT NULL,
                    [Start] datetime2(7) NOT NULL,
                    [End] datetime2(7) NOT NULL,
                    [Reason] nvarchar(500) NOT NULL,
                    [EmployeeId] int NOT NULL,
                    [Status] int NOT NULL,
                    [RequestedDate] datetime2(7) NOT NULL,
                    [ApproverId] int NULL,
                    [ApprovedDate] datetime2(7) NULL,
                    [ApprovalComments] nvarchar(1000) NULL,
                    CONSTRAINT [PK_AbsenceRequests] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_AbsenceRequests_Resources_ApproverId] FOREIGN KEY ([ApproverId]) REFERENCES [Resources] ([Id]),
                    CONSTRAINT [FK_AbsenceRequests_Resources_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE
                )");
            migrationBuilder.Sql("CREATE INDEX [IX_AbsenceRequests_ApproverId] ON [AbsenceRequests] ([ApproverId])");
            migrationBuilder.Sql("CREATE INDEX [IX_AbsenceRequests_EmployeeId] ON [AbsenceRequests] ([EmployeeId])");
        }
    }
}
