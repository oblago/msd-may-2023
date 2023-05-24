using DotNetCore.CAP;
using JobListingsApi.Models;
using Marten;
using Microsoft.AspNetCore.Mvc;
using DomainEvents = MessageContracts.JobsApi;
namespace JobListingsApi.Controllers;

public class MessageSubscriberController : ControllerBase
{
    private readonly ILogger<MessageSubscriberController> _logger;
    private readonly IDocumentSession _session;


    public MessageSubscriberController(ILogger<MessageSubscriberController> logger, IDocumentSession session)
    {
        _logger = logger;
        _session = session;
    }

    [HttpPost("cap-stuff")]
    [CapSubscribe("JobsApi.JobCreated")]
    public async Task<ActionResult> GetNewJob([FromBody] DomainEvents.JobCreated request)
    {
        _logger.LogInformation($"Got a job created request {request.Id}, {request.Title}");
        // if(request.Description.ToLower().Contains("dancer"))
        // {
        //     throw new ArgumentOutOfRangeException();
        // }
        // TODO: - save this to the database so we can check when someone creates a job opening.

        var job = new JobModel
        {
            Id = request.Id,
            Title = request.Title
        };
        _session.Store(job);
        await _session.SaveChangesAsync();
        //       - talk about mutable data from the event stream
        //          our example will be retiring a job. but there is more.
        return Ok();
    }
}
