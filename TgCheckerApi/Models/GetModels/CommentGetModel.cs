﻿using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.Models.GetModels
{
    public class CommentGetModel : Comment
    {

        [System.Text.Json.Serialization.JsonIgnore]
        public int UserId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int ChannelId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int? ParentId { get; set; }

        public List<ReplyGetModel> Replies { get; set; }

        public string Username { get; set; }
    }
}
