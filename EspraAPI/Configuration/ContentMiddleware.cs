namespace EspraAPI.Configuration
{
    public static class ContentMiddleware
    {
        private static string[] ContentTypes = { 
            "image", 
            "text",
        };

        public static async Task UploadFilter(HttpContext context, RequestDelegate next) 
        {
            var request = context.Request;
            var hasContent = request.HasFormContentType;

            if (hasContent)
            {
                var contentKeys = request.Form.Keys;
                var contentKeySize = contentKeys.Count;
                var allowedKeysCount = 0;

                foreach (var key in contentKeys)
                    allowedKeysCount += ContentTypes.Contains(key) ? 1 : 0;

                if (allowedKeysCount == contentKeySize)
                    await next.Invoke(context);
                else
                    return;

            }
            else
                await next.Invoke(context);
        }
    }
}
