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


        private static string GetScriptPath()
            => Path.Combine(GetEnvironmentVariable("HOME"), @"site\wwwroot");

        public static string GetEnvironmentVariable(string name)
            => System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}
