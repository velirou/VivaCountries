using Microsoft.AspNetCore.Mvc;

namespace VivaCountries.Controllers
{
    public class SecondLargestController : Controller
    {
        public class RequestObj
        {
            public IEnumerable<int> RequestArrayObj { get; set; }
        }

        [HttpPost]
        public ActionResult SecondLargest([FromBody] RequestObj requestObj)
        {
            if (requestObj?.RequestArrayObj == null || !requestObj.RequestArrayObj.Any())
            {
                return BadRequest("Invalid or empty array in request.");
            }

            var distinctList = requestObj.RequestArrayObj.Distinct().OrderByDescending(item => item);

            if (distinctList.Count() < 2)
            {
                return BadRequest("Array does not have enough unique elements to determine the second largest value.");
            }

            return Ok(distinctList.Skip(1).First());
        }
    }
}
