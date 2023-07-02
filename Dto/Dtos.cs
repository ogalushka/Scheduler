namespace Scheduler.Dtos;

public class Event
{
    public Guid Id { get; set; }

    public DateTime TimeStart { get; set; }
    public DateTime TimeEnd { get; set; }
    public string Artist { get; set; } = "";
    public bool Attending { get; set; }
    public Attendee[] Attendees { get; set; } = Array.Empty<Attendee>();
}

public class Day
{
    public DateOnly Date { get; set; }
    public string WeekDay { get; set; } = "";

    public Location[] Stages { get; set; } = Array.Empty<Location>();
}

public class Location
{
    public string Stage { get; set; } = "";
    public Event[] Artists { get; set; } = Array.Empty<Event>();
}

public class Week
{
    public string WeekName { get; set; } = "";
    public int WeekNumber { get; set; }
    public Day[] Days { get; set; } =  Array.Empty<Day>();
}

public class Attendee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<Guid> Following { get; set; } = new List<Guid>();
}

public class ScheduleDto
{
    public Attendee? Me { get; set; } = null;
    public Attendee? Owner { get; set; } = null;
    public Week[] Schedule { get; set; } = Array.Empty<Week>();
}
