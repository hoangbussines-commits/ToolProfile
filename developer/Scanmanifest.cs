public class ScanManifest
{
    public int version { get; set; } = 1;
    public int totalScans { get; set; }
    public List<ManifestEntry> entries { get; set; } = new();
}

public class ManifestEntry
{
    public string file { get; set; }
    public string input { get; set; }
    public string type { get; set; }
    public DateTime created { get; set; }
    public DateTime lastSeen { get; set; }
    public int scanCount { get; set; }
}
