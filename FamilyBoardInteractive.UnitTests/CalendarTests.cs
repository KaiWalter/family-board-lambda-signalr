using FamilyBoardInteractive.Services;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

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
        public async Task TestGoogleCalendarService1Week()
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
            var result = await service.GetEvents(start, end);

            // assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Count, 0);
        }

        [Test]
        public async Task TestPublicHolidaysServiceYearCutOver()
        {
            // arrange
            var service = new PublicHolidaysService();
            var start = new DateTime(2018,12,23);
            var end = new DateTime(2019,1,2);

            // act
            var result = await service.GetEvents(start, end);

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Count, 3);
        }
    }
}