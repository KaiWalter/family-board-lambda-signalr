namespace FamilyBoardInteractive
{
    public static class Constants
    {
        public const int CalendarWeeks = 3;

        public const string SCHEDULEUPDATECALENDAR = "0 */5 4-20 * * *";
        public const string SCHEDULEUPDATEIMAGE = "0 */1 4-22 * * *";

        public const string QUEUEMESSAGEREFRESHMSATOKEN = "refreshmsatoken";
        public const string QUEUEMESSAGEREFRESHGOOGLETOKEN = "refreshgoogletoken";
        public const string QUEUEMESSAGEUPDATECALENDER = "updatecalendar";
        public const string QUEUEMESSAGEUPDATEIMAGE = "updateimage";
        public const string QUEUEMESSAGEPUSHIMAGE = "pushimage";
        public const string QUEUEMESSAGECONFIGURUATION = "configuration";

        public const string BLOBPATHBOARDIMAGE = "images/boardimage.jpg";
        public const string BLOBPATHCONTAINER = "familyboard";
        public const string BLOBPATHIMAGESPLAYED = "familyboard/playedimages.json";
        public const string BLOBCONTENTTYPEIMAGESPLAYED = "application/json";

        public const string TOKEN_TABLE = "Tokens";
        public const string TOKEN_PARTITIONKEY = "Token";
        public const string MSATOKEN_ROWKEY = "MSA";
        public const string GOOGLETOKEN_ROWKEY = "Google";

        public const string DEFAULT_TIMEZONE = "Europe/Berlin";
    }
}
