using Microsoft.AspNetCore.Mvc;

namespace WorkflowEngine.API.Controllers;

/// <summary>Legacy MVC controller scaffold for integration events.</summary>
public class IntegrationEventsController : Controller
{
    /// <summary>Renders the integration events index view.</summary>
    public IActionResult Index()
    {
        return View();
    }
}
