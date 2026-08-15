using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LivestockManager.Domain.Common;

namespace LivestockManager.Web.Controllers;

[Authorize(Policy = PermissionNames.Administration.Companies)]
public class CompaniesController : Controller
{
    [HttpGet]
    [Authorize(Policy = PermissionNames.Administration.Companies)]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Administration.Companies)]
    public IActionResult Create()
    {
        return View();
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Administration.Companies)]
    public IActionResult Details(int id)
    {
        _ = id;
        return View();
    }

    [HttpGet]
    [Authorize(Policy = PermissionNames.Administration.Companies)]
    public IActionResult Edit(int id)
    {
        _ = id;
        return View();
    }
}
