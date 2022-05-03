using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EspraAPI.Service;


namespace EspraAPI.Handlers
{
    public static class GroupHandler
    {
        static GroupHandler()
        {
            // Init
        }

        [Authorize(Roles = "Admin")]
        public static async Task<object> GetGroupInfo(string groupId, GroupService groupService, CancellationToken token)
        {
            var info = await groupService.GetGroupInfoAsync(groupId, token);

            return info == null ? Results.BadRequest() : Results.Ok(info);
        }
    }
}
