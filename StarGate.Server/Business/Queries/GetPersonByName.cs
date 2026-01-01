using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StarGate.Server.Data;
using StarGate.Server.Business.Dtos;
using StarGate.Server.Controllers;

namespace StarGate.Server.Business.Queries
{
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class GetPersonByNameHandler : IRequestHandler<GetPersonByName, GetPersonByNameResult>
    {
        private readonly StargateContext _context;
        public GetPersonByNameHandler(StargateContext context)
        {
            _context = context;
        }

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

    public class GetPersonByNameResult : BaseResponse
    {
        public PersonAstronaut? Person { get; set; }
    }
}
