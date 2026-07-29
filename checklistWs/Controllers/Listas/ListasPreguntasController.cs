using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Listas
{
    public class ListasPreguntasController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
