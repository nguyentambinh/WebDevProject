using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QLNSVATC.Areas.HR.Data
{
    public class ProjectSectionViewModel
    {
        public string TypeCode { get; set; }    
        public string TypeName { get; set; }      
        public IList<ProjectItemViewModel> Items { get; set; }
    }
}