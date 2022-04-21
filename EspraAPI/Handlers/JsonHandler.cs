using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EspraAPI.Service;

namespace EspraAPI.Handlers
{
    public static class JsonHandler
    {
        static JsonHandler()
        {
            // Init
        }

        [Authorize(Roles = "Admin")]
        public static async Task<object> PostJson(string group, [FromBody] dynamic data, JsonService jsonService, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var added = await jsonService.AddAsync(group, data, token);
            
            return added ? Results.Ok() : Results.BadRequest();
        }

        [Authorize(Roles = "Admin")]
        public static async Task<object> GetJsonById(string group, JsonService jsonService, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var collection = await jsonService.GetCollectionByGroupAsync(group, token);

            return collection == null ? Results.BadRequest() : Results.Ok(collection);
        }
}
}
