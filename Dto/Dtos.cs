namespace Scheduler.Dtos;

public class Event
{
    public Guid Id { get; set; }

    public DateTime TimeStart { get; set; }
    public DateTime TimeEnd { get; set; }
    public string Artist { get; set; } = "";
    public bool Attending { get; set; }
    public Atendee[] Atendees { get; set; } = Array.Empty<Atendee>();
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
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string WeekName { get; set; } = "";
    public int WeekNumber { get; set; }
    public Day[] Days { get; set; } =  Array.Empty<Day>();
}

public class Atendee
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}
