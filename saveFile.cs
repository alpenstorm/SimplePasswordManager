namespace SimplePasswordManager
{
    /// <summary>
    /// Represents the structure of the configuration save file.
    /// </summary>
    internal class SaveFile
    {
        /// <summary>
        /// Gets or sets the configuration settings.
        /// </summary>
        public Config config { get; set; }

        /// <summary>
        /// Gets or sets the version of the application.
        /// </summary>
        public string version { get; set; }
    }

    /// <summary>
    /// Represents the configuration settings stored in the save file.
    /// </summary>
    internal class Config
    {
        /// <summary>
        /// Gets or sets whether this is the first load of the application.
        /// </summary>
        public bool isFirstLoad { get; set; }

        /// <summary>
        /// Gets or sets whether debug mode is enabled.
        /// </summary>
        public bool debugMode { get; set; }
    }
}