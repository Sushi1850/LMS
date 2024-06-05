using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class Courses
    {
        public Courses()
        {
            Classes = new HashSet<Classes>();
        }

        public ushort CourseId { get; set; }
        public string Subject { get; set; }
        public ushort Number { get; set; }
        public string Name { get; set; }

        public virtual Departments SubjectNavigation { get; set; }
        public virtual ICollection<Classes> Classes { get; set; }
    }
}
