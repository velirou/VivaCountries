using System.Security.Cryptography;

namespace VivaCountries.Models
{
    public class CountryResponse
    {   
        public NameResponse Name { get; set; }
        public List<string> Capital { get; set; }
        public List<string> Borders { get; set; }
    }

    public class NameResponse
    {
        public string Common { get; set; }
    }
}
