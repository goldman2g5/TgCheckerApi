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

    public virtual DbSet<Channel> Channels { get; set; }

    public virtual DbSet<ChannelAccess> ChannelAccesses { get; set; }

    public virtual DbSet<ChannelHasSubscription> ChannelHasSubscriptions { get; set; }

    public virtual DbSet<ChannelHasTag> ChannelHasTags { get; set; }

    public virtual DbSet<SubType> SubTypes { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Database=TgDb;Username=postgres;Password=vagina21519687");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Channel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Channel_pkey");

            entity.ToTable("Channel");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Avatar).HasColumnName("avatar");
            entity.Property(e => e.Bumps).HasColumnName("bumps");
            entity.Property(e => e.Description).HasColumnName("description");
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

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChannelId).HasColumnName("channel_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Channel).WithMany(p => p.ChannelAccesses)
                .HasForeignKey(d => d.ChannelId)
                .HasConstraintName("channel_fk");

            entity.HasOne(d => d.User).WithMany(p => p.ChannelAccesses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_fk");
        });

        modelBuilder.Entity<ChannelHasSubscription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ChannelHasSubscription_pkey");

            entity.ToTable("ChannelHasSubscription");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChannelId).HasColumnName("channel_id");
            entity.Property(e => e.Expires)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expires");
            entity.Property(e => e.TypeId).HasColumnName("type_id");

            entity.HasOne(d => d.Channel).WithMany(p => p.ChannelHasSubscriptions)
                .HasForeignKey(d => d.ChannelId)
                .HasConstraintName("fk_Sub_channel");

            entity.HasOne(d => d.Type).WithMany(p => p.ChannelHasSubscriptions)
                .HasForeignKey(d => d.TypeId)
                .HasConstraintName("fk_Sub_type");
        });

        modelBuilder.Entity<ChannelHasTag>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ChannelHasTag_pkey");

            entity.ToTable("ChannelHasTag");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Channel).HasColumnName("channel");
            entity.Property(e => e.Tag).HasColumnName("tag");

            entity.HasOne(d => d.ChannelNavigation).WithMany(p => p.ChannelHasTags)
                .HasForeignKey(d => d.Channel)
                .HasConstraintName("fk_ChannelHasTag_Channel");

            entity.HasOne(d => d.TagNavigation).WithMany(p => p.ChannelHasTags)
                .HasForeignKey(d => d.Tag)
                .HasConstraintName("fk_ChannelHasTag_Tag");
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
            entity.Property(e => e.ChatId).HasColumnName("chat_id");
            entity.Property(e => e.TelegramId).HasColumnName("telegram_id");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
