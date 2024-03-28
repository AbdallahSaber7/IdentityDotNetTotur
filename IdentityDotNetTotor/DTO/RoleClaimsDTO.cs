namespace IdentityDotNetTotor.DTO
{
    public class RoleClaimsDTO
    {
        public RoleClaimsDTO()
        {
            Claims = new List<RoleClaim>();
        }
        public string RoleName{ get; set; }
        public List<RoleClaim> Claims { get; set; }
    }
}
