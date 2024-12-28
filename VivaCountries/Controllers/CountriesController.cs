using Microsoft.AspNetCore.Mvc;
using VivaCountries.Services;

namespace VivaCountries.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountriesController : ControllerBase
    {
        private readonly CountriesService _countriesService;

        public CountriesController(CountriesService countriesService)
        {
            _countriesService = countriesService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllCountries()
        {
            try
            {
                var countries = await _countriesService.GetAllCountriesAsync();
                return Ok(countries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
