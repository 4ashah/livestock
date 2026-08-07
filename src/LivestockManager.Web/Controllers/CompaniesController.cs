using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = "CanManageSystem")]
public class CompaniesController : Controller
{
    [HttpGet]
    [Authorize(Policy = "CanManageSystem")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Policy = "CanManageSystem")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Policy = "CanManageSystem")]
    public IActionResult Details(int id)
    {
        return View();
    }

    [HttpGet]
    [Authorize(Policy = "CanManageSystem")]
    public IActionResult Edit(int id)
    {
        return View();
    }
}
