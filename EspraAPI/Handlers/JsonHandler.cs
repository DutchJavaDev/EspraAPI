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
        public static async Task<object> GetJsonByGroupId(string group, JsonService jsonService, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var collection = await jsonService.GetCollectionByGroupAsync(group, token);

            return collection == null ? Results.NotFound() : Results.Ok(collection);
        }

        [Authorize(Roles = "Admin")]
        public static async Task<object> GetJsonById(string id, JsonService jsonService, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var json = await jsonService.GetByIdAsync(id, token);

            return json == null ? Results.NotFound(json) : Results.Ok(json);
        }

        [Authorize(Roles = "Admin")]
        public static async Task<object> UpdateJsonById(string id, [FromBody] dynamic ndata, JsonService jsonService, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return await jsonService.UpdateByIdAsync(id, ndata, token) ? Results.Ok() : Results.BadRequest();
        }

        [Authorize(Roles = "Admin")]
        public static async Task<object> DeleteJsonById(string id, JsonService jsonService, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            return await jsonService.DeleteByIdAsync(id, token) ? Results.Ok() : Results.BadRequest();
        }
}

}
