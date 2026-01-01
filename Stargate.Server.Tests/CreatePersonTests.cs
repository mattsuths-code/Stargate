using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using StarGate.Server.Business.Commands;
using StarGate.Server.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Stargate.Server.Tests
{
    [TestFixture]
    public class CreatePersonTests
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

        private DbContextOptions<StargateContext> CreateOptions()
        {
            // Use a file-backed in-memory SQLite database with a unique name so multiple contexts
            // created with the same connection string share the same in-memory database.
            var name = Guid.NewGuid().ToString();
            var connectionString = $"Data Source=file:{name}?mode=memory&cache=shared";

            return new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(connectionString)
                .Options;
        }

        private static async Task SeedSampleData(DbContextOptions<StargateContext> options)
        {
            await using var context = new StargateContext(options);
            await context.Database.EnsureCreatedAsync();

            if (!context.People.Any())
            {
                var existing = new Person { Name = "Existing" };
                var john = new Person { Name = "John Doe" };

                await context.People.AddRangeAsync(existing, john);
                await context.SaveChangesAsync();

                var johnDetail = new AstronautDetail
                {
                    PersonId = john.Id,
                    CurrentRank = "Lieutenant",
                    CareerStartDate = DateTime.UtcNow.AddYears(-2)
                };

                var johnDuty = new AstronautDuty
                {
                    PersonId = john.Id,
                    DutyTitle = "Pilot",
                    DutyStartDate = DateTime.UtcNow.AddYears(-1)
                };

                await context.AstronautDetails.AddAsync(johnDetail);
                await context.AstronautDuties.AddAsync(johnDuty);
                await context.SaveChangesAsync();
            }
        }

        // PreProcessor tests

        [Test]
        public async Task PreProcessor_Throws_WhenCreatingPersonWithoutDuty()
        {
            // Arrange: in-memory SQLite connection (must stay open for the DbContext lifetime)
            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(connection)
                .Options;

            // Seed fake data
            await SeedSampleData(options);

            await using (var context = new StargateContext(options))
            {
                var pre = new CreatePersonPreProcessor(context);

                var request = new AddEditPerson
                {
                    Name = "New Person",
                    Rank = "Ensign",
                    Duty = null // missing duty -> should throw according to the pre-processor logic
                };

                // Expect BadHttpRequestException
                Assert.ThrowsAsync<BadHttpRequestException>(async () =>
                    await pre.Process(request, CancellationToken.None));
            }
        }

        [Test]
        public async Task PreProcessor_Throws_WhenUpdatingPersonWithDutyProvided()
        {
            // Arrange: in-memory SQLite connection (must stay open for the DbContext lifetime)
            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(connection)
                .Options;

            // Seed fake data (creates 'Existing' person)
            await SeedSampleData(options);

            // seed a person so the preprocessor sees an existing person (redundant-safe)
            await using (var context = new StargateContext(options))
            {
                context.People.Add(new Person { Name = "Existing" });
                await context.SaveChangesAsync();
            }

            await using (var context = new StargateContext(options))
            {
                var pre = new CreatePersonPreProcessor(context);

                var request = new AddEditPerson
                {
                    Name = "Existing",
                    Rank = "Commander",
                    Duty = "New Duty" // updating duty on existing person -> should throw
                };

                Assert.ThrowsAsync<BadHttpRequestException>(async () =>
                    await pre.Process(request, CancellationToken.None));
            }
        }

        [Test]
        public async Task PreProcessor_Completes_WhenCreatingPersonWithRankAndDuty()
        {
            // Arrange: in-memory SQLite connection (must stay open for the DbContext lifetime)
            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(connection)
                .Options;

            // Seed data for test consistency
            await SeedSampleData(options);

            await using (var context = new StargateContext(options))
            {
                var pre = new CreatePersonPreProcessor(context);

                var request = new AddEditPerson
                {
                    Name = "New Person 2",
                    Rank = "Lieutenant",
                    Duty = "Pilot"
                };

                // Should not throw
                Assert.DoesNotThrowAsync(async () => await pre.Process(request, CancellationToken.None));
            }
        }

        // Handler tests

        [Test]
        public async Task Handler_CreatesPersonAndRelatedEntities_WhenPersonDoesNotExist()
        {
            // Arrange: in-memory SQLite connection (must stay open for the DbContext lifetime)
            await using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<StargateContext>()
                .UseSqlite(connection)
                .Options;

            // ensure DB created and seeded (keeps DB consistent)
            await SeedSampleData(options);

            // Arrange - empty DB (we'll create a new person different from seeded names)
            await using (var context = new StargateContext(options))
            {
                await context.Database.EnsureCreatedAsync();
            }

            AddEditPersonResult result;
            // Act - call handler which should create Person, AstronautDetail and AstronautDuty and save changes
            await using (var context = new StargateContext(options))
            {
                var handler = new CreatePersonHandler(context);

                var request = new AddEditPerson
                {
                    Name = "NewPersonCreate",
                    Rank = "Captain",
                    Duty = "Explorer"
                };

                result = await handler.Handle(request, CancellationToken.None);
            }

            // Assert - examine persisted state
            await using (var context = new StargateContext(options))
            {
                var person = context.People.FirstOrDefault(p => p.Name == "NewPersonCreate");
                Assert.That(person is not null, "Person should have been created");

                var detail = context.AstronautDetails.FirstOrDefault(d => d.PersonId == person!.Id);
                Assert.That(detail is not null, "AstronautDetail should have been created");
                Assert.That(detail!.CurrentRank, Is.EqualTo("Captain"));

                var duty = context.AstronautDuties.FirstOrDefault(d => d.PersonId == person.Id && d.DutyTitle == "Explorer");
                Assert.That(duty is not null, "AstronautDuty should have been created");
                Assert.That(result.Id, Is.EqualTo(person.Id));
            }
        }

        [Test]
        public async Task Handler_ReturnsExistingIdAndMarksDetailModified_WhenPersonExists()
        {
            var options = CreateOptions();

            int existingPersonId;
            // Seed person and an existing detail
            await using (var context = new StargateContext(options))
            {
                // Ensure database schema exists for the created in-memory shared DB
                await context.Database.EnsureCreatedAsync();

                var person = new Person { Name = "ExistingPerson" };
                context.People.Add(person);
                await context.SaveChangesAsync();
                existingPersonId = person.Id;

                var detail = new AstronautDetail
                {
                    PersonId = existingPersonId,
                    CurrentRank = "Lieutenant",
                    CareerStartDate = DateTime.UtcNow.AddYears(-1)
                };
                context.AstronautDetails.Add(detail);
                await context.SaveChangesAsync();
            }

            // Use same DB instance (in-memory) and call handler
            await using (var context = new StargateContext(options))
            {
                var handler = new CreatePersonHandler(context);

                var request = new AddEditPerson
                {
                    Name = "ExistingPerson",
                    Rank = "Commander",
                    Duty = null // no duty when updating existing person per preprocessor rules
                };

                var result = await handler.Handle(request, CancellationToken.None);

                // Handler should return existing person's id
                Assert.That(result.Id, Is.EqualTo(existingPersonId));

                // Because the handler loads the detail AsNoTracking(), then calls Update(personDetail),
                // the change tracker in this context should have an entry for AstronautDetail marked Modified
                var modifiedDetailEntry = context.ChangeTracker.Entries<AstronautDetail>()
                    .FirstOrDefault(e => e.State == EntityState.Modified);

                Assert.That(modifiedDetailEntry is not null, "AstronautDetail should be marked as Modified in the context");
                Assert.That(((AstronautDetail)modifiedDetailEntry.Entity).CurrentRank, Is.EqualTo("Commander"));
            }
        }
    }
}