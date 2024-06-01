using MongoDB.Bson.Serialization.Attributes;

namespace Scheduler.Db.Models;

public class ScheduleHistory : IEntity<string>
{
    public ScheduleHistory(string id, List<ScheduleEntity> schedules)
    {
        Id = id;
        Schedules = schedules;
    }

    public string Id { get; set; }
    public List<ScheduleEntity> Schedules { get; set; }
}

public class ScheduleEntity : IEntity<string>
{
    public ScheduleEntity(string id, EventEntity[] events, DateTime updateTime)
    {
        Id = id;
        Events = events;
        UpdateTime = updateTime;
    }

    public string Id { get; set; }
    public EventEntity[] Events { get; set; }
    public DateTime UpdateTime { get; set; }
}

public class EventEntity
{
    public EventEntity(
        Guid id,
        DateTime start,
        DateTime end,
        string artist,
        string weekend,
        string day,
        DateOnly date,
        string location
        )
    {
        Id = id;
        Start = start;
        End = end;
        Artist = artist;

        Weekend = weekend;
        Day = day;
        Date = date;
        Location = location;
    }

    public Guid Id { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Artist { get; set; }

    public string Weekend { get; set; }
    public string Location { get; set; }
    public string Day { get; set; }
    public DateOnly Date { get; set; }
}

//Users
public class UserEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
    public Guid PublicId { get; set; }
    public string? GoogleId { get; set; }
    public string? GoogleMail { get; set; }

    public string Name { get; set; } = "";
    public List<Guid> Attending { get; set; } = null!;
    public List<Guid> Following { get; set; } = null!;
    [BsonIgnore]
    public bool SavedUser = true;
}
