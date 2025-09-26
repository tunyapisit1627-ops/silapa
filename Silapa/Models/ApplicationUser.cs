using Microsoft.AspNetCore.Identity;

namespace Silapa.Models
{
    public class ApplicationUser: IdentityUser
    {
        public string ?titlename { get; set;}
        public string ?FirstName { get; set; }
        public string ?LastName { get; set; }
        public int? s_id { get; set; }
        public string? m_id{ get; set; }
    }
}