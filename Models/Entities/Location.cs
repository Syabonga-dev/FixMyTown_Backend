using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{
    [Table("Locations")]
    public class Location
    {
        [Key]
        public int LocationId { get; set; }

        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Ward { get; set; }
        public string? Suburb { get; set; }

        public string? AddressDescription { get; set; }
        public string? StreetAddress { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
