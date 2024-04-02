namespace SystemData.DTO
{
    public class SystemDataDTO
    {
        /// <summary>
        /// Login of authorized user
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Server machine name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Local server IP
        /// </summary>
        public string ServerIp { get; set; }

        /// <summary>
        /// Remote client IP
        /// </summary>
        public string ClientIp { get; set; }
        
        /// <summary>
        /// Commit hash
        /// </summary>
        public string CommitHash { get; set; }
        
        /// <summary>
        /// Docker info
        /// </summary>
        public DockerInfoDTO DockerInfo { get; set; }
        
        /// <summary>
        /// Docker metadata
        /// </summary>
        public DockerMetadataDTO DockerMetadata { get; set; }

        public string OperatingSystem { get; set; }
    }
}
