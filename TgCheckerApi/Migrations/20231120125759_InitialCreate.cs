using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TgCheckerApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admin",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    key = table.Column<string>(type: "text", nullable: false),
                    telegram_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Admin_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "APIkeys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_name = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("APIkeys_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Channel",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    members = table.Column<int>(type: "integer", nullable: true),
                    avatar = table.Column<byte[]>(type: "bytea", nullable: true),
                    user = table.Column<int>(type: "integer", nullable: true),
                    notifications = table.Column<bool>(type: "boolean", nullable: true),
                    bumps = table.Column<int>(type: "integer", nullable: true),
                    last_bump = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    telegram_id = table.Column<long>(type: "bigint", nullable: true),
                    notification_sent = table.Column<bool>(type: "boolean", nullable: true),
                    promo_post = table.Column<bool>(type: "boolean", nullable: true),
                    promo_post_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    promo_post_interval = table.Column<int>(type: "integer", nullable: true),
                    promo_post_sent = table.Column<bool>(type: "boolean", nullable: true),
                    promo_post_last = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    language = table.Column<string>(type: "text", nullable: true),
                    flag = table.Column<string>(type: "text", nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true),
                    Hidden = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Channel_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SubType",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    price = table.Column<int>(type: "integer", nullable: true),
                    multiplier = table.Column<decimal>(type: "numeric", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("SubType_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Tag",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    text = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Tag_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    telegram_id = table.Column<long>(type: "bigint", nullable: true),
                    chat_id = table.Column<long>(type: "bigint", nullable: true),
                    username = table.Column<string>(type: "text", nullable: false),
                    avatar = table.Column<byte[]>(type: "bytea", nullable: true),
                    unique_key = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelHasSubscription",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type_id = table.Column<int>(type: "integer", nullable: true),
                    expires = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    channel_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ChannelHasSubscription_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_Sub_type",
                        column: x => x.type_id,
                        principalTable: "SubType",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_channelhassubscription_channel",
                        column: x => x.channel_id,
                        principalTable: "Channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelHasTag",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tag = table.Column<int>(type: "integer", nullable: true),
                    channel = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ChannelHasTag_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_ChannelHasTag_Tag",
                        column: x => x.tag,
                        principalTable: "Tag",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_channelhastag_channel",
                        column: x => x.channel,
                        principalTable: "Channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChannelAccess",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    channel_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("ChannelAccess_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_channelaccess_channel",
                        column: x => x.channel_id,
                        principalTable: "Channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "user_fk",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    content = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    channel_id = table.Column<int>(type: "integer", nullable: false),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Comment_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_comment_channel",
                        column: x => x.channel_id,
                        principalTable: "Channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "parent_id_fk",
                        column: x => x.parent_id,
                        principalTable: "Comment",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "user_id_fk",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Staff",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Staff_pkey", x => x.id);
                    table.ForeignKey(
                        name: "user_id_fk",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Report",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    channel_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    report_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    text = table.Column<string>(type: "text", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    notification_sent = table.Column<bool>(type: "boolean", nullable: true),
                    status = table.Column<string>(type: "text", nullable: true),
                    staff_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Report_pkey", x => x.id);
                    table.ForeignKey(
                        name: "channel_id_fk",
                        column: x => x.channel_id,
                        principalTable: "Channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "staff_id_fk",
                        column: x => x.staff_id,
                        principalTable: "Staff",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "user_id_fk",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAccess_channel_id",
                table: "ChannelAccess",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelAccess_user_id",
                table: "ChannelAccess",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelHasSubscription_channel_id",
                table: "ChannelHasSubscription",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelHasSubscription_type_id",
                table: "ChannelHasSubscription",
                column: "type_id");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelHasTag_channel",
                table: "ChannelHasTag",
                column: "channel");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelHasTag_tag",
                table: "ChannelHasTag",
                column: "tag");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_channel_id",
                table: "Comment",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_parent_id",
                table: "Comment",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_user_id",
                table: "Comment",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Report_channel_id",
                table: "Report",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_Report_staff_id",
                table: "Report",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_Report_user_id",
                table: "Report",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Staff_user_id",
                table: "Staff",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "chat_id_uq",
                table: "User",
                column: "chat_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "telegram_id_uq",
                table: "User",
                column: "telegram_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admin");

            migrationBuilder.DropTable(
                name: "APIkeys");

            migrationBuilder.DropTable(
                name: "ChannelAccess");

            migrationBuilder.DropTable(
                name: "ChannelHasSubscription");

            migrationBuilder.DropTable(
                name: "ChannelHasTag");

            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropTable(
                name: "Report");

            migrationBuilder.DropTable(
                name: "SubType");

            migrationBuilder.DropTable(
                name: "Tag");

            migrationBuilder.DropTable(
                name: "Channel");

            migrationBuilder.DropTable(
                name: "Staff");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
