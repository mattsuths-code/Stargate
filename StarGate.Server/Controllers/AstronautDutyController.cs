using MediatR;
using Microsoft.AspNetCore.Mvc;
using StarGate.Server.Business.Queries;
using StarGate.Server.Infrastructure.Logging.Interface;
using StargateAPI.Business.Commands;
using System.Net;

namespace StargateAPI.Controllers
{
    /// <summary>
    /// API controller exposing endpoints to manage astronaut duties.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class AstronautDutyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IStarGateLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AstronautDutyController"/> class.
        /// </summary>
        /// <param name="mediator">The <see cref="IMediator"/> used to dispatch application requests.</param>
        /// <param name="logger">The <see cref="IStarGateLogger"/> used for logging.</param>
        public AstronautDutyController(IMediator mediator, IStarGateLogger logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Gets astronaut duties for the specified person by name.
        /// </summary>
        /// <param name="name">The name of the person whose duties should be returned.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> wrapping a <see cref="BaseResponse"/> that contains
        /// the <see cref="GetAstronautDutiesByName"/> result on success, or an error response on failure.
        /// </returns>
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

        /// <summary>
        /// Creates a new astronaut duty for a person.
        /// </summary>
        /// <param name="request">The <see cref="CreateAstronautDuty"/> command containing duty data.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> wrapping a <see cref="BaseResponse"/> that contains
        /// the <see cref="CreateAstronautDuty"/> result on success, or an error response on failure.
        /// </returns>
        [HttpPost("")]
        public async Task<IActionResult> CreateAstronautDuty([FromBody] CreateAstronautDuty request)
        {
            try
            {
                var result = await _mediator.Send(request);

                _logger.LogInformation("Created astronaut duty with id of " + result.Id);
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