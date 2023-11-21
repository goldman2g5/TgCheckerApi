using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TgCheckerApi.Models.BaseModels;

public partial class TgDbContext : DbContext
{
    public TgDbContext()
    {
    }

    public TgDbContext(DbContextOptions<TgDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Apikey> Apikeys { get; set; }

    public virtual DbSet<Channel> Channels { get; set; }

    public virtual DbSet<ChannelAccess> ChannelAccesses { get; set; }

    public virtual DbSet<ChannelHasSubscription> ChannelHasSubscriptions { get; set; }

    public virtual DbSet<ChannelHasTag> ChannelHasTags { get; set; }

    public virtual DbSet<Comment> Comments { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationType> NotificationTypes { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<SubType> SubTypes { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Database=TgDb;Username=postgres;Password=vagina21519687");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Admin_pkey");

            entity.ToTable("Admin");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Key).HasColumnName("key");
            entity.Property(e => e.TelegramId).HasColumnName("telegram_id");
        });

        modelBuilder.Entity<Apikey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("APIkeys_pkey");

            entity.ToTable("APIkeys");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ClientName).HasColumnName("client_name");
            entity.Property(e => e.Key).HasColumnName("key");
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Channel_pkey");

            entity.ToTable("Channel");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Avatar).HasColumnName("avatar");
            entity.Property(e => e.Bumps).HasColumnName("bumps");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Flag).HasColumnName("flag");
            entity.Property(e => e.Language).HasColumnName("language");
            entity.Property(e => e.LastBump)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("last_bump");
            entity.Property(e => e.Members).HasColumnName("members");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.NotificationSent).HasColumnName("notification_sent");
            entity.Property(e => e.Notifications).HasColumnName("notifications");
            entity.Property(e => e.PromoPost).HasColumnName("promo_post");
            entity.Property(e => e.PromoPostInterval).HasColumnName("promo_post_interval");
            entity.Property(e => e.PromoPostLast)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("promo_post_last");
            entity.Property(e => e.PromoPostSent).HasColumnName("promo_post_sent");
            entity.Property(e => e.PromoPostTime).HasColumnName("promo_post_time");
            entity.Property(e => e.TelegramId).HasColumnName("telegram_id");
            entity.Property(e => e.User).HasColumnName("user");
        });

        modelBuilder.Entity<ChannelAccess>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ChannelAccess_pkey");

            entity.ToTable("ChannelAccess");

            entity.HasIndex(e => e.ChannelId, "IX_ChannelAccess_channel_id");

            entity.HasIndex(e => e.UserId, "IX_ChannelAccess_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChannelId).HasColumnName("channel_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Channel).WithMany(p => p.ChannelAccesses)
                .HasForeignKey(d => d.ChannelId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_channelaccess_channel");

            entity.HasOne(d => d.User).WithMany(p => p.ChannelAccesses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_fk");
        });

        modelBuilder.Entity<ChannelHasSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ChannelHasSubscription_pkey");

            entity.ToTable("ChannelHasSubscription");

            entity.HasIndex(e => e.ChannelId, "IX_ChannelHasSubscription_channel_id");

            entity.HasIndex(e => e.TypeId, "IX_ChannelHasSubscription_type_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChannelId).HasColumnName("channel_id");
            entity.Property(e => e.Expires)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expires");
            entity.Property(e => e.TypeId).HasColumnName("type_id");

            entity.HasOne(d => d.Channel).WithMany(p => p.ChannelHasSubscriptions)
                .HasForeignKey(d => d.ChannelId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_channelhassubscription_channel");

            entity.HasOne(d => d.Type).WithMany(p => p.ChannelHasSubscriptions)
                .HasForeignKey(d => d.TypeId)
                .HasConstraintName("fk_Sub_type");
        });

        modelBuilder.Entity<ChannelHasTag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ChannelHasTag_pkey");

            entity.ToTable("ChannelHasTag");

            entity.HasIndex(e => e.Channel, "IX_ChannelHasTag_channel");

            entity.HasIndex(e => e.Tag, "IX_ChannelHasTag_tag");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Channel).HasColumnName("channel");
            entity.Property(e => e.Tag).HasColumnName("tag");

            entity.HasOne(d => d.ChannelNavigation).WithMany(p => p.ChannelHasTags)
                .HasForeignKey(d => d.Channel)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_channelhastag_channel");

            entity.HasOne(d => d.TagNavigation).WithMany(p => p.ChannelHasTags)
                .HasForeignKey(d => d.Tag)
                .HasConstraintName("fk_ChannelHasTag_Tag");
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Comment_pkey");

            entity.ToTable("Comment");

            entity.HasIndex(e => e.ChannelId, "IX_Comment_channel_id");

            entity.HasIndex(e => e.ParentId, "IX_Comment_parent_id");

            entity.HasIndex(e => e.UserId, "IX_Comment_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChannelId).HasColumnName("channel_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Channel).WithMany(p => p.Comments)
                .HasForeignKey(d => d.ChannelId)
                .HasConstraintName("fk_comment_channel");

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent)
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("parent_id_fk");

            entity.HasOne(d => d.User).WithMany(p => p.Comments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_id_fk");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Notification_pkey");

            entity.ToTable("Notification");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChannelId).HasColumnName("channel_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.IsNew).HasColumnName("is_new");
            entity.Property(e => e.TypeId).HasColumnName("type_id");

            entity.HasOne(d => d.Channel).WithMany(p => p.NotificationsNavigation)
                .HasForeignKey(d => d.ChannelId)
                .HasConstraintName("channel_id_fk");

            entity.HasOne(d => d.Type).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.TypeId)
                .HasConstraintName("type_id_fk");
        });

        modelBuilder.Entity<NotificationType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("NotificationType_pkey");

            entity.ToTable("NotificationType");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Text).HasColumnName("text");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Report_pkey");

            entity.ToTable("Report");

            entity.HasIndex(e => e.ChannelId, "IX_Report_channel_id");

            entity.HasIndex(e => e.StaffId, "IX_Report_staff_id");

            entity.HasIndex(e => e.UserId, "IX_Report_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChannelId).HasColumnName("channel_id");
            entity.Property(e => e.NotificationSent).HasColumnName("notification_sent");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.ReportTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("report_time");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Text).HasColumnName("text");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Channel).WithMany(p => p.Reports)
                .HasForeignKey(d => d.ChannelId)
                .HasConstraintName("channel_id_fk");

            entity.HasOne(d => d.Staff).WithMany(p => p.Reports)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("staff_id_fk");

            entity.HasOne(d => d.User).WithMany(p => p.Reports)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_id_fk");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Staff_pkey");

            entity.HasIndex(e => e.UserId, "IX_Staff_user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Staff)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_id_fk");
        });

        modelBuilder.Entity<SubType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("SubType_pkey");

            entity.ToTable("SubType");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Multiplier).HasColumnName("multiplier");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Price).HasColumnName("price");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Tag_pkey");

            entity.ToTable("Tag");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Text).HasColumnName("text");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_pkey");

            entity.ToTable("User");

            entity.HasIndex(e => e.ChatId, "chat_id_uq").IsUnique();

            entity.HasIndex(e => e.TelegramId, "telegram_id_uq").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Avatar).HasColumnName("avatar");
            entity.Property(e => e.ChatId).HasColumnName("chat_id");
            entity.Property(e => e.TelegramId).HasColumnName("telegram_id");
            entity.Property(e => e.UniqueKey).HasColumnName("unique_key");
            entity.Property(e => e.Username).HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
