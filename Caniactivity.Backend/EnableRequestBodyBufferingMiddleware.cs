namespace Caniactivity.Backend
{
    public class EnableRequestBodyBufferingMiddleware
    {
        private readonly RequestDelegate _next;

        public EnableRequestBodyBufferingMiddleware(RequestDelegate next) =>
            _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            Stream originalBody = context.Response.Body;
            try
            {
                using (var memStream = new MemoryStream())
                {
                    context.Request.EnableBuffering();

                    await context.Request.Body.CopyToAsync(memStream);
                    memStream.Position = 0; // rewind

                    string body = new StreamReader(memStream).ReadToEnd();

                    memStream.Position = 0; // rewind again
                    context.Request.Body = memStream; // put back in place for downstream handlers

                    await _next(context);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }
    }
}
