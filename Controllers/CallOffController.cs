using InvoiceApp.Data;
using InvoiceApp.Models;
using InvoiceApp.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InvoiceApp.Controllers
{
    public class CallOffController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public CallOffController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCallOffsList()
        {
            var callOffs = await dbContext.CallOffs.ToListAsync();
            return Json(new { data = callOffs });
        }

        [HttpGet]
        public IActionResult Create(int itemsCount)
        {
            var callOffVM = new CallOffVM
            {
                CallOff = new CallOff(),
                ItemsSelectList = dbContext.Items.Where(i => i.Quantity > 0).Select(i => new SelectListItem
                {
                    Text = "Item Number: " + i.ItemNo.ToString() + " Item Quantity: " + i.Quantity + " Item Contractor: " + i.Contractor.Name,
                    Value = i.Id.ToString()
                })
            };

            for (int i = 0; i < itemsCount; i++)
            {
                callOffVM.CallOffItems.Add(new CallOffItems
                {
                    ItemId = 0,
                    CallOffId = 0,
                    Quantity = 0,
                    Allowed = 1
                });
            }

            return View(callOffVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CallOffVM callOffVM)
        {
            if (ModelState.IsValid)
            {
                if(!await dbContext.CallOffs.AnyAsync(co => co.CallOffNumber == callOffVM.CallOff.CallOffNumber))
                {
                    var callOffItemList = new List<CallOffItems>();
                    var itemList = await dbContext.Items.ToListAsync();
                    var quantityChecks = true;

                    foreach (var callOffItem in callOffVM.CallOffItems)
                    {
                        var callOffItemObj = callOffItemList.FirstOrDefault(i => i.ItemId == callOffItem.ItemId);
                        var item = itemList.FirstOrDefault(i => i.Id == callOffItem.ItemId);

                        if (callOffItemObj != null)
                        {
                            if (item.Quantity >= (callOffItemObj.Quantity + callOffItem.Quantity))
                            {
                                callOffItemObj.Quantity += callOffItem.Quantity;
                            }
                            else
                            {
                                quantityChecks = false;
                                TempData["Error"] = "You have crossed the item quantity for item:"+ item.ItemNo;
                                return RedirectToAction(nameof(Create), new { itemsCount = callOffVM.CallOffItems.Count });
                            }
                        }
                        else
                        {
                            if (item.Quantity >= callOffItem.Quantity)
                            {
                                callOffItemList.Add(new CallOffItems { ItemId = callOffItem.ItemId, Quantity = callOffItem.Quantity });
                            }
                            else
                            {
                                quantityChecks = false;
                                TempData["Error"] = "You have crossed the item quantity for item:" + item.ItemNo;
                                return RedirectToAction(nameof(Create), new { itemsCount = callOffVM.CallOffItems.Count });
                            }
                        }
                    }

                    if (quantityChecks)
                    {
                        //update item quantity and total price in items table
                        foreach (var callOffItemL in callOffItemList)
                        {
                            var item = dbContext.Items.FirstOrDefault(i => i.Id == callOffItemL.ItemId);
                            item.Quantity -= callOffItemL.Quantity;
                            item.TotalPrice = item.Quantity * item.Price;
                            dbContext.Items.Update(item);
                        }

                        await dbContext.CallOffs.AddAsync(callOffVM.CallOff);
                        await dbContext.SaveChangesAsync();

                        foreach (var callOffItem in callOffVM.CallOffItems)
                        {
                            await dbContext.CallOffItems.AddAsync(new CallOffItems
                            {
                                CallOffId = callOffVM.CallOff.Id,
                                ItemId = callOffItem.ItemId,
                                Quantity = callOffItem.Quantity,
                                Allowed = 1
                            });
                        }

                        TempData["Success"] = "CallOff number:" + callOffVM.CallOff.CallOffNumber + " created";
                        await dbContext.SaveChangesAsync();
                        return RedirectToAction(nameof(Index));
                    }

                    TempData["Error"] = "You have crossed the item quantity";
                    return RedirectToAction(nameof(Create), new { itemsCount = callOffVM.CallOffItems.Count });
                }
                TempData["Error"] = "CallOff number should be unique";
                return RedirectToAction(nameof(Create), new { itemsCount = callOffVM.CallOffItems.Count });
            }
            TempData["Error"] = "Check your fields";
            return RedirectToAction(nameof(Create), new { itemsCount = callOffVM.CallOffItems.Count});
        }

        public async Task<IActionResult> Edit(int callOffId)
        {
            if (callOffId > 0)
            {
                var callOff = await dbContext.CallOffs.FindAsync(callOffId);
                if (callOff != null)
                {
                    var callOffVM = new CallOffVM
                    {
                        CallOff = callOff,
                        ItemsSelectList = dbContext.Items.Where(i => i.Quantity > 0).Select(i => new SelectListItem
                        {
                            Text = "Item Number: " + i.ItemNo.ToString() + " Item Quantity: " + i.Quantity + " Item Contractor: " + i.Contractor.Name,
                            Value = i.Id.ToString()
                        }),
                        CallOffItems = await dbContext.CallOffItems.Where(coi => coi.CallOffId == callOffId).ToListAsync()
                    };

                    return View(callOffVM);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetCallOffItemsList(int callOffId)
        {
            var callOffItems = await dbContext.CallOffItems
                .Include(coi => coi.Item)
                .Where(coi => coi.CallOffId == callOffId).ToListAsync();
            return Json(new { data = callOffItems });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CallOffVM callOffVM)
        {
            if (ModelState.IsValid)
            {
                if (callOffVM.CallOff.Id > 0)
                {
                    var callOffs = await dbContext.CallOffs.Where(co => co.Id != callOffVM.CallOff.Id).ToListAsync();

                    if(!callOffs.Any(coi => coi.CallOffNumber == callOffVM.CallOff.CallOffNumber))
                    {
                        dbContext.CallOffs.Update(callOffVM.CallOff);
                        await dbContext.SaveChangesAsync();
                        TempData["Success"] = "CallOff number:"+ callOffVM.CallOff.CallOffNumber +" updated";
                        return RedirectToAction(nameof(Index));
                    }
                    TempData["Error"] = "CallOff number should be unique";
                    return RedirectToAction(nameof(Edit), new { callOffId = callOffVM.CallOff.Id });
                }
                TempData["Error"] = "Check your data";
                return RedirectToAction(nameof(Edit), new { callOffId = callOffVM.CallOff.Id });
            }
            TempData["Error"] = "Check your data";
            return RedirectToAction(nameof(Edit), new { callOffId = callOffVM.CallOff.Id });
        }





        public async Task<IActionResult> AssignItems(int callOffId, int itemsCount)
        {
            var callOffVM = new CallOffVM
            {
                CallOff = await dbContext.CallOffs.FirstOrDefaultAsync(co => co.Id == callOffId),
                ItemsSelectList = dbContext.Items.Select(i => new SelectListItem
                {
                    Text = "Item Number: " + i.ItemNo.ToString() + " Item Quantity: " + i.Quantity + " Item Contractor: " + i.Contractor.Name,
                    Value = i.Id.ToString()
                })
            };

            for (int i = 0; i < itemsCount; i++)
            {
                callOffVM.CallOffItems.Add(new CallOffItems
                {
                    ItemId = 0,
                    Quantity = 0,
                    CallOffId = 0,
                    Allowed = 1
                });
            }

            return View(callOffVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignItems(CallOffVM callOffVM)
        {
            if (ModelState.IsValid)
            {
                var callOffItemList = new List<CallOffItems>();
                var itemList = await dbContext.Items.ToListAsync();
                var quantityChecks = true;

                foreach (var callOffItem in callOffVM.CallOffItems)
                {
                    var callOffItemObj = callOffItemList.FirstOrDefault(i => i.ItemId == callOffItem.ItemId);
                    var item = itemList.FirstOrDefault(i => i.Id == callOffItem.ItemId);

                    if (callOffItemObj != null)
                    {
                        if (item.Quantity >= (callOffItemObj.Quantity + callOffItem.Quantity))
                        {
                            callOffItemObj.Quantity += callOffItem.Quantity;
                        }
                        else
                        {
                            quantityChecks = false;
                            break;
                        }
                    }
                    else
                    {
                        if (item.Quantity >= callOffItem.Quantity)
                        {
                            callOffItemList.Add(new CallOffItems { ItemId = callOffItem.ItemId, Quantity = callOffItem.Quantity });
                        }
                        else
                        {
                            quantityChecks = false;
                            break;
                        }
                    }
                }

                if (quantityChecks)
                {
                    //update item quantity and total price in items table
                    foreach (var callOffItemL in callOffItemList)
                    {
                        var item = dbContext.Items.FirstOrDefault(i => i.Id == callOffItemL.ItemId);
                        item.Quantity -= callOffItemL.Quantity;
                        item.TotalPrice = item.Quantity * item.Price;
                        dbContext.Items.Update(item);
                    }

                    foreach (var callOffItem in callOffVM.CallOffItems)
                    {
                        await dbContext.CallOffItems.AddAsync(new CallOffItems
                        {
                            CallOffId = callOffVM.CallOff.Id,
                            ItemId = callOffItem.ItemId,
                            Quantity = callOffItem.Quantity,
                            Allowed = 1
                        });
                    }
                    await dbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(callOffVM);
        }



        public async Task<IActionResult> Delete(int callOffId)
        {
            if (callOffId > 0)
            {
                var callOffItems = await dbContext.CallOffItems.Include(coi => coi.Item).Where(coi => coi.CallOffId == callOffId).ToListAsync();
                foreach (var callOffItem in callOffItems)
                {
                    var item = await dbContext.Items.FirstOrDefaultAsync(i => i.Id == callOffItem.ItemId);
                    item.Quantity += callOffItem.Quantity;
                    item.TotalPrice = item.Quantity * item.Price;
                    dbContext.Items.Update(item);
                }

                var callOff = await dbContext.CallOffs.FindAsync(callOffId);
                if (callOff != null)
                {
                    dbContext.CallOffs.Remove(callOff);
                    await dbContext.SaveChangesAsync();
                    TempData["Success"] = "CallOff number:" + callOff.CallOffNumber + " deleted";
                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = "Check your data";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Check your data";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DeleteCallOffItem(int callOffItemId)
        {
            if(callOffItemId > 0)
            {
                var callOffItem = await dbContext.CallOffItems
                    .Where(coi => coi.Id == callOffItemId)
                    .FirstOrDefaultAsync();
                dbContext.CallOffItems.Remove(callOffItem);

                var item = await dbContext.Items.FirstOrDefaultAsync(i => i.Id == callOffItem.ItemId);
                item.Quantity += callOffItem.Quantity;
                item.TotalPrice = item.Price * item.Quantity;
                dbContext.Items.Update(item);
                await dbContext.SaveChangesAsync();

                TempData["Success"] = "CallOff Item deleted";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "Check your data";
            return RedirectToAction(nameof(Index));
        }

        //Excel Management
        public async Task<IActionResult> ExportToExcel()
        {
            var callOffs = await dbContext.CallOffs
                .Include(co => co.CallOffItems)
                .ThenInclude(coi => coi.Item)
                .ThenInclude(i => i.Unit)
                .Include(co => co.CallOffItems)
                .ThenInclude(coi => coi.Item)
                .ThenInclude(i => i.Contractor)
                .ToListAsync();

            //Start Exporting to excel
            var stream = new MemoryStream();
            using (var xlPackage = new ExcelPackage(stream))
            {
                //Define Worksheet
                var worksheet = xlPackage.Workbook.Worksheets.Add("CallOffItemss");


                //First Row
                var startRow = 2;
                var row = startRow;


                //Table Header
                worksheet.Cells["A1"].Value = "CallOff Id";
                worksheet.Cells["B1"].Value = "CallOff Number";
                worksheet.Cells["C1"].Value = "CallOff Description";
                worksheet.Cells["D1"].Value = "CallOff Quantity";
                worksheet.Cells["E1"].Value = "Item ID";
                worksheet.Cells["F1"].Value = "Item Number";
                worksheet.Cells["G1"].Value = "Item Description";
                worksheet.Cells["H1"].Value = "Item Price";
                worksheet.Cells["I1"].Value = "Item Total Price";
                worksheet.Cells["J1"].Value = "Item Quantity";
                worksheet.Cells["K1"].Value = "Item Unit ID";
                worksheet.Cells["L1"].Value = "Item Unit Name";
                worksheet.Cells["M1"].Value = "Item Contractor ID";
                worksheet.Cells["N1"].Value = "Item Contractor Name";
                worksheet.Cells["O1"].Value = "Item Contractor Number";
                worksheet.Cells["A1:O1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells["A1:O1"].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

                row = 2;
                foreach (var callOff in callOffs)
                {
                    if(callOff.CallOffItems.Count() > 0)
                    {
                        foreach (var callOffItem in callOff.CallOffItems)
                        {
                            worksheet.Cells[row, 1].Value = callOff.Id;
                            worksheet.Cells[row, 2].Value = callOff.CallOffNumber;
                            worksheet.Cells[row, 3].Value = callOff.Description;
                            worksheet.Cells[row, 4].Value = callOffItem.Quantity;
                            worksheet.Cells[row, 5].Value = callOffItem.Item.Id;
                            worksheet.Cells[row, 6].Value = callOffItem.Item.ItemNo;
                            worksheet.Cells[row, 7].Value = callOffItem.Item.Description;
                            worksheet.Cells[row, 8].Value = callOffItem.Item.Price;
                            worksheet.Cells[row, 9].Value = callOffItem.Item.TotalPrice;
                            worksheet.Cells[row, 10].Value = callOffItem.Item.Quantity;
                            worksheet.Cells[row, 11].Value = callOffItem.Item.UnitId;
                            worksheet.Cells[row, 12].Value = callOffItem.Item.Unit.Name;
                            worksheet.Cells[row, 13].Value = callOffItem.Item.ContractorId;
                            worksheet.Cells[row, 14].Value = callOffItem.Item.Contractor.Name;
                            worksheet.Cells[row, 15].Value = callOffItem.Item.Contractor.Number;
                            row++;
                        }
                    }else
                    {
                        worksheet.Cells[row, 1].Value = callOff.Id;
                        worksheet.Cells[row, 2].Value = callOff.CallOffNumber;
                        worksheet.Cells[row, 3].Value = callOff.Description;
                        row++;
                    }
                }

                xlPackage.Workbook.Properties.Title = "CallOffItems";
                xlPackage.Save();
            }

            stream.Position = 0;
            TempData["Success"] = "CallOffItems excel sheet exported successfully";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "callOffItems.xlssx.xlsx");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (ModelState.IsValid)
            {
                if (file?.Length > 0)
                {
                    //Convert file to a stream
                    var stream = file.OpenReadStream();
                    var callOffs = new List<CallOff>();
                    var dataValidations = true;
                    try
                    {
                        using (var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets.First();
                            var rowCount = worksheet.Dimension.Rows;

                            for (int row = 2; row < rowCount + 1; row++)
                            {
                                try
                                {
                                    var callOffNumber = worksheet.Cells[row, 1].Value?.ToString();
                                    var callOffDescription = worksheet.Cells[row, 2].Value?.ToString();

                                    //client side validation
                                    var clientCallOffNumber = callOffs.Any(c => c.CallOffNumber == long.Parse(callOffNumber));
                                    
                                    if (clientCallOffNumber)
                                    {
                                        dataValidations = false;
                                        TempData["Error"] = "Client number duplicated inside the excel sheet";
                                        return RedirectToAction(nameof(Index));
                                    }

                                    var checkCallOffNumber = await dbContext.CallOffs.AnyAsync(c => c.CallOffNumber == long.Parse(callOffNumber));
                                    
                                    if (checkCallOffNumber)
                                    {
                                        dataValidations = false;
                                        TempData["Error"] = "There is client number in sheet already exists inside the database";
                                        return RedirectToAction(nameof(Index));
                                    }

                                    var callOff = new CallOff
                                    {
                                        CallOffNumber = long.Parse(callOffNumber),
                                        Description = callOffDescription
                                    };
                                    callOffs.Add(callOff);
                                }
                                catch (Exception ex)
                                {
                                    TempData["Error"] = ex.Message.ToString();
                                    return RedirectToAction(nameof(Index));
                                }
                            }
                        }

                        if (dataValidations)
                        {
                            await dbContext.CallOffs.AddRangeAsync(callOffs);
                            await dbContext.SaveChangesAsync();
                            TempData["Success"] = "CallOff excel sheet uploaded successfully";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = ex.Message.ToString();
                        return RedirectToAction(nameof(Index));
                    }

                }
            }
            TempData["Error"] = "Check fields data";
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public IActionResult UploadCallOffItemsExcel()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCallOffItemsExcel(IFormFile file)
        {
            if (ModelState.IsValid)
            {
                if (file?.Length > 0)
                {
                    //Convert file to a stream
                    var stream = file.OpenReadStream();
                    var callOffItems = new List<CallOffItems>();
                    var dataValidations = true;

                    var validationList = "";
                    
                    try
                    {
                        using (var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets.First();
                            var rowCount = worksheet.Dimension.Rows;
                            
                            for (int row = 2; row < rowCount + 1; row++)
                            {
                                try
                                {
                                    var callOffId = worksheet.Cells[row, 1].Value?.ToString();
                                    var ItemId = worksheet.Cells[row, 2].Value?.ToString();
                                    var Quantity = worksheet.Cells[row, 3].Value?.ToString();

                                    var callOffItem = new CallOffItems
                                    {
                                        CallOffId = int.Parse(callOffId),
                                        ItemId = int.Parse(ItemId)
                                    };

                                    var ItemDb = dbContext.Items.FirstOrDefault(i => i.Id == int.Parse(ItemId));

                                    if(ItemDb.Quantity == 0 && int.Parse(Quantity) > 0)
                                    {
                                        callOffItem.Quantity = 0;
                                        callOffItem.Remains = int.Parse(Quantity);
                                        validationList += "CallOff Id:"+callOffId+", Item Id:"+ItemId+", Item quantity is equal to zero \n";
                                    }

                                    if (int.Parse(Quantity) > ItemDb.Quantity)
                                    {
                                        callOffItem.Remains = int.Parse(Quantity) - ItemDb.Quantity;
                                        callOffItem.Quantity = ItemDb.Quantity;
                                        ItemDb.Quantity = 0;
                                        ItemDb.TotalPrice = ItemDb.Price * ItemDb.Quantity;
                                        validationList += "CallOff Id:" + callOffId + ", Item Id:" + ItemId + ", callOff quantity is greater than item quantity \n";
                                    }

                                    if(ItemDb.Quantity >= int.Parse(Quantity))
                                    {
                                        callOffItem.Quantity = int.Parse(Quantity);
                                        callOffItem.Remains = 0;
                                        ItemDb.Quantity -= int.Parse(Quantity);
                                        ItemDb.TotalPrice = ItemDb.Price * ItemDb.Quantity;
                                    }

                                    callOffItems.Add(callOffItem);
                                }
                                catch (Exception ex)
                                {
                                    TempData["Error"] = ex.Message.ToString();
                                    return RedirectToAction(nameof(Index));
                                }
                            }
                        }

                        if (dataValidations)
                        {
                            await dbContext.CallOffItems.AddRangeAsync(callOffItems);
                            await dbContext.SaveChangesAsync();
                            TempData["Success"] = "CallOffItems excel sheet uploaded successfully";
                            if(validationList.Length > 0)
                            {
                                TempData["Error"] = validationList;
                            }
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = ex.Message.ToString();
                        return RedirectToAction(nameof(Index));
                    }

                }
            }
            TempData["Error"] = "Check fields data";
            return RedirectToAction(nameof(Index));
        }
    }
}
