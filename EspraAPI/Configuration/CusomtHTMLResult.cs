using System.Net.Mime;
using System.Text;

namespace EspraAPI.Configuration
{
    public class CusomtHTMLResult : IResult
    {
        private readonly string _htmlContent;
        public CusomtHTMLResult(string htmlContent)
        {
            _htmlContent = htmlContent;
        }
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.ContentType = MediaTypeNames.Text.Html;
            httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(_htmlContent);
            await httpContext.Response.WriteAsync(_htmlContent);
        }
    }

    public static class CustomResultExtensions
    {
        public static IResult HtmlResponse(this IResultExtensions extensions, string html)
        {
            return new CusomtHTMLResult(html);
        }
    }
}
