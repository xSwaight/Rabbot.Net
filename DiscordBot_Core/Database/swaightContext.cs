using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DiscordBot_Core.Database
{
    public partial class swaightContext : DbContext
    {
        public swaightContext()
        {
        }

        public swaightContext(DbContextOptions<swaightContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Badwords> Badwords { get; set; }
        public virtual DbSet<Currentday> Currentday { get; set; }
        public virtual DbSet<Event> Event { get; set; }
        public virtual DbSet<Experience> Experience { get; set; }
        public virtual DbSet<Guild> Guild { get; set; }
        public virtual DbSet<Musicrank> Musicrank { get; set; }
        public virtual DbSet<Muteduser> Muteduser { get; set; }
        public virtual DbSet<Songlist> Songlist { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<Warning> Warning { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseMySql("server=localhost;database=swaight;user=swaight;pwd=eSmh9HqKWWjNibM1;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Badwords>(entity =>
            {
                entity.ToTable("badwords");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.BadWord)
                    .IsRequired()
                    .HasColumnName("badWord")
                    .HasColumnType("varchar(50)");
            });

            modelBuilder.Entity<Currentday>(entity =>
            {
                entity.ToTable("currentday");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Date)
                    .HasColumnName("date")
                    .HasColumnType("datetime");
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("event");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'0'");
            });

            modelBuilder.Entity<Experience>(entity =>
            {
                entity.ToTable("experience");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Exp)
                    .HasColumnName("exp")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Gain)
                    .HasColumnName("gain")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Goats)
                    .HasColumnName("goats")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Lastdaily)
                    .HasColumnName("lastdaily")
                    .HasColumnType("datetime");

                entity.Property(e => e.Lastmessage)
                    .HasColumnName("lastmessage")
                    .HasColumnType("datetime");

                entity.Property(e => e.ServerId)
                    .HasColumnName("serverId")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Trades)
                    .HasColumnName("trades")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

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

                entity.Property(e => e.Botchannelid)
                    .HasColumnName("botchannelid")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Level)
                    .HasColumnName("level")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Log)
                    .HasColumnName("log")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'0'");

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

            modelBuilder.Entity<Musicrank>(entity =>
            {
                entity.ToTable("musicrank");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.Date)
                    .HasColumnName("date")
                    .HasColumnType("datetime");

                entity.Property(e => e.Sekunden)
                    .HasColumnName("sekunden")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.ServerId)
                    .HasColumnName("serverId")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.UserId)
                    .HasColumnName("userId")
                    .HasColumnType("bigint(20)");
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

            modelBuilder.Entity<Songlist>(entity =>
            {
                entity.ToTable("songlist");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.Active)
                    .HasColumnName("active")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Link)
                    .IsRequired()
                    .HasColumnName("link")
                    .HasColumnType("varchar(200)");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(200)");
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

            modelBuilder.Entity<Warning>(entity =>
            {
                entity.ToTable("warning");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.ActiveUntil)
                    .HasColumnName("activeUntil")
                    .HasColumnType("datetime");

                entity.Property(e => e.Counter)
                    .HasColumnName("counter")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ServerId)
                    .HasColumnName("serverId")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.UserId)
                    .HasColumnName("userId")
                    .HasColumnType("bigint(20)");
            });
        }
    }
}
