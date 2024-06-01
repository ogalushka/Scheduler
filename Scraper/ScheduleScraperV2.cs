using Scheduler.Db.Models;
using Scheduler.Scraper.dto;
using Scheduler.Scraper.settings;
using Scheduler.Service;

namespace Scheduler.Scraper
{
    public class ScheduleScraperV2 : IHostedService
    {
        private readonly ScheduleUpdaterService scheduleUpdaterService;
        private readonly ILogger logger;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ScraperSettings scraperSettings;

        private System.Threading.Timer? timer;

        public ScheduleScraperV2(
            IConfiguration config,
            ScheduleUpdaterService scheduleUpdaterService,
            ILogger<ScheduleScraper> logger,
            IHttpClientFactory httpClientFactory)
        {
            scraperSettings = config.GetSection(nameof(ScraperSettings)).Get<ScraperSettings>()!;
            this.scheduleUpdaterService = scheduleUpdaterService;
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (scraperSettings == null)
            {
                logger.LogError("No ScrapperSettings in config, scrapper is disabled");
                return Task.CompletedTask;
            }

            var interval = (int)TimeSpan.FromMinutes(scraperSettings.DelayMinutes).TotalMilliseconds;
            timer = new System.Threading.Timer(DoScrape, null, interval, interval);
            DoScrape(null);

            return Task.CompletedTask;
        }

        private async void DoScrape(object? state)
        {
            var httpClient = httpClientFactory.CreateClient();
            var stages = await httpClient.GetFromJsonAsync<ScrapeStagesDto>(scraperSettings.StagesApi);
            var artists = await httpClient.GetFromJsonAsync<ScrapeArtistsDto>(scraperSettings.ArtistsApi);

            if (stages == null || stages.Stages.Length == 0 || artists == null || artists.Artists.Length == 0)
            {
                Console.WriteLine("Error failed to load stages or artists");
                return;
            }

            var events = new List<EventEntity>();

            var stageMap = new Dictionary<string, ScrapeStageDto>();
            foreach (var stage in stages.Stages)
            {
                stageMap.Add(stage.Id, stage);
            }

            foreach (var artist in artists.Artists)
            {
                foreach (var e in artist.Performances)
                {
                    var startTime = DateTime.Parse(e.StartTime).ToUniversalTime();
                    var endTime = DateTime.Parse(e.EndTime).ToUniversalTime();
                    events.Add(new EventEntity(
                        Guid.Empty,
                        startTime,
                        endTime,
                        artist.Name,
                        "",
                        "",
                        DateOnly.FromDateTime(startTime),
                        stageMap[e.StageId].Name
                        ));
                }
            }

            await scheduleUpdaterService.UpdateSchedule(new ScheduleEntity("Tomorrow", events.ToArray(), DateTime.Now));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.DisposeAsync();
            return Task.CompletedTask;
        }
    }
}
