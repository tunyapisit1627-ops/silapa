using Microsoft.VisualBasic.CompilerServices;
using System.Collections;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Silapa.Controllers;
using Silapa.Models;
using Syncfusion.EJ2.Base;
using Newtonsoft.Json.Converters;
using System.Dynamic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;


public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    public readonly ApplicationDbContext _connectDbContext;
    // private readonly ApplicationDbContext _context;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext connectDbContext)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _connectDbContext = connectDbContext;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByNameAsync(model.Username);
        // var sql=await _connectDbContext.FindAsync(model.)
        if (user != null)
        {
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (User.IsInRole("Member"))
                {
                    var data = await _connectDbContext.school.Where(x => x.Id == user.s_id).FirstOrDefaultAsync();
                    if (data != null)
                    {
                        if (data.a_id == 0)
                        {
                            return RedirectToAction("frmschoolAdd", "Admin", new { id = user.s_id });
                        }
                    }
                }
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }
    [HttpGet]
    public IActionResult Register()
    {
        // ตัวอย่างการสร้างรายการ ParentList สำหรับ DropDownList
        var parentList = new List<SelectListItem>
        {
            new SelectListItem { Value = "Admin", Text = "Admin" },
            new SelectListItem { Value = "Manager", Text = "Manager" },
            new SelectListItem { Value = "Member", Text = "Member" }
        };
        ViewBag.ParentList = new SelectList(parentList, "Value", "Text");
        // ตัวอย่างการสร้างรายการ CompetitionList สำหรับ DropDownList
        ViewBag.CompetitionList = new SelectList(_connectDbContext.category.Where(x => x.status == "1").ToList(), "Id", "Name");
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            string m_id = "";
            if (model.a_id == "Manager")
            {
                m_id = string.Join(",", model.m_id);
            }

            var user = new ApplicationUser { UserName = model.UserName, Email = model.Email, titlename = model.titlename, FirstName = model.FirstName, LastName = model.LastName, m_id = m_id, s_id = model.s_id };
            //var user=new IdentityUser{ UserName = model.Email, Email = model.Email};
            


            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                //await _signInManager.SignInAsync(user, isPersistent: false);
                await _userManager.AddToRoleAsync(user, model.a_id);
                return RedirectToAction("ListRegister", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                // ตัวอย่างการสร้างรายการ ParentList สำหรับ DropDownList
                var parentList = new List<SelectListItem>
        {
            new SelectListItem { Value = "Admin", Text = "Admin" },
            new SelectListItem { Value = "Manager", Text = "Manager" },
            new SelectListItem { Value = "Member", Text = "Member" }
        };
                ViewBag.ParentList = new SelectList(parentList, "Value", "Text");
                // ตัวอย่างการสร้างรายการ CompetitionList สำหรับ DropDownList
                ViewBag.CompetitionList = new SelectList(_connectDbContext.category.Where(x => x.status == "1").ToList(), "Id", "Name");
            }
        }

        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
    public async Task<IActionResult> ListRegisterAsync()
    {
        /* var data = _userManager.Users.Select(u => new RegisterViewModel
         {
             Id =u.Id.ToString(),
             UserName = u.UserName,
             titlename = u.titlename,
             FirstName = u.FirstName,
             LastName = u.LastName,
             Email = u.Email,
             Roles = _userManager.GetRolesAsync(u).Result // ดึงบทบาทของผู้ใช้แต่ละคน
         }).ToList(); */
        var users = _userManager.Users.ToList(); // ดึงข้อมูลผู้ใช้ทั้งหมดมาเป็นลิสต์ก่อน

        var data = new List<RegisterViewModel>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u); // ใช้ await เพื่อดึงบทบาทแบบ async

            var registerViewModel = new RegisterViewModel
            {
                Id = u.Id.ToString(),
                UserName = u.UserName,
                titlename = u.titlename,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                Roles = roles // กำหนดบทบาทที่ดึงมาให้กับ ViewModel
            };

            data.Add(registerViewModel); // เพิ่มข้อมูลในลิสต์
        }


        return View(data);
        //return View(model);
    }
    public IActionResult UrlDatasource([FromBody] DataManagerRequest dm)
    {
        var dt = _userManager.Users;
        IEnumerable<object> DataSource = dt; //Here dt is the dataTable
        DataOperations operation = new DataOperations();
        if (dm.Search != null && dm.Search.Count > 0)
        {
            DataSource = operation.PerformSearching(DataSource, dm.Search);  //Search
        }
        if (dm.Sorted != null && dm.Sorted.Count > 0) //Sorting
        {
            DataSource = operation.PerformSorting(DataSource, dm.Sorted);
        }
        if (dm.Where != null && dm.Where.Count > 0) //Filtering
        {
            DataSource = operation.PerformFiltering(DataSource, dm.Where, dm.Where[0].Operator);
        }
        List<string> str = new List<string>();
        if (dm.Aggregates != null)
        {
            for (var i = 0; i < dm.Aggregates.Count; i++)
                str.Add(dm.Aggregates[i].Field);
        }
        IEnumerable aggregate = operation.PerformSelect(DataSource, str);
        int count = DataSource.Cast<object>().Count();
        if (dm.Skip != 0)
        {
            DataSource = operation.PerformSkip(DataSource, dm.Skip);   //Paging
        }
        if (dm.Take != 0)
        {
            DataSource = operation.PerformTake(DataSource, dm.Take);
        }
        return dm.RequiresCounts ? Json(new { result = DataSource, count = count, aggregate = aggregate }) : Json(DataSource);
    }
    public ActionResult Update(ExpandoObject value)
    {
        //Here you can Update a record based on your scenario
        return Json(value);
    }

    //private static List<RegisterViewModel> dataStore = new List<RegisterViewModel>();
    [HttpPost]
    public ActionResult Insert([FromBody] RegisterViewModel1 value)
    {
        //var id = data.GetProperty("Id").GetInt32();
        return Json(value);
    }


    public ActionResult Delete(int key)
    {
        //Here you can Delete a record based on your scenario
        return Json(key);
    }
    public ActionResult Editpartial(RegisterViewModel value)
    {
        var order = _userManager.Users;
        ViewBag.datasource = order;
        return PartialView("_DialogEditpartial", value);
    }
    public ActionResult AddPartial()
    {
        var order = _userManager.Users;
        ViewBag.datasource = order;
        return PartialView("_DialogAddpartial");
    }
    public class RegisterViewModel1
    {
        internal string? titlename;

        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string? UserName { get; internal set; }
        public IList<string> Roles { get; internal set; }
        public object Tel { get; internal set; }
    }
    [HttpGet]
    public async Task<IActionResult> RegisterEdit(string id)
    {
        var user = _userManager.Users.FirstOrDefault(u => u.Id == id);
        var dt = new RegisterViewModel();

        dt.Id = user.Id;
        dt.FirstName = user.FirstName;
        dt.LastName = user.LastName;
        dt.Email = user.Email;
        dt.UserName = user.UserName;
        dt.titlename = user.titlename;
        dt.tel = user.PhoneNumber;


        return View(dt); ;
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterEdit([Bind("Id,titlename,FirstName,LastName,tel,CurrentPassword,NewPassword,ConfirmNewPassword,m_id")] RegisterViewModel data)
    {
        if (!ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            if (data.CurrentPassword != null && data.ConfirmNewPassword != null && data.NewPassword != null)
            {
                var m_id = "";
                if (User.IsInRole("Manager"))
                {
                     m_id = string.Join(",",user.m_id.ToString());
                }

                user.titlename = data.titlename;
                user.FirstName = data.LastName;
                user.LastName = data.LastName;
                user.PhoneNumber = data.tel;
                user.m_id = m_id;
                await _userManager.UpdateAsync(user);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account"); // ส่งผู้ใช้ไปหน้า Login หากไม่มีการล็อกอิน
                }
                // เปลี่ยนรหัสผ่าน
                var result = await _userManager.ChangePasswordAsync(user, data.CurrentPassword, data.NewPassword);
                // หากการเปลี่ยนรหัสผ่านล้มเหลว ให้แสดงข้อผิดพลาด
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                if (result.Succeeded)
                {
                    // รหัสผ่านเปลี่ยนสำเร็จ
                    await _signInManager.RefreshSignInAsync(user); // อัปเดตสถานะการล็อกอินของผู้ใช้
                    return RedirectToAction("Index", "Home"); // ไปยังหน้าแสดงผลสำเร็จ
                }
                else
                {
                    return View(data);
                }
            }
            if (User.IsInRole("Member"))
            {
                await _connectDbContext.school.Where(x => x.Id == user.s_id).ExecuteUpdateAsync(x => x.SetProperty(
                       i => i.titlename, data.titlename)
                       .SetProperty(i => i.FirstName, data.FirstName)
                       .SetProperty(i => i.LastName, data.LastName)
                       .SetProperty(i => i.tel, data.tel)
                   );
            }
            else if (User.IsInRole("Manager"))
            {
                string m_id = string.Join(",", data.m_id);
                user.titlename = data.titlename;
                user.FirstName = data.FirstName;
                user.LastName = data.LastName;
                user.PhoneNumber = data.tel;
                user.m_id = m_id;
                await _userManager.UpdateAsync(user);

            }

        }
        return RedirectToAction("Index", "Home");
    }

}