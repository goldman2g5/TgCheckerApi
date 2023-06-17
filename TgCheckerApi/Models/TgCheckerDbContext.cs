using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TgCheckerApi.Models;

public partial class TgCheckerDbContext : DbContext
{
    public TgCheckerDbContext()
    {
    }

    public TgCheckerDbContext(DbContextOptions<TgCheckerDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Channel> Channels { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=localhost;Database=TgCheckerDb;Username=postgres;Password=vagina21519687");

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
            entity.Property(e => e.Notifications).HasColumnName("notifications");
            entity.Property(e => e.User).HasColumnName("user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
