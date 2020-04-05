using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Rabbot.Database
{
    public partial class rabbotContext : DbContext
    {
        public rabbotContext()
        {
        }

        public rabbotContext(DbContextOptions<rabbotContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Attacks> Attacks { get; set; }
        public virtual DbSet<Badwords> Badwords { get; set; }
        public virtual DbSet<Combi> Combi { get; set; }
        public virtual DbSet<Currentday> Currentday { get; set; }
        public virtual DbSet<Easterevent> Easterevent { get; set; }
        public virtual DbSet<Event> Event { get; set; }
        public virtual DbSet<Guild> Guild { get; set; }
        public virtual DbSet<Inventory> Inventory { get; set; }
        public virtual DbSet<Items> Items { get; set; }
        public virtual DbSet<Musicrank> Musicrank { get; set; }
        public virtual DbSet<Muteduser> Muteduser { get; set; }
        public virtual DbSet<Namechanges> Namechanges { get; set; }
        public virtual DbSet<Officialplayer> Officialplayer { get; set; }
        public virtual DbSet<Pot> Pot { get; set; }
        public virtual DbSet<Randomanswer> Randomanswer { get; set; }
        public virtual DbSet<Remnantsplayer> Remnantsplayer { get; set; }
        public virtual DbSet<Roles> Roles { get; set; }
        public virtual DbSet<Songlist> Songlist { get; set; }
        public virtual DbSet<Stream> Stream { get; set; }
        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<Userfeatures> Userfeatures { get; set; }
        public virtual DbSet<Warning> Warning { get; set; }
        public virtual DbSet<Youtubevideo> Youtubevideo { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql(Config.Bot.ConnectionString);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Attacks>(entity =>
            {
                entity.ToTable("attacks");

                entity.HasIndex(e => e.ServerId)
                    .HasName("serverId");

                entity.HasIndex(e => e.TargetId)
                    .HasName("targetId");

                entity.HasIndex(e => e.UserId)
                    .HasName("userId");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.AttackEnds)
                    .HasColumnName("attackEnds")
                    .HasColumnType("datetime");

                entity.Property(e => e.ChannelId).HasColumnName("channelId");

                entity.Property(e => e.MessageId).HasColumnName("messageId");

                entity.Property(e => e.ServerId).HasColumnName("serverId");

                entity.Property(e => e.TargetId).HasColumnName("targetId");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.HasOne(d => d.Server)
                    .WithMany(p => p.Attacks)
                    .HasForeignKey(d => d.ServerId)
                    .HasConstraintName("attacks_ibfk_1");

                entity.HasOne(d => d.Target)
                    .WithMany(p => p.AttacksTarget)
                    .HasForeignKey(d => d.TargetId)
                    .HasConstraintName("attacks_ibfk_3");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AttacksUser)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("attacks_ibfk_2");
            });

            modelBuilder.Entity<Badwords>(entity =>
            {
                entity.ToTable("badwords");

                entity.HasIndex(e => e.ServerId)
                    .HasName("serverId");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.BadWord)
                    .IsRequired()
                    .HasColumnName("badWord")
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.ServerId).HasColumnName("serverId");

                entity.HasOne(d => d.Server)
                    .WithMany(p => p.Badwords)
                    .HasForeignKey(d => d.ServerId)
                    .HasConstraintName("badwords_ibfk_1");
            });

            modelBuilder.Entity<Combi>(entity =>
            {
                entity.ToTable("combi");

                entity.HasIndex(e => e.CombiUserId)
                    .HasName("combiUserId");

                entity.HasIndex(e => e.ServerId)
                    .HasName("serverId");

                entity.HasIndex(e => e.UserId)
                    .HasName("userId");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Accepted)
                    .IsRequired()
                    .HasColumnName("accepted")
                    .HasColumnType("bit(1)")
                    .HasDefaultValueSql("'b\\'0\\''");

                entity.Property(e => e.CombiUserId).HasColumnName("combiUserId");

                entity.Property(e => e.Date)
                    .HasColumnName("date")
                    .HasColumnType("date");

                entity.Property(e => e.MessageId).HasColumnName("messageId");

                entity.Property(e => e.ServerId).HasColumnName("serverId");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.HasOne(d => d.CombiUser)
                    .WithMany(p => p.CombiCombiUser)
                    .HasForeignKey(d => d.CombiUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("combi_ibfk_3");

                entity.HasOne(d => d.Server)
                    .WithMany(p => p.Combi)
                    .HasForeignKey(d => d.ServerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("combi_ibfk_1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.CombiUser)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("combi_ibfk_2");
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

            modelBuilder.Entity<Easterevent>(entity =>
            {
                entity.HasKey(e => e.MessageId)
                    .HasName("PRIMARY");

                entity.ToTable("easterevent");

                entity.HasIndex(e => e.UserId)
                    .HasName("userId");

                entity.Property(e => e.MessageId).HasColumnName("messageId");

                entity.Property(e => e.CatchTime)
                    .HasColumnName("catchTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.DespawnTime)
                    .HasColumnName("despawnTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.SpawnTime)
                    .HasColumnName("spawnTime")
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("'CURRENT_TIMESTAMP'");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Easterevent)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("easterevent_ibfk_1");
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("event");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.BonusPercent)
                    .HasColumnName("bonusPercent")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.Status)
                    .HasColumnName("status")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'0'");
            });

            modelBuilder.Entity<Guild>(entity =>
            {
                entity.HasKey(e => e.ServerId)
                    .HasName("PRIMARY");

                entity.ToTable("guild");

                entity.Property(e => e.ServerId).HasColumnName("serverId");

                entity.Property(e => e.Botchannelid).HasColumnName("botchannelid");

                entity.Property(e => e.Level)
                    .HasColumnName("level")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.LevelchannelId).HasColumnName("levelchannelId");

                entity.Property(e => e.Log)
                    .HasColumnName("log")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.LogchannelId).HasColumnName("logchannelId");

                entity.Property(e => e.NotificationchannelId).HasColumnName("notificationchannelId");

                entity.Property(e => e.Notify)
                    .HasColumnName("notify")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.StreamchannelId).HasColumnName("streamchannelId");

                entity.Property(e => e.Trash)
                    .HasColumnName("trash")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.TrashchannelId).HasColumnName("trashchannelId");
            });

            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.ToTable("inventory");

                entity.HasIndex(e => e.FeatureId)
                    .HasName("featureId");

                entity.HasIndex(e => e.ItemId)
                    .HasName("itemId");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Durability)
                    .HasColumnName("durability")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.ExpirationDate)
                    .HasColumnName("expirationDate")
                    .HasColumnType("datetime");

                entity.Property(e => e.FeatureId)
                    .HasColumnName("featureId")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ItemId)
                    .HasColumnName("itemId")
                    .HasColumnType("int(11)");

                entity.HasOne(d => d.Feature)
                    .WithMany(p => p.Inventory)
                    .HasForeignKey(d => d.FeatureId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("inventory_ibfk_2");

                entity.HasOne(d => d.Item)
                    .WithMany(p => p.Inventory)
                    .HasForeignKey(d => d.ItemId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("inventory_ibfk_1");
            });

            modelBuilder.Entity<Items>(entity =>
            {
                entity.ToTable("items");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Atk)
                    .HasColumnName("atk")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Def)
                    .HasColumnName("def")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasColumnType("varchar(80)");
            });

            modelBuilder.Entity<Musicrank>(entity =>
            {
                entity.ToTable("musicrank");

                entity.HasIndex(e => e.ServerId)
                    .HasName("serverId");

                entity.HasIndex(e => e.UserId)
                    .HasName("userId");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.Date)
                    .HasColumnName("date")
                    .HasColumnType("datetime");

                entity.Property(e => e.Sekunden)
                    .HasColumnName("sekunden")
                    .HasColumnType("bigint(20)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.ServerId).HasColumnName("serverId");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.HasOne(d => d.Server)
                    .WithMany(p => p.Musicrank)
                    .HasForeignKey(d => d.ServerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("musicrank_ibfk_1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Musicrank)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("musicrank_ibfk_2");
            });

            modelBuilder.Entity<Muteduser>(entity =>
            {
                entity.ToTable("muteduser");

                entity.HasIndex(e => e.ServerId)
                    .HasName("serverId");

                entity.HasIndex(e => e.UserId)
                    .HasName("userId");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.Duration)
                    .HasColumnName("duration")
                    .HasColumnType("datetime");

                entity.Property(e => e.Roles)
                    .IsRequired()
                    .HasColumnName("roles")
                    .HasColumnType("varchar(500)");

                entity.Property(e => e.ServerId).HasColumnName("serverId");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.HasOne(d => d.Server)
                    .WithMany(p => p.Muteduser)
                    .HasForeignKey(d => d.ServerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("muteduser_ibfk_1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Muteduser)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("muteduser_ibfk_2");
            });

            modelBuilder.Entity<Namechanges>(entity =>
            {
                entity.ToTable("namechanges");

                entity.HasIndex(e => e.UserId)
                    .HasName("userId");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Date)
                    .HasColumnName("date")
                    .HasColumnType("datetime");

                entity.Property(e => e.NewName)
                    .IsRequired()
                    .HasColumnName("newName")
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Namechanges)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("namechanges_ibfk_1");
            });

            modelBuilder.Entity<Officialplayer>(entity =>
            {
                entity.ToTable("officialplayer");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Date)
                    .HasColumnName("date")
                    .HasColumnType("datetime");

                entity.Property(e => e.Playercount)
                    .HasColumnName("playercount")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");
            });

            modelBuilder.Entity<Pot>(entity =>
            {
                entity.ToTable("pot");

                entity.HasIndex(e => e.ServerId)
                    .HasName("serverId");

                entity.HasIndex(e => e.UserId)
                    .HasName("userId");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Goats)
                    .HasColumnName("goats")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.ServerId).HasColumnName("serverId");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.HasOne(d => d.Server)
                    .WithMany(p => p.Pot)
                    .HasForeignKey(d => d.ServerId)
                    .HasConstraintName("pot_ibfk_1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Pot)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("pot_ibfk_2");
            });

            modelBuilder.Entity<Randomanswer>(entity =>
            {
                entity.ToTable("randomanswer");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Answer)
                    .IsRequired()
                    .HasColumnName("answer")
                    .HasColumnType("varchar(500)");
            });

            modelBuilder.Entity<Remnantsplayer>(entity =>
            {
                entity.ToTable("remnantsplayer");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("bigint(20)");

                entity.Property(e => e.Date)
                    .HasColumnName("date")
                    .HasColumnType("datetime");

                entity.Property(e => e.Playercount)
                    .HasColumnName("playercount")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");
            });

            modelBuilder.Entity<Roles>(entity =>
            {
                entity.ToTable("roles");

                entity.HasIndex(e => e.ServerId)
                    .HasName("serverId");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnName("description")
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.RoleId).HasColumnName("roleId");

                entity.Property(e => e.ServerId).HasColumnName("serverId");

                entity.HasOne(d => d.Server)
                    .WithMany(p => p.Roles)
                    .HasForeignKey(d => d.ServerId)
                    .HasConstraintName("roles_ibfk_1");
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

            modelBuilder.Entity<Stream>(entity =>
            {
                entity.ToTable("stream");

                entity.Property(e => e.StreamId)
                    .HasColumnName("streamId")
                    .HasColumnType("bigint(11)");

                entity.Property(e => e.StartTime)
                    .HasColumnName("startTime")
                    .HasColumnType("datetime");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasColumnName("title")
                    .HasColumnType("varchar(300)");

                entity.Property(e => e.TwitchUserId)
                    .HasColumnName("twitchUserId")
                    .HasColumnType("bigint(11)");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("user");

                entity.HasIndex(e => e.Id)
                    .HasName("id")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Name)
                    .HasColumnName("name")
                    .HasColumnType("varchar(45)");

                entity.Property(e => e.Notify)
                    .HasColumnName("notify")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'1'");
            });

            modelBuilder.Entity<Userfeatures>(entity =>
            {
                entity.ToTable("userfeatures");

                entity.HasIndex(e => e.ServerId)
                    .HasName("serverId");

                entity.HasIndex(e => e.UserId)
                    .HasName("userId");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Attacks)
                    .HasColumnName("attacks")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.CombiExp)
                    .HasColumnName("combiExp")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Eggs)
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Exp)
                    .HasColumnName("exp")
                    .HasColumnType("int(11)");

                entity.Property(e => e.Gain)
                    .HasColumnName("gain")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'1'");

                entity.Property(e => e.Gewinn)
                    .HasColumnName("gewinn")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Goats)
                    .HasColumnName("goats")
                    .HasColumnType("int(11)");

                entity.Property(e => e.HasLeft)
                    .IsRequired()
                    .HasColumnName("hasLeft")
                    .HasColumnType("bit(1)")
                    .HasDefaultValueSql("'b\\'0\\''");

                entity.Property(e => e.Lastdaily)
                    .HasColumnName("lastdaily")
                    .HasColumnType("datetime");

                entity.Property(e => e.Lastmessage)
                    .HasColumnName("lastmessage")
                    .HasColumnType("datetime");

                entity.Property(e => e.Locked)
                    .HasColumnName("locked")
                    .HasColumnType("int(1)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Loses)
                    .HasColumnName("loses")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.NamechangeUntil)
                    .HasColumnName("namechangeUntil")
                    .HasColumnType("datetime");

                entity.Property(e => e.ServerId).HasColumnName("serverId");

                entity.Property(e => e.Spins)
                    .HasColumnName("spins")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.StreakLevel)
                    .HasColumnName("streakLevel")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.TodaysWords)
                    .HasColumnName("todaysWords")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.TotalWords)
                    .HasColumnName("totalWords")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.Trades)
                    .HasColumnName("trades")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.Property(e => e.Wins)
                    .HasColumnName("wins")
                    .HasColumnType("int(11)")
                    .HasDefaultValueSql("'0'");

                entity.HasOne(d => d.Server)
                    .WithMany(p => p.Userfeatures)
                    .HasForeignKey(d => d.ServerId)
                    .HasConstraintName("userfeatures_ibfk_1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Userfeatures)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("userfeatures_ibfk_2");
            });

            modelBuilder.Entity<Warning>(entity =>
            {
                entity.ToTable("warning");

                entity.HasIndex(e => e.ServerId)
                    .HasName("serverId");

                entity.HasIndex(e => e.UserId)
                    .HasName("userId");

                entity.Property(e => e.Id).HasColumnType("int(11)");

                entity.Property(e => e.ActiveUntil)
                    .HasColumnName("activeUntil")
                    .HasColumnType("datetime");

                entity.Property(e => e.Counter)
                    .HasColumnName("counter")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ServerId).HasColumnName("serverId");

                entity.Property(e => e.UserId).HasColumnName("userId");

                entity.HasOne(d => d.Server)
                    .WithMany(p => p.Warning)
                    .HasForeignKey(d => d.ServerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("warning_ibfk_1");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Warning)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("warning_ibfk_2");
            });

            modelBuilder.Entity<Youtubevideo>(entity =>
            {
                entity.ToTable("youtubevideo");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.VideoId)
                    .HasColumnName("videoId")
                    .HasColumnType("varchar(50)");

                entity.Property(e => e.VideoTitle)
                    .HasColumnName("videoTitle")
                    .HasColumnType("varchar(200)");
            });
        }
    }
}
