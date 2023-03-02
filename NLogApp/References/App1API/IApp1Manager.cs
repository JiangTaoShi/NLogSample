using Refit;

namespace NLogApp.References.App1API
{
    public interface IApp1Manager
    {

        [Get("/weatherforecast/getData")]
        Task<string> GetData([AliasAs("name")] string name);
    }
}
