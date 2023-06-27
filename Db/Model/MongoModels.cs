using MongoDB.Bson.Serialization.Attributes;

namespace Scheduler.Db.Models;

//Schedule
public class ScheduleEntity : IEntity<string>
{
    public ScheduleEntity(string id, WeekEntity[] weeks)
    {
        Id = id;
        Weeks = weeks;
    }

    public string Id { get; set; }
    public WeekEntity[] Weeks { get; set; }
}

public record WeekEntity(string Name, DayEntity[] Days);
public record DayEntity(DateOnly Date, string Name, LocationEntity[] Locations);
public record LocationEntity(string Name, EventEntity[] Events);

public class EventEntity
{
    public EventEntity(Guid id, DateTime start, DateTime end, string artist)
    {
        Id = id;
        Start = start;
        End = end;
        Artist = artist;
    }

    public Guid Id { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Artist { get; set; }
}


//Users
public class UserEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
    public Guid PublicId { get; set; }

    public string Name { get; set; } = "";
    public List<Guid> Attending { get; set; } = null!;
    public List<Guid> Following { get; set; } = null!;
    [BsonIgnore]
    public bool SavedUser = true;
}
