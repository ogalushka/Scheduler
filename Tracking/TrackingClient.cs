namespace Scheduler.Tracking;

public class TrackingClient
{
    public const string Time = "time";
    public const string UserId = "distinct_id";
    public const string EventId = "$insert_id";

    private readonly ILogger<TrackingClient> logger;
    private readonly HttpClient httpClient;

    public TrackingClient(ILogger<TrackingClient> logger, IHttpClientFactory httpClientFactory)
    {
        this.logger = logger;
        this.httpClient = httpClientFactory.CreateClient(nameof(TrackingClient));
    }

    public async Task Test(Guid userId)
    {
        var dto = new TrackingDto[] { new TrackingDto("Test", GetEventProperties(userId)) };

        var result = await httpClient.PostAsJsonAsync("import?strict=1", dto);
        if (!result.IsSuccessStatusCode)
        {
            var error = await result.Content.ReadAsStringAsync();
            logger.LogCritical(error);
        }
    }

    public Dictionary<string, object> GetEventProperties(Guid userId)
    {
        var result = new Dictionary<string, object>();
        result[Time] = Math.Floor((DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds);
        result[UserId] = userId.ToString();
        result[EventId] = Guid.NewGuid().ToString();

        return result;
    }
}