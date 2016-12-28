using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaxFm.Core.Models
{
    public class Setup
    {
        [Key]
        public string Profile { get; set; }
        public bool Initialized { get; set; }
    }
}
