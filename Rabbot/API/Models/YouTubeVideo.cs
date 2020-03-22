using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot.API.Models
{
    public class YouTubeVideo
    {
        public string Title { get; set; }
        public string ChannelName { get; set; }
        public string Id { get; set; }
        public DateTimeOffset UploadDate { get; set; }
    }
}
