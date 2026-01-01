using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using StarGate.Server.Business.Commands;
using StarGate.Server.Data;

namespace Stargate.Server.Tests
{
    [TestFixture]
    public class CreateAstronautDutyTests
    {
        private MockRepository _mocks = null!;

        [SetUp]
        public void SetUp()
        {
            _mocks = new MockRepository(MockBehavior.Strict);
        }

        [TearDown]
        public void TearDown()
        {
            _mocks.VerifyAll();
        }

        private CreateAstronautDuty CreateSampleCommand()
        {
            return new CreateAstronautDuty
            {
                Name = "John Doe",
                DutyTitle = "Mission Specialist",
                DutyStartDate = new DateTime(2022, 5, 1, 12, 0, 0, DateTimeKind.Utc)
            };
        }

        [Test]
        public void Initialization_AllPropertiesSet()
        {
            var start = new DateTime(2022, 5, 1, 12, 0, 0, DateTimeKind.Utc);
            var cmd = new CreateAstronautDuty
            {
                Name = "John Doe",
                DutyTitle = "Mission Specialist",
                DutyStartDate = start
            };

            Assert.That("John Doe".Equals(cmd.Name));
            Assert.That("Mission Specialist".Equals(cmd.DutyTitle));
            Assert.That(start.Equals(cmd.DutyStartDate));
        }

        [Test]
        public void Implements_IRequestOfCreateAstronautDutyResult()
        {
            Assert.That(typeof(MediatR.IRequest<CreateAstronautDutyResult>).IsAssignableFrom(typeof(CreateAstronautDuty)),
                "CreateAstronautDuty should implement IRequest<CreateAstronautDutyResult>");
        }

        [Test]
        public void DefaultDutyStartDate_IsDefaultDateTime()
        {
            var cmd = new CreateAstronautDuty
            {
                Name = "Bob",
                DutyTitle = "Pilot"
                // DutyStartDate left unset -> default(DateTime)
            };

            Assert.That(default(DateTime).Equals(cmd.DutyStartDate));
        }

        [Test]
        public void AllowsEmptyStrings_ForNameAndDutyTitleAtRuntime()
        {
            var cmd = new CreateAstronautDuty
            {
                Name = string.Empty,
                DutyTitle = string.Empty,
                DutyStartDate = DateTime.Now
            };

            Assert.That(string.Empty.Equals(cmd.Name));
            Assert.That(string.Empty.Equals(cmd.DutyTitle));
        }

        [Test]
        public async Task Handler_Mock_Handle_IsInvokedAndReturnsResult()
        {
            var cmd = CreateSampleCommand();
            var expected = new CreateAstronautDutyResult();

            var mockHandler = _mocks.Create<MediatR.IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>>();
            mockHandler
                .Setup(h => h.Handle(It.Is<CreateAstronautDuty>(c => c.Name == cmd.Name && c.DutyTitle == cmd.DutyTitle), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await mockHandler.Object.Handle(cmd, CancellationToken.None);

            Assert.That(expected.Equals(result));
        }

        // Integration-style unit test that exercises CreateAstronautDutyHandler.Handle using an in-memory SQLite DB.
        // This avoids trying to mock extension-method Dapper calls and validates the handler behavior end-to-end.
        [Test]
        public async Task Handler_Handle_CreatesAstronautDutyAndDetail_WhenPersonExists()
        {
            // Arrange: in-memory SQLite connection (must stay open for the DbContext lifetime)
            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(connection)
                .Options;

            // Create schema
            await using (var context = new StargateContext(options))
            {
                context.Database.EnsureCreated();

                // Seed a Person that the handler will query via Dapper
                var person = new Person { Name = "John Doe" };
                context.People.Add(person);
                await context.SaveChangesAsync();
            }

            // Act: create a new context that shares the same connection so Dapper queries see the same data
            CreateAstronautDutyResult result;
            await using (var context = new StargateContext(options))
            {
                var handler = new CreateAstronautDutyHandler(context);
                var request = CreateSampleCommand();

                result = await handler.Handle(request, CancellationToken.None);
            }

            // Assert: verify DB state in a fresh context using same connection
            await using (var context = new StargateContext(options))
            {
                // New duty was added
                var duty = context.AstronautDuties.OrderByDescending(d => d.Id).FirstOrDefault();
                Assert.That(duty is not null, "Expected an AstronautDuty to be created.");
                Assert.That("Mission Specialist".Equals(duty!.DutyTitle));
                Assert.That(new DateTime(2022, 5, 1).Date.Equals(duty.DutyStartDate.Date));

                // Result.Id should match the created duty's Id (if handler sets it)
                if (result.Id.HasValue)
                {
                    Assert.That(duty.Id.Equals(result.Id.Value));
                }

                // AstronautDetail should exist and have career start date set
                var detail = context.AstronautDetails.FirstOrDefault(d => d.PersonId == duty.PersonId);
                Assert.That(detail is not null, "Expected an AstronautDetail to be created or updated.");
                Assert.That(new DateTime(2022, 5, 1).Date.Equals(detail!.CareerStartDate.Date));
            }
        }
    }
}