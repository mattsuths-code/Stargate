using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarGate.Server.Data;
using StarGate.Server.Business.Dtos;
using StarGate.Server.Controllers;

namespace StarGate.Server.Business.Queries
{
    /// <summary>
    /// Request to retrieve a person and their current astronaut information by name.
    /// </summary>
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        /// <summary>
        /// The full name of the person to retrieve.
        /// </summary>
        public required string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Handler that executes the <see cref="GetPersonByName"/> request using a SQL query via Dapper.
    /// </summary>
    public class GetPersonByNameHandler : IRequestHandler<GetPersonByName, GetPersonByNameResult>
    {
        private readonly StargateContext _context;  

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPersonByNameHandler"/> class.
        /// </summary>
        /// <param name="context">The <see cref="StargateContext"/> used to access the database connection.</param>
        public GetPersonByNameHandler(StargateContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Handles the request to fetch a person and their current astronaut detail and duty.
        /// </summary>
        /// <param name="request">The <see cref="GetPersonByName"/> request containing the person's name.</param>
        /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A <see cref="GetPersonByNameResult"/> containing the matched <see cref="PersonAstronaut"/>, or null if not found.
        /// </returns>
        public async Task<GetPersonByNameResult> Handle(GetPersonByName request, CancellationToken cancellationToken)
        {
            var result = new GetPersonByNameResult();

            var sql = @"SELECT
                            a.Id AS PersonId,
                            a.Name AS Name,
                            d.CurrentRank AS CurrentRank,
                            ad.DutyTitle AS CurrentDuty,
                            d.CareerStartDate AS CareerStartDate,
                            d.CareerEndDate AS CareerEndDate
                        FROM [Person] a
                        INNER JOIN [AstronautDetail] d ON d.PersonId = a.Id
                        INNER JOIN [AstronautDuty] ad ON ad.PersonId = a.Id and ad.DutyEndDate IS NULL
                        WHERE a.Name = @Name";

            var person = await _context.Connection.QueryFirstOrDefaultAsync<PersonAstronaut>(sql, new { Name = request.Name });

            result.Person = person;

            return result;
        }
    }

    /// <summary>
    /// Result returned by <see cref="GetPersonByNameHandler"/> containing the person and their astronaut data.
    /// </summary>
    public class GetPersonByNameResult : BaseResponse
    {
        /// <summary>
        /// The person and associated astronaut data, or null when no match is found.
        /// </summary>
        public PersonAstronaut? Person { get; set; }
    }
}
