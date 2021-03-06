﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Rabbot.Database.Rabbot
{
    [Table("Streams")]
    public class StreamEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Column]
        public ulong StreamId { get; set; }
        [Column]
        public ulong TwitchUserId { get; set; }
        [Column]
        public DateTime StartTime { get; set; }
        [Column]
        public string Title { get; set; }
        [Column]
        public ulong? AnnouncedGuildId { get; set; }
        public GuildEntity Guild { get; set; }
    }
}
