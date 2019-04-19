using FamilyBoardInteractive.Services;
using NUnit.Framework;
using System;

namespace FamilyBoardInteractive.UnitTests
{
    public class CalendarTests
    {
        GoogleCalendarService service;

        [SetUp]
        public void Setup()
        {
            service = new GoogleCalendarService(
                    serviceAccount: TestContext.Parameters["GOOGLE_SERVICE_ACCOUNT"],
                    certificateThumbprint: TestContext.Parameters["GOOGLE_CERTIFICATE_THUMBPRINT"],
                    calendarId: TestContext.Parameters["GOOGLE_CALENDAR_ID"]
                );
        }

        [Test]
        public void TestGoogleCalendarServiceSample()
        {
            // arrange
            // act
            var result = service.GetEventsSample();

            // assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Count, 0);
        }

        [Test]
        public void TestGoogleCalendarService1Week()
        {
            // arrange
            var start = DateTime.Now.Date;
            var end = DateTime.Now.Date.AddDays(7);

            // act
            var result = service.GetEvents(start, end);

            // assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Count, 0);
        }
    }
}