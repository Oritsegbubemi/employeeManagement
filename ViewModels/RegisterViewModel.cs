using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Utilities;

namespace EmployeeManagement.ViewModels
{
    public class RegisterViewModel
    {
        [ValidEmailDomainAttribute(allowedDomain: "gmail.com", ErrorMessage = "Email domain must be gmail.com")]    //custom validation - defined using a class
        [Required, EmailAddress, Remote(action: "IsEmailInUse", controller: "Account")] //Remote enables client side validation with some jquery plugins
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
        
        [Display(Name = "Confirm password"), DataType(DataType.Password)]
        [Required, Compare("Password", ErrorMessage = "Password and confirmation password does not match")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string City { get; set; }
    }
}
