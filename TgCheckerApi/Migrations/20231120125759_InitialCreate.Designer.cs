﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TgCheckerApi.Models.BaseModels;

#nullable disable

namespace TgCheckerApi.Migrations
{
    [DbContext(typeof(TgDbContext))]
    [Migration("20231120125759_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0-preview.5.23280.1")
                .HasAnnotation("Proxies:ChangeTracking", false)
                .HasAnnotation("Proxies:CheckEquality", false)
                .HasAnnotation("Proxies:LazyLoading", true)
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Admin", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("key");

                    b.Property<long?>("TelegramId")
                        .HasColumnType("bigint")
                        .HasColumnName("telegram_id");

                    b.HasKey("Id")
                        .HasName("Admin_pkey");

                    b.ToTable("Admin", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Apikey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClientName")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("client_name");

                    b.Property<string>("Key")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("key");

                    b.HasKey("Id")
                        .HasName("APIkeys_pkey");

                    b.ToTable("APIkeys", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Channel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<byte[]>("Avatar")
                        .HasColumnType("bytea")
                        .HasColumnName("avatar");

                    b.Property<int?>("Bumps")
                        .HasColumnType("integer")
                        .HasColumnName("bumps");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<string>("Flag")
                        .HasColumnType("text")
                        .HasColumnName("flag");

                    b.Property<bool?>("Hidden")
                        .HasColumnType("boolean");

                    b.Property<string>("Language")
                        .HasColumnType("text")
                        .HasColumnName("language");

                    b.Property<DateTime?>("LastBump")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("last_bump");

                    b.Property<int?>("Members")
                        .HasColumnType("integer")
                        .HasColumnName("members");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<bool?>("NotificationSent")
                        .HasColumnType("boolean")
                        .HasColumnName("notification_sent");

                    b.Property<bool?>("Notifications")
                        .HasColumnType("boolean")
                        .HasColumnName("notifications");

                    b.Property<bool?>("PromoPost")
                        .HasColumnType("boolean")
                        .HasColumnName("promo_post");

                    b.Property<int?>("PromoPostInterval")
                        .HasColumnType("integer")
                        .HasColumnName("promo_post_interval");

                    b.Property<DateTime?>("PromoPostLast")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("promo_post_last");

                    b.Property<bool?>("PromoPostSent")
                        .HasColumnType("boolean")
                        .HasColumnName("promo_post_sent");

                    b.Property<TimeOnly?>("PromoPostTime")
                        .HasColumnType("time without time zone")
                        .HasColumnName("promo_post_time");

                    b.Property<long?>("TelegramId")
                        .HasColumnType("bigint")
                        .HasColumnName("telegram_id");

                    b.Property<string>("Url")
                        .HasColumnType("text");

                    b.Property<int?>("User")
                        .HasColumnType("integer")
                        .HasColumnName("user");

                    b.HasKey("Id")
                        .HasName("Channel_pkey");

                    b.ToTable("Channel", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.ChannelAccess", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int?>("ChannelId")
                        .HasColumnType("integer")
                        .HasColumnName("channel_id");

                    b.Property<int?>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("ChannelAccess_pkey");

                    b.HasIndex("ChannelId");

                    b.HasIndex("UserId");

                    b.ToTable("ChannelAccess", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.ChannelHasSubscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int?>("ChannelId")
                        .HasColumnType("integer")
                        .HasColumnName("channel_id");

                    b.Property<DateTime?>("Expires")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("expires");

                    b.Property<int?>("TypeId")
                        .HasColumnType("integer")
                        .HasColumnName("type_id");

                    b.HasKey("Id")
                        .HasName("ChannelHasSubscription_pkey");

                    b.HasIndex("ChannelId");

                    b.HasIndex("TypeId");

                    b.ToTable("ChannelHasSubscription", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.ChannelHasTag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int?>("Channel")
                        .HasColumnType("integer")
                        .HasColumnName("channel");

                    b.Property<int?>("Tag")
                        .HasColumnType("integer")
                        .HasColumnName("tag");

                    b.HasKey("Id")
                        .HasName("ChannelHasTag_pkey");

                    b.HasIndex("Channel");

                    b.HasIndex("Tag");

                    b.ToTable("ChannelHasTag", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Comment", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ChannelId")
                        .HasColumnType("integer")
                        .HasColumnName("channel_id");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("content");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<int?>("ParentId")
                        .HasColumnType("integer")
                        .HasColumnName("parent_id");

                    b.Property<int?>("Rating")
                        .HasColumnType("integer")
                        .HasColumnName("rating");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("Comment_pkey");

                    b.HasIndex("ChannelId");

                    b.HasIndex("ParentId");

                    b.HasIndex("UserId");

                    b.ToTable("Comment", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Report", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("ChannelId")
                        .HasColumnType("integer")
                        .HasColumnName("channel_id");

                    b.Property<bool?>("NotificationSent")
                        .HasColumnType("boolean")
                        .HasColumnName("notification_sent");

                    b.Property<string>("Reason")
                        .HasColumnType("text")
                        .HasColumnName("reason");

                    b.Property<DateTime?>("ReportTime")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("report_time");

                    b.Property<int?>("StaffId")
                        .HasColumnType("integer")
                        .HasColumnName("staff_id");

                    b.Property<string>("Status")
                        .HasColumnType("text")
                        .HasColumnName("status");

                    b.Property<string>("Text")
                        .HasColumnType("text")
                        .HasColumnName("text");

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("Report_pkey");

                    b.HasIndex("ChannelId");

                    b.HasIndex("StaffId");

                    b.HasIndex("UserId");

                    b.ToTable("Report", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Staff", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("UserId")
                        .HasColumnType("integer")
                        .HasColumnName("user_id");

                    b.HasKey("Id")
                        .HasName("Staff_pkey");

                    b.HasIndex("UserId");

                    b.ToTable("Staff");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.SubType", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal?>("Multiplier")
                        .HasColumnType("numeric")
                        .HasColumnName("multiplier");

                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.Property<int?>("Price")
                        .HasColumnType("integer")
                        .HasColumnName("price");

                    b.HasKey("Id")
                        .HasName("SubType_pkey");

                    b.ToTable("SubType", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Text")
                        .HasColumnType("text")
                        .HasColumnName("text");

                    b.HasKey("Id")
                        .HasName("Tag_pkey");

                    b.ToTable("Tag", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<byte[]>("Avatar")
                        .HasColumnType("bytea")
                        .HasColumnName("avatar");

                    b.Property<long?>("ChatId")
                        .HasColumnType("bigint")
                        .HasColumnName("chat_id");

                    b.Property<long?>("TelegramId")
                        .HasColumnType("bigint")
                        .HasColumnName("telegram_id");

                    b.Property<string>("UniqueKey")
                        .HasColumnType("text")
                        .HasColumnName("unique_key");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("username");

                    b.HasKey("Id")
                        .HasName("user_pkey");

                    b.HasIndex(new[] { "ChatId" }, "chat_id_uq")
                        .IsUnique();

                    b.HasIndex(new[] { "TelegramId" }, "telegram_id_uq")
                        .IsUnique();

                    b.ToTable("User", (string)null);
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.ChannelAccess", b =>
                {
                    b.HasOne("TgCheckerApi.Models.BaseModels.Channel", "Channel")
                        .WithMany("ChannelAccesses")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("fk_channelaccess_channel");

                    b.HasOne("TgCheckerApi.Models.BaseModels.User", "User")
                        .WithMany("ChannelAccesses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("user_fk");

                    b.Navigation("Channel");

                    b.Navigation("User");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.ChannelHasSubscription", b =>
                {
                    b.HasOne("TgCheckerApi.Models.BaseModels.Channel", "Channel")
                        .WithMany("ChannelHasSubscriptions")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("fk_channelhassubscription_channel");

                    b.HasOne("TgCheckerApi.Models.BaseModels.SubType", "Type")
                        .WithMany("ChannelHasSubscriptions")
                        .HasForeignKey("TypeId")
                        .HasConstraintName("fk_Sub_type");

                    b.Navigation("Channel");

                    b.Navigation("Type");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.ChannelHasTag", b =>
                {
                    b.HasOne("TgCheckerApi.Models.BaseModels.Channel", "ChannelNavigation")
                        .WithMany("ChannelHasTags")
                        .HasForeignKey("Channel")
                        .OnDelete(DeleteBehavior.Cascade)
                        .HasConstraintName("fk_channelhastag_channel");

                    b.HasOne("TgCheckerApi.Models.BaseModels.Tag", "TagNavigation")
                        .WithMany("ChannelHasTags")
                        .HasForeignKey("Tag")
                        .HasConstraintName("fk_ChannelHasTag_Tag");

                    b.Navigation("ChannelNavigation");

                    b.Navigation("TagNavigation");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Comment", b =>
                {
                    b.HasOne("TgCheckerApi.Models.BaseModels.Channel", "Channel")
                        .WithMany("Comments")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("fk_comment_channel");

                    b.HasOne("TgCheckerApi.Models.BaseModels.Comment", "Parent")
                        .WithMany("InverseParent")
                        .HasForeignKey("ParentId")
                        .HasConstraintName("parent_id_fk");

                    b.HasOne("TgCheckerApi.Models.BaseModels.User", "User")
                        .WithMany("Comments")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("user_id_fk");

                    b.Navigation("Channel");

                    b.Navigation("Parent");

                    b.Navigation("User");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Report", b =>
                {
                    b.HasOne("TgCheckerApi.Models.BaseModels.Channel", "Channel")
                        .WithMany("Reports")
                        .HasForeignKey("ChannelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("channel_id_fk");

                    b.HasOne("TgCheckerApi.Models.BaseModels.Staff", "Staff")
                        .WithMany("Reports")
                        .HasForeignKey("StaffId")
                        .HasConstraintName("staff_id_fk");

                    b.HasOne("TgCheckerApi.Models.BaseModels.User", "User")
                        .WithMany("Reports")
                        .HasForeignKey("UserId")
                        .IsRequired()
                        .HasConstraintName("user_id_fk");

                    b.Navigation("Channel");

                    b.Navigation("Staff");

                    b.Navigation("User");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Staff", b =>
                {
                    b.HasOne("TgCheckerApi.Models.BaseModels.User", "User")
                        .WithMany("Staff")
                        .HasForeignKey("UserId")
                        .IsRequired()
                        .HasConstraintName("user_id_fk");

                    b.Navigation("User");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Channel", b =>
                {
                    b.Navigation("ChannelAccesses");

                    b.Navigation("ChannelHasSubscriptions");

                    b.Navigation("ChannelHasTags");

                    b.Navigation("Comments");

                    b.Navigation("Reports");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Comment", b =>
                {
                    b.Navigation("InverseParent");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Staff", b =>
                {
                    b.Navigation("Reports");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.SubType", b =>
                {
                    b.Navigation("ChannelHasSubscriptions");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.Tag", b =>
                {
                    b.Navigation("ChannelHasTags");
                });

            modelBuilder.Entity("TgCheckerApi.Models.BaseModels.User", b =>
                {
                    b.Navigation("ChannelAccesses");

                    b.Navigation("Comments");

                    b.Navigation("Reports");

                    b.Navigation("Staff");
                });
#pragma warning restore 612, 618
        }
    }
}
