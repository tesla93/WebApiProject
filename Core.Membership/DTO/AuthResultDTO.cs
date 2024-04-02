namespace Core.Membership.DTO
{
    public class AuthResultDTO
    {
        public string UserId { get; set; }
        public bool AuthenticatorEnabled { get; set; }
        public bool U2FEnabled { get; set; }
        public U2FAuthenticationRequestDTO U2FAuthenticationRequest { get; set; }
        public bool IsRealUserEditor { get; set; }
        public UserDTO LoggedUser { get; set; }
    }
}