using Microsoft.EntityFrameworkCore;
using MySql.Data.EntityFrameworkCore.Extensions;
using Rabbot.Database.Rabbot;

namespace Rabbot.Database
{
    public class RabbotContext : DbContext
    {
        public DbSet<GuildEntity> Guilds { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<FeatureEntity> Features { get; set; }
        public DbSet<StreamEntity> Streams { get; set; }
        public DbSet<WarningEntity> Warnings { get; set; }
        public DbSet<YouTubeVideoEntity> YouTubeVideos { get; set; }
        public DbSet<SongEntity> Songs { get; set; }
        public DbSet<RemnantsPlayerEntity> RemnantsPlayers { get; set; }
        public DbSet<RandomAnswerEntity> RandomAnswers { get; set; }
        public DbSet<PotEntity> Pots { get; set; }
        public DbSet<OfficialPlayerEntity> OfficialPlayers { get; set; }
        public DbSet<NamechangeEntity> Namechanges { get; set; }
        public DbSet<MutedUserEntity> MutedUsers { get; set; }
        public DbSet<MusicrankEntity> Musicranks { get; set; }
        public DbSet<ItemEntity> Items { get; set; }
        public DbSet<InventoryEntity> Inventorys { get; set; }
        public DbSet<EventEntity> Events { get; set; }
        public DbSet<CurrentDayEntity> CurrentDay { get; set; }
        public DbSet<CombiEntity> Combis { get; set; }
        public DbSet<BadWordEntity> BadWords { get; set; }
        public DbSet<AttackEntity> Attacks { get; set; }
        public DbSet<RoleEntity> Roles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("server=localhost;database=newrabbot;user=root");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Users
            modelBuilder.Entity<UserEntity>(entity =>
            {
                entity.Property(x => x.Notify)
                .HasDefaultValue(true);

                entity.Property(x => x.Name)
                .IsUnicode();
            });

            //Guilds
            modelBuilder.Entity<GuildEntity>(entity =>
            {
                entity.Property(x => x.Level)
                .HasDefaultValue(true);

                entity.Property(x => x.Notify)
                .HasDefaultValue(false);

                entity.Property(x => x.Log)
                .HasDefaultValue(false);

                entity.Property(x => x.Trash)
                .HasDefaultValue(false);
            });

            //Features
            modelBuilder.Entity<FeatureEntity>(entity =>
            {
                entity.Property(x => x.HasLeft)
                .HasDefaultValue(false);

                entity.Property(x => x.Exp)
                .HasDefaultValue(0);

                entity.Property(x => x.Goats)
                .HasDefaultValue(0);

                entity.Property(x => x.Eggs)
                .HasDefaultValue(0);

                entity.Property(x => x.StreakLevel)
                .HasDefaultValue(0);

                entity.Property(x => x.TodaysWords)
                .HasDefaultValue(0);

                entity.Property(x => x.TotalWords)
                .HasDefaultValue(0);

                entity.Property(x => x.CombiExp)
                .HasDefaultValue(0);

                entity.Property(x => x.Wins)
                .HasDefaultValue(0);

                entity.Property(x => x.Loses)
                .HasDefaultValue(0);

                entity.Property(x => x.Trades)
                .HasDefaultValue(0);

                entity.Property(x => x.Attacks)
                .HasDefaultValue(0);

                entity.Property(x => x.Spins)
                .HasDefaultValue(0);

                entity.Property(x => x.Gewinn)
                .HasDefaultValue(0);

                entity.Property(x => x.GainExp)
                .HasDefaultValue(true);

                entity.Property(x => x.Locked)
                .HasDefaultValue(false);

                entity.HasOne(x => x.Guild)
                .WithMany(x => x.Features)
                .HasForeignKey(x => x.GuildId);

                entity.HasOne(x => x.User)
                .WithMany(x => x.Features)
                .HasForeignKey(x => x.UserId);
            });

            //Namechanges
            modelBuilder.Entity<NamechangeEntity>(entity =>
            {
                entity.Property(x => x.NewName)
                .IsUnicode();

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Namechanges)
                    .HasForeignKey(x => x.UserId);
            });

            //Songs
            modelBuilder.Entity<SongEntity>(entity =>
            {
                entity.Property(x => x.Active)
                .HasDefaultValue(false);
            });

            //Pots
            modelBuilder.Entity<PotEntity>(entity =>
            {
                entity.Property(x => x.Goats)
                    .HasDefaultValue(0);

                entity.HasOne(d => d.Guild)
                    .WithMany(p => p.Pots)
                    .HasForeignKey(x => x.GuildId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Pots)
                    .HasForeignKey(x => x.UserId);
            });

            //Roles
            modelBuilder.Entity<RoleEntity>(entity =>
            {
                entity.HasOne(d => d.Guild)
                    .WithMany(p => p.Roles)
                    .HasForeignKey(x => x.GuildId);
            });

            //MutedUsers
            modelBuilder.Entity<MutedUserEntity>(entity =>
            {
                entity.HasOne(d => d.Guild)
                    .WithMany(p => p.MutedUsers)
                    .HasForeignKey(x => x.GuildId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.MutedUsers)
                    .HasForeignKey(x => x.UserId);
            });

            //Musicranks
            modelBuilder.Entity<MusicrankEntity>(entity => {
                entity.Property(x => x.Seconds)
                .HasDefaultValue(0);

                entity.HasOne(d => d.Guild)
                    .WithMany(p => p.Musicranks)
                    .HasForeignKey(x => x.GuildId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Musicranks)
                    .HasForeignKey(x => x.UserId);
            });

            //Inventorys
            modelBuilder.Entity<InventoryEntity>(entity => { 
                entity.Property(x => x.Durability)
                .HasDefaultValue(0);

                entity.HasOne(d => d.Feature)
                    .WithMany(p => p.Inventory)
                    .HasForeignKey(x => x.FeatureId);

                entity.HasOne(d => d.Item)
                    .WithMany(p => p.Inventory)
                    .HasForeignKey(x => x.ItemId);
            });

            //Combis
            modelBuilder.Entity<CombiEntity>(entity => {
                entity.Property(x => x.Accepted)
                .HasDefaultValue(false);

                entity.HasOne(d => d.Guild)
                .WithMany(p => p.Combis)
                .HasForeignKey(x => x.GuildId);

                entity.HasOne(d => d.CombiUser)
                .WithMany(p => p.CombiCombiUsers)
                .HasForeignKey(x => x.CombiUserId);

                entity.HasOne(d => d.User)
                .WithMany(p => p.CombiUsers)
                .HasForeignKey(x => x.UserId);

            });

            //Attacks
            modelBuilder.Entity<AttackEntity>(entity => {
                entity.HasOne(d => d.Target)
                    .WithMany(p => p.AttackTargets)
                    .HasForeignKey(x => x.TargetId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.AttackUsers)
                    .HasForeignKey(x => x.UserId);

                entity.HasOne(d => d.Guild)
                    .WithMany(p => p.Attacks)
                    .HasForeignKey(x => x.GuildId);
            });

            //Badwords
            modelBuilder.Entity<BadWordEntity>(entity => {
                entity.HasOne(d => d.Guild)
                    .WithMany(p => p.BadWords)
                    .HasForeignKey(x => x.GuildId);
            });

            //Warnings
            modelBuilder.Entity<WarningEntity>(entity => {
                entity.HasOne(d => d.Guild)
                    .WithMany(p => p.Warnings)
                    .HasForeignKey(x => x.GuildId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Warnings)
                    .HasForeignKey(x => x.UserId);
            });
        }
    }
}
