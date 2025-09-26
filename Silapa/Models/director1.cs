using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class director1
    {
        [Key]
        public int id { get; set; }
        public string ?name { get; set; }
        public string ?role1{ get; set; }
        public string ?position1{ get; set; }
        public string ?ImageUrl{ get; set; }
        public string ?u_id { get; set; }
        public string ?status{ get; set; }
        public DateTime? lastupdate{ get; set; }

    }
}