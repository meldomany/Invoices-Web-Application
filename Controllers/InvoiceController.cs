using InvoiceApp.Data;
using InvoiceApp.Models;
using InvoiceApp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InvoiceApp.Controllers
{
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public InvoiceController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetInvoicesList()
        {
            var invoices = await dbContext.Invoices.ToListAsync();
            return Json(new { data = invoices });
        }

        public IActionResult Create()
        {
            var invoiceVM = new InvoiceVM
            {
                Invoice = new Invoice(),
                CallOffsSelectList = dbContext.CallOffs.Select(c => new SelectListItem
                {
                    Text = "CallOff NO:" + c.CallOffNumber + " Desc:" + c.Description,
                    Value = c.Id.ToString()
                })
            };

            return View(invoiceVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceVM invoiceVM)
        {
            if (ModelState.IsValid)
            {
                //check invoice number is unique
                var invoiceNumFounded = await dbContext.Invoices.AnyAsync(i => i.InvoiceNumber == invoiceVM.Invoice.InvoiceNumber);
                if (!invoiceNumFounded)
                {
                    await dbContext.Invoices.AddAsync(invoiceVM.Invoice);
                    await dbContext.SaveChangesAsync();

                    var invoiceCallOffs = new List<InvoiceCallOffs>();

                    foreach (var callOffId in invoiceVM.CallOffsIDs)
                    {
                        var invoiceCallOff = new InvoiceCallOffs
                        {
                            InvoiceId = invoiceVM.Invoice.Id,
                            CallOffId = callOffId
                        };
                        invoiceCallOffs.Add(invoiceCallOff);
                    }

                    if(invoiceCallOffs.Count > 0)
                    {
                        await dbContext.InvoiceCallOffs.AddRangeAsync(invoiceCallOffs);
                        await dbContext.SaveChangesAsync();
                    }

                    TempData["Success"] = "Invoice number:" + invoiceVM.Invoice.InvoiceNumber + " created";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Error"] = "Invoice number should be unique";
                return RedirectToAction(nameof(Create));
            }
            TempData["Error"] = "Check fields data";
            return RedirectToAction(nameof(Create));
        }

        public async Task<IActionResult> Edit(int invoiceId)
        {
            if (invoiceId > 0)
            {
                var invoiceVM = new InvoiceVM
                {
                    Invoice = await dbContext.Invoices.FindAsync(invoiceId),
                    CallOffsSelectList = dbContext.CallOffs.Select(c => new SelectListItem
                    {
                        Text = "CallOff NO:" + c.CallOffNumber + " Desc:" + c.Description,
                        Value = c.Id.ToString()
                    }),
                    InvoiceCallOffs = await dbContext.InvoiceCallOffs.Include(ico => ico.CallOff).Where(ico => ico.InvoiceId == invoiceId).ToListAsync()
                };

                return View(invoiceVM);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InvoiceVM invoiceVM)
        {
            if (ModelState.IsValid)
            {
                if (invoiceVM.Invoice.Id > 0)
                {
                    //check invoice number is unique
                    var invoices = await dbContext.Invoices.Where(i => i.Id != invoiceVM.Invoice.Id).ToListAsync();
                    var invoiceNumFounded = invoices.Any(i => i.InvoiceNumber == invoiceVM.Invoice.InvoiceNumber);
                    if (!invoiceNumFounded)
                    {
                        dbContext.Invoices.Update(invoiceVM.Invoice);
                        await dbContext.SaveChangesAsync();
                        TempData["Success"] = "Invoice number:" + invoiceVM.Invoice.InvoiceNumber + " updated";
                        return RedirectToAction(nameof(Index));
                    }
                    TempData["Error"] = "Invoice number should be unique";
                    return RedirectToAction(nameof(Edit), new { invoiceId = invoiceVM.Invoice.Id });
                }
                TempData["Error"] = "Check fields data";
                return RedirectToAction(nameof(Edit), new { invoiceId = invoiceVM.Invoice.Id });
            }
            TempData["Error"] = "Check fields data";
            return RedirectToAction(nameof(Edit), new { invoiceId = invoiceVM.Invoice.Id });
        }

        public async Task<IActionResult> Delete(int invoiceId)
        {
            if (invoiceId > 0)
            {
                var invoice = await dbContext.Invoices.FindAsync(invoiceId);
                if (invoice != null)
                {
                    dbContext.Invoices.Remove(invoice);
                    await dbContext.SaveChangesAsync();
                    TempData["Success"] = "Invoice number:" + invoice.InvoiceNumber + " deleted";
                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = "Check fields data";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Check fields data";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> RemoveCallOff(int callOffId, int invoiceId)
        {
            if(callOffId > 0 && invoiceId > 0)
            {
                var invoiceCallOff = await dbContext.InvoiceCallOffs
                    .Where(ico => ico.InvoiceId == invoiceId)
                    .Where(ico => ico.CallOffId == callOffId)
                    .FirstOrDefaultAsync();
                if (invoiceCallOff != null)
                {
                    dbContext.InvoiceCallOffs.Remove(invoiceCallOff);
                    await dbContext.SaveChangesAsync();
                    TempData["Success"] = "Call off removed";
                    return RedirectToAction(nameof(Edit), new { invoiceId = invoiceId });
                }
                TempData["Success"] = "Check fields data";
                return RedirectToAction(nameof(Edit), new { invoiceId = invoiceId });
            }
            TempData["Success"] = "Check fields data";
            return RedirectToAction(nameof(Edit), new { invoiceId = invoiceId });
        }

        public async Task<IActionResult> AssignCallOffs(int invoiceId)
        {
            if (invoiceId > 0)
            {
                //all calloffs
                var callOffs = await dbContext.CallOffs.ToListAsync();

                //all calloffs in this invoice
                var invoiceCallOffs = await dbContext.InvoiceCallOffs.Where(i => i.InvoiceId == invoiceId).ToListAsync();


                //update calloffs list and remove all the calloffs that the invoice already taken
                for (int i = 0; i < invoiceCallOffs.Count; i++)
                {
                    if(callOffs.Any(co => co.Id == invoiceCallOffs[i].CallOffId))
                    {
                        var callOff = callOffs.FirstOrDefault(co => co.Id == invoiceCallOffs[i].CallOffId);
                        callOffs.Remove(callOff);
                    }
                }

                var invoiceCallOffVM = new InvoiceCallOffsVM
                {
                    Invoice = await dbContext.Invoices.FindAsync(invoiceId),
                    CallOffsSelectList = callOffs.Select(c => new SelectListItem
                    {
                        Text = "CallOff NO:" + c.CallOffNumber + " Desc:" + c.Description,
                        Value = c.Id.ToString()
                    })
                };

                return View(invoiceCallOffVM);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignCallOffs(InvoiceCallOffsVM invoiceCallOffsVM)
        {
            if (ModelState.IsValid)
            {
                if (invoiceCallOffsVM.Invoice.Id > 0)
                {
                    var invoiceCallOffs = new List<InvoiceCallOffs>();
                    foreach (var callOffId in invoiceCallOffsVM.CallOffsIDS)
                    {
                        var invoiceCallOff = new InvoiceCallOffs
                        {
                            InvoiceId = invoiceCallOffsVM.Invoice.Id,
                            CallOffId = callOffId
                        };

                        invoiceCallOffs.Add(invoiceCallOff);
                    }

                    if(invoiceCallOffs.Count > 0)
                    {
                        await dbContext.InvoiceCallOffs.AddRangeAsync(invoiceCallOffs);
                        await dbContext.SaveChangesAsync();
                        TempData["Success"] = "CallOff assigned successfully";
                        return RedirectToAction(nameof(Index));
                    }
                }
                TempData["Error"] = "Check fields data";
                return RedirectToAction(nameof(AssignCallOffs), new { invoiceId = invoiceCallOffsVM.Invoice.Id });
            }
            TempData["Error"] = "Check fields data";
            return RedirectToAction(nameof(AssignCallOffs), new { invoiceId = invoiceCallOffsVM.Invoice.Id });
        }

        public async Task<IActionResult> RemoveItem(int callOffItemId, int invoiceId)
        {
            if (callOffItemId > 0 && invoiceId > 0)
            {
                var invoiceCallOffItems = await dbContext.InvoiceCallOffItems
                    .Where(ico => ico.InvoiceId == invoiceId)
                    .Where(ico => ico.CallOffItemId == callOffItemId)
                    .FirstOrDefaultAsync();
                if (invoiceCallOffItems != null)
                {
                    dbContext.InvoiceCallOffItems.Remove(invoiceCallOffItems);
                    await dbContext.SaveChangesAsync();
                    TempData["Success"] = "Item removed";
                    return RedirectToAction(nameof(Details), new { invoiceId = invoiceId });
                }
                TempData["Success"] = "Check fields data";
                return RedirectToAction(nameof(Details), new { invoiceId = invoiceId });
            }
            TempData["Success"] = "Check fields data";
            return RedirectToAction(nameof(Details), new { invoiceId = invoiceId });
        }

        public async Task<IActionResult> AssignItems(int invoiceId)
        {
            //select all invoice calloffs
            var invoiceCallOffs = await dbContext.InvoiceCallOffs.Where(ico => ico.InvoiceId == invoiceId).ToListAsync();

            var callOffItemsList = new List<CallOffItems>();

            foreach (var invoiceCallOff in invoiceCallOffs)
            {
                var callOffItems = await dbContext.CallOffItems
                    .Include(coi => coi.CallOff)
                    .Include(coi => coi.Item)
                    .Where(coi => coi.Allowed == 1)
                    .Where(coi => coi.CallOffId == invoiceCallOff.CallOffId)
                    .ToListAsync();

                callOffItemsList.AddRange(callOffItems);
            }

            //select all invoice calloff items for this invoice
            var invoiceCallOffItems = await dbContext.InvoiceCallOffItems
                .Where(ici => ici.InvoiceId == invoiceId)
                .ToListAsync();

            foreach (var invoiceCallOffItem in invoiceCallOffItems)
            {
                if(callOffItemsList.Any(coi => coi.Id == invoiceCallOffItem.CallOffItemId))
                {
                    var callOffItem = callOffItemsList.FirstOrDefault(coi => coi.Id == invoiceCallOffItem.CallOffItemId);
                    callOffItemsList.Remove(callOffItem);
                }
            }

            var invoiceCallOffItemsVM = new InvoiceCallOffItemsVM
            {
                Invoice = await dbContext.Invoices.FindAsync(invoiceId),
                CallOffItems = callOffItemsList.Select(c => new SelectListItem
                {
                    Text = "Item Number:" + c.Item.ItemNo + "| Quantity:" + c.Quantity + "| CallOff:" + c.CallOff.CallOffNumber,
                    Value = c.Id.ToString()
                })
            };

            return View(invoiceCallOffItemsVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignItems(InvoiceCallOffItemsVM invoiceCallOffItemsVM)
        {
            if (ModelState.IsValid)
            {
                var invoiceCallOffItems = new List<InvoiceCallOffItems>();

                foreach (var callOffItemId in invoiceCallOffItemsVM.CallOffItemsIDS)
                {
                    invoiceCallOffItems.Add(new InvoiceCallOffItems
                    {
                        CallOffItemId = callOffItemId,
                        InvoiceId = invoiceCallOffItemsVM.Invoice.Id
                    });

                    var callOffItem = await dbContext.CallOffItems.FindAsync(callOffItemId);
                    callOffItem.Allowed = 0;
                    dbContext.CallOffItems.Update(callOffItem);
                    await dbContext.SaveChangesAsync();
                }

                await dbContext.InvoiceCallOffItems.AddRangeAsync(invoiceCallOffItems);
                await dbContext.SaveChangesAsync();
                TempData["Success"] = "Items assigned successfully";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Check fields data";
            return RedirectToAction(nameof(AssignItems), new { invoiceId = invoiceCallOffItemsVM.Invoice.Id });

        }

        public IActionResult Details(int invoiceId)
        {
            if (invoiceId > 0)
            {
                return View();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GetInvoiceDetailsList(int invoiceId)
        {
            if (invoiceId > 0)
            {
                var invoiceCallOffItems = await dbContext.InvoiceCallOffItems
                .Where(icoi => icoi.InvoiceId == invoiceId)
                .ToListAsync();

                var callOffItems = new List<CallOffItems>();

                foreach (var invoiceCallOffItem in invoiceCallOffItems)
                {
                    var callOffItem = await dbContext.CallOffItems
                            .Include(coi => coi.Item)
                            .ThenInclude(i => i.Unit)
                            .Include(coi => coi.Item)
                            .ThenInclude(i => i.Contractor)
                            .Include(coi => coi.CallOff)
                            .FirstOrDefaultAsync(coi => coi.Id == invoiceCallOffItem.CallOffItemId);
                    callOffItems.Add(callOffItem);   
                }

                return Json(new { data = callOffItems });
            }
            return RedirectToAction(nameof(Index));
        }
    }
}