﻿using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.TempScaffold;

public partial class TgClient
{
    public int Id { get; set; }

    public string? ApiId { get; set; }

    public string? ApiHash { get; set; }

    public string? PhoneNumber { get; set; }
}
