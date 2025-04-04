﻿using Labys.Application.Services.Interfaces;
using Labys.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Threading.Tasks;

using ExcelDataReader;
using System.Text;
using System.Data;
namespace Labys.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "SuperAdmin,Admin")]
    public class ContactsController : ControllerBase
    {
        private readonly IContactService _contactService;

        public ContactsController(IContactService contactService)
        {
            _contactService = contactService;
        }

        /// <summary>
        /// Get all contacts with pagination and filtering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetContacts(
            int page = 1,
            int pageSize = 20,
            string searchTerm = null,
            string tag = null,
            bool? optedIn = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var response = await _contactService.GetContactsAsync(page, pageSize, searchTerm);
            return Ok(response);
        }

        /// <summary>
        /// Get a specific contact by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetContact(int id)
        {
            var response = await _contactService.GetContactAsync(id);

            if (response.Success)
                return Ok(response.Data);

            return NotFound(new { Error = response.Message });
        }

        /// <summary>
        /// Get a contact by phone number
        /// </summary>
        [HttpGet("phone/{phoneNumber}")]
        public async Task<IActionResult> GetContactByPhone(string phoneNumber)
        {
            // Clean phone number
            phoneNumber = phoneNumber.Replace("whatsapp:", "").Trim();

            var response = await _contactService.GetContactByPhoneNumberAsync(phoneNumber);

            if (response.Success)
                return Ok(response.Data);

            return NotFound(new { Error = response.Message });
        }

        /// <summary>
        /// Create a new contact
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateContact([FromBody] Contact contactDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _contactService.CreateContactAsync(contactDto);

            if (response.Success)
                return CreatedAtAction(nameof(GetContact), new { id = response.Data.Id }, response.Data);

            return BadRequest(new { Error = response.Message });
        }



        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportContactsFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a valid Excel file.");

            var contacts = new List<Contact>();

            try
            {
                // Required for ExcelDataReader to work in .NET Core
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0; // Reset stream position

                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        var dataset = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = _ => new ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = true // Treat first row as header
                            }
                        });

                        var dataTable = dataset.Tables[0]; // First sheet

                        foreach (DataRow row in dataTable.Rows)
                        {
                            string phoneNumber = row[0]?.ToString().Trim(); // Column A
                            string tag = row[1]?.ToString().Trim(); // Column B

                            if (!string.IsNullOrEmpty(phoneNumber))
                            {
                                contacts.Add(new Contact
                                {
                                    PhoneNumber = phoneNumber,
                                    Tags = tag
                                });
                            }
                        }
                    }
                }

                // Save contacts with tags
                var response = await _contactService.ImportContactsAsync(contacts);
                return Ok(new { Message = $"Imported {contacts.Count} contacts successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Error processing the file", Details = ex.Message });
            }
        }


        /// <summary>
        /// Update an existing contact
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContact(int id, [FromBody] Contact contactDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _contactService.UpdateContactAsync(id, contactDto);

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Delete a contact
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContact(int id)
        {
            var response = await _contactService.DeleteContactAsync(id);

            if (response.Success)
                return NoContent();

            return NotFound(new { Error = response.Message });
        }

        /// <summary>
        /// Get all tags for filtering
        /// </summary>
        [HttpGet("tags")]
        public async Task<IActionResult> GetTags()
        {
            var response = await _contactService.GetAllTagsAsync();

            if (response.Success)
                return Ok(response.Data);

            return BadRequest(new { Error = response.Message });
        }

        /// <summary>
        /// Import contacts from invoices
        /// </summary>
        [HttpPost("import-from-invoices")]
        public async Task<IActionResult> ImportFromInvoices()
        {
            var response = await _contactService.ImportFromInvoicesAsync();

            if (response.Success)
                return Ok(new
                {
                    Message = $"Successfully imported {response.Data} contacts",
                    ImportedCount = response.Data
                });

            return BadRequest(new { Error = response.Message });
        }
    }
}