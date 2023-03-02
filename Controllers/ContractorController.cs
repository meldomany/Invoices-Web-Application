using InvoiceApp.Data;
using InvoiceApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    public class ContractorController : Controller
    {
        private readonly ApplicationDbContext dbContext;

        public ContractorController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetContractorsList()
        {
            var contractors = await dbContext.Contractors.ToListAsync();
            return Json(new { data = contractors });
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contractor contractor)
        {
            if (ModelState.IsValid)
            {
                var contractorNumber = await dbContext.Contractors.AnyAsync(c => c.Number == contractor.Number);
                var contractorName = await dbContext.Contractors.AnyAsync(c => c.Name == contractor.Name);

                if(!contractorNumber && !contractorName)
                {
                    await dbContext.Contractors.AddAsync(contractor);
                    await dbContext.SaveChangesAsync();
                    TempData["Success"] = "Contractor number: "+ contractor.Number +" created successfully";
                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = "The contractor number and name should be unique";
                return View(contractor);
            }
            TempData["Error"] = "Check your fields";
            return View(contractor);
        }

        public async Task<IActionResult> Edit(int contractorId)
        {
            if (contractorId > 0)
            {
                var contractor = await dbContext.Contractors.FindAsync(contractorId);
                if (contractor != null)
                {
                    return View(contractor);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Contractor contractor)
        {
            if (ModelState.IsValid)
            {
                if(contractor.Id > 0)
                {
                    var contractors = await dbContext.Contractors.Where(c => c.Id != contractor.Id).ToListAsync();
                    var contractorNumbers = contractors.Any(c => c.Number == contractor.Number);
                    var contractorNames = contractors.Any(c => c.Name == contractor.Name);

                    if (!contractorNumbers && !contractorNames)
                    {
                        dbContext.Contractors.Update(contractor);
                        await dbContext.SaveChangesAsync();
                        TempData["Success"] = "Contractor number: " + contractor.Number + "updated";
                        return RedirectToAction(nameof(Index));
                    }
                    TempData["Error"] = "The contractor number and name should be unique";
                    return View(contractor);
                }
                TempData["Error"] = "There is no contractor to delete";
                return View(contractor);
            }
            TempData["Error"] = "Check your fields";
            return View(contractor);
        }

        public async Task<IActionResult> Delete(int contractorId)
        {
            if (contractorId > 0)
            {
                var contractor = await dbContext.Contractors.FindAsync(contractorId);
                if (contractor != null)
                {
                    dbContext.Contractors.Remove(contractor);
                    await dbContext.SaveChangesAsync();
                    TempData["Success"] = "Contractor number: " + contractor.Number + " deleted";
                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = "There is no contractor to delete";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = "There is no contractor to delete";
            return RedirectToAction(nameof(Index));
        }


        // Excel Management
        public async Task<IActionResult> ExportToExcel()
        {
            var contractors = await dbContext.Contractors.ToListAsync();

            //Start Exporting to excel
            var stream = new MemoryStream();
            using(var xlPackage = new ExcelPackage(stream))
            {
                //Define Worksheet
                var worksheet = xlPackage.Workbook.Worksheets.Add("Contractors");

                //First Row
                var startRow = 2;
                var row = startRow;

                //Table Header
                worksheet.Cells["A1"].Value = "Id";
                worksheet.Cells["B1"].Value = "Name";
                worksheet.Cells["C1"].Value = "Number";
                worksheet.Cells["A1:C1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells["A1:C1"].Style.Fill.BackgroundColor.SetColor(Color.Yellow);

                row = 2;
                foreach (var contractor in contractors)
                {
                    worksheet.Cells[row, 1].Value = contractor.Id;
                    worksheet.Cells[row, 2].Value = contractor.Name;
                    worksheet.Cells[row, 3].Value = contractor.Number;
                    row++;
                }

                xlPackage.Workbook.Properties.Title = "Contractors";
                xlPackage.Save();
            }

            stream.Position = 0;
            TempData["Succcess"] = "Contractors excel sheet exported successfully";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "contractors.xlssx.xlsx");
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
                if(file?.Length > 0)
                {
                    //Convert file to a stream
                    var stream = file.OpenReadStream();
                    var contractors = new List<Contractor>();
                    var dataValidations = true;
                    try
                    {
                        using(var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets.First();
                            var rowCount = worksheet.Dimension.Rows;

                            for (int row = 2; row < rowCount+1; row++)
                            {
                                try
                                {
                                    var name = worksheet.Cells[row, 1].Value?.ToString();
                                    var number = worksheet.Cells[row, 2].Value.ToString();

                                    //client side validation
                                    var clientContractorName = contractors.Any(c => c.Name == name);
                                    var clientContractorNumber = contractors.Any(c => c.Number == int.Parse(number));

                                    if (clientContractorName || clientContractorNumber)
                                    {
                                        dataValidations = false;
                                        TempData["Error"] = "Contractor name or number duplicated inside excel sheet";
                                        return RedirectToAction(nameof(Index));
                                    }

                                    var checkContractorName = await dbContext.Contractors.AnyAsync(c => c.Name == name);
                                    var checkContractorNumber = await dbContext.Contractors.AnyAsync(c => c.Number == int.Parse(number));

                                    if(checkContractorName || checkContractorNumber)
                                    {
                                        dataValidations = false;
                                        TempData["Error"] = "There is contractor name or number in sheet already exists inside the system";
                                        return RedirectToAction(nameof(Index));
                                    }

                                    var contractor = new Contractor
                                    {
                                        Name = name,
                                        Number = int.Parse(number)
                                    };
                                    contractors.Add(contractor);
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
                            await dbContext.Contractors.AddRangeAsync(contractors);
                            await dbContext.SaveChangesAsync();
                            TempData["Success"] = "Contractors excel sheet uploaded successfully";
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
