using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FamilyBoardInteractive.Services
{
    /// <summary>
    /// store certificate to App Service
    /// https://jan-v.nl/post/loading-certificates-with-azure-functions
    /// </summary>
    public class GoogleCalendarService : ICalendarService
    {
        static string[] Scopes = { CalendarService.Scope.CalendarReadonly };
        static string APPLICATION_NAME = "Family Board";

        string CalendarId;

        CalendarService service;

        public GoogleCalendarService() :
            this(
                serviceAccount: Util.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT"), 
                certificateThumbprint: Util.GetEnvironmentVariable("GOOGLE_CERTIFICATE_THUMBPRINT"),
                calendarId: Util.GetEnvironmentVariable("GOOGLE_CALENDAR_ID"))
        { }

        public GoogleCalendarService(string serviceAccount, string certificateThumbprint, string calendarId)
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            var certificateCollection = store.Certificates.Find(X509FindType.FindByThumbprint, certificateThumbprint, false);
            var certificate = certificateCollection[0];

            var credential = new ServiceAccountCredential(new ServiceAccountCredential.Initializer(serviceAccount)
            {
                Scopes = Scopes
            }.FromCertificate(certificate));

            service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = APPLICATION_NAME,
            });

            CalendarId = calendarId;
        }

        public async Task<List<Models.CalendarEntry>> GetEvents(DateTime startDate, DateTime endDate)
        {
            List<Models.CalendarEntry> eventResults = new List<Models.CalendarEntry>();

            // Define parameters of request.
            EventsResource.ListRequest request = service.Events.List(CalendarId);
            request.TimeMin = startDate;
            request.TimeMax = endDate;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            Events events = await request.ExecuteAsync();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    eventResults.AddRange(CreateCalendarEntries(eventItem));
                }
            }

            return eventResults;
        }

        public async Task<List<Models.CalendarEntry>> GetEventsSample()
        {
            List<Models.CalendarEntry> eventResults = new List<Models.CalendarEntry>();

            // Define parameters of request.
            EventsResource.ListRequest request = service.Events.List(CalendarId);
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            Events events = await request.ExecuteAsync();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    eventResults.AddRange(CreateCalendarEntries(eventItem));
                }
            }

            return eventResults;
        }

        private static List<Models.CalendarEntry> CreateCalendarEntries(Event eventItem)
        {
            var results = new List<Models.CalendarEntry>();

            // for all days events generate an entry for each day
            if (eventItem.Start.DateTime == null && eventItem.End.DateTime == null)
            {
                var currentDT = DateTime.Parse(eventItem.Start.Date);
                var endDT = DateTime.Parse(eventItem.End.Date);
                while(currentDT < endDT)
                {
                    var eventResult = new Models.CalendarEntry()
                    {
                        Date = currentDT.ToString("u").Substring(0,10),
                        Description = eventItem.Summary,
                        AllDayEvent = true
                    };
                    results.Add(eventResult);
                    currentDT = currentDT.AddDays(1);
                }
            }
            // for event on one day create one entry
            else if(eventItem.Start.DateTimeRaw != null && !string.IsNullOrEmpty(eventItem.Start.TimeZone))
            {
                var eventResult = new Models.CalendarEntry()
                {
                    Date = eventItem.Start.DateTimeRaw.Substring(0,10),
                    Time = eventItem.Start.DateTimeRaw.Substring(11, 5),
                    Description = eventItem.Summary
                };
                results.Add(eventResult);
            }

            return results;
        }
    }
}
