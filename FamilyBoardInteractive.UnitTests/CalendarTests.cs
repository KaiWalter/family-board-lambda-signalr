using FamilyBoardInteractive.Services;
using NUnit.Framework;

namespace FamilyBoardInteractive.UnitTests
{
    public class CalendarTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestGoogleCalendarService()
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
    }
}