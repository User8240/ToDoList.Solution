using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using ToDoList.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Security.Claims;

namespace ToDoList.Controllers
{
  //This allows access to the ItemsController only if a user is logged in
  //the entirety of the controller is now shielded from unauthorized users
  [Authorize]
  public class ItemsController : Controller
  {
    private readonly ToDoListContext _db;
    //We need an instance of UserManager to work with signed-in users
    private readonly UserManager<ApplicationUser> _userManager;
    //include a constructor to instantiate private readonly instances of the database and the UserManager
    public ItemsController(UserManager<ApplicationUser> userManager, ToDoListContext db)
    {
      _userManager = userManager;
      _db = db;
    }
    //Because the action is asynchronous, it also returns a Task containing an action result
    public async Task<ActionResult> Index()
    { //locate the unique identifier for the currently-logged-in user and assign it the variable name userId
      //FOR "var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;" LINE BELOW: 
      // - this refers to the ItemController itself
      // - FindFirst() is a method that locates the first record that meets the provided criteria
      // - FindFirst() is a method that locates the first record that meets the provided criteria
      // - NameIdentifier is a property that refers to an Entity's unique ID

      // - For the ? operator: This is called an existential operator
      //   It states that we should only call the property to the right
      //   of the ? if the method to the left of the ? doesn't return null
      //   So below: 
      //   if this.User.FindFirst(ClaimTypes.NameIdentifier) returns null, 
      //   don't call the property to the right of the existential operator. 
      //   However, if it doesn't return null, it retrieves Value property.

      //   if we successfully locate the NameIdentifier of the current user, 
      //   we'll call Value to retrieve the actual unique identifier value.
      var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      // Once we have the userId value, we're ready to call our async method:
      // First we call the UserManager service that we've injected into this controller
      // We provide the userId we just located as an argument to FindByIdAsync()
      // We include the await keyword so the code will wait for Identity to locate the correct user before moving on
      var currentUser = await _userManager.FindByIdAsync(userId);
      // Create a variable to store a collection containing only the Items that are
      // associated with the currently-logged-in user's unique Id property:

      // We use the Where() method, which is a LINQ method we can use to query a 
      // collection in a way that echoes the logic of SQL. 

      // We're simply asking Entity to find items in the database where the user
      // id associated with the item is the same id as the id that belongs to the currentUser
      var userItems = _db.Items.Where(entry => entry.User.Id == currentUser.Id).ToList();
      return View(userItems);
    }

    public ActionResult Create()
    {
      ViewBag.CategoryId = new SelectList(_db.Categories, "CategoryId", "Name");
      return View();
    }

    // We start by finding the value of the current user. Then we associate the current 
    // user with the Item's User property. This makes the association so that an Item 
    // belongs to a User. Finally, we add the item to the database and save it 
    [HttpPost]
    public async Task<ActionResult> Create(Item item, int CategoryId)
    {
        var userId = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var currentUser = await _userManager.FindByIdAsync(userId);
        item.User = currentUser;
        _db.Items.Add(item);
        _db.SaveChanges();
        if (CategoryId != 0)
        {
            _db.CategoryItem.Add(new CategoryItem() { CategoryId = CategoryId, ItemId = item.ItemId });
        }
        _db.SaveChanges();
        return RedirectToAction("Index");
    }

    public ActionResult Details(int id)
    {
      var thisItem = _db.Items
          .Include(item => item.JoinEntities)
          .ThenInclude(join => join.Category)
          .FirstOrDefault(item => item.ItemId == id);
      return View(thisItem);
    }

    public ActionResult Edit(int id)
    {
      var thisItem = _db.Items.FirstOrDefault(item => item.ItemId == id);
      ViewBag.CategoryId = new SelectList(_db.Categories, "CategoryId", "Name");
      return View(thisItem);
    }

    [HttpPost]
    public ActionResult Edit(Item item, int CategoryId)
    {
      if (CategoryId != 0)
      {
        _db.CategoryItem.Add(new CategoryItem() { CategoryId = CategoryId, ItemId = item.ItemId });
      }
      _db.Entry(item).State = EntityState.Modified;
      _db.SaveChanges();
      return RedirectToAction("Index");
    }

    public ActionResult AddCategory(int id)
    {
      var thisItem = _db.Items.FirstOrDefault(item => item.ItemId == id);
      ViewBag.CategoryId = new SelectList(_db.Categories, "CategoryId", "Name");
      return View(thisItem);
    }

    [HttpPost]
    public ActionResult AddCategory(Item item, int CategoryId)
    {
      if (CategoryId != 0)
      {
        _db.CategoryItem.Add(new CategoryItem() { CategoryId = CategoryId, ItemId = item.ItemId });
        _db.SaveChanges();
      }
      return RedirectToAction("Index");
    }

    public ActionResult Delete(int id)
    {
      var thisItem = _db.Items.FirstOrDefault(item => item.ItemId == id);
      return View(thisItem);
    }

    [HttpPost, ActionName("Delete")]
    public ActionResult DeleteConfirmed(int id)
    {
      var thisItem = _db.Items.FirstOrDefault(item => item.ItemId == id);
      _db.Items.Remove(thisItem);
      _db.SaveChanges();
      return RedirectToAction("Index");
    }

    [HttpPost]
    public ActionResult DeleteCategory(int joinId)
    {
      var joinEntry = _db.CategoryItem.FirstOrDefault(entry => entry.CategoryItemId == joinId);
      _db.CategoryItem.Remove(joinEntry);
      _db.SaveChanges();
      return RedirectToAction("Index");
    }
  }
}
