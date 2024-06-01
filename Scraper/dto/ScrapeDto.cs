using System.Text.Json.Serialization;
using ThirdParty.Json.LitJson;

namespace Scheduler.Scraper.dto
{
    public class ScrapeStagesDto
    {
        public ScrapeStageDto[] Stages { get; set; } = Array.Empty<ScrapeStageDto>();
    }

    public class ScrapeStageDto
    {
        public string Id { get; set; } = "";
        public string Host { get; set; } = "";
        public string Name { get; set; } = "";
        public int Priority { get; set; }
        public string Color { get; set; } = "";
    }

    public class ScrapeArtistsDto
    {
        public ScrapeArtistDto[] Artists {get;set;} =  Array.Empty<ScrapeArtistDto>();
    }

    public class ScrapeArtistDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Uid { get; set; } = "";
        public ScrapePerformanceDto[] Performances { get; set; } = Array.Empty<ScrapePerformanceDto>();
    }

    public class ScrapePerformanceDto
    {
        public string Id { get; set; } = "";
        [JsonPropertyName("stage_id")]
        public string StageId { get; set; } = "";
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = ""; // 	"2024-07-19 12:00:00+02:00"
        [JsonPropertyName("end_time")]
        public string EndTime { get; set; } = "";
    }
}
