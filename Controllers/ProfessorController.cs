using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using LMS.Models.LMSModels;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Controllers {
  [Authorize(Roles = "Professor")]
  public class ProfessorController : CommonController {
    public IActionResult Index() {
      return View();
    }

    public IActionResult Students(string subject, string num, string season, string year) {
      ViewData["subject"] = subject;
      ViewData["num"] = num;
      ViewData["season"] = season;
      ViewData["year"] = year;
      return View();
    }

    public IActionResult Class(string subject, string num, string season, string year) {
      ViewData["subject"] = subject;
      ViewData["num"] = num;
      ViewData["season"] = season;
      ViewData["year"] = year;
      return View();
    }

    public IActionResult Categories(string subject, string num, string season, string year) {
      ViewData["subject"] = subject;
      ViewData["num"] = num;
      ViewData["season"] = season;
      ViewData["year"] = year;
      return View();
    }

    public IActionResult CatAssignments(string subject, string num, string season, string year, string cat) {
      ViewData["subject"] = subject;
      ViewData["num"] = num;
      ViewData["season"] = season;
      ViewData["year"] = year;
      ViewData["cat"] = cat;
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

    public IActionResult Submissions(string subject, string num, string season, string year, string cat, string aname) {
      ViewData["subject"] = subject;
      ViewData["num"] = num;
      ViewData["season"] = season;
      ViewData["year"] = year;
      ViewData["cat"] = cat;
      ViewData["aname"] = aname;
      return View();
    }

    public IActionResult Grade(string subject, string num, string season, string year, string cat, string aname, string uid) {
      ViewData["subject"] = subject;
      ViewData["num"] = num;
      ViewData["season"] = season;
      ViewData["year"] = year;
      ViewData["cat"] = cat;
      ViewData["aname"] = aname;
      ViewData["uid"] = uid;
      return View();
    }

    /*******Begin code to modify********/


    /// <summary>
    /// Returns a JSON array of all the students in a class.
    /// Each object in the array should have the following fields:
    /// "fname" - first name
    /// "lname" - last name
    /// "uid" - user ID
    /// "dob" - date of birth
    /// "grade" - the student's grade in this class
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <returns>The JSON array</returns>
    public IActionResult GetStudentsInClass(string subject, int num, string season, int year) {
      var query =
        from depts in db.Departments
        join courses in db.Courses on depts.Subject equals courses.Subject
        join classes in db.Classes on courses.CourseId equals classes.CourseId
        join enrolled in db.Enrolled on classes.ClassId equals enrolled.ClassId
        join students in db.Students on enrolled.SId equals students.UId
        where depts.Subject == subject && courses.Number == num && classes.Semester == (season + year)
        select new {
          fname = students.FirstName,
          lname = students.LastName,
          uid = students.UId,
          dob = students.Dob,
          grade = enrolled.Grade ?? "--" // TODO: Might change this later?
        };

      return Json(query.ToArray());
    }

    /// <summary>
    /// Returns a JSON array with all the assignments in an assignment category for a class.
    /// If the "category" parameter is null, return all assignments in the class.
    /// Each object in the array should have the following fields:
    /// "aname" - The assignment name
    /// "cname" - The assignment category name.
    /// "due" - The due DateTime
    /// "submissions" - The number of submissions to the assignment
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <param name="category">The name of the assignment category in the class, 
    /// or null to return assignments from all categories</param>
    /// <returns>The JSON array</returns>
    public IActionResult GetAssignmentsInCategory(string subject, int num, string season, int year, string category) {
      var query =
        from dept in db.Departments
        join course in db.Courses on dept.Subject equals course.Subject
        join _class in db.Classes on course.CourseId equals _class.CourseId
        join ac in db.AssignmentCategories on _class.ClassId equals ac.ClassId
        join asg in db.Assignments on ac.CategoryId equals asg.CategoryId
        where dept.Subject == subject && course.Number == num && _class.Semester == (season + year) && (category == null ? true : ac.Name == category)
        select new {
          aname = asg.Name,
          cname = ac.Name,
          due = asg.DueDate,
          submissions =
            (from submission in db.Submission
             where submission.AssignmentId == asg.AssignmentId
             select submission).Count()
        };

      return Json(query.ToArray());
    }

    /// <summary>
    /// Returns a JSON array of the assignment categories for a certain class.
    /// Each object in the array should have the folling fields:
    /// "name" - The category name
    /// "weight" - The category weight
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <param name="category">The name of the assignment category in the class</param>
    /// <returns>The JSON array</returns>
    public IActionResult GetAssignmentCategories(string subject, int num, string season, int year) {
      var query =
        from depts in db.Departments
        join courses in db.Courses on depts.Subject equals courses.Subject
        join classes in db.Classes on courses.CourseId equals classes.CourseId
        join assignmentCategories in db.AssignmentCategories on classes.ClassId equals assignmentCategories.ClassId
        where depts.Subject == subject && courses.Number == num && classes.Semester == (season + year)
        select new {
          name = assignmentCategories.Name,
          weight = assignmentCategories.Weight
        };

      return Json(query.ToArray());
    }

    /// <summary>
    /// Creates a new assignment category for the specified class.
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <param name="category">The new category name</param>
    /// <param name="catweight">The new category weight</param>
    /// <returns>A JSON object containing {success = true/false},
    ///	false if an assignment category with the same name already exists in the same class.</returns>
    public IActionResult CreateAssignmentCategory(string subject, int num, string season, int year, string category, int catweight) {
      var classIdQuery =
        from depts in db.Departments
        join courses in db.Courses on depts.Subject equals courses.Subject
        join classes in db.Classes on courses.CourseId equals classes.CourseId
        where depts.Subject == subject && courses.Number == num && classes.Semester == (season + year)
        select classes.ClassId;

      if (classIdQuery.Count() > 0) {
        var classId = classIdQuery.SingleOrDefault();

        // Ensure we aren't duplicating a category.
        var cat =
          from c in db.AssignmentCategories
          where c.Name == category && c.ClassId == classId
          select c;

        if (!cat.Any()) {
          var assignmentCategory = new AssignmentCategories {
            Name = category,
            Weight = (byte)catweight,
            ClassId = classId
          };
          db.AssignmentCategories.Add(assignmentCategory);
          db.SaveChanges();
          return Json(new { success = true });
        }
      }
      return Json(new { success = false });
    }

    /// <summary>
    /// Creates a new assignment for the given class and category.
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <param name="category">The name of the assignment category in the class</param>
    /// <param name="asgname">The new assignment name</param>
    /// <param name="asgpoints">The max point value for the new assignment</param>
    /// <param name="asgdue">The due DateTime for the new assignment</param>
    /// <param name="asgcontents">The contents of the new assignment</param>
    /// <returns>A JSON object containing success = true/false,
    /// false if an assignment with the same name already exists in the same assignment category.</returns>
    public IActionResult CreateAssignment(string subject, int num, string season, int year, string category, string asgname, int asgpoints, DateTime asgdue, string asgcontents) {
      var query =
        from depts in db.Departments
        join courses in db.Courses on depts.Subject equals courses.Subject
        join classes in db.Classes on courses.CourseId equals classes.CourseId
        join assignmentCategories in db.AssignmentCategories on classes.ClassId equals assignmentCategories.ClassId
        where depts.Subject == subject && courses.Number == num && classes.Semester == (season + year) && assignmentCategories.Name == category
        select assignmentCategories.CategoryId;

      var query2 =
        from dept in db.Departments
        join course in db.Courses on dept.Subject equals course.Subject
        join _class in db.Classes on course.CourseId equals _class.CourseId
        join c in db.AssignmentCategories on _class.ClassId equals c.ClassId
        join asg in db.Assignments on c.CategoryId equals asg.CategoryId
        where dept.Subject == subject && course.Number == num && _class.Semester == (season + year) && c.Name == category && asg.Name == asgname
        select asg;

      if (query2.Count() > 0) return Json(new { success = false });

      if (query.Count() > 0) {
        var assignment = new Assignments {
          Name = asgname,
          CategoryId = query.SingleOrDefault(),
          DueDate = asgdue,
          Points = (byte)asgpoints,
          Contents = asgcontents
        };

        db.Assignments.Add(assignment);
        db.SaveChanges();

        // TODO: call letter grade
        var classId =
          (from depts in db.Departments
           join courses in db.Courses on depts.Subject equals courses.Subject
           join classes in db.Classes on courses.CourseId equals classes.CourseId
           where depts.Subject == subject && courses.Number == num && classes.Semester == (season + year)
           select classes.ClassId).SingleOrDefault();

        var students =
          from dept in db.Departments
          join course in db.Courses on dept.Subject equals course.Subject
          join _class in db.Classes on course.CourseId equals _class.CourseId
          join enr in db.Enrolled on _class.ClassId equals enr.ClassId
          where enr.ClassId == classId
          select enr;

        foreach (var student in students) {
          student.Grade = GetLetterGrade(CalculateGradeForSId(student.SId, classId));
        }
        db.SaveChanges();

        return Json(new { success = true });
      }

      return Json(new { success = false });
    }

    /// <summary>
    /// Gets a JSON array of all the submissions to a certain assignment.
    /// Each object in the array should have the following fields:
    /// "fname" - first name
    /// "lname" - last name
    /// "uid" - user ID
    /// "time" - DateTime of the submission
    /// "score" - The score given to the submission
    /// 
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <param name="category">The name of the assignment category in the class</param>
    /// <param name="asgname">The name of the assignment</param>
    /// <returns>The JSON array</returns>
    public IActionResult GetSubmissionsToAssignment(string subject, int num, string season, int year, string category, string asgname) {
      var query =
        from depts in db.Departments
        join courses in db.Courses on depts.Subject equals courses.Subject
        join classes in db.Classes on courses.CourseId equals classes.CourseId
        join assignmentCategories in db.AssignmentCategories on classes.ClassId equals assignmentCategories.ClassId
        join assignments in db.Assignments on assignmentCategories.CategoryId equals assignments.CategoryId
        join submission in db.Submission on assignments.AssignmentId equals submission.AssignmentId
        join students in db.Students on submission.SId equals students.UId
        where depts.Subject == subject && courses.Number == num && classes.Semester == (season + year) && assignmentCategories.Name == category && assignments.Name == asgname
        select new {
          fname = students.FirstName,
          lname = students.LastName,
          uid = students.UId,
          time = submission.Time,
          score = submission.Score
        };

      return Json(query.ToArray());
    }


    /// <summary>
    /// Set the score of an assignment submission
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <param name="category">The name of the assignment category in the class</param>
    /// <param name="asgname">The name of the assignment</param>
    /// <param name="uid">The uid of the student who's submission is being graded</param>
    /// <param name="score">The new score for the submission</param>
    /// <returns>A JSON object containing success = true/false</returns>
    public IActionResult GradeSubmission(string subject, int num, string season, int year, string category, string asgname, string uid, int score) {
      try {
        // Grade submission.
        var query =
          (from depts in db.Departments
           join courses in db.Courses on depts.Subject equals courses.Subject
           join classes in db.Classes on courses.CourseId equals classes.CourseId
           join assignmentCategories in db.AssignmentCategories on classes.ClassId equals assignmentCategories.ClassId
           join assignments in db.Assignments on assignmentCategories.CategoryId equals assignments.CategoryId
           join submission in db.Submission on assignments.AssignmentId equals submission.AssignmentId
           where depts.Subject == subject && courses.Number == num && classes.Semester == (season + year) && assignmentCategories.Name == category && assignments.Name == asgname && submission.SId == uid
           select submission).SingleOrDefault();
        query.Score = (byte)score;

        var classId =
          (from dept in db.Departments
           join course in db.Courses on dept.Subject equals course.Subject
           join _class in db.Classes on course.CourseId equals _class.CourseId
           where dept.Subject == subject && course.Number == num && _class.Semester == (season + year)
           select _class.ClassId).SingleOrDefault();

        var enr =
          from enrolled in db.Enrolled
          where enrolled.SId == uid && enrolled.ClassId == classId
          select enrolled;

        db.SaveChanges();

        if (enr.Count() > 0) {
          var enrGrd = enr.SingleOrDefault();
          enrGrd.Grade = GetLetterGrade(CalculateGradeForSId(uid, classId));
          db.SaveChanges();
        }

        return Json(new { success = true });
      } catch {
        return Json(new { success = false });
      }
    }


    /// <summary>
    /// Returns a JSON array of the classes taught by the specified professor
    /// Each object in the array should have the following fields:
    /// "subject" - The subject abbreviation of the class (such as "CS")
    /// "number" - The course number (such as 5530)
    /// "name" - The course name
    /// "season" - The season part of the semester in which the class is taught
    /// "year" - The year part of the semester in which the class is taught
    /// </summary>
    /// <param name="uid">The professor's uid</param>
    /// <returns>The JSON array</returns>
    public IActionResult GetMyClasses(string uid) {
      var sem = new Regex("(?<Season>[a-zA-Z]*)(?<Year>[0-9]*)");

      var query =
        from depts in db.Departments // TODO: do we need this?
        join courses in db.Courses on depts.Subject equals courses.Subject
        join classes in db.Classes on courses.CourseId equals classes.CourseId
        where classes.PId == uid
        select new {
          subject = courses.Subject,
          number = courses.Number,
          name = courses.Name,
          season = sem.Match(classes.Semester).Groups["Season"].Value,
          year = sem.Match(classes.Semester).Groups["Year"].Value
        };

      return Json(query.ToArray());
    }

    private float CalculateGradeForSId(string uid, ushort classId) {
      // Get all categories and loop through them.
      var categories =
        from dept in db.Departments
        join course in db.Courses on dept.Subject equals course.Subject
        join _class in db.Classes on course.CourseId equals _class.CourseId
        join c in db.AssignmentCategories on _class.ClassId equals c.ClassId
        where c.ClassId == classId
        select c;

      float scaledCategoryTotals = 0;
      foreach (var c in categories) {
        // Ensure we only perform calculations on non-empty assignment categories.
        var asgs =
          from asg in db.Assignments
          where asg.CategoryId == c.CategoryId
          select asg;

        if (asgs.Count() > 0) {
          float currPts = 0;
          float maxPts = 0;

          foreach (var asg in asgs) {
            maxPts += asg.Points; // Add up all the points for every assignment.

            var subm =
              from s in db.Submission
              where s.AssignmentId == asg.AssignmentId && s.SId == uid
              select s.Score;

            // This student submitted this assignment.
            if (subm.Count() > 0) {
              currPts += subm.SingleOrDefault();
            }

            // If they didn't submit, currPts should still be accurate.
          }

          float cPercent = currPts / maxPts;

          float cWgtPercent = ((float)c.Weight / (float)100) * cPercent;

          scaledCategoryTotals += cWgtPercent;
        }
      }

      // Calculate scaling factor.
      float sumOfWeights = (float)(from c in db.AssignmentCategories where c.ClassId == classId select (int)c.Weight).Sum();
      float cScalingFactor = 100 / sumOfWeights;
      float totalPercentage = cScalingFactor * scaledCategoryTotals;

      return totalPercentage;
    }

    private string GetLetterGrade(float percentage) {
      if (percentage >= 0.93) {
        return "A";
      } else if (percentage >= .90) {
        return "A-";
      } else if (percentage >= .87) {
        return "B+";
      } else if (percentage >= .83) {
        return "B";
      } else if (percentage >= .80) {
        return "B-";
      } else if (percentage >= .77) {
        return "C+";
      } else if (percentage >= .73) {
        return "C";
      } else if (percentage >= .70) {
        return "C-";
      } else if (percentage >= .67) {
        return "D+";
      } else if (percentage >= .63) {
        return "D";
      } else if (percentage >= .60) {
        return "D-";
      } else {
        return "E";
      }
    }

    /*******End code to modify********/

  }
}