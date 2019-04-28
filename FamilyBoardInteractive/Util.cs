using System;
using System.IO;
using System.Text.RegularExpressions;

namespace FamilyBoardInteractive
{


    public static class Util
    {
        public static string GetApplicationRoot()
        {
            // check for Azure App Service hosting
            if (!string.IsNullOrEmpty(GetEnvironmentVariable("WEBSITE_SITE_NAME")) &&
                !string.IsNullOrEmpty(GetEnvironmentVariable("HOME")))
            {
                return GetScriptPath();
            }

            var exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            Regex appPathMatcher = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)");
            var appRoot = appPathMatcher.Match(exePath).Value;
            return appRoot;
        }

        public static string GetImagePath()
        {
            string imagePath = Path.Combine(Path.GetTempPath(), "FamilyBoard/images");

            if (!string.IsNullOrEmpty(GetEnvironmentVariable("WEBSITE_SITE_NAME")) &&
                !string.IsNullOrEmpty(GetEnvironmentVariable("HOME")))
            {
                imagePath = Path.Combine(GetEnvironmentVariable("HOME"), @"data/images");
            }

            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }

            return imagePath;
        }

        private static string GetScriptPath()
            => Path.Combine(GetEnvironmentVariable("HOME"), @"site\wwwroot");

        public static string GetEnvironmentVariable(string name)
            => System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}
