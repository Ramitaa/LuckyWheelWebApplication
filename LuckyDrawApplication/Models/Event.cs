using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LuckyDrawApplication.Models
{

    public class Event
    {
        [Display(Name = "Event ID")]
        public int EventID { get; set; }

        [Required]
        [Display(Name = "Event Code")]
        public string EventCode { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Event Password")]
        public string EventPassword { get; set; }

        [Display(Name = "Event Salt")]
        public string EventSalt { get; set; }

        [Display(Name = "Event Location")]
        public string EventLocation { get; set; }

    }
}
