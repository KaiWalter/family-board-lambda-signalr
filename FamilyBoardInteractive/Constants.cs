namespace FamilyBoardInteractive
{
    internal static class Constants
    {
        internal const int CalendarWeeks = 3;

        internal const string SCHEDULEUPDATECALENDAR = "0 */5 4-20 * * *";
        internal const string SCHEDULEUPDATEIMAGE = "0 */1 4-22 * * *";

        internal const string BLOBPATHBOARDIMAGE = "images/boardimage.jpg";
        internal const string BLOBPATHCONTAINER = "familyboard";
        internal const string BLOBPATHIMAGESPLAYED = "familyboard/playedimages.json";
        internal const string BLOBCONTENTTYPEIMAGESPLAYED = "application/json";

        internal const string TOKEN_TABLE = "Tokens";
        internal const string TOKEN_PARTITIONKEY = "Token";
        internal const string MSATOKEN_ROWKEY = "MSA";
        internal const string GOOGLETOKEN_ROWKEY = "Google";

        internal const string DEFAULT_TIMEZONE = "Europe/Berlin";

        internal const string GOOGLE_TOKEN_URI = "https://www.googleapis.com/oauth2/v4/token";
        internal const string MSA_TOKEN_URI = "https://login.live.com/oauth20_token.srf";
    }
}
