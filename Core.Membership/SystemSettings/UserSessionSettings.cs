namespace Core.Membership.SystemSettings
{
    public class UserSessionSettings
    {
        public UserSessionSettings()
        {
            IdleTime = 30;
            IdleTimeEnabled = true;
        }

        /// <summary>
        /// User's idle time (in minutes) before it will be logged out
        /// </summary>
        public int? IdleTime { get; set; }

        /// <summary>
        /// Determines if is enable user's idle time or not
        /// </summary>
        public bool IdleTimeEnabled { get; set; }
    }
}