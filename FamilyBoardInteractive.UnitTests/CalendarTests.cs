using FamilyBoardInteractive.Services;
using NUnit.Framework;
using System;

namespace FamilyBoardInteractive.UnitTests
{
    public class CalendarTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestGoogleCalendarServiceSample()
        {
            // arrange
            var service = new GoogleCalendarService(
                    serviceAccount: TestContext.Parameters["GOOGLE_SERVICE_ACCOUNT"],
                    certificateThumbprint: TestContext.Parameters["GOOGLE_CERTIFICATE_THUMBPRINT"],
                    calendarId: TestContext.Parameters["GOOGLE_CALENDAR_ID"]
                );

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
            var service = new GoogleCalendarService(
                    serviceAccount: TestContext.Parameters["GOOGLE_SERVICE_ACCOUNT"],
                    certificateThumbprint: TestContext.Parameters["GOOGLE_CERTIFICATE_THUMBPRINT"],
                    calendarId: TestContext.Parameters["GOOGLE_CALENDAR_ID"]
                );

            var start = DateTime.Now.Date;
            var end = DateTime.Now.Date.AddDays(7);

            // act
            var result = service.GetEvents(start, end);

            // assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Count, 0);
        }

        [Test]
        public void TestHolidaysService1Week()
        {
            // arrange
            var service = new SchoolHolidaysService();
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