using System.IO;

namespace MassageSaas.Cs.Services;

public class AppSettings
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5000/api";
    public string TokenFilePath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassageSaas.Cs", "session.json");
}
