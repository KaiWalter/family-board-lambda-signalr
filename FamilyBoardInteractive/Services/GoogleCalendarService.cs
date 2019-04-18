using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

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

        public List<Models.CalendarEntry> GetEvents(DateTime startDate, DateTime endDate)
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
            Events events = request.Execute();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    Models.CalendarEntry eventResult = CreateCalendarEntry(eventItem);
                    eventResults.Add(eventResult);
                }
            }

            return eventResults;
        }

        public List<Models.CalendarEntry> GetEventsSample()
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
            Events events = request.Execute();
            if (events.Items != null && events.Items.Count > 0)
            {
                foreach (var eventItem in events.Items)
                {
                    Models.CalendarEntry eventResult = CreateCalendarEntry(eventItem);
                    eventResults.Add(eventResult);
                }
            }

            return eventResults;
        }

        private static Models.CalendarEntry CreateCalendarEntry(Event eventItem)
        {
            var eventResult = new Models.CalendarEntry()
            {
                Date = eventItem.Start.Date ?? eventItem.Start.DateTime?.ToShortDateString(),
                Description = eventItem.Summary
            };

            return eventResult;
        }
    }
}
