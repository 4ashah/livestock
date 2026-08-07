using Microsoft.AspNetCore.Mvc;

namespace LivestockManager.Web.Controllers;

public class CompaniesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    public IActionResult Details(int id)
    {
        return View();
    }

    public IActionResult Edit(int id)
    {
        return View();
    }
}
