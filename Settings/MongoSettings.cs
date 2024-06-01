namespace Scheduler.Settings;

public class MongoSettings
{
    public string Connection { get; set; } = "";
    public string Database { get; set; } = "";
    public string ScheduleCollection { get; set; } = "";
    public string UserCollection { get; set; } = "";
    public string HistoryCollection { get; set; } = "";
}