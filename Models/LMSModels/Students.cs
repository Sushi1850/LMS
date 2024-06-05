using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class Students
    {
        public Students()
        {
            Enrolled = new HashSet<Enrolled>();
            Submission = new HashSet<Submission>();
        }

        public string Subject { get; set; }
        public string UId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime Dob { get; set; }

        public virtual Departments SubjectNavigation { get; set; }
        public virtual ICollection<Enrolled> Enrolled { get; set; }
        public virtual ICollection<Submission> Submission { get; set; }
    }
}
