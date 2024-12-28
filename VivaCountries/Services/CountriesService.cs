using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VivaCountries.Data;
using VivaCountries.Models;
using System.Net.Http.Json;
using System.Net;
using System.Security.Cryptography;

namespace VivaCountries.Services
{
    public class CountriesService
    {
        private readonly HttpClient _httpClient;
        private readonly CountriesDbContext _dbContext;
        private readonly IMemoryCache _memoryCache;
        private const string CountriesCacheKey = "CountriesCache";

        public CountriesService(HttpClient httpClient, CountriesDbContext dbContext, IMemoryCache memoryCache)
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            _httpClient = new HttpClient(handler);
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Set timeout
            _dbContext = dbContext;
            _memoryCache = memoryCache;
        }

        public async Task<IEnumerable<CountryResponse>> GetAllCountriesAsync()
        {
            // First, try to get countries from cache
            if (_memoryCache.TryGetValue(CountriesCacheKey, out IEnumerable<CountryResponse> cachedCountries) && cachedCountries != null)
            {
                return cachedCountries;
            }

            // If cache is empty or expired, check the database
            // Fetch data from the database
            var dbCountries = await _dbContext.Countries.ToListAsync();

            var result = dbCountries.Select(c => new CountryResponse
            {
                Name = new NameResponse { Common = c.CommonName },
                Capital = c.Capital?.Split(',').ToList(),
                Borders = c.Borders?.Split(',').ToList()
            });

            if (result.Any() && result != null)
            {
                // Save the database result to the cache before returning
                _memoryCache.Set(CountriesCacheKey, result, TimeSpan.FromHours(1));
                return result;
            }

            // If both cache and database are empty, make the HTTP API call
            var apiResponse = await _httpClient.GetFromJsonAsync<List<CountryResponse>>("https://restcountries.com/v3.1/all");

            if (apiResponse == null || !apiResponse.Any())
            {
                throw new Exception("Failed to fetch data from the API");
            }

            // Save the fetched data to cache
            _memoryCache.Set(CountriesCacheKey, apiResponse, TimeSpan.FromHours(1));

            // Map and save to database
            var countriesToSave = apiResponse.Select(c => new Country
            {
                CommonName = c.Name.Common,
                Capital = c.Capital != null ? string.Join(",", c.Capital) : null,
                Borders = c.Borders != null ? string.Join(",", c.Borders) : null
            });

            foreach (var country in countriesToSave)
            {
                if (!_dbContext.Countries.Any(dbCountry => dbCountry.CommonName == country.CommonName))
                {
                    _dbContext.Countries.Add(country);
                }
            }

            await _dbContext.SaveChangesAsync();

            return apiResponse;
        }
    }
}
