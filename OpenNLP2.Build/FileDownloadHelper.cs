namespace OpenNLP2.Build;

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public static class FileDownloadHelper
{
    private static readonly HttpClient httpClient = new HttpClient();

    /// <summary>
    /// Lädt eine Datei von einer URL herunter und speichert sie an einem angegebenen Speicherort.
    /// </summary>
    /// <param name="url">Die URL der Datei, die heruntergeladen werden soll.</param>
    /// <param name="destinationPath">Der Pfad, an dem die heruntergeladene Datei gespeichert werden soll.</param>
    /// <returns>Task, das den Abschluss des Downloads anzeigt.</returns>
    public static async Task DownloadFileAsync(string url, string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("Die URL darf nicht leer sein.", nameof(url));

        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Der Speicherort darf nicht leer sein.", nameof(destinationPath));

        #if DEBUG
        if (File.Exists(destinationPath))
            return;
        #endif
        
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using (var stream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fileStream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Herunterladen der Datei: {ex.Message}");
            throw;
        }
    }
}