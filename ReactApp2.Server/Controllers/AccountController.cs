using Microsoft.AspNetCore.Mvc;

namespace KarttaBackEnd2.Server.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
