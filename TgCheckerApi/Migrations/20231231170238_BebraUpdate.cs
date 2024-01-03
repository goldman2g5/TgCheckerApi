using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TgCheckerApi.Migrations
{
    /// <inheritdoc />
    public partial class BebraUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "notification_settings",
                table: "User",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tag_limit",
                table: "SubType",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "comment_id",
                table: "Report",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "report_type",
                table: "Report",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationSettings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Bump = table.Column<bool>(type: "boolean", nullable: false),
                    Important = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("NotificationSettings_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationType",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("NotificationType_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    subscription_type_id = table.Column<int>(type: "integer", nullable: false),
                    duration = table.Column<int>(type: "integer", nullable: false),
                    autoRenewal = table.Column<bool>(type: "boolean", nullable: false),
                    discount = table.Column<int>(type: "integer", nullable: false),
                    channelId = table.Column<int>(type: "integer", nullable: false),
                    channel_name = table.Column<string>(type: "text", nullable: false),
                    userId = table.Column<int>(type: "integer", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    expires = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("payments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "channel_id_fk",
                        column: x => x.channelId,
                        principalTable: "Channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "subtype_fk",
                        column: x => x.subscription_type_id,
                        principalTable: "SubType",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "user_id_fk",
                        column: x => x.userId,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "ReportType",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ReportType_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StatisticsSheet",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("StatisticsSheet_pkey", x => x.id);
                    table.ForeignKey(
                        name: "channel_id_fk",
                        column: x => x.channel_id,
                        principalTable: "Channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel_id = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    is_new = table.Column<bool>(type: "boolean", nullable: false),
                    type_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Notification_pkey", x => x.id);
                    table.ForeignKey(
                        name: "channel_id_fk",
                        column: x => x.channel_id,
                        principalTable: "Channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "type_id_fk",
                        column: x => x.type_id,
                        principalTable: "NotificationType",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "user_id_fk",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscribersRecord",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subscribers = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sheet = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("SubscribersRecord_pkey", x => x.id);
                    table.ForeignKey(
                        name: "sub_sheet_fk",
                        column: x => x.sheet,
                        principalTable: "StatisticsSheet",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ViewsRecord",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    views = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sheet = table.Column<int>(type: "integer", nullable: false),
                    last_message_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ViewsRecord_pkey", x => x.id);
                    table.ForeignKey(
                        name: "StatisticsSheet_fk",
                        column: x => x.sheet,
                        principalTable: "StatisticsSheet",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_notification_settings",
                table: "User",
                column: "notification_settings");

            migrationBuilder.CreateIndex(
                name: "IX_Report_comment_id",
                table: "Report",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "IX_Report_report_type",
                table: "Report",
                column: "report_type");

            migrationBuilder.CreateIndex(
                name: "IX_Channel_user",
                table: "Channel",
                column: "user");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_channel_id",
                table: "Notification",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_type_id",
                table: "Notification",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_user_id",
                table: "Notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_channelId",
                table: "payments",
                column: "channelId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_subscription_type_id",
                table: "payments",
                column: "subscription_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_userId",
                table: "payments",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_StatisticsSheet_channel_id",
                table: "StatisticsSheet",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_SubscribersRecord_sheet",
                table: "SubscribersRecord",
                column: "sheet");

            migrationBuilder.CreateIndex(
                name: "IX_ViewsRecord_sheet",
                table: "ViewsRecord",
                column: "sheet");

            migrationBuilder.AddForeignKey(
                name: "fk_user_id",
                table: "Channel",
                column: "user",
                principalTable: "User",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "comment_id_fk",
                table: "Report",
                column: "comment_id",
                principalTable: "Comment",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "report_type_fk",
                table: "Report",
                column: "report_type",
                principalTable: "ReportType",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "notification_settings_fk",
                table: "User",
                column: "notification_settings",
                principalTable: "NotificationSettings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_id",
                table: "Channel");

            migrationBuilder.DropForeignKey(
                name: "user_id_fk",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "channel_id_fk",
                table: "Report");

            migrationBuilder.DropForeignKey(
                name: "comment_id_fk",
                table: "Report");

            migrationBuilder.DropForeignKey(
                name: "report_type_fk",
                table: "Report");

            migrationBuilder.DropForeignKey(
                name: "notification_settings_fk",
                table: "User");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "ReportType");

            migrationBuilder.DropTable(
                name: "SubscribersRecord");

            migrationBuilder.DropTable(
                name: "ViewsRecord");

            migrationBuilder.DropTable(
                name: "NotificationType");

            migrationBuilder.DropTable(
                name: "StatisticsSheet");

            migrationBuilder.DropIndex(
                name: "IX_User_notification_settings",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_Report_comment_id",
                table: "Report");

            migrationBuilder.DropIndex(
                name: "IX_Report_report_type",
                table: "Report");

            migrationBuilder.DropIndex(
                name: "IX_Channel_user",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "notification_settings",
                table: "User");

            migrationBuilder.DropColumn(
                name: "tag_limit",
                table: "SubType");

            migrationBuilder.DropColumn(
                name: "comment_id",
                table: "Report");

            migrationBuilder.DropColumn(
                name: "report_type",
                table: "Report");
        }
    }
}
