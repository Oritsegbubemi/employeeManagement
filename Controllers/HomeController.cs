using EmployeeManagement.Models;
using EmployeeManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeManagement.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IWebHostEnvironment hostingEnvironment;
        private readonly ILogger logger;

        
        //CONSTRUCTOR
        public HomeController(IEmployeeRepository employeeRepository, IWebHostEnvironment hostingEnvironment, ILogger<HomeController> logger)
        {
            _employeeRepository = employeeRepository;
            this.hostingEnvironment = hostingEnvironment;
            this.logger = logger;
        }


        //INDEX
        [AllowAnonymous]
        public ViewResult Index()
        {
            var model = _employeeRepository.GetAllEmployee();
            return View(model);
        }


        //DETAILS
        [AllowAnonymous]
        public ViewResult Details(int? id = 3)
        {
            logger.LogTrace("Trace log");
            logger.LogWarning("Warning Log");
            logger.LogDebug("Debug Log");
            logger.LogError("Error Log");
            logger.LogCritical("Critical log");
            logger.LogInformation("Information Log");

            Employee employee = _employeeRepository.GetEmployee(id ?? 1);
            if (employee == null)
            {
                Response.StatusCode = 404;
                return View("EmployeeNotFound", id.Value);
            }
            HomeDetailsViewModel model = new HomeDetailsViewModel()
            {
                Employee = employee,
            };
            return View(model);
        }



        //CREATE GET
        [HttpGet]
        public ViewResult Create()
        {
            return View();
        }


        //CREATE POST
        [HttpPost]
        public IActionResult Create(CreateEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                string UniqueFileName = ProcessUploadedFile(model);
                Employee newEmployee = new Employee
                {
                    Name = model.Name,
                    Department = model.Department,
                    Email = model.Email,
                    PhotoPath = UniqueFileName
                };
                _employeeRepository.Add(newEmployee);
                return RedirectToAction("details", new { id = newEmployee.Id });
            }
            return View();
        }


        //EDIT GET
        [HttpGet]
        public ViewResult Edit(int id)
        {
            Employee employee = _employeeRepository.GetEmployee(id);
            EditEmployeeViewModel employeeEditViewModel = new EditEmployeeViewModel
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Department = employee.Department,
                existingPhotoPath = employee.PhotoPath
            };
            return View(employeeEditViewModel);
        }


        //EDIT POST
        [HttpPost]
        public IActionResult Edit(EditEmployeeViewModel model)
        {
            if (ModelState.IsValid)
            {
                Employee employee = _employeeRepository.GetEmployee(model.Id);
                employee.Name = model.Name;
                employee.Email = model.Email;
                employee.Department = model.Department;
                if (model.Photo != null)
                {
                    if (model.existingPhotoPath != null)
                    {
                        string filePath = Path.Combine(hostingEnvironment.WebRootPath, "images", model.existingPhotoPath);
                        System.IO.File.Delete(filePath);
                    }
                    employee.PhotoPath = ProcessUploadedFile(model);
                }
                _employeeRepository.Update(employee);

                return RedirectToAction("details", new { id = employee.Id });
            }
            return View();
        }


        //PROCESSUPLOADEDFILE
        private string ProcessUploadedFile(CreateEmployeeViewModel model)
        {
            string UniqueFileName = null;
            if (model.Photo != null)
            {
                string UploadPath = Path.Combine(hostingEnvironment.WebRootPath, "images");
                UniqueFileName = Guid.NewGuid().ToString() + "_" + model.Photo.FileName;
                string filePath = Path.Combine(UploadPath, UniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    model.Photo.CopyTo(fileStream);
                }
            }
            return UniqueFileName;
        }
    } 
}
