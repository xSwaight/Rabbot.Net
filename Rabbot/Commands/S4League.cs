using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Rabbot.Database;
using Rabbot.ImageGenerator;
using Discord.WebSocket;
using Rabbot.Preconditions;
using Serilog;
using Serilog.Core;
using Rabbot.Database.Rabbot;
using Rabbot.Services;
using Microsoft.Extensions.DependencyInjection;
using Discord.Addons.Interactive;

namespace Rabbot.Commands
{
    public class S4League : InteractiveBase
    {
        private static readonly ILogger _logger = Log.ForContext(Serilog.Core.Constants.SourceContextPropertyName, nameof(S4League));
        private DatabaseService Database => DatabaseService.Instance;
    }
}
