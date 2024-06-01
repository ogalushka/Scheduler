using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Scheduler.Db.Models;
using Scheduler.Scraper.settings;
using Scheduler.Service;
using System;
using System.Globalization;
using System.Timers;

namespace Scheduler.Scraper
{
    public class ScheduleScraper : IHostedService
    {
        private readonly ScheduleUpdaterService scheduleUpdaterService;
        private readonly ILogger logger;
        private readonly ScraperSettings? scraperSettings;

        private System.Threading.Timer? timer;

        public ScheduleScraper(
            IConfiguration config,
            ScheduleUpdaterService scheduleUpdaterService,
            ILogger<ScheduleScraper> logger)
        {
            scraperSettings = config.GetSection(nameof(ScraperSettings))?.Get<ScraperSettings>();
            this.scheduleUpdaterService = scheduleUpdaterService;
            this.logger = logger;
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

        private async void DoScrape(object? state) {
            try
            {
                var results = new List<EventEntity>();
                if (scraperSettings == null)
                {
                    return;
                }

                var webpage = new HtmlWeb();
                var page = await webpage.LoadFromWebAsync(scraperSettings?.ScheduleUrl);
                var weekends = ParseWeekends(page);
                var days = page.DocumentNode.SelectNodes(".//div[@class='eventday']");
                foreach (var day in days)
                {
                    ParseDay(day, weekends, results);
                }

                await scheduleUpdaterService.UpdateSchedule(new ScheduleEntity("Tomorrow", results.ToArray(), DateTime.Now));
            }
            catch (Exception e)
            {
                logger.LogError(e, "");
            }
        }

        private List<Weekend> ParseWeekends(HtmlDocument page)
        {
            var weekends = new List<Weekend>();


            var weeksData = page.DocumentNode.SelectSingleNode(".//div[@class='eventdays-wrapper']");
            foreach (var week in weeksData.SelectNodes("div"))
            {
                var weekEnd = new Weekend();
                var weekTitle = week.SelectSingleNode("span[@class='weekend-title']").InnerHtml;
                weekEnd.name = weekTitle;

                var days = week.SelectNodes("div");
                foreach (var day in days) {
                    var dayDate = DateOnly.ParseExact(day.Attributes["data-date"].Value, "yyyy/mm/dd");
                    weekEnd.dates.Add(dayDate);
                }

                weekends.Add(weekEnd);
            }

            return weekends;
        }

        private void ParseDay(HtmlNode day, List<Weekend> weekends, List<EventEntity> results)
        {
            var dayDateString = day.Attributes["data-eventday"].Value;

            var date = DateOnly.ParseExact(dayDateString, "dddd d MMMM yyyy", CultureInfo.InvariantCulture);

            var weekendName = "";
            foreach (var weekend in weekends)
            {
                if (weekend.dates.IndexOf(date) != -1)
                {
                    weekendName = weekend.name;
                    break;
                }
            }

            var stages = day.SelectNodes(".//div[@class='stage']");
            foreach (var stage in stages)
            {
                ParseStage(stage, weekendName, date, results);
            }

        }

        private void ParseStage(HtmlNode stage, string weekendName, DateOnly date, List<EventEntity> results)
        {
            var stageName = stage.SelectSingleNode(".//h4").InnerText;

            var events = stage.SelectNodes(".//a").Select(n => n.InnerText.Trim());

            foreach (var e in events)
            {
                var eventEntity = new EventEntity(
                    Guid.Empty,
                    date.ToDateTime(new TimeOnly(0), DateTimeKind.Utc),
                    date.ToDateTime(new TimeOnly(0), DateTimeKind.Utc),
                    e,
                    weekendName,
                    date.DayOfWeek.ToString(),
                    date,
                    stageName);
                results.Add(eventEntity);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.DisposeAsync();
            return Task.CompletedTask;
        }
    }

    public class Weekend
    {
        public string name = "";
        public List<DateOnly> dates = new List<DateOnly>();
    }
}
