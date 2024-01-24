using System;
using System.Collections.Generic;

namespace TgCheckerApi.Models.TempScaffold;

public partial class Apikey
{
    public int Id { get; set; }

    public string ClientName { get; set; } = null!;

    public string Key { get; set; } = null!;
}
