using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VivaCountries.Data;
using VivaCountries.Models;

namespace VivaCountries.Controllers
{
    [Route("api/[controller]")]
    public class CountriesInfoController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CountriesInfoController> _logger;
        private readonly CountriesDbContext _dbContext;

        public CountriesInfoController(HttpClient httpClient, ILogger<CountriesInfoController> logger, CountriesDbContext dbContext)
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Set timeout
            _dbContext = dbContext;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetData()
        {
            const string restCountriesUrl = "https://restcountries.com/v3.1/all";

            try
            {
                // Check if data is already in the database
                if (!_dbContext.Countries.Any())
                {
                    _logger.LogInformation($"Sending request to {restCountriesUrl}");
                    using var response = await _httpClient.GetAsync(restCountriesUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError($"API returned status code: {response.StatusCode}");
                        return StatusCode((int)response.StatusCode, "Error fetching countries from the API");
                    }

                    _logger.LogInformation("Content-Length: " + response.Content.Headers.ContentLength);

                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Response content received: " + content);

                    var apiCountries = JsonSerializer.Deserialize<List<CountryResponse>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (apiCountries == null || apiCountries.Count == 0)
                    {
                        _logger.LogWarning("No countries data received from API.");
                        return NotFound("No countries data available.");
                    }

                    // Map API response to the database entities
                    var countryEntities = apiCountries.Select(c => new Country
                    {
                        CommonName = c.Name.Common,
                        Capital = c.Capital?.FirstOrDefault(),
                        Borders = c.Borders != null ? string.Join(",", c.Borders) : null
                    });

                    // Save countries to the database
                    await _dbContext.Countries.AddRangeAsync(countryEntities);
                    await _dbContext.SaveChangesAsync();
                }

                // Fetch data from the database
                var dbCountries = await _dbContext.Countries.ToListAsync();

                var result = dbCountries.Select(c => new CountryResponseDto
                {
                    CommonName = c.CommonName,
                    Capital = c.Capital,
                    Borders = c.Borders?.Split(',').ToList()
                });

                return Ok(result);
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Error deserializing JSON: {ex.Message}");
                return StatusCode(500, "Error deserializing JSON data.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"Request to RestCountries API failed: {ex.Message}");
                return StatusCode(500, $"Error fetching data from API: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred.");
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }
}
