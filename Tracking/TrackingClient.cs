namespace Scheduler.Tracking;

public class TrackingClient
{
    private const string Time = "time";
    private const string UserId = "distinct_id";
    private const string TrackEventId = "$insert_id";
    public const string OtherUserId = "other_user_id";
    public const string EventId = "event_id";

    private readonly ILogger<TrackingClient> logger;
    private readonly HttpClient httpClient;

    public TrackingClient(ILogger<TrackingClient> logger, IHttpClientFactory httpClientFactory)
    {
        this.logger = logger;
        this.httpClient = httpClientFactory.CreateClient(nameof(TrackingClient));
    }

    public async Task Track(string name, Guid userId, Dictionary<string, object>? parameters = null)
    {
        if (parameters == null)
        {
            parameters = new Dictionary<string, object>();
        }

        var dto = new TrackingDto[] { new TrackingDto(name, SetDefaultEventProperties(userId, parameters)) };

        var result = await httpClient.PostAsJsonAsync("import?strict=1", dto);
        if (!result.IsSuccessStatusCode)
        {
            var error = await result.Content.ReadAsStringAsync();
            logger.LogCritical(error);
        }
    }

    public Dictionary<string, object> SetDefaultEventProperties(Guid userId, Dictionary<string, object> parameters)
    {
        parameters[Time] = Math.Floor((DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds);
        parameters[UserId] = userId.ToString();
        parameters[TrackEventId] = Guid.NewGuid().ToString();

        return parameters;
    }
}