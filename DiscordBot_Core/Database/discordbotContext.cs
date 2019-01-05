using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DiscordBot_Core.Database
{
    public partial class discordbotContext : DbContext
    {
        public discordbotContext()
        {
        }

        public discordbotContext(DbContextOptions<discordbotContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Experience> Experience { get; set; }
        public virtual DbSet<Guild> Guild { get; set; }
        public virtual DbSet<Muteduser> Muteduser { get; set; }
        public virtual DbSet<User> User { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql(Config.bot.connectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Experience>(entity =>
            {
                entity.ToTable("experience");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Exp)
                    .HasColumnName("exp")
                    .HasColumnType("int(11)");

                entity.Property(e => e.UserId)
                    .HasColumnName("userId")
                    .HasColumnType("bigint(11)");
            });

            modelBuilder.Entity<Guild>(entity =>
            {
                entity.HasKey(e => e.ServerId)
                    .HasName("PRIMARY");

                entity.ToTable("guild");

                entity.Property(e => e.ServerId)
                    .HasColumnName("serverId")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.LogchannelId)
                    .HasColumnName("logchannelId")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.NotificationchannelId)
                    .HasColumnName("notificationchannelId")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Notify)
                    .HasColumnName("notify")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'0'");
            });

            modelBuilder.Entity<Muteduser>(entity =>
            {
                entity.ToTable("muteduser");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.Duration)
                    .HasColumnName("duration")
                    .HasColumnType("datetime");

                entity.Property(e => e.Roles)
                    .IsRequired()
                    .HasColumnName("roles")
                    .HasColumnType("varchar(500)");

                entity.Property(e => e.ServerId)
                    .HasColumnName("serverId")
                    .HasColumnType("bigint(11)");

                entity.Property(e => e.UserId)
                    .HasColumnName("userId")
                    .HasColumnType("bigint(11)");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("user");

                entity.HasIndex(e => e.Id)
                    .HasName("id")
                    .IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("bigint(11)");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("varchar(45)");
            });
        }
    }
}
