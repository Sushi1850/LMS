using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using LMS.Models.LMSModels;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.Controllers {
  public class CommonController : Controller {

    /*******Begin code to modify********/

    // TODO: Uncomment and change 'X' after you have scaffoled

    protected Team24LMSContext db;

    public CommonController() {
      db = new Team24LMSContext();
    }

    /*
     * WARNING: This is the quick and easy way to make the controller
     *          use a different LibraryContext - good enough for our purposes.
     *          The "right" way is through Dependency Injection via the constructor 
     *          (look this up if interested).
    */

    // TODO: Uncomment and change 'X' after you have scaffoled
    public void UseLMSContext(Team24LMSContext ctx) {
      db = ctx;
    }

    protected override void Dispose(bool disposing) {
      if (disposing) {
        db.Dispose();
      }
      base.Dispose(disposing);
    }



    /// <summary>
    /// Retreive a JSON array of all departments from the database.
    /// Each object in the array should have a field called "name" and "subject",
    /// where "name" is the department name and "subject" is the subject abbreviation.
    /// </summary>
    /// <returns>The JSON array</returns>
    public IActionResult GetDepartments() {
      var query =
        from department in db.Departments
        select new {
          Name = department.Name,
          Subject = department.Subject,
        };

      return Json(query.ToArray());
    }



    /// <summary>
    /// Returns a JSON array representing the course catalog.
    /// Each object in the array should have the following fields:
    /// "subject": The subject abbreviation, (e.g. "CS")
    /// "dname": The department name, as in "Computer Science"
    /// "courses": An array of JSON objects representing the courses in the department.
    ///            Each field in this inner-array should have the following fields:
    ///            "number": The course number (e.g. 5530)
    ///            "cname": The course name (e.g. "Database Systems")
    /// </summary>
    /// <returns>The JSON array</returns>
    public IActionResult GetCatalog() {
      var query =
        from department in db.Departments
        select new {
          subject = department.Subject,
          dname = department.Name,
          courses = (
            from course in db.Courses
            where course.Subject == department.Subject
            select new {
              number = course.Number,
              cname = course.Name,
            }
          ),
        };
      return Json(query.ToArray());
    }

    /// <summary>
    /// Returns a JSON array of all class offerings of a specific course.
    /// Each object in the array should have the following fields:
    /// "season": the season part of the semester, such as "Fall"
    /// "year": the year part of the semester
    /// "location": the location of the class
    /// "start": the start time in format "hh:mm:ss"
    /// "end": the end time in format "hh:mm:ss"
    /// "fname": the first name of the professor
    /// "lname": the last name of the professor
    /// </summary>
    /// <param name="subject">The subject abbreviation, as in "CS"</param>
    /// <param name="number">The course number, as in 5530</param>
    /// <returns>The JSON array</returns>
    public IActionResult GetClassOfferings(string subject, int number) {
      // https://stackoverflow.com/questions/3720012/regular-expression-to-split-string-and-number
      var sem = new Regex("(?<Season>[a-zA-Z]*)(?<Year>[0-9]*)");

      var query =
        from classes in db.Classes
        join courses in db.Courses on classes.CourseId equals courses.CourseId
        join professors in db.Professors on classes.PId equals professors.UId
        where courses.Subject == subject && courses.Number == number
        select new {
          season = sem.Match(classes.Semester).Groups["Season"].Value,
          year = sem.Match(classes.Semester).Groups["Year"].Value,
          location = classes.Location,
          start = classes.StartTime,
          end = classes.EndTime,
          fname = professors.FirstName,
          lname = professors.LastName,
        };
      return Json(query.ToArray());
    }

    /// <summary>
    /// This method does NOT return JSON. It returns plain text (containing html).
    /// Use "return Content(...)" to return plain text.
    /// Returns the contents of an assignment.
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <param name="category">The name of the assignment category in the class</param>
    /// <param name="asgname">The name of the assignment in the category</param>
    /// <returns>The assignment contents</returns>
    public IActionResult GetAssignmentContents(string subject, int num, string season, int year, string category, string asgname) {
      var query =
        from course in db.Courses
        join _class in db.Classes on course.CourseId equals _class.CourseId
        join aC in db.AssignmentCategories on _class.ClassId equals aC.ClassId
        join assignment in db.Assignments on aC.CategoryId equals assignment.CategoryId
        where course.Subject == subject && course.Number == num && _class.Semester == (season + year)
        && aC.Name == category && assignment.Name == asgname
        select assignment.Contents;
      return Content(query.FirstOrDefault());
    }


    /// <summary>
    /// This method does NOT return JSON. It returns plain text (containing html).
    /// Use "return Content(...)" to return plain text.
    /// Returns the contents of an assignment submission.
    /// Returns the empty string ("") if there is no submission.
    /// </summary>
    /// <param name="subject">The course subject abbreviation</param>
    /// <param name="num">The course number</param>
    /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
    /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
    /// <param name="category">The name of the assignment category in the class</param>
    /// <param name="asgname">The name of the assignment in the category</param>
    /// <param name="uid">The uid of the student who submitted it</param>
    /// <returns>The submission text</returns>
    public IActionResult GetSubmissionText(string subject, int num, string season, int year, string category, string asgname, string uid) {
      var query =
        from course in db.Courses
        join _class in db.Classes on course.CourseId equals _class.CourseId
        join aC in db.AssignmentCategories on _class.ClassId equals aC.ClassId
        join assignment in db.Assignments on aC.CategoryId equals assignment.CategoryId
        join submission in db.Submission on assignment.AssignmentId equals submission.AssignmentId
        where course.Subject == subject && course.Number == num && _class.Semester == (season + year)
        && aC.Name == category && assignment.Name == asgname && submission.SId == uid
        select submission.Contents;
      return Content(query.FirstOrDefault());
    }


    /// <summary>
    /// Gets information about a user as a single JSON object.
    /// The object should have the following fields:
    /// "fname": the user's first name
    /// "lname": the user's last name
    /// "uid": the user's uid
    /// "department": (professors and students only) the name (such as "Computer Science") of the department for the user. 
    ///               If the user is a Professor, this is the department they work in.
    ///               If the user is a Student, this is the department they major in.    
    ///               If the user is an Administrator, this field is not present in the returned JSON
    /// </summary>
    /// <param name="uid">The ID of the user</param>
    /// <returns>
    /// The user JSON object 
    /// or an object containing {success: false} if the user doesn't exist
    /// </returns>
    public IActionResult GetUser(string uid) {
      var studentQuery =
        from student in db.Students
        where student.UId == uid
        select new {
          fname = student.FirstName,
          lname = student.LastName,
          uid = student.UId,
          department = student.Subject
        };

      var professorQuery =
        from professor in db.Professors
        where professor.UId == uid
        select new {
          fname = professor.FirstName,
          lname = professor.LastName,
          uid = professor.UId,
          department = professor.Subject
        };

      var administratorQuery =
        from administrator in db.Administrators
        where administrator.UId == uid
        select new {
          fname = administrator.FirstName,
          lname = administrator.LastName,
          uid = administrator.UId,
        };

      if (studentQuery.Count() > 0) {
        return Json(studentQuery.FirstOrDefault());
      }

      if (professorQuery.Count() > 0) {
        return Json(professorQuery.FirstOrDefault());
      }

      if (administratorQuery.Count() > 0) {
        return Json(administratorQuery.FirstOrDefault());
      }

      return Json(new { success = false });
    }


    /*******End code to modify********/

  }
}