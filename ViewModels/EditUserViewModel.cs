using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Utilities;

namespace EmployeeManagement.ViewModels
{
    public class EditUserViewModel
    {
        public EditUserViewModel()
        {
            Claims = new List<string>();
            Roles = new List<string>();
        }
        public string Id { set; get; }

        [ValidEmailDomainAttribute(allowedDomain: "gmail.com", ErrorMessage = "Email domain must be gmail.com")]    //custom validation - defined using a class
        [Required, EmailAddress, Remote(action: "IsEmailInUse", controller: "Account")] //Remote enables client side validation with some jquery plugins
        public string Email { get; set; }

        [Required]
        public string Username { get; set; }

        public string City { get; set; }

        public List<string> Claims { get; set; }

        public IList<string> Roles { get; set; }    
    }
}
