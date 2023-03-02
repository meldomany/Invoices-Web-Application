using InvoiceApp.Data;
using InvoiceApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace InvoiceApp.Controllers
{
    public class UnitController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public UnitController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUnitsList()
        {
            var units = await dbContext.Units.ToListAsync();
            return Json(new { data = units });
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Unit unit)
        {
            if (ModelState.IsValid)
            {
                var unitName = await dbContext.Units.AnyAsync(c => c.Name == unit.Name);

                if (!unitName)
                {
                    await dbContext.Units.AddAsync(unit);
                    await dbContext.SaveChangesAsync();
                    TempData["Success"] = "Unit name: " + unit.Name + " created successfully";
                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = "Unit name should be unique";
                return View(unit);
            }
            TempData["Error"] = "Check your fields";
            return View(unit);
        }

        public async Task<IActionResult> Edit(int unitId)
        {
            if (unitId > 0)
            {
                var unit = await dbContext.Units.FindAsync(unitId);
                if (unit != null)
                {
                    return View(unit);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Unit unit)
        {
            if (ModelState.IsValid)
            {
                if (unit.Id > 0)
                {
                    var units = await dbContext.Units.Where(c => c.Id != unit.Id).ToListAsync();
                    var unitNames = units.Any(c => c.Name == unit.Name);

                    if (!unitNames)
                    {
                        dbContext.Units.Update(unit);
                        await dbContext.SaveChangesAsync();
                        TempData["Success"] = "Unit name: " + unit.Name + " updated";
                        return RedirectToAction(nameof(Index));
                    }
                    TempData["Error"] = "Unit name should be unique";
                    return View(unit);
                }
                TempData["Error"] = "There is no unit to update";
                return View(unit);
            }
            TempData["Error"] = "Check your fields";
            return View(unit);
        }

        public async Task<IActionResult> Delete(int unitId)
        {
            if (unitId > 0)
            {
                var unit = await dbContext.Units.FindAsync(unitId);
                if (unit != null)
                {
                    dbContext.Units.Remove(unit);
                    await dbContext.SaveChangesAsync();
                    TempData["Success"] = "Unit name: " + unit.Name + " deleted";
                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = "There is no unit to delete";
                return View(unit);
            }
            TempData["Error"] = "There is no unit to delete";
            return RedirectToAction(nameof(Index));
        }


        // Excel Management
        public async Task<IActionResult> ExportToExcel()
        {
            var units = await dbContext.Units.ToListAsync();

            //Start Exporting to excel
            var stream = new MemoryStream();
            using (var xlPackage = new ExcelPackage(stream))
            {
                //Define Worksheet
                var worksheet = xlPackage.Workbook.Worksheets.Add("Units");

 
                //First Row
                var startRow = 2;
                var row = startRow;


                //Table Header
                worksheet.Cells["A1"].Value = "Id";
                worksheet.Cells["B1"].Value = "Name";
                worksheet.Cells["A1:B1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells["A1:B1"].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

                row = 2;
                foreach (var unit in units)
                {
                    worksheet.Cells[row, 1].Value = unit.Id;
                    worksheet.Cells[row, 2].Value = unit.Name;
                    row++;
                }

                xlPackage.Workbook.Properties.Title = "Units";
                xlPackage.Save();
            }

            stream.Position = 0;
            TempData["Success"] = "Units excel sheet exported successfully";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "units.xlssx.xlsx");
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
                    var units = new List<Unit>();
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
                                    var name = worksheet.Cells[row, 1].Value?.ToString();

                                    //client side validation
                                    var clientUnitName = units.Any(c => c.Name == name);

                                    if (clientUnitName)
                                    {
                                        dataValidations = false;
                                        TempData["Error"] = "Unit name is duplicated inside the excel sheet";
                                        return RedirectToAction(nameof(Index));
                                    }

                                    var checkUnitName = await dbContext.Units.AnyAsync(c => c.Name == name);

                                    if (checkUnitName)
                                    {
                                        dataValidations = false;
                                        TempData["Error"] = "There is unit name in sheet already exists inside the system";
                                        return RedirectToAction(nameof(Index));
                                    }

                                    var unit = new Unit
                                    {
                                        Name = name,
                                    };
                                    units.Add(unit);
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
                            await dbContext.Units.AddRangeAsync(units);
                            await dbContext.SaveChangesAsync();
                            TempData["Success"] = "Units excel sheet uploaded successfully";
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