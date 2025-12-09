namespace ZUMI_Converter
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using System.Threading.Channels;
    using System.Diagnostics;

    // Das Job-Objekt
    public record ConversionJob(Guid MediaId, string InputPath, string OutputPath);

    public class Program
    {
        private const string BaseUploadPath = "/app/wwwroot/uploads";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Services.AddHttpClient();

            // 1. Die Warteschlange registrieren (Unbegrenzte Kapazität)
            builder.Services.AddSingleton(Channel.CreateUnbounded<ConversionJob>());

            // 2. Den Hintergrund-Arbeiter registrieren (Der die Queue abarbeitet)
            builder.Services.AddHostedService<VideoProcessorWorker>();

            var app = builder.Build();

            app.MapGet("/", () => "ZUMI Video Converter (Queue Active) 🚀");

            // Der Endpoint nimmt den Job nur an und wirft ihn in die Queue
            app.MapPost("/convert", async ([FromBody] ConversionJob job, Channel<ConversionJob> channel) =>
            {
                Console.WriteLine($"[Queue] Job eingereiht: {job.MediaId}");
                
                // Job in die Warteschlange schreiben
                await channel.Writer.WriteAsync(job);

                return Results.Accepted(); // "Habs notiert, kümmere mich später drum"
            });

            app.Run();
        }
    }

    // --- Der Arbeiter, der im Hintergrund läuft ---
    public class VideoProcessorWorker : BackgroundService
    {
        private readonly Channel<ConversionJob> _channel;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string BaseUploadPath = "/app/wwwroot/uploads";

        public VideoProcessorWorker(Channel<ConversionJob> channel, IHttpClientFactory httpClientFactory)
        {
            _channel = channel;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("[Worker] Warte auf Jobs...");

            // Hier lesen wir Jobs einzeln aus der Queue
            await foreach (var job in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    // WICHTIG: Hier wird gewartet, bis EIN Video fertig ist, bevor das nächste drankommt!
                    await ProcessVideoAsync(job);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Critical Error] Job abgestürzt: {ex.Message}");
                }
            }
        }

        private async Task ProcessVideoAsync(ConversionJob job)
        {
            var client = _httpClientFactory.CreateClient();
            var inputFile = Path.Combine(BaseUploadPath, job.InputPath);
            var outputFile = Path.Combine(BaseUploadPath, job.OutputPath);

            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"[Error] Datei weg: {inputFile}");
                return;
            }

            // Status: Processing (2)
            await SendCallbackAsync(client, job.MediaId, 2);

            try
            {
                // Entscheidung: Audio oder Video?
                var isAudioTarget = Path.GetExtension(outputFile).ToLower() == ".mp3";
                string arguments;

                if (isAudioTarget)
                {
                    // Audio Settings
                    arguments = $"-i \"{inputFile}\" -vn -c:a libmp3lame -q:a 2 -y \"{outputFile}\"";
                    Console.WriteLine($"[FFmpeg] Starte Audio-Konvertierung: {job.MediaId}");
                }
                else
                {
                    // Video Settings (AV1)
                    // -cpu-used 4 ist guter Mix aus Speed/Größe
                    arguments = $"-i \"{inputFile}\" -c:v libaom-av1 -crf 30 -cpu-used 6 -c:a aac -y \"{outputFile}\"";
                    Console.WriteLine($"[FFmpeg] Starte Video-Konvertierung: {job.MediaId}");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // FFmpeg schreibt Status in Error
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                
                // Wir warten hier, bis FFmpeg fertig ist. 
                // Da wir in einer Queue sind, blockiert das NICHT die API, sondern nur den nächsten Job.
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"[Success] Fertig: {job.MediaId}");
                    await SendCallbackAsync(client, job.MediaId, 3); // Completed
                    try { File.Delete(inputFile); } catch { }
                }
                else
                {
                    Console.WriteLine($"[Fail] FFmpeg Error Code: {process.ExitCode}");
                    await SendCallbackAsync(client, job.MediaId, 4); // Failed
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Exception] {ex.Message}");
                await SendCallbackAsync(client, job.MediaId, 4);
            }
        }

        private async Task SendCallbackAsync(HttpClient client, Guid mediaId, int status)
        {
            try
            {
                // Service Name "app" nutzen
                await client.PostAsJsonAsync($"http://app:8000/api/v1/internal/callback/{mediaId}", new { Status = status });
            }
            catch
            {
                Console.WriteLine("[Callback Error] API nicht erreichbar.");
            }
        }
    }
}