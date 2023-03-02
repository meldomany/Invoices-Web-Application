using InvoiceApp.Data;
using InvoiceApp.Models;
using InvoiceApp.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InvoiceApp.Controllers
{
    public class ItemController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public ItemController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetItemsList()
        {
            var items = await dbContext.Items
                .Include(i => i.Contractor)
                .Include(i => i.Unit)
                //.Include(i => i.CallOffItems)
                //.ThenInclude(coi => coi.CallOff)
                //.Include(i => i.CallOffItems)
                //.ThenInclude(coi => coi.InvoiceCallOffItems)
                //.ThenInclude(icoi => icoi.Invoice)
                .ToListAsync();

            return Json(new { data = items });
        }

        public IActionResult Create()
        {
            var itemContractorsVM = new ItemVM
            {
                Item = new Item(),
                ContractorSelectList = dbContext.Contractors.Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }),
                UnitSelectList = dbContext.Units.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                })
            };

            return View(itemContractorsVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ItemVM itemContractorsVM)
        {
            if (ModelState.IsValid)
            {
                if(itemContractorsVM.Item.Quantity > 0)
                {
                    if(!await dbContext.Items.Where(i => i.ContractorId == itemContractorsVM.Item.ContractorId)
                        .AnyAsync(i => i.ItemNo == itemContractorsVM.Item.ItemNo))
                    {
                        itemContractorsVM.Item.Status = false;
                        itemContractorsVM.Item.TotalPrice = itemContractorsVM.Item.Quantity * itemContractorsVM.Item.Price;
                        await dbContext.Items.AddAsync(itemContractorsVM.Item);
                        await dbContext.SaveChangesAsync();
                        TempData["Success"] = "Item number: " + itemContractorsVM.Item.ItemNo + "created";
                        return RedirectToAction(nameof(Index));
                    }
                    TempData["Error"] = "Item number should be unique";
                    return RedirectToAction(nameof(Create));
                }
                TempData["Error"] = "Item quantity should be greater than 0";
                return RedirectToAction(nameof(Create));
            }
            TempData["Error"] = "Check your fields";
            return RedirectToAction(nameof(Create));
        }

        public async Task<IActionResult> Edit(int itemId)
        {
            if (itemId > 0)
            {
                var item = await dbContext.Items
                .Include(i => i.CallOffItems)
                .ThenInclude(coi => coi.CallOff)
                .Include(i => i.CallOffItems)
                .ThenInclude(coi => coi.InvoiceCallOffItems)
                .ThenInclude(icoi => icoi.Invoice)
                .FirstOrDefaultAsync(i => i.Id == itemId);
                
                if (item != null)
                {
                    var itemContractorsVM = new ItemVM
                    {
                        Item = item,
                        ContractorSelectList = dbContext.Contractors.Select(c => new SelectListItem
                        {
                            Text = c.Name,
                            Value = c.Id.ToString()
                        }),
                        UnitSelectList = dbContext.Units.Select(u => new SelectListItem
                        {
                            Text = u.Name,
                            Value = u.Id.ToString()
                        })
                    };

                    return View(itemContractorsVM);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ItemVM itemContractorsVM)
        {
            if (ModelState.IsValid)
            {
                if (itemContractorsVM.Item.Id > 0)
                {
                    if (itemContractorsVM.Item.Quantity > 0)
                    {
                        var items = await dbContext.Items.Where(i => i.Id != itemContractorsVM.Item.Id).ToListAsync();
                        if(!items.Where(i => i.ContractorId == itemContractorsVM.Item.ContractorId)
                            .Any(i => i.ItemNo == itemContractorsVM.Item.ItemNo))
                        {
                            itemContractorsVM.Item.Status = false;
                            itemContractorsVM.Item.TotalPrice = itemContractorsVM.Item.Quantity * itemContractorsVM.Item.Price;
                            dbContext.Items.Update(itemContractorsVM.Item);
                            await dbContext.SaveChangesAsync();
                            TempData["Success"] = "Item number " + itemContractorsVM.Item.ItemNo + " created";
                            return RedirectToAction(nameof(Index));
                        }
                        TempData["Error"] = "Item number should be unique";
                        return RedirectToAction(nameof(Edit), new { itemId = itemContractorsVM.Item.Id });
                    }
                    TempData["Error"] = "Item quantity should be greater than 0";
                    return RedirectToAction(nameof(Edit), new { itemId = itemContractorsVM.Item.Id });
                }
                TempData["Error"] = "There is no item to update";
                return RedirectToAction(nameof(Edit), new { itemId = itemContractorsVM.Item.Id });
            }
            TempData["Error"] = "Check your fields";
            return RedirectToAction(nameof(Edit), new { itemId = itemContractorsVM.Item.Id});
        }

        public async Task<IActionResult> Delete(int itemId)
        {
            if (itemId > 0)
            {
                var item = await dbContext.Items.FindAsync(itemId);
                if (item != null)
                {
                    dbContext.Items.Remove(item);
                    await dbContext.SaveChangesAsync();
                    TempData["Success"] = "Item number " + item.ItemNo + " deleted";
                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = "There is no item to delete";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "There is no item to delete";
            return RedirectToAction(nameof(Index));
        }


        // Excel Management
        public async Task<IActionResult> ExportToExcel()
        {
            var items = await dbContext.Items.Include(i => i.Contractor).Include(i => i.Unit).ToListAsync();

            //Start Exporting to excel
            var stream = new MemoryStream();
            using (var xlPackage = new ExcelPackage(stream))
            {
                //Define Worksheet
                var worksheet = xlPackage.Workbook.Worksheets.Add("Items");

                //First Row
                var startRow = 2;
                var row = startRow;

                //Table Header
                worksheet.Cells["A1"].Value = "Id";
                worksheet.Cells["B1"].Value = "Item Number";
                worksheet.Cells["C1"].Value = "Item Description";
                worksheet.Cells["D1"].Value = "Item Quantity";
                worksheet.Cells["E1"].Value = "Item Price";
                worksheet.Cells["F1"].Value = "Item Total Price";
                worksheet.Cells["G1"].Value = "Item Unit Id";
                worksheet.Cells["H1"].Value = "Item Unit Name";
                worksheet.Cells["I1"].Value = "Item Contractor Id";
                worksheet.Cells["J1"].Value = "Item Contractor Name";
                worksheet.Cells["A1:J1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells["A1:J1"].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

                row = 2;
                foreach (var item in items)
                {
                    worksheet.Cells[row, 1].Value = item.Id;
                    worksheet.Cells[row, 2].Value = item.ItemNo;
                    worksheet.Cells[row, 3].Value = item.Description;
                    worksheet.Cells[row, 4].Value = item.Quantity;
                    worksheet.Cells[row, 5].Value = item.Price;
                    worksheet.Cells[row, 6].Value = item.TotalPrice;
                    worksheet.Cells[row, 7].Value = item.UnitId;
                    worksheet.Cells[row, 8].Value = item.Unit.Name;
                    worksheet.Cells[row, 9].Value = item.ContractorId;
                    worksheet.Cells[row, 10].Value = item.Contractor.Name;
                    row++;
                }

                xlPackage.Workbook.Properties.Title = "Items";
                xlPackage.Save();
            }

            stream.Position = 0;
            TempData["Success"] = "Items excel sheet exported successfully";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "items.xlssx.xlsx");
        }

        [HttpGet]
        public IActionResult UploadExcel()
        {
            return View();
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
                    var items = new List<Item>();
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
                                    var itemNumber = worksheet.Cells[row, 1].Value?.ToString();
                                    var itemDescription = worksheet.Cells[row, 2].Value.ToString();
                                    var itemQuantity = worksheet.Cells[row, 3].Value.ToString();
                                    var itemPrice = worksheet.Cells[row, 4].Value.ToString();
                                    var itemTotalPrice = worksheet.Cells[row, 5].Value.ToString();
                                    var itemUnitId = worksheet.Cells[row, 6].Value.ToString();
                                    var itemContractorId = worksheet.Cells[row, 7].Value.ToString();
                                    
                                    //client side validation
                                    var clientItemNumber = items.Any(c => c.ItemNo == int.Parse(itemNumber));

                                    if (clientItemNumber)
                                    {
                                        dataValidations = false;
                                        TempData["Error"] = "Item number duplicated inside excel sheet";
                                        return RedirectToAction(nameof(Index));
                                    }

                                    var checkItemNumber = await dbContext.Items.AnyAsync(c => c.ItemNo == int.Parse(itemNumber));

                                    if (checkItemNumber)
                                    {
                                        dataValidations = false;
                                        TempData["Error"] = "There is item number in sheet already exists inside the system";
                                        return RedirectToAction(nameof(Index));
                                    }

                                    var item = new Item
                                    {
                                        ItemNo = int.Parse(itemNumber),
                                        Description = itemDescription,
                                        Quantity = int.Parse(itemQuantity),
                                        Price = double.Parse(itemPrice),
                                        TotalPrice = double.Parse(itemTotalPrice),
                                        UnitId = int.Parse(itemUnitId),
                                        ContractorId = int.Parse(itemContractorId)
                                    };
                                    items.Add(item);
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
                            await dbContext.Items.AddRangeAsync(items);
                            await dbContext.SaveChangesAsync();
                            TempData["Success"] = "Items excel sheet uploaded successfully";
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
