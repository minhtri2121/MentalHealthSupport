using Microsoft.AspNetCore.Mvc;

namespace MentalHealthSupport.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}