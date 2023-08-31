namespace TgCheckerApi.MiddleWare
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class BypassApiKeyAttribute : Attribute { }
}
