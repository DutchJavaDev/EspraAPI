using Microsoft.AspNetCore.Mvc.Filters;

namespace EspraAPI.Identity
{
    public class UploadLimitAttribute : Attribute, IAuthorizationFilter
    {
        private int Limit;

        private string[] FileNames;

        public UploadLimitAttribute(int limit = 1600, params string[] ids)
        {
            Limit = limit;
            FileNames = ids;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;
            var hasContent = request.HasFormContentType;

            if (hasContent)
            {
                var file = request.Form.Files.First();
            }
        }

        private bool HasIds()
        {
            return FileNames.Any();
        }
    }
}
