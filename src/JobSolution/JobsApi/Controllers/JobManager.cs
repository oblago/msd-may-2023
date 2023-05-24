using DotNetCore.CAP;
using Marten;
using SlugGenerators;
using DomainEvents = MessageContracts.JobsApi;

namespace JobsApi.Controllers;

public class JobManager
{
    private readonly SlugGenerator _slugGenerator;
    private readonly IDocumentStore _documentStore;
    private readonly ICapPublisher _publisher;

    public JobManager(SlugGenerator slugGenerator, IDocumentStore documentStore, ICapPublisher publisher)
    {
        _slugGenerator = slugGenerator;
        _documentStore = documentStore;
        _publisher = publisher;
    }

    public async Task<JobItemModel> CreateJobAsync(JobCreateItem request)
    {
        // JobCreateItem -> JobEntity (save it the database) -> JobItemModel
        // Map from the JobCreateItem to something we want to store in the database.
        var jobToSave = new JobEntity
        {
            Title = request.Title,
            Slug = await _slugGenerator.GenerateSlugForAsync(request.Title),
            Description = request.Description,
        };


        // We'll talk about lightweight sessions later, but super fact..
        using var session = _documentStore.LightweightSession();

        session.Insert(jobToSave);

        await session.SaveChangesAsync(); // This will save it in the database

        // Map it to the JobItemModel that we return to the client.
        var response = new JobItemModel
        {
            Id = jobToSave.Slug,
            Title = jobToSave.Title,
            Description = jobToSave.Description,
        };
        // Write that event out  *somewhere*.
        var domainEvent = new DomainEvents.JobCreated
        {
            Id = response.Id,
            Title = response.Title,
            Description = response.Description,
        };
        await _publisher.PublishAsync(DomainEvents.JobCreated.MessageId, domainEvent);


        return response;

    }

    public async Task<CollectionResponse<JobItemModel>> GetAllCurrentJobsAsync()
    {
        // Here we will have to map from JobEntity -> JobItemModel
        using var session = _documentStore.LightweightSession();
        var jobs = await session.Query<JobEntity>()
            .Where(j => j.IsRetired == false)
            .Select(job => new JobItemModel
            {
                Title = job.Title,
                Id = job.Slug,
                Description = job.Description,
            }).ToListAsync();

        return new CollectionResponse<JobItemModel> { Data = jobs.ToList() };
    }

    public async Task<JobItemModel?> GetJobBySlugAsync(string slug)
    {
        using var session = _documentStore.LightweightSession();
        var job = await session.Query<JobEntity>()
            .Where(j => j.IsRetired == false && j.Slug == slug)
            .Select(job => new JobItemModel
            {
                Title = job.Title,
                Id = job.Slug,
                Description = job.Description,
            }).SingleOrDefaultAsync();
        return job;
    }

    public async Task<bool> CheckForJobAsync(string slug)
    {
        using var session = _documentStore.LightweightSession();
        return await session.Query<JobEntity>()
        .Where(j => j.IsRetired == false && j.Slug == slug)
        .AnyAsync();
    }
}