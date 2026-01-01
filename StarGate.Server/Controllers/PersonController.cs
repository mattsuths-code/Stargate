using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StarGate.Server.Business.Queries;
using System.Net;
using StarGate.Server.Infrastructure.Logging.Interface;

namespace StargateAPI.Controllers
{
    /// <summary>
    /// API controller that exposes endpoints for managing Person resources and related astronaut operations.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IStarGateLogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonController"/> class.
        /// </summary>
        /// <param name="mediator">The <see cref="IMediator"/> used to dispatch requests.</param>
        /// <param name="logger">The application logger implementing <see cref="IStarGateLogger"/>.</param>
        public PersonController(IMediator mediator, IStarGateLogger logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Returns a list of people with their current astronaut details.
        /// </summary>
        /// <returns>
        /// An <see cref="IActionResult"/> that contains a <see cref="BaseResponse"/> wrapping a <see cref="GetPeopleResult"/>.
        /// On failure returns a <see cref="BaseResponse"/> with an error message and HTTP 500 status code.
        /// </returns>
        [HttpGet("")]
        public async Task<IActionResult> GetPeople()
        {
            try
            {
                var result = await _mediator.Send(new GetPeople()
                {

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
        /// Retrieves a single person by name and their astronaut details.
        /// </summary>
        /// <param name="name">The name of the person to retrieve.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> that contains a <see cref="BaseResponse"/> wrapping a <see cref="GetPersonByNameResult"/>.
        /// On failure returns a <see cref="BaseResponse"/> with an error message and HTTP 500 status code.
        /// </returns>
        [HttpGet("{name}")]
        public async Task<IActionResult> GetPersonByName(string name)
        {
            try
            {
                var result = await _mediator.Send(new GetPersonByName()
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
        /// Creates a new person along with initial astronaut detail and duty.
        /// </summary>
        /// <param name="person">The <see cref="CreatePerson"/> command containing the new person's data.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> that contains a <see cref="BaseResponse"/> wrapping a <see cref="CreatePersonResult"/>.
        /// On failure returns a <see cref="BaseResponse"/> with an error message and HTTP 500 status code.
        /// </returns>
        [HttpPost("")]
        public async Task<IActionResult> CreatePerson([FromBody] CreatePerson person)
        {
            Console.WriteLine("I'm here!!!!");
            try
            {
                var result = await _mediator.Send(person);

                _logger.LogInformation("Created astronaut with id of " + result.Id);
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