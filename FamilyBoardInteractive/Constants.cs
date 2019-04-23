namespace FamilyBoardInteractive
{
    public static class Constants
    {
        public const int CalendarWeeks = 3;

        public const string SCHEDULEUPDATECALENDAR = "0 */5 * * * *";
        public const string SCHEDULEUPDATEIMAGE = "0 */1 * * * *";

        public const string QUEUEMESSAGEREFRESHMSATOKEN = "refreshmsatoken";
        public const string QUEUEMESSAGEUPDATECALENDER = "updatecalendar";
        public const string QUEUEMESSAGEUPDATEIMAGE = "updateimage";
        public const string QUEUEMESSAGEPUSHIMAGE = "pushimage";

        public const string BLOBPATHBOARDIMAGE = "images/boardimage.jpg";

        public const string TOKEN_TABLE = "Tokens";
        public const string TOKEN_PARTITIONKEY = "Token";
        public const string MSATOKEN_ROWKEY = "MSA";

        public const string DEFAULT_TIMEZONE = "Europe/Berlin";
    }
}
