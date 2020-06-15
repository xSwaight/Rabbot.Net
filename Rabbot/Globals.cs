using Discord.Addons.Interactive;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rabbot
{
    public class Globals
    {
        public static PaginatedAppearanceOptions PaginatorOptions = new PaginatedAppearanceOptions()
        {
            DisplayInformationIcon = false,
            JumpDisplayOptions = JumpDisplayOptions.Never,
            FieldsPerPage = 10
        };
    }
}
