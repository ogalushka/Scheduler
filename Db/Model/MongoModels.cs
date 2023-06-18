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
public record EventEntity(Guid Id, DateTime Start, DateTime End, string Artist);



//Users
public class UserEntity : IEntity<Guid>
{
    public Guid Id { get; set; }
    public Guid PublicId { get; set; }

    public List<Guid> Attending = null!;
    public List<FollowingEntity> Following = null!;
}

public class FollowingEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}
