using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class Assignments
    {
        public Assignments()
        {
            Submission = new HashSet<Submission>();
        }

        public ushort AssignmentId { get; set; }
        public ushort CategoryId { get; set; }
        public string Name { get; set; }
        public string Contents { get; set; }
        public DateTime DueDate { get; set; }
        public byte Points { get; set; }

        public virtual AssignmentCategories Category { get; set; }
        public virtual ICollection<Submission> Submission { get; set; }
    }
}
