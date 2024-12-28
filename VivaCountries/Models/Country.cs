namespace VivaCountries.Models
{
    public class Country
    {
        public int Id { get; set; } // Primary key
        public string CommonName { get; set; }
        public string? Capital { get; set; }
        public string? Borders { get; set; }
    }
}
