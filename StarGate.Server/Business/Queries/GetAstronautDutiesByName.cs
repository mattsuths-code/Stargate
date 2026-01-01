using Dapper;
using MediatR;
using StarGate.Server.Data;
using StarGate.Server.Business.Dtos;
using StarGate.Server.Controllers;

namespace StarGate.Server.Business.Queries
{
    /// <summary>
    /// Request to retrieve a person's astronaut duties and related detail information by person name.
    /// </summary>
    public class GetAstronautDutiesByName : IRequest<GetAstronautDutiesByNameResult>
    {
        /// <summary>
        /// The full name of the person whose duties should be returned.
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler for <see cref="GetAstronautDutiesByName"/> that queries the database using Dapper.
    /// </summary>
    public class GetAstronautDutiesByNameHandler : IRequestHandler<GetAstronautDutiesByName, GetAstronautDutiesByNameResult>
    {
        private readonly StargateContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAstronautDutiesByNameHandler"/> class.
        /// </summary>
        /// <param name="context">The <see cref="StargateContext"/> used to obtain a database connection.</param>
        public GetAstronautDutiesByNameHandler(StargateContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Handles the request to fetch a person's astronaut detail and their list of duties ordered by start date (descending).
        /// </summary>
        /// <param name="request">The <see cref="GetAstronautDutiesByName"/> request containing the person's name.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A <see cref="GetAstronautDutiesByNameResult"/> containing the person's detail and a list of <see cref="AstronautDuty"/> entries.
        /// </returns>
        public async Task<GetAstronautDutiesByNameResult> Handle(GetAstronautDutiesByName request, CancellationToken cancellationToken)
        {

            var result = new GetAstronautDutiesByNameResult();

            var query = $"SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE a.Name = @Name";

            var person = await _context.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(query, new { Name = request.Name });

            result.Person = person;

            query = $"SELECT * FROM [AstronautDuty] WHERE PersonId = @PersonId Order By DutyStartDate Desc";

            var duties = await _context.Connection.QueryAsync<AstronautDuty>(query, new {PersonId = person.PersonId});

            result.AstronautDuties = duties.ToList();

            return result;

        }
    }

    /// <summary>
    /// Result model returned by <see cref="GetAstronautDutiesByNameHandler"/>.
    /// Contains the person's astronaut detail and their duties.
    /// </summary>
    public class GetAstronautDutiesByNameResult : BaseResponse
    {
        /// <summary>
        /// The person information and related astronaut detail.
        /// </summary>
        public PersonAstronaut Person { get; set; }

        /// <summary>
        /// The list of duties associated with the person, ordered by DutyStartDate descending.
        /// </summary>
        public List<AstronautDuty> AstronautDuties { get; set; } = new List<AstronautDuty>();
    }
}
