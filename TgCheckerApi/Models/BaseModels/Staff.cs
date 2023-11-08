using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.BaseModels;

public partial class Staff
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public virtual User User { get; set; } = null!;
}
