using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class deleteregister
    {
        [Key]
        public int id { get; set; }
        public int c_id { get; set; }
        public string name { get; set; }
        public string u_id { get; set; }
        public DateTime lastupdate{ get; set; }
    }
}