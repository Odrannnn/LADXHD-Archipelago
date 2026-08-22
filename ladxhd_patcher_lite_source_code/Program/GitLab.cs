using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LADXHD_Patcher_Lite
{
    internal class Gitlab
    {
        private static readonly HttpClient _http = new();

        private const string VersionUrl = "https://gitlab.com/api/v4/projects/bighead.0%2Fladxhd_updated/releases/permalink/latest";
        private const string ResourcesUrl = "https://gitlab.com/bighead.0/ladxhd_updated/-/raw/main/ladxhd_patcher_source_code/Resources/";

        public static async Task<string?> GetLatestTagAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, VersionUrl);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("LADXHD_Patcher_Lite", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("tag_name").GetString();
        }

        public static async Task DownloadFileAsync(string fileName, string destinationPath, IProgress<int> progress, CancellationToken cancellationToken = default)
        {
            string url = ResourcesUrl + fileName;

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("LADXHD_Patcher_Lite", "1.0"));

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long? totalBytes = response.Content.Headers.ContentLength;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream  = File.Create(destinationPath);

            var buffer = new byte[81920];
            long totalRead = 0;
            int  bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes.HasValue)
                {
                    int percent = (int)(totalRead * 100L / totalBytes.Value);
                    progress.Report(percent);
                }
            }
        }
    }
}