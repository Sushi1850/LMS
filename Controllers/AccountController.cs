﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LMS.Models;
using LMS.Models.AccountViewModels;
using LMS.Services;
using LMS.Models.LMSModels;

namespace LMS.Controllers {
  [Authorize]
  [Route("[controller]/[action]")]
  public class AccountController : CommonController {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    //private readonly IEmailSender _emailSender;
    private readonly ILogger _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender,
        ILogger<AccountController> logger) {
      _userManager = userManager;
      _signInManager = signInManager;
      //_emailSender = emailSender;
      _logger = logger;
    }


    [TempData]
    public string ErrorMessage { get; set; }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string returnUrl = null) {
      // Clear the existing external cookie to ensure a clean login process
      await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

      ViewData["ReturnUrl"] = returnUrl;
      return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null) {
      ViewData["ReturnUrl"] = returnUrl;
      if (ModelState.IsValid) {
        // This doesn't count login failures towards account lockout
        // To enable password failures to trigger account lockout, set lockoutOnFailure: true
        var result = await _signInManager.PasswordSignInAsync(model.UID, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded) {
          _logger.LogInformation("User logged in.");
          //return View("../Home/StudentHome");
          return RedirectToLocal(returnUrl);
        }
        if (result.RequiresTwoFactor) {
          return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
        }
        if (result.IsLockedOut) {
          _logger.LogWarning("User account locked out.");
          return RedirectToAction(nameof(Lockout));
        } else {
          ModelState.AddModelError(string.Empty, "Invalid login attempt.");
          return View(model);
        }
      }

      // If we got this far, something failed, redisplay form
      return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null) {
      // Ensure the user has gone through the username & password screen first
      var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

      if (user == null) {
        throw new ApplicationException($"Unable to load two-factor authentication user.");
      }

      var model = new LoginWith2faViewModel { RememberMe = rememberMe };
      ViewData["ReturnUrl"] = returnUrl;

      return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null) {
      if (!ModelState.IsValid) {
        return View(model);
      }

      var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
      if (user == null) {
        throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
      }

      var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

      var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

      if (result.Succeeded) {
        _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
        return RedirectToLocal(returnUrl);
      } else if (result.IsLockedOut) {
        _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
        return RedirectToAction(nameof(Lockout));
      } else {
        _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
        ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
        return View();
      }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null) {
      // Ensure the user has gone through the username & password screen first
      var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
      if (user == null) {
        throw new ApplicationException($"Unable to load two-factor authentication user.");
      }

      ViewData["ReturnUrl"] = returnUrl;

      return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null) {
      if (!ModelState.IsValid) {
        return View(model);
      }

      var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
      if (user == null) {
        throw new ApplicationException($"Unable to load two-factor authentication user.");
      }

      var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

      var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

      if (result.Succeeded) {
        _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
        return RedirectToLocal(returnUrl);
      }
      if (result.IsLockedOut) {
        _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
        return RedirectToAction(nameof(Lockout));
      } else {
        _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
        ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
        return View();
      }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout() {
      return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string returnUrl = null) {
      ViewData["ReturnUrl"] = returnUrl;
      var model = new RegisterViewModel();
      model.Role = "Student";

      dynamic departments = (GetDepartments() as JsonResult).Value;

      model.Department = "";
      List<SelectListItem> depts = new List<SelectListItem>();

      foreach (var x in departments) {
        depts.Add(new SelectListItem { Value = x.Subject, Text = x.Subject + ": " + x.Name });
      }

      model.Departments = depts;

      return View(model);
    }


    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null) {
      ViewData["ReturnUrl"] = returnUrl;
      if (ModelState.IsValid) {
        // invoke the student's controller to generate a new uID, pass it in below as UserName and Email
        string uID = CreateNewUser(model.FirstName, model.LastName, model.DOB, model.Department, model.Role);
        //var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var user = new ApplicationUser { UserName = uID, Email = uID };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded) {
          await _userManager.AddToRoleAsync(user, model.Role);

          _logger.LogInformation("User created a new account with password.");

          //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
          //var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
          //await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl);

          await _signInManager.SignInAsync(user, isPersistent: false);
          _logger.LogInformation("User created a new account with password.");

          //if(model.Role == "Student")
          //{
          //  return View("../Home/StudentHome");
          //}

          return RedirectToLocal(returnUrl);
        }
        AddErrors(result);
      }

      // If we got this far, something failed, redisplay form
      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() {
      await _signInManager.SignOutAsync();
      _logger.LogInformation("User logged out.");
      return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string returnUrl = null) {
      // Request a redirect to the external login provider.
      var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
      var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
      return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null) {
      if (remoteError != null) {
        ErrorMessage = $"Error from external provider: {remoteError}";
        return RedirectToAction(nameof(Login));
      }
      var info = await _signInManager.GetExternalLoginInfoAsync();
      if (info == null) {
        return RedirectToAction(nameof(Login));
      }

      // Sign in the user with this external login provider if the user already has a login.
      var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
      if (result.Succeeded) {
        _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
        return RedirectToLocal(returnUrl);
      }
      if (result.IsLockedOut) {
        return RedirectToAction(nameof(Lockout));
      } else {
        // If the user does not have an account, then ask the user to create an account.
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["LoginProvider"] = info.LoginProvider;
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        return View("ExternalLogin", new ExternalLoginViewModel { Email = email });
      }
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null) {
      if (ModelState.IsValid) {
        // Get the information about the user from the external login provider
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null) {
          throw new ApplicationException("Error loading external login information during confirmation.");
        }
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await _userManager.CreateAsync(user);
        if (result.Succeeded) {
          result = await _userManager.AddLoginAsync(user, info);
          if (result.Succeeded) {
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
            return RedirectToLocal(returnUrl);
          }
        }
        AddErrors(result);
      }

      ViewData["ReturnUrl"] = returnUrl;
      return View(nameof(ExternalLogin), model);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string code) {
      if (userId == null || code == null) {
        return RedirectToAction(nameof(HomeController.Index), "Home");
      }
      var user = await _userManager.FindByIdAsync(userId);
      if (user == null) {
        throw new ApplicationException($"Unable to load user with ID '{userId}'.");
      }
      var result = await _userManager.ConfirmEmailAsync(user, code);
      return View(result.Succeeded ? "ConfirmEmail" : "Error");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() {
      return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model) {
      // resetting password not supported
      /*
      if (ModelState.IsValid)
      {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
          // Don't reveal that the user does not exist or is not confirmed
          return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        // For more information on how to enable account confirmation and password reset please
        // visit https://go.microsoft.com/fwlink/?LinkID=532713
        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
        //await _emailSender.SendEmailAsync(model.Email, "Reset Password",
        //  $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
      }
      */
      // If we got this far, something failed, redisplay form
      return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation() {
      return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string code = null) {
      if (code == null) {
        throw new ApplicationException("A code must be supplied for password reset.");
      }
      var model = new ResetPasswordViewModel { Code = code };
      return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model) {
      if (!ModelState.IsValid) {
        return View(model);
      }
      var user = await _userManager.FindByEmailAsync(model.Email);
      if (user == null) {
        // Don't reveal that the user does not exist
        return RedirectToAction(nameof(ResetPasswordConfirmation));
      }
      var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
      if (result.Succeeded) {
        return RedirectToAction(nameof(ResetPasswordConfirmation));
      }
      AddErrors(result);
      return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation() {
      return View();
    }


    [HttpGet]
    public IActionResult AccessDenied() {
      return View();
    }


    /*******Begin code to modify********/

    /// <summary>
    /// Create a new user of the LMS with the specified information.
    /// Assigns the user a unique uID consisting of a 'u' followed by 7 digits.
    /// </summary>
    /// <param name="fName">First Name</param>
    /// <param name="lName">Last Name</param>
    /// <param name="DOB">Date of Birth</param>
    /// <param name="SubjectAbbrev">The department the user belongs to (professors and students only)</param>
    /// <param name="role">The user's role: one of "Administrator", "Professor", "Student"</param> 
    /// <returns>A unique uID that is not be used by anyone else</returns>
    public string CreateNewUser(string fName, string lName, DateTime DOB, string SubjectAbbrev, string role) {
      switch (role) {
        case "Administrator":
          var administrator = new Administrators {
            FirstName = fName,
            LastName = lName,
            Dob = DOB,
            UId = getID()
          };

          db.Administrators.Add(administrator);
          db.SaveChanges();

          var q =
            from a in db.Administrators
            where a.FirstName == fName && a.LastName == lName && a.Dob == DOB
            select a.UId;

          return q.FirstOrDefault();
        case "Professor":
          var professor = new Professors {
            FirstName = fName,
            LastName = lName,
            Dob = DOB,
            Subject = SubjectAbbrev,
            UId = getID()
          };

          db.Professors.Add(professor);
          db.SaveChanges();

          var q2 =
            from a in db.Professors
            where a.FirstName == fName && a.LastName == lName && a.Dob == DOB && a.Subject == SubjectAbbrev
            select a.UId;

          return q2.FirstOrDefault();
        case "Student":
          var student = new Students {
            FirstName = fName,
            LastName = lName,
            Dob = DOB,
            Subject = SubjectAbbrev,
            UId = getID()
          };

          db.Students.Add(student);
          db.SaveChanges();

          var q3 =
            from a in db.Students
            where a.FirstName == fName && a.LastName == lName && a.Dob == DOB && a.Subject == SubjectAbbrev
            select a.UId;

          return q3.FirstOrDefault();
      }
      return "";
    }


    private string getID() {
      string adminHighestID, profHighestID, studentHighestID = "";
      int aNumID, pNumID, sNumID;

      var q1 = (from a in db.Administrators select a).Count();
      var q2 = (from p in db.Professors select p).Count();
      var q3 = (from s in db.Students select s).Count();

      if (q1 == 0 && q2 == 0 && q3 == 0)
        return "u0000001";

      var adminHighest = (from a in db.Administrators orderby a.UId descending select a.UId).Take(1);
      var profHighest = (from p in db.Professors orderby p.UId descending select p.UId).Take(1);
      var studentHighest = (from s in db.Students orderby s.UId descending select s.UId).Take(1);

      adminHighestID = adminHighest.FirstOrDefault();
      profHighestID = profHighest.FirstOrDefault();
      studentHighestID = studentHighest.FirstOrDefault();

      if (adminHighestID == null) aNumID = 0000000;
      else aNumID = int.Parse(adminHighestID.Substring(1, 7));

      if (profHighestID == null) pNumID = 0000000;
      else pNumID = int.Parse(profHighestID.Substring(1, 7));

      if (studentHighestID == null) sNumID = 0000000;
      else sNumID = int.Parse(studentHighestID.Substring(1, 7));


      //Admin ID is the largest
      if (aNumID > pNumID && aNumID > sNumID) {
        aNumID++;
        return "u" + (aNumID).ToString("D7");

      } else if (pNumID > aNumID && pNumID > sNumID) { //Professor ID is the largest
        pNumID++;
        return "u" + (pNumID).ToString("D7");

      } else { //Student ID is the largest
        sNumID++;
        return "u" + (sNumID).ToString("D7");

      }
    }

    /*******End code to modify********/




    #region Helpers

    private void AddErrors(IdentityResult result) {
      foreach (var error in result.Errors) {
        ModelState.AddModelError(string.Empty, error.Description);
      }
    }

    private IActionResult RedirectToLocal(string returnUrl) {
      if (Url.IsLocalUrl(returnUrl)) {
        return Redirect(returnUrl);
      } else {
        return RedirectToAction(nameof(HomeController.Index), "Home");
      }
    }

    #endregion
  }
}
