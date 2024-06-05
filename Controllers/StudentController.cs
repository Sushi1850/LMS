using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using LMS.Models.LMSModels;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Controllers {
  [Authorize(Roles = "Student")]
  public class StudentController : CommonController {

    public IActionResult Index() {
      return View();
    }

    public IActionResult Catalog() {
      return View();
    }

    public IActionResult Class(string subject, string num, string season, string year) {
      ViewData["subject"] = subject;
      ViewData["num"] = num;
      ViewData["season"] = season;
      ViewData["year"] = year;
      return View();
    }

    public IActionResult Assignment(string subject, string num, string season, string year, string cat, string aname) {
      ViewData["subject"] = subject;
      ViewData["num"] = num;
      ViewData["season"] = season;
      ViewData["year"] = year;
      ViewData["cat"] = cat;
      ViewData["aname"] = aname;
      return View();
    }


    public IActionResult ClassListings(string subject, string num) {
      System.Diagnostics.Debug.WriteLine(subject + num);
      ViewData["subject"] = subject;
      ViewData["num"] = num;
      return View();
    }


    /*******Begin code to modify********/

    /// <summary>
    /// Returns a JSON array of the classes the given student is enrolled in.
    /// Each object in the array should have the following fields:
    /// "subject" - The subject abbreviation of the class (such as "CS")
    /// "number" - The course number (such as 5530)
    /// "name" - The course name
    /// "season" - The season part of the semester
    /// "year" - The year part of the semester
    /// "grade" - The grade earned in the class, or "--" if one hasn't been assigned
    /// </summary>
    /// <param name="uid">The uid of the student</param>
    /// <returns>The JSON array</returns>
    public IActionResult GetMyClasses(string uid) {
      var sem = new Regex("(?<Season>[a-zA-Z]*)(?<Year>[0-9]*)");

      var query = from enrolls in db.Enrolled
                  join classes in db.Classes on enrolls.ClassId equals classes.ClassId
                  join courses in db.Courses on classes.CourseId equals courses.CourseId
                  where enrolls.SId == uid
                  select new {
                    subject = courses.Subject,
                    number = courses.Number,
                    name = courses.Name,
                    season = sem.Match(classes.Semester).Groups["Season"].Value,
                    year = sem.Match(classes.Semester).Groups["Year"].Value,
                    grade = (enrolls.Grade == "") ? "--" : enrolls.Grade
                  };

      return Json(query.ToArray());
    }

    /// <summary>
    /// Returns a JSON array of all the assignments in the given class that the given student is enrolled in.
    /// Each object in the array should have the following fields:
    /// "aname" - The assignment name
    /// "cname" - The category name that the assignment belongs to
    /// "due" - The due Date/Time
    /// "score" - The score earned by the student, or null if the student has not submitted to this assignment.
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <param name="uid"></param>
    /// <returns>The JSON array</returns>
    public IActionResult GetAssignmentsInClass(string subject, int num, string season, int year, string uid) {
      //var query =
      //  from dept in db.Departments
      //  join course in db.Courses on dept.Subject equals course.Subject
      //  join _class in db.Classes on course.CourseId equals _class.CourseId
      //  join enrolled in db.Enrolled on _class.ClassId equals enrolled.ClassId
      //  join ac in db.AssignmentCategories on _class.ClassId equals ac.ClassId
      //  join asg in db.Assignments on ac.CategoryId equals asg.CategoryId
      //  join submission in db.Submission on asg.AssignmentId equals submission.AssignmentId into submissionTemp
      //  from submission2 in submissionTemp.DefaultIfEmpty()
      //  where dept.Subject == subject && course.Number == num && _class.Semester == (season + year) && enrolled.SId == uid 
      //  select new {
      //    aname = asg.Name,
      //    cname = ac.Name,
      //    due = asg.DueDate,
      //    score = (submission2.Score == null) ? null : submission2.Score.ToString(),
      //  };

      var query =
        from enroll in db.Enrolled
        where enroll.SId == uid && enroll.Class.Semester == (season + year) && enroll.Class.Course.Subject == subject && enroll.Class.Course.Number == num
        from asg in db.Assignments
        where asg.Category.ClassId == enroll.Class.ClassId
        select asg;

      var query2 =
        from a in query
        join submission in db.Submission
        on new { X = a.AssignmentId, Y = uid } equals new { X = submission.AssignmentId, Y = submission.SId }
        into temp
        from m in temp.DefaultIfEmpty()
        select new {
          aname = a.Name,
          cname = a.Category.Name,
          due = a.DueDate,
          score = m == null ? null : (uint?)m.Score
        };

      //var rand = query.ToArray();
      return Json(query2.ToArray());
    }



    /// <summary>
    /// Adds a submission to the given assignment for the given student
    /// The submission should use the current time as its DateTime
    /// You can get the current time with DateTime.Now
    /// The score of the submission should start as 0 until a Professor grades it
    /// If a Student submits to an assignment again, it should replace the submission contents
    /// and the submission time (the score should remain the same).
    /// Does *not* automatically reject late submissions.
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <param name="category">The name of the assignment category in the class</param>
    /// <param name="asgname">The new assignment name</param>
    /// <param name="uid">The student submitting the assignment</param>
    /// <param name="contents">The text contents of the student's submission</param>
    /// <returns>A JSON object containing {success = true/false}.</returns>
    public IActionResult SubmitAssignmentText(string subject, int num, string season, int year,
      string category, string asgname, string uid, string contents) {
      try {
        var asgId =
          (from depts in db.Departments
           join courses in db.Courses on depts.Subject equals courses.Subject
           join classes in db.Classes on courses.CourseId equals classes.CourseId
           join assignmentCategories in db.AssignmentCategories on classes.ClassId equals assignmentCategories.ClassId
           join assignments in db.Assignments on assignmentCategories.CategoryId equals assignments.CategoryId
           where depts.Subject == subject && courses.Number == num && classes.Semester == (season + year) && assignmentCategories.Name == category && assignments.Name == asgname
           select assignments.AssignmentId).SingleOrDefault();

        var query =
          from submission in db.Submission
          where submission.AssignmentId == asgId && submission.SId == uid
          select submission;

        // If a submission already exists.
        if (query.Count() > 0) {
          var submission = query.SingleOrDefault();
          submission.Contents = contents;
          submission.Time = DateTime.Now;
          db.SaveChanges();

          return Json(new { success = true });
        }

        // Otherwise, create new submission.
        var newSubmission = new Submission();
        newSubmission.AssignmentId = asgId;
        newSubmission.SId = uid;
        newSubmission.Score = 0;
        newSubmission.Time = DateTime.Now;
        newSubmission.Contents = contents;
        db.Submission.Add(newSubmission);
        db.SaveChanges();

        return Json(new { success = true });
      } catch {
        return Json(new { success = false });
      }
    }


    /// <summary>
    /// Enrolls a student in a class.
    /// </summary>
    /// <param name="subject">The department subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester</param>
    /// <param name="year">The year part of the semester</param>
    /// <param name="uid">The uid of the student</param>
    /// <returns>A JSON object containing {success = {true/false},
    /// false if the student is already enrolled in the Class.</returns>
    public IActionResult Enroll(string subject, int num, string season, int year, string uid) {
      // Check if there isn't an enrollment already.
      var query = from depts in db.Departments
                  join courses in db.Courses on depts.Subject equals courses.Subject
                  join classes in db.Classes on courses.CourseId equals classes.CourseId
                  join enrolls in db.Enrolled on classes.ClassId equals enrolls.ClassId
                  where depts.Subject == subject && courses.Number == num && classes.Semester == (season + year) && enrolls.SId == uid
                  select enrolls;

      if (query.Count() > 0)
        return Json(new { success = false });

      // Check to see if this class exists.
      var classID = from depts in db.Departments
                    join courses in db.Courses on depts.Subject equals courses.Subject
                    join classes in db.Classes on courses.CourseId equals classes.CourseId
                    where depts.Subject == subject && courses.Number == num && classes.Semester == (season + year)
                    select classes.ClassId;

      if (classID.Count() <= 0)
        return Json(new { success = false });

      var newEnroll = new Enrolled();
      newEnroll.SId = uid;
      newEnroll.Grade = "";
      newEnroll.ClassId = classID.SingleOrDefault();
      db.Enrolled.Add(newEnroll);
      db.SaveChanges();
      return Json(new { success = true });
    }



    /// <summary>
    /// Calculates a student's GPA
    /// A student's GPA is determined by the grade-point representation of the average grade in all their classes.
    /// Assume all classes are 4 credit hours.
    /// If a student does not have a grade in a class ("--"), that class is not counted in the average.
    /// If a student does not have any grades, they have a GPA of 0.0.
    /// Otherwise, the point-value of a letter grade is determined by the table on this page:
    /// https://advising.utah.edu/academic-standards/gpa-calculator-new.php
    /// </summary>
    /// <param name="uid">The uid of the student</param>
    /// <returns>A JSON object containing a single field called "gpa" with the number value</returns>
    public IActionResult GetGPA(string uid) {
      double totalGpaPoints = 0.0;
      int counter = 0;

      var query = from enrolls in db.Enrolled
                  where enrolls.SId == uid
                  select enrolls.Grade;

      if (query.Count() < 1)
        return Json(new { gpa = 0.0 });

      foreach (var grade in query) {
        if (grade == "--")
          continue;

        counter++;
        switch (grade) {
          case "A":
            totalGpaPoints += 4.0;
            break;
          case "A-":
            totalGpaPoints += 3.7;
            break;
          case "B+":
            totalGpaPoints += 3.3;
            break;
          case "B-":
            totalGpaPoints += 3.0;
            break;
          case "C+":
            totalGpaPoints += 2.3;
            break;
          case "C":
            totalGpaPoints += 2.0;
            break;
          case "C-":
            totalGpaPoints += 1.7;
            break;
          case "D+":
            totalGpaPoints += 1.3;
            break;
          case "D":
            totalGpaPoints += 1.0;
            break;
          case "D-":
            totalGpaPoints += 0.7;
            break;
          case "E":
            totalGpaPoints += 0.0;
            break;
          default:
            break;
        }
      }

      double newGpa = totalGpaPoints / counter;
      return Json(new { gpa = newGpa });
    }

    /*******End code to modify********/

  }
}