using System.Linq;
using Awesomium.Core;
using System.IO;

namespace BtcClicksBot
{
    class CustomInterceptor : IResourceInterceptor
    {

        private static string[] blackList = {
                                                  ".png", 
                                                  ".jpg",
                                                  ".jpeg",
                                                  ".gif",
                                                  ".bmp",
                                                  ".ico",
                                                  ".js",
                                                  ".html",
                                                  ".css",
                                                  ".ajax",
                                                  ".php",
                                                  ".axd",
                                                  ".script"
                                                };

        public ResourceResponse OnRequest(ResourceRequest request)
        {
            var _host = request.Url.Host;

            if ((_host != BtcBot.host) & (_host != BtcBot.solveHost))
            {
                string ext = Path.GetExtension(request.Url.ToString()).ToLower();

                if (ext != string.Empty)
                {
                    int idx = ext.IndexOf('?');
                    if (idx > 0)
                    {
                        ext = ext.Substring(0, idx);
                    }

                    if (blackList.Contains(ext))
                    {
                        request.Cancel();
                    }
                }
            }

            return null;
        }

        public bool OnFilterNavigation(NavigationRequest request)
        {
            return false;
        }
    }

}
