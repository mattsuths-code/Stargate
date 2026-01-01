using MediatR;
using Microsoft.AspNetCore.Mvc;
using StarGate.Server.Business.Queries;
using StarGate.Server.Infrastructure.Logging.Interface;
using StargateAPI.Business.Commands;
using System.Net;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AstronautDutyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IStarGateLogger _logger;
        public AstronautDutyController(IMediator mediator, IStarGateLogger logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetAstronautDutiesByName(string name)
        {
            try
            {
                var result = await _mediator.Send(new GetAstronautDutiesByName()
                {
                    Name = name
                });

                return this.GetResponse(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });
            }            
        }

        [HttpPost("")]
        public async Task<IActionResult> CreateAstronautDuty([FromBody] CreateAstronautDuty request)
        {
            try
            {
                var result = await _mediator.Send(request);

                return this.GetResponse(result);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                return this.GetResponse(new BaseResponse()
                {
                    Message = ex.Message,
                    Success = false,
                    ResponseCode = (int)HttpStatusCode.InternalServerError
                });

            }
        }
    }
}