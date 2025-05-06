namespace SimplePasswordManager
{
    internal class SaveFile
    {
        public Config config {  get; set; }
        public string version { get; set; }
    }

    internal class Config
    {
        // these are loaded into memory in
        // LoadConfig, in init.cs

        public bool isFirstLoad { get; set; }
        public bool debugMode { get; set; }
        public int timeout { get; set; }
    }
}
