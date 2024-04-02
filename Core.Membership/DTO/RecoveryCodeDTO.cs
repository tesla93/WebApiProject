namespace Core.Membership.DTO
{
	public class RecoveryCodeDTO
	{
		public string UserId { get; set; }
		public string Code { get; set; }
        public string Browser { get; set; }
        public string Fingerprint { get; set; }
    }
}