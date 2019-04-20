using System;
using System.Collections.Generic;
using System.Text;

namespace FamilyBoardInteractive
{
    public static class Constants
    {
        public const int CalendarWeeks = 3;

        public const string SCHEDULEUPDATECALENDAR = "0 */5 * * * *";

        public const string QUEUEMESSAGEREFRESHMSATOKEN = "refreshmsatoken";
        public const string QUEUEMESSAGEUPDATECALENDER = "updatecalendar";
        public const string QUEUEMESSAGEUPDATEIMAGE = "updateimage";
        public const string QUEUEMESSAGEPUSHIMAGE = "pushimage";
    }
}
