using System.Net.Http.Headers;
using System.Text;

namespace Scheduler.Tracking;

public static class TrackingSetup
{
    public static void SetupTracking(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient(nameof(TrackingClient), (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>()
                .GetSection("MixPanel")
                .Get<TrackingSettings>();
            if (config == null)
            {
                throw new Exception("Can't load tracking config");
            }
            client.BaseAddress = new Uri(config.Url);
            var authValue = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"{config.Secret}:"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        });
        serviceCollection.AddSingleton<TrackingClient>();
    }
}