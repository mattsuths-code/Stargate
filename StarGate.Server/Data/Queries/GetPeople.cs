using Dapper;
using MediatR;
using StarGate.Server.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StarGate.Server.Data.Queries
{
    /// <summary>
    /// Represents a request to retrieve all people along with their astronaut details.
    /// </summary>
    public class GetPeople : IRequest<GetPeopleResult>
    {

    }

    /// <summary>
    /// Handles <see cref="GetPeople"/> requests by querying the database for people and their current astronaut duties.
    /// </summary>
    public class GetPeopleHandler : IRequestHandler<GetPeople, GetPeopleResult>
    {
        /// <summary>
        /// The Stargate database context used to access the database connection.
        /// </summary>
        public readonly StargateContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPeopleHandler"/> class.
        /// </summary>
        /// <param name="context">The <see cref="StargateContext"/> used to access database connections.</param>
        public GetPeopleHandler(StargateContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Handles the incoming <see cref="GetPeople"/> request.
        /// Executes a SQL query that joins Person, AstronautDetail and AstronautDuty to return current duties.
        /// </summary>
        /// <param name="request">The <see cref="GetPeople"/> request (no properties required).</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A <see cref="Task{GetPeopleResult}"/> that resolves to a <see cref="GetPeopleResult"/> containing a list of <see cref="PersonAstronaut"/>.
        /// </returns>
        public async Task<GetPeopleResult> Handle(GetPeople request, CancellationToken cancellationToken)
        {
            var result = new GetPeopleResult();

            var query = $"SELECT a.Id as PersonId, a.Name, b.CurrentRank, c.DutyTitle AS CurrentDuty, b.CareerStartDate, b.CareerEndDate " +
                        "FROM [Person] a INNER JOIN [AstronautDetail] b on b.PersonId = a.Id INNER JOIN [AstronautDuty] c on a.id = c.PersonId and c.DutyEndDate is null";

            var people = await _context.Connection.QueryAsync<PersonAstronaut>(query);

            result.People = people.ToList();

            return result;
        }
    }

    /// <summary>
    /// Response model for <see cref="GetPeople"/> requests.
    /// Contains a collection of people with their astronaut details.
    /// </summary>
    public class GetPeopleResult : BaseResponse
    {
        /// <summary>
        /// Gets or sets the people returned by the query, each containing astronaut information.
        /// </summary>
        public List<PersonAstronaut> People { get; set; } = new List<PersonAstronaut> { };

    }
}
