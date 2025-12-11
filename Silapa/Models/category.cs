using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class category
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string? fullname{get; set; }
        public string? status { get; set; }

        public ICollection<groupreferee>? groupreferee { get; set;}
        public ICollection<Competitionlist>? competitionlists{get; set;} 
    }
}