using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class Submission
    {
        public string SId { get; set; }
        public ushort AssignmentId { get; set; }
        public byte Score { get; set; }
        public string Contents { get; set; }
        public DateTime Time { get; set; }

        public virtual Assignments Assignment { get; set; }
        public virtual Students S { get; set; }
    }
}
