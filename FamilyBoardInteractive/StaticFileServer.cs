using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MimeTypes;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FamilyBoardInteractive
{
    public static class StaticFileServer
    {
        private const string StaticFilesFolder = "wwwroot";
        private const string DefaultPage = "index.html";

        [FunctionName("ProtectedStaticFileServer")]
        public static HttpResponseMessage Protected(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]HttpRequest req,
            ILogger logger)
        {
            try
            {
                var filePath = GetFilePath(req, logger);

                HttpResponseMessage response = ServeFile(filePath, logger);
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        [FunctionName("StaticFileServer")]
        public static HttpResponseMessage Unprotected(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequest req,
            ILogger logger)
        {
            try
            {
                var filePath = GetFilePath(req, logger);

                // do not offer index.html unprotected
                if(filePath.ToLower().EndsWith(DefaultPage))
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                HttpResponseMessage response = ServeFile(filePath, logger);
                return response;
            }
            catch
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        }

        internal static HttpResponseMessage ServeFile(string filePath, ILogger logger)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = new FileStream(filePath, FileMode.Open);
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
            return response;
        }

        private static string GetFilePath(HttpRequest req, ILogger logger)
        {
            var pathValue = req.Query.FirstOrDefault(q => string.Compare(q.Key, "file", true) == 0).Value[0];

            var path = pathValue ?? "";

            var staticFilesPath = Path.GetFullPath(Path.Combine(Util.GetApplicationRoot(), StaticFilesFolder));
            var fullPath = Path.GetFullPath(Path.Combine(staticFilesPath, path));

            if (!IsInDirectory(staticFilesPath, fullPath))
            {
                throw new ArgumentException("Invalid path");
            }

            return fullPath;
        }

        private static string GetMimeType(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return MimeTypeMap.GetMimeType(fileInfo.Extension);
        }

        private static bool IsInDirectory(string parentPath, string childPath)
        {
            var parent = new DirectoryInfo(parentPath);
            var child = new DirectoryInfo(childPath);

            var dir = child;
            do
            {
                if (dir.FullName == parent.FullName)
                {
                    return true;
                }
                dir = dir.Parent;
            } while (dir != null);

            return false;
        }
    }
}
