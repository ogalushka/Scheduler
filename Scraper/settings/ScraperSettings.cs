namespace Scheduler.Scraper.settings
{
    public class ScraperSettings
    {
        public string ScheduleUrl { get; set; } = "";
        public string StagesApi { get; set; } = "";
        public string ArtistsApi { get; set; } = "";
        public int DelayMinutes { get; set; }
    }
}
