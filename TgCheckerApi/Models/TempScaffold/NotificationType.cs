using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.TempScaffold;

public partial class NotificationType
{
    public int Id { get; set; }

    public string Text { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
