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
using Microsoft.AspNetCore.Authorization;


public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    public readonly ApplicationDbContext _connectDbContext;
    // private readonly ApplicationDbContext _context;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext connectDbContext, RoleManager<IdentityRole> roleManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _connectDbContext = connectDbContext;
        _roleManager = roleManager;
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
    public async Task<IActionResult> ListRegisterAsync(string searchString, string roleName)
    {
        // 1. ดึงผู้ใช้ทั้งหมดมาก่อน
        var users = _userManager.Users.ToList();
        var userViewModels = new List<RegisterViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new RegisterViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                titlename = user.titlename,
                Roles = roles.ToList() // ส่งเป็น List
            });
        }

        // 2. กรองข้อมูลตามเงื่อนไขที่ส่งมา
        if (!String.IsNullOrEmpty(searchString))
        {
            userViewModels = userViewModels.Where(u =>
                (u.FirstName != null && u.FirstName.Contains(searchString)) ||
                (u.UserName != null && u.UserName.Contains(searchString))
            ).ToList();
        }

        if (!String.IsNullOrEmpty(roleName))
        {
           userViewModels = userViewModels.Where(u => u.Roles != null && u.Roles.Contains(roleName)).ToList();
        }

        // 3. ส่งรายชื่อกลุ่มทั้งหมดไปให้ Dropdown
        var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();
        ViewBag.RolesList = new SelectList(allRoles);

        return View(userViewModels);
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

        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? UserName { get; internal set; }
        public IList<string>? Roles { get; internal set; }
        public object? Tel { get; internal set; }
    }
    [HttpGet]
    public async Task<IActionResult> RegisterEdit(string id)
    {
        var user = _userManager.Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            return NotFound();
        }

        var dt = new RegisterEditViewModel();
        dt.Id = user.Id;
        dt.FirstName = user.FirstName;
        dt.LastName = user.LastName;
        dt.titlename = user.titlename;
        dt.tel = user.PhoneNumber;

        // ----------------------------------------------------
        // ⬇️ (สำคัญมาก) ส่วนที่ต้องเพิ่ม 3 อย่าง
        // ----------------------------------------------------

        // 1. ดึง Role ของผู้ใช้คนนี้ (ส่ง "Manager" ไปให้ a_id)
        var roles = await _userManager.GetRolesAsync(user);
        dt.a_id = roles.FirstOrDefault();

        // 2. แปลง m_id (string "1,2,3") กลับเป็น List<string> (ส่งไปให้ Model.m_id)
        if (!string.IsNullOrEmpty(user.m_id))
        {
            dt.m_id = user.m_id
                .Split(',')                         // 1. ได้ string[] { "1", "2", "3" }
                .Select(idStr => int.Parse(idStr))  // 2. แปลง "1" -> 1, "2" -> 2 ...
                .ToList();                          // 3. ได้ List<int> { 1, 2, 3 }
        }
        else
        {
            dt.m_id = new List<int>(); // 4. ถ้าว่าง ก็ต้องเป็น List<int> ที่ว่าง
        }

        // 3. ส่ง List กลุ่มการแข่งขันไปให้ Dropdown (ส่งไปให้ ViewBag.CompetitionList)
        ViewBag.CompetitionList = new SelectList(
    _connectDbContext.category.Where(x => x.status == "1").ToList(), // 1. รายการตัวเลือก
    "Id",        // 2. ชื่อฟิลด์ Value
    "Name",      // 3. ชื่อฟิลด์ Text
    dt.m_id      // 4. ⬅️⬅️ (สำคัญ) ส่ง "List ของค่าที่ถูกเลือก" เข้าไปตรงนี้
);
        // ----------------------------------------------------

        return View(dt);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterEdit([Bind("Id,titlename,FirstName,LastName,tel,CurrentPassword,NewPassword,ConfirmNewPassword,m_id")] RegisterEditViewModel data)
    {
        // --- 1. ตรวจสอบ Password Validation ---
        if (string.IsNullOrEmpty(data.CurrentPassword) &&
            string.IsNullOrEmpty(data.NewPassword) &&
            string.IsNullOrEmpty(data.ConfirmNewPassword))
        {
            ModelState.Remove("CurrentPassword");
            ModelState.Remove("NewPassword");
            ModelState.Remove("ConfirmNewPassword");
            ModelState.Remove("Email");
        }

        // (เพิ่ม) ถ้าคนบันทึกไม่ใช่ Admin, ลบ m_id ออกจาก Validation
        if (!User.IsInRole("Admin"))
        {
            ModelState.Remove(nameof(data.m_id));
        }

        // --- 2. ตรวจสอบ ModelState "ก่อน" ทำงาน ---
        if (!ModelState.IsValid)
        {
            // (แก้ไข ⬇️) เพิ่ม Argument ที่ 4 (data.m_id)
            ViewBag.CompetitionList = new SelectList(
                _connectDbContext.category.Where(x => x.status == "1").ToList(),
                "Id",
                "Name",
                data.m_id // ⬅️ (ค่าที่ถูกเลือก)
            );
            return View(data);
        }

        // --- 3. ค้นหา "ผู้ใช้ที่กำลังถูกแก้ไข" ---
        var userToUpdate = await _userManager.FindByIdAsync(data.Id);
        if (userToUpdate == null)
        {
            return NotFound();
        }

        // --- 4. จัดการการเปลี่ยนรหัสผ่าน (ถ้ามีการกรอก) ---
        if (!string.IsNullOrEmpty(data.CurrentPassword) && !string.IsNullOrEmpty(data.NewPassword))
        {
            var passResult = await _userManager.ChangePasswordAsync(userToUpdate, data.CurrentPassword, data.NewPassword);

            if (!passResult.Succeeded)
            {
                foreach (var error in passResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                // (แก้ไข ⬇️) เพิ่ม Argument ที่ 4 (data.m_id)
                ViewBag.CompetitionList = new SelectList(
                    _connectDbContext.category.Where(x => x.status == "1").ToList(),
                    "Id",
                    "Name",
                    data.m_id // ⬅️ (ค่าที่ถูกเลือก)
                );
                return View(data);
            }
        }

        // --- 5. อัปเดตข้อมูล Profile ---
        userToUpdate.titlename = data.titlename;
        userToUpdate.FirstName = data.FirstName;
        userToUpdate.LastName = data.LastName;
        userToUpdate.PhoneNumber = data.tel;

        // --- 6. อัปเดต m_id โดยมี Admin Check ---
        var roles = await _userManager.GetRolesAsync(userToUpdate);
        if (roles.Contains("Manager"))
        {
            if (User.IsInRole("Admin"))
            {
                userToUpdate.m_id = (data.m_id != null && data.m_id.Any())
                                    ? string.Join(",", data.m_id)
                                    : string.Empty;
            }
            // (ถ้าไม่ใช่ Admin, ไม่ต้องทำอะไร, m_id จะคงเดิม)
        }

        // --- 7. บันทึก "userToUpdate" ---
        var updateResult = await _userManager.UpdateAsync(userToUpdate);

        if (updateResult.Succeeded)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id == userToUpdate.Id)
            {
                await _signInManager.RefreshSignInAsync(userToUpdate);
            }
            return RedirectToAction("ListRegister", "Account");
        }

        // ถ้า Update ไม่สำเร็จ
        foreach (var error in updateResult.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        // (แก้ไข ⬇️) เพิ่ม Argument ที่ 4 (data.m_id)
        ViewBag.CompetitionList = new SelectList(
            _connectDbContext.category.Where(x => x.status == "1").ToList(),
            "Id",
            "Name",
            data.m_id // ⬅️ (ค่าที่ถูกเลือก)
        );
        return View(data);
    }
    [HttpPost]
    [ValidateAntiForgeryToken] // เพื่อความปลอดภัย
    [Authorize(Roles = "Admin")] // *** สำคัญ: จำกัดสิทธิ์ให้เฉพาะ Admin เท่านั้นที่เรียกใช้เมธอดนี้ได้ ***
    public async Task<IActionResult> ResetPassword(string id)
    {
        // 1. ค้นหาผู้ใช้จาก ID ที่ส่งมา
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // 2. ตรวจสอบอีกครั้งเพื่อความปลอดภัยว่าผู้ใช้เป้าหมายไม่ใช่ Admin
        var isUserAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isUserAdmin)
        {
            // ไม่ควรเกิดขึ้นถ้า View ถูกต้อง แต่เป็นการป้องกันที่ Server-side
            TempData["ErrorMessage"] = "ไม่สามารถรีเซ็ตรหัสผ่านของ Admin ได้";
            return RedirectToAction("ListRegister");
        }

        // 3. กำหนดรหัสผ่านใหม่ (ควรตั้งเป็นค่าที่คาดเดายากและแจ้งให้ผู้ใช้เปลี่ยนในภายหลัง)
        const string defaultPassword = "Kr@123"; // <--- คุณสามารถเปลี่ยนค่านี้ได้

        // 4. สร้าง Token สำหรับการรีเซ็ตรหัสผ่าน
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // 5. ทำการรีเซ็ตรหัสผ่านด้วย Token และรหัสผ่านใหม่
        var result = await _userManager.ResetPasswordAsync(user, token, defaultPassword);

        if (result.Succeeded)
        {
            // ส่งข้อความแจ้งเตือนว่าสำเร็จ
            TempData["SuccessMessage"] = $"รีเซ็ตรหัสผ่านของ {user.UserName} เป็น '{defaultPassword}' เรียบร้อยแล้ว";
        }
        else
        {
            // หากเกิดข้อผิดพลาด ให้รวบรวม Error
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            TempData["ErrorMessage"] = $"เกิดข้อผิดพลาดในการรีเซ็ตรหัสผ่าน: {errors}";
        }

        // 6. กลับไปที่หน้ารายชื่อผู้ใช้
        return RedirectToAction("ListRegister");
    }
    [HttpPost] // 1. ใช้ [HttpPost] เสมอสำหรับการลบข้อมูล
    [ValidateAntiForgeryToken] // 2. ป้องกันการโจมตีแบบ CSRF
    [Authorize(Roles = "Admin")] // 3. จำกัดสิทธิ์เฉพาะ Admin เท่านั้น
    public async Task<IActionResult> Deletestudent(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        // 4. ค้นหาผู้ใช้ที่จะลบ
        var userToDelete = await _userManager.FindByIdAsync(id);
        if (userToDelete == null)
        {
            TempData["ErrorMessage"] = "ไม่พบผู้ใช้ที่ต้องการลบ";
            return RedirectToAction("ListRegister");
        }

        // 5. [สำคัญ] ตรวจสอบว่า Admin ไม่ได้ลบตัวเอง
        var currentAdminId = _userManager.GetUserId(User);
        if (userToDelete.Id == currentAdminId)
        {
            TempData["ErrorMessage"] = "ไม่สามารถลบผู้ดูแลระบบ (Admin) ที่กำลังใช้งานอยู่ได้";
            return RedirectToAction("ListRegister");
        }

        // 6. [สำคัญ] ตรวจสอบว่าไม่ได้พยายามลบ Admin คนอื่น
        // (เหมือนกับตรรกะใน ResetPassword ของคุณ)
        var isUserAdmin = await _userManager.IsInRoleAsync(userToDelete, "Admin");
        if (isUserAdmin)
        {
            TempData["ErrorMessage"] = "ไม่สามารถลบผู้ใช้ที่เป็น Admin ได้";
            return RedirectToAction("ListRegister");
        }

        // 7. ทำการลบผู้ใช้
        var result = await _userManager.DeleteAsync(userToDelete);

        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"ลบผู้ใช้ {userToDelete.UserName} เรียบร้อยแล้ว";
        }
        else
        {
            // รวบรวมข้อผิดพลาด
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            TempData["ErrorMessage"] = $"เกิดข้อผิดพลาดในการลบผู้ใช้: {errors}";
        }

        // 8. กลับไปที่หน้ารายชื่อ
        return RedirectToAction("ListRegister");
    }
}