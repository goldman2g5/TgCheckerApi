﻿using System.Text.Json.Serialization;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Models.PostModels
{
    public class ReportPostModel : Report
    {
        [JsonIgnore]
        new public virtual int? UserId
        {
            get { return base.UserId; }
            set { base.UserId = (int)value; }
        }
        [JsonIgnore]
        new public virtual int? ChannelId
        {
            get { return base.ChannelId; }
            set { base.ChannelId = (int)value; }
        }

        [JsonIgnore]
        new public virtual DateTime? ReportTime
        {
            get { return base.ReportTime; }
            set { base.ReportTime = value; }
        }
        [JsonIgnore]
        new public virtual Channel? Channel { get; set; } = null!;


    }
}