using MediatR;
using MediatR.Pipeline;
using Microsoft.EntityFrameworkCore;
using StarGate.Server.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    /// <summary>
    /// Request to create a new person with initial astronaut detail and duty.
    /// </summary>
    public class CreatePerson : IRequest<CreatePersonResult>
    {
        public required string Name { get; set; } = string.Empty;

        public required string Rank { get; set; } = string.Empty;

        public required string Duty { get; set; } = string.Empty;
    }

    /// <summary>
    /// Pre-processor that validates a <see cref="CreatePerson"/> request before the handler runs.
    /// </summary>
    public class CreatePersonPreProcessor : IRequestPreProcessor<CreatePerson>
    {
        private readonly StargateContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePersonPreProcessor"/> class.
        /// </summary>
        /// <param name="context">The <see cref="StargateContext"/> used to query existing people.</param>
        public CreatePersonPreProcessor(StargateContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Ensures the person does not already exist.
        /// Throws <see cref="BadHttpRequestException"/> when validation fails.
        /// </summary>
        /// <param name="request">The incoming <see cref="CreatePerson"/> request.</param>
        /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
        /// <returns>A completed task when validation succeeds.</returns>
        /// <exception cref="BadHttpRequestException">Thrown when a person with the same name already exists.</exception>
        public Task Process(CreatePerson request, CancellationToken cancellationToken)
        {
            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            if (person is not null) throw new BadHttpRequestException("Bad Request");

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles <see cref="CreatePerson"/> requests by creating the Person, associated AstronautDetail, and AstronautDuty.
    /// </summary>
    public class CreatePersonHandler : IRequestHandler<CreatePerson, CreatePersonResult>
    {
        private readonly StargateContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreatePersonHandler"/> class.
        /// </summary>
        /// <param name="context">The <see cref="StargateContext"/> used for data persistence.</param>
        public CreatePersonHandler(StargateContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Handles the create person request and persists the new entities.
        /// </summary>
        /// <param name="request">The <see cref="CreatePerson"/> command.</param>
        /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="CreatePersonResult"/> containing the Id of the created person.</returns>
        public async Task<CreatePersonResult> Handle(CreatePerson request, CancellationToken cancellationToken)
        {

            var newPerson = new Person()
            {
                Name = request.Name
            };

            await _context.People.AddAsync(newPerson);

            var newAstronautDetail = new AstronautDetail()
            {
                Person = newPerson,
                CurrentRank = request.Rank,
                CareerStartDate = DateTime.UtcNow
            };

            await _context.AstronautDetails.AddAsync(newAstronautDetail);

            var newDuty = new AstronautDuty()
            {
                Person = newPerson,
                DutyTitle = request.Duty,
                DutyStartDate = DateTime.UtcNow
            };

            await _context.SaveChangesAsync();

            return new CreatePersonResult()
            {
                Id = newPerson.Id
            };
          
        }
    }

    /// <summary>
    /// Result returned after processing a <see cref="CreatePerson"/> request.
    /// </summary>
    public class CreatePersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}
