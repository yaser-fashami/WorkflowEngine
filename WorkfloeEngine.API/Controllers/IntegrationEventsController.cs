using Microsoft.AspNetCore.Mvc;

namespace WorkflowEngine.API.Controllers;

public class IntegrationEventsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
