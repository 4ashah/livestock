using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Domain.Common;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PolicyNames.CanManageSystem)]
public class CompaniesController : Controller
{
    [HttpGet]
    [Authorize(Policy = PolicyNames.CanManageSystem)]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanManageSystem)]
    public IActionResult Create()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanManageSystem)]
    public IActionResult Details(int id)
    {
        return View();
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.CanManageSystem)]
    public IActionResult Edit(int id)
    {
        return View();
    }
}
