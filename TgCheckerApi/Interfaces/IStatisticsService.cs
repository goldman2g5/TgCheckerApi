using Microsoft.AspNetCore.Mvc;

namespace TgCheckerApi.Interfaces
{
    public interface IStatisticsService
    {
        Task<IActionResult> CallFunctionAsync(string functionName, object parameters, TimeSpan timeout);
        T ResponseToObject<T>(IActionResult response);
    }
}