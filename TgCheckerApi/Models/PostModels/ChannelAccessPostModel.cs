﻿using Newtonsoft.Json;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Models
{
    public class ChannelAccessPostModel : ChannelAccess
    {
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual Channel? Channel { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        new public virtual User? User { get; set; }

    }
}
