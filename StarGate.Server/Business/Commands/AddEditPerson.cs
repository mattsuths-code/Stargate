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
    public class AddEditPerson : IRequest<AddEditPersonResult>
    {
        public string Name { get; set; } = string.Empty;

        public string Rank { get; set; } = string.Empty;

        public string Duty { get; set; } = null;
    }

    /// <summary>
    /// Pre-processor that validates a <see cref="AddEditPerson"/> request before the handler runs.
    /// </summary>
    public class CreatePersonPreProcessor : IRequestPreProcessor<AddEditPerson>
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
        /// Ensures a valid request to create a person.
        /// Throws <see cref="BadHttpRequestException"/> when validation fails.
        /// </summary>
        /// <param name="request">The incoming <see cref="AddEditPerson"/> request.</param>
        /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
        /// <returns>A completed task when validation succeeds.</returns>
        /// <exception cref="BadHttpRequestException">Thrown when a person with the same name already exists.</exception>
        public Task Process(AddEditPerson request, CancellationToken cancellationToken)
        {
            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);

            if (person is null && string.IsNullOrEmpty(request.Duty))
            {
                //They are trying to create a person, but duties must be present for every person
                throw new BadHttpRequestException("Person must have a rank");
            }
            if (person is null && string.IsNullOrEmpty(request.Rank))
            {
                //They are trying to create a person, but duties must be present for every person
                throw new BadHttpRequestException("Person must have a rank");
            }
            if (person is not null && !string.IsNullOrEmpty(request.Duty))
            {
                //They are trying to create a person, but duties must be present for every person
                throw new BadHttpRequestException("Cannot update duty here, must use the duty API");
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handles <see cref="AddEditPerson"/> requests by creating the Person, associated AstronautDetail, and AstronautDuty.
    /// </summary>
    public class CreatePersonHandler : IRequestHandler<AddEditPerson, AddEditPersonResult>
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
        /// <param name="request">The <see cref="AddEditPerson"/> command.</param>
        /// <param name="cancellationToken">Token to observe while waiting for the task to complete.</param>
        /// <returns>A <see cref="AddEditPersonResult"/> containing the Id of the created person.</returns>
        public async Task<AddEditPersonResult> Handle(AddEditPerson request, CancellationToken cancellationToken)
        {
            var returnId = 0;

            var person = _context.People.AsNoTracking().FirstOrDefault(z => z.Name == request.Name);


            if (person is null)
            {
                //create the person, their details and duties, they don't exist
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

                await _context.AstronautDuties.AddAsync(newDuty);

                await _context.SaveChangesAsync(cancellationToken);

                returnId = newPerson.Id;

            }
            else
            {
                //the person exits, so lets update their rank
                var personDetail = _context.AstronautDetails.AsNoTracking().FirstOrDefault(z => z.PersonId == person.Id);

                if (personDetail is null)
                {
                    personDetail = new AstronautDetail()
                    {
                        PersonId = person.Id,
                        CurrentRank = request.Rank,
                        CareerStartDate = DateTime.UtcNow
                    };
                }
                else
                {
                    personDetail.CurrentRank = request.Rank;
                }
                    
                _context.AstronautDetails.Update(personDetail);

                returnId = person.Id;

            }

            return new AddEditPersonResult()
            {
                Id = returnId
            };
        }
    }

    /// <summary>
    /// Result returned after processing a <see cref="AddEditPerson"/> request.
    /// </summary>
    public class AddEditPersonResult : BaseResponse
    {
        public int Id { get; set; }
    }
}
