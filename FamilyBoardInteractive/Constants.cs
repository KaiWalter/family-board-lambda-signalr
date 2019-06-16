namespace FamilyBoardInteractive
{
    internal static class Constants
    {
        internal const int CalendarWeeks = 3;

        internal const string BLOBPATHBOARDIMAGE = "images/boardimage.jpg";
        internal const string BLOBPATHCONTAINER = "familyboard";
        internal const string BLOBPATHIMAGESPLAYED = "familyboard/playedimages.json";
        internal const string BLOBCONTENTTYPEIMAGESPLAYED = "application/json";

        internal const string TOKEN_TABLE = "Tokens";
        internal const string TOKEN_PARTITIONKEY = "Token";
        internal const string MSATOKEN_ROWKEY = "MSA";
        internal const string GOOGLETOKEN_ROWKEY = "Google";

        internal const string DEFAULT_TIMEZONE = "Europe/Berlin";

        internal const int IMAGES_PLAYED_CUTOFF = 10;
        internal const int IMAGES_SELECTION_POOL_TOP_X_PERCENT = 20;

        internal const string GOOGLE_TOKEN_URI = "https://www.googleapis.com/oauth2/v4/token";
        internal const string MSA_TOKEN_URI = "https://login.live.com/oauth20_token.srf";

        internal const string ONEDRIVEPATH = "https://graph.microsoft.com/v1.0/me/drive/root:/{0}:/children";
    }
}
