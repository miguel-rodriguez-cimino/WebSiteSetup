using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MyWebSite.Models
{
    public class HomeModel
    {
        [Required]
        public string Name { get; set; }
    }
}