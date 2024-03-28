namespace IdentityDotNetTotor.DTO
{
    public class UserClaimsDTO
    {
        public UserClaimsDTO()
        {
            //To Avoid runtime exception, we are initializing the Cliams property
            Cliams = new List<UserClaim>();
        }
        public string UserEmail { get; set; }
        public List<UserClaim> Cliams { get; set; }// ref to UserClaim class (one to many relation)
    }
}
