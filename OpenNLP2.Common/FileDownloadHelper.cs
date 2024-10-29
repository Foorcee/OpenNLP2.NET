namespace OpenNLP2.Build;

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public static class FileDownloadHelper
{
    private static readonly HttpClient httpClient = new ();
    
    /// <summary>
    /// Downloads a file from a specified URL and saves it to a designated location.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="destinationPath">The path where the downloaded file should be saved.</param>
    /// <returns>A task that represents the completion of the download operation.</returns>
    public static async Task DownloadFileAsync(string url, string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("The URL cannot be empty.", nameof(url));

        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("The destination path cannot be empty.", nameof(destinationPath));

#if DEBUG
        if (File.Exists(destinationPath))
        {
            Console.WriteLine("File already exists at the destination path. Skipping download.");
            return;
        }
#endif

        try
        {
            Console.WriteLine($"Starting download from URL: {url}");
        
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
        
            Console.WriteLine("Connected to server, starting file download...");

            await using (var stream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fileStream);
            }

            Console.WriteLine($"File downloaded successfully and saved to: {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading file: {ex.Message}");
            throw;
        }
    }
}