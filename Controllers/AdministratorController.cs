﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.Controllers {
  [Authorize(Roles = "Administrator")]
  public class AdministratorController : CommonController {
    public IActionResult Index() {
      return View();
    }

    public IActionResult Department(string subject) {
      ViewData["subject"] = subject;
      return View();
    }

    public IActionResult Course(string subject, string num) {
      ViewData["subject"] = subject;
      ViewData["num"] = num;
      return View();
    }

    /*******Begin code to modify********/

    /// <summary>
    /// Returns a JSON array of all the courses in the given department.
    /// Each object in the array should have the following fields:
    /// "number" - The course number (as in 5530)
    /// "name" - The course name (as in "Database Systems")
    /// </summary>
    /// <param name="subject">The department subject abbreviation (as in "CS")</param>
    /// <returns>The JSON result</returns>
    public IActionResult GetCourses(string subject) {
      var query =
        from courses in db.Courses
        where courses.Subject == subject
        select new {
          number = courses.Number,
          name = courses.Name,
        };
      return Json(query.ToArray());
    }





    /// <summary>
    /// Returns a JSON array of all the professors working in a given department.
    /// Each object in the array should have the following fields:
    /// "lname" - The professor's last name
    /// "fname" - The professor's first name
    /// "uid" - The professor's uid
    /// </summary>
    /// <param name="subject">The department subject abbreviation</param>
    /// <returns>The JSON result</returns>
    public IActionResult GetProfessors(string subject) {
      var query =
        from professors in db.Professors
        where professors.Subject == subject
        select new {
          lname = professors.LastName,
          fname = professors.FirstName,
          uid = professors.UId
        };

      return Json(query.ToArray());
    }



    /// <summary>
    /// Creates a course.
    /// A course is uniquely identified by its number + the subject to which it belongs
    /// </summary>
    /// <param name="subject">The subject abbreviation for the department in which the course will be added</param>
    /// <param name="number">The course number</param>
    /// <param name="name">The course name</param>
    /// <returns>A JSON object containing {success = true/false},
    /// false if the Course already exists.</returns>
    public IActionResult CreateCourse(string subject, int number, string name) {
      try {
        var course = new Courses {
          Subject = subject,
          Number = (ushort)number,
          Name = name
        };

        db.Courses.Add(course);
        db.SaveChanges();
        return Json(new { success = true });
      } catch (Exception) {
        return Json(new { success = false });
      }
    }



    /// <summary>
    /// Creates a class offering of a given course.
    /// </summary>
    /// <param name="subject">The department subject abbreviation</param>
    /// <param name="number">The course number</param>
    /// <param name="season">The season part of the semester</param>
    /// <param name="year">The year part of the semester</param>
    /// <param name="start">The start time</param>
    /// <param name="end">The end time</param>
    /// <param name="location">The location</param>
    /// <param name="instructor">The uid of the professor</param>
    /// <returns>A JSON object containing {success = true/false}. 
    /// false if another class occupies the same location during any time 
    /// within the start-end range in the same semester, or if there is already
    /// a Class offering of the same Course in the same Semester.</returns>
    public IActionResult CreateClass(string subject, int number, string season, int year, DateTime start, DateTime end, string location, string instructor) {
      if (IsConflicted(start, end, location, season, year))
        return Json(new { success = false });

      if (IsDuplicate(subject, number, season, year))
        return Json(new { success = false });

      try {
        var query =
          from courses in db.Courses
          where courses.Subject == subject && courses.Number == number
          select courses.CourseId;

        if (query.Count() > 0) {
          var c = new Classes {
            CourseId = query.First(),
            PId = instructor,
            Semester = season + year,
            Location = location,
            StartTime = start.TimeOfDay,
            EndTime = end.TimeOfDay
          };

          db.Classes.Add(c);
          db.SaveChanges();

          return Json(new { success = true });
        } else {
          return Json(new { success = false });
        }
      } catch {
        return Json(new { success = false });
      }
    }

    private bool IsConflicted(DateTime start, DateTime end, string location, string season, int year) {
      try {
        var query =
          from classes in db.Classes
          where classes.Location == location && classes.Semester == (season + year)
          select classes;

        foreach (var c in query) {
          if (c.StartTime.Ticks >= start.TimeOfDay.Ticks && c.StartTime.Ticks <= end.TimeOfDay.Ticks) {
            return true;
          }

          if (c.EndTime.Ticks >= start.TimeOfDay.Ticks && c.EndTime.Ticks <= end.TimeOfDay.Ticks) {
            return true;
          }
        }

        return false;
      } catch (Exception) {
        return true;
      }
    }

    private bool IsDuplicate(string subject, int number, string season, int year) {
      try {
        var courseIdQuery =
          from courses in db.Courses
          where courses.Subject == subject && courses.Number == number
          select courses.CourseId;

        if (courseIdQuery.Any()) {
          var query =
            from classes in db.Classes
            where classes.Semester == (season + year) && classes.CourseId == courseIdQuery.First()
            select classes;

          if (query.Any()) {
            return true;
          }
        }

        return false;
      } catch {
        return true;
      }
    }


    /*******End code to modify********/

  }
}