﻿using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.TempScaffold;

public partial class ReportType
{
    public int Id { get; set; }

    public string Text { get; set; } = null!;

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
