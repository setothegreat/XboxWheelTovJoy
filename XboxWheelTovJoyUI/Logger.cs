using System;
using System.IO;
using System.Threading.Tasks;

public class Logger
{
    private string? logFilePath;

    public string LogFilePath
    {
        get
        {
            if (logFilePath == null)
            {
                string localFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                logFilePath = Path.Combine(localFolderPath, "log.txt");
            }

            return logFilePath;
        }
    }

    public async Task Log(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string logMessage = $"{timestamp}: {message}\n";
        await File.AppendAllTextAsync(LogFilePath, logMessage);
    }

}
