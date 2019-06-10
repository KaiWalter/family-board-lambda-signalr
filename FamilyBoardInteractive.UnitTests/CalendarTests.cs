using FamilyBoardInteractive.Models;
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
        public async Task TestGoogleCalendarServiceSample()
        {
            // arrange
            var service = new GoogleCalendarService(
                    googleToken: new TokenEntity()
                    {
                        TokenType = "Bearer",
                        AccessToken = TestContext.Parameters["GOOGLE_TEST_TOKEN"]
                    },
                    calendarId: TestContext.Parameters["GOOGLE_CALENDAR_ID"]
                );

            // act
            var result = await service.GetEventsSample();

            // assert
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task TestGoogleCalendarService1Week()
        {
            // arrange
            var service = new GoogleCalendarService(
                    googleToken: new TokenEntity()
                    {
                        TokenType = "Bearer",
                        AccessToken = TestContext.Parameters["GOOGLE_TEST_TOKEN"]
                    },
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
        public async Task TestOutlookCalendarServiceSample()
        {
            // arrange
            var service = new OutlookCalendarService(
                    msaToken: new TokenEntity()
                    {
                        TokenType = "bearer",
                        AccessToken = TestContext.Parameters["OUTLOOK_TEST_TOKEN"]
                    }
                );

            // act
            var result = await service.GetEventsSample();

            // assert
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task TestOutlookCalendarService1Week()
        {
            // arrange
            var service = new OutlookCalendarService(
                    msaToken: new TokenEntity()
                    {
                        TokenType = "bearer",
                        AccessToken = TestContext.Parameters["OUTLOOK_TEST_TOKEN"]
                    }
                );

            var start = new DateTime(2019, 4, 22);
            var end = new DateTime(2019, 5, 25);

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
            var start = new DateTime(2018, 12, 23);
            var end = new DateTime(2019, 1, 2);

            // act
            var result = await service.GetEvents(start, end);

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(result.Count, 3);
        }

        [Test]
        public async Task TestSchoolHolidaysServiceEaster2019()
        {
            // arrange
            var service = new SchoolHolidaysService();
            var start = new DateTime(2019, 4, 1);
            var end = new DateTime(2019, 5, 1);

            // act
            var result = await service.GetEvents(start, end);

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(13, result.Count);
        }

        [Test]
        public async Task TestSchoolHolidaysServicePentecost2019()
        {
            // arrange
            var service = new SchoolHolidaysService();
            var start = new DateTime(2019, 6, 3);
            var end = new DateTime(2019, 6, 28);

            // act
            var result = await service.GetEvents(start, end);

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(11, result.Count);
        }
    }
}