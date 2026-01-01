using Dapper;
using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StarGate.Server.Data;
using StarGate.Server.Controllers;
using System.Net;

namespace StarGate.Server.Business.Commands
{
    /// <summary>
    /// Request to create a new astronaut duty for an existing person.
    /// </summary>
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        /// <summary>
        /// The name of the person to assign the duty to.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The title of the duty to create (e.g. "Pilot").
        /// </summary>
        public required string DutyTitle { get; set; }

        /// <summary>
        /// The start date for the duty.
        /// </summary>
        public DateTime DutyStartDate { get; set; }
    }

    /// <summary>
    /// Pre-processor that validates a <see cref="CreateAstronautDuty"/> request before the handler runs.
    /// </summary>
    public class CreateAstronautDutyPreProcessor : IRequestPreProcessor<CreateAstronautDuty>
    {
        private readonly StargateContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAstronautDutyPreProcessor"/> class.
        /// </summary>
        /// <param name="context">The database context to use for validation queries.</param>
        public CreateAstronautDutyPreProcessor(StargateContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Validates the incoming request. Throws <see cref="BadHttpRequestException"/> when the person does not exist
        /// or when an identical duty with the same start date already exists.
        /// </summary>
        /// <param name="request">The incoming <see cref="CreateAstronautDuty"/> request.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A completed task if validation succeeds.</returns>
        /// <exception cref="BadHttpRequestException">Thrown when validation fails.</exception>
        public Task Process(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            if (person is null) throw new BadHttpRequestException("Bad Request");

            var verifyNoPreviousDuty = _context.AstronautDuties.FirstOrDefault(z => z.DutyTitle == request.DutyTitle && z.DutyStartDate == request.DutyStartDate);

            if (verifyNoPreviousDuty is not null) throw new BadHttpRequestException("Bad Request");

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handler that creates or updates the astronaut detail and appends a new duty record.
    /// </summary>
    public class CreateAstronautDutyHandler : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly StargateContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAstronautDutyHandler"/> class.
        /// </summary>
        /// <param name="context">The database context used to perform queries and save changes.</param>
        public CreateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Handles the request by ensuring the person's astronaut detail exists (or updating it) and
        /// by creating a new <see cref="AstronautDuty"/> record. If a previous duty exists it will
        /// be closed by setting its <see cref="AstronautDuty.DutyEndDate"/>.
        /// </summary>
        /// <param name="request">The duty creation request.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="CreateAstronautDutyResult"/> containing the id of the created duty.</returns>
        public async Task<CreateAstronautDutyResult> Handle(CreateAstronautDuty request, CancellationToken cancellationToken)
        {

            var query = "SELECT * FROM [Person] WHERE Name = @Name";

            var person = await _context.Connection.QueryFirstOrDefaultAsync<Person>(query, new { Name = request.Name });

            query = $"SELECT * FROM [AstronautDetail] WHERE PersonId = @PersonId";

            var astronautDetail = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDetail>(query, new { PersonId = person.Id });

            if (astronautDetail == null)
            {
                astronautDetail = new AstronautDetail();
                astronautDetail.PersonId = person.Id;
                astronautDetail.CareerStartDate = request.DutyStartDate.Date;
                if (request.DutyTitle.ToUpper() == "RETIRED")
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.Date;
                }

                await _context.AstronautDetails.AddAsync(astronautDetail);

            }
            else
            {
                if (request.DutyTitle.ToUpper() == "RETIRED")
                {
                    astronautDetail.CareerEndDate = request.DutyStartDate.AddDays(-1).Date;
                }
                _context.AstronautDetails.Update(astronautDetail);
            }

            query = $"SELECT * FROM [AstronautDuty] WHERE {person.Id} = PersonId Order By DutyStartDate Desc";

            var astronautDuty = await _context.Connection.QueryFirstOrDefaultAsync<AstronautDuty>(query);

            if (astronautDuty != null)
            {
                astronautDuty.DutyEndDate = request.DutyStartDate.AddDays(-1).Date;
                _context.AstronautDuties.Update(astronautDuty);
            }

            var newAstronautDuty = new AstronautDuty()
            {
                PersonId = person.Id,
                DutyTitle = request.DutyTitle,
                DutyStartDate = request.DutyStartDate.Date,
                DutyEndDate = null
            };

            await _context.AstronautDuties.AddAsync(newAstronautDuty);

            await _context.SaveChangesAsync();

            return new CreateAstronautDutyResult()
            {
                Id = newAstronautDuty.Id
            };
        }
    }

    /// <summary>
    /// Result returned after creating an astronaut duty.
    /// </summary>
    public class CreateAstronautDutyResult : BaseResponse
    {
        /// <summary>
        /// The created duty id, or null when creation did not occur.
        /// </summary>
        public int? Id { get; set; }
    }
}
