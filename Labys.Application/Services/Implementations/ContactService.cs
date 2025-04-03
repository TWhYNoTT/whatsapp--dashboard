using Labys.Application.Services.Interfaces;
using Labys.Domain.Common;
using Labys.Domain.Entities;
using Labys.Infrastructure.Repositories.Interfaces;

namespace Labys.Application.Services.Implementations
{
    /// <summary>
    /// Contact service implementation
    /// </summary>
    public class ContactService : IContactService
    {
        private readonly IContactRepository _contactRepository;
        private readonly IInvoiceRepository _invoiceRepository;

        public ContactService(
            IContactRepository contactRepository,
            IInvoiceRepository invoiceRepository)
        {
            _contactRepository = contactRepository;
            _invoiceRepository = invoiceRepository;
        }

        /// <summary>
        /// Get a contact by ID
        /// </summary>
        public async Task<ApiResponse<Contact>> GetContactAsync(int id)
        {
            try
            {
                var contact = await _contactRepository.GetByIdAsync(id);
                if (contact == null)
                {
                    return ApiResponse<Contact>.ErrorResponse("Contact not found");
                }

                return ApiResponse<Contact>.SuccessResponse(contact);
            }
            catch (Exception ex)
            {
                return ApiResponse<Contact>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get a contact by phone number
        /// </summary>
        public async Task<ApiResponse<Contact>> GetContactByPhoneNumberAsync(string phoneNumber)
        {
            try
            {
                // Clean the phone number
                phoneNumber = phoneNumber.Replace("whatsapp:", "").Trim();

                var contact = await _contactRepository.GetByPhoneNumberAsync(phoneNumber);
                if (contact == null)
                {
                    return ApiResponse<Contact>.ErrorResponse("Contact not found");
                }

                return ApiResponse<Contact>.SuccessResponse(contact);
            }
            catch (Exception ex)
            {
                return ApiResponse<Contact>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get contacts with pagination and filtering
        /// </summary>
        public async Task<PaginatedResponse<Contact>> GetContactsAsync(int page, int pageSize, string searchTerm = null, string tag = null)
        {
            try
            {
                IEnumerable<Contact> contacts;
                int totalContacts;

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    contacts = await _contactRepository.SearchContactsAsync(searchTerm, page, pageSize);
                    totalContacts = await _contactRepository.CountAsync(c =>
                        c.Name.Contains(searchTerm) ||
                        c.PhoneNumber.Contains(searchTerm) ||
                        c.Email.Contains(searchTerm));
                }
                else if (!string.IsNullOrWhiteSpace(tag))
                {
                    contacts = await _contactRepository.GetContactsByTagAsync(tag, page, pageSize);
                    totalContacts = await _contactRepository.CountAsync(c => c.Tags != null && c.Tags.Contains(tag));
                }
                else
                {
                    contacts = await _contactRepository.FindAsync(c => true);
                    totalContacts = await _contactRepository.CountAsync();

                    // Apply pagination manually
                    contacts = contacts
                        .OrderBy(c => c.Name)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                }

                return PaginatedResponse<Contact>.CreatePaginated(
                    contacts,
                    totalContacts,
                    pageSize,
                    page
                );
            }
            catch (Exception ex)
            {
                return new PaginatedResponse<Contact>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        /// <summary>
        /// Create a new contact
        /// </summary>
        public async Task<ApiResponse<Contact>> CreateContactAsync(Contact contact)
        {
            try
            {
                // Validate that phone number is unique
                bool exists = await _contactRepository.PhoneNumberExistsAsync(contact.PhoneNumber);
                if (exists)
                {
                    return ApiResponse<Contact>.ErrorResponse("A contact with this phone number already exists");
                }

                // Set default values
                contact.LastContactDate = DateTime.UtcNow;
                if (contact.HasOptedIn && !contact.OptInDate.HasValue)
                {
                    contact.OptInDate = DateTime.UtcNow;
                }

                // Save to repository
                var createdContact = await _contactRepository.AddAsync(contact);
                return ApiResponse<Contact>.SuccessResponse(createdContact, "Contact created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<Contact>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update an existing contact
        /// </summary>
        public async Task<ApiResponse<Contact>> UpdateContactAsync(int id, Contact contact)
        {
            try
            {
                var existingContact = await _contactRepository.GetByIdAsync(id);
                if (existingContact == null)
                {
                    return ApiResponse<Contact>.ErrorResponse("Contact not found");
                }

                // Check for phone number conflict (if changed)
                if (existingContact.PhoneNumber != contact.PhoneNumber)
                {
                    bool exists = await _contactRepository.PhoneNumberExistsAsync(contact.PhoneNumber);
                    if (exists)
                    {
                        return ApiResponse<Contact>.ErrorResponse("Another contact with this phone number already exists");
                    }
                }

                // Update properties
                existingContact.PhoneNumber = contact.PhoneNumber;
                existingContact.Name = contact.Name;
                existingContact.Email = contact.Email;
                existingContact.Language = contact.Language;
                existingContact.Tags = contact.Tags;
                existingContact.Notes = contact.Notes;
                existingContact.CustomerId = contact.CustomerId;

                // Track opt-in status changes
                if (contact.HasOptedIn && !existingContact.HasOptedIn)
                {
                    existingContact.HasOptedIn = true;
                    existingContact.OptInDate = DateTime.UtcNow;
                }
                else if (!contact.HasOptedIn && existingContact.HasOptedIn)
                {
                    existingContact.HasOptedIn = false;
                    // Keep the OptInDate for record-keeping
                }

                await _contactRepository.UpdateAsync(existingContact);
                return ApiResponse<Contact>.SuccessResponse(existingContact, "Contact updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<Contact>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete a contact
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteContactAsync(int id)
        {
            try
            {
                var contact = await _contactRepository.GetByIdAsync(id);
                if (contact == null)
                {
                    return ApiResponse<bool>.ErrorResponse("Contact not found");
                }

                await _contactRepository.DeleteAsync(contact);
                return ApiResponse<bool>.SuccessResponse(true, "Contact deleted successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Ensure a contact exists in the database
        /// </summary>
        public async Task<ApiResponse<(bool IsNew, Contact Contact)>> EnsureContactExistsAsync(string phoneNumber, string name = null)
        {
            try
            {
                // Clean the phone number
                phoneNumber = phoneNumber.Replace("whatsapp:", "").Trim();

                // Check if exists
                var existingContact = await _contactRepository.GetByPhoneNumberAsync(phoneNumber);
                if (existingContact != null)
                {
                    return ApiResponse<(bool IsNew, Contact Contact)>.SuccessResponse((false, existingContact));
                }

                // Create new contact
                var newContact = new Contact
                {
                    PhoneNumber = phoneNumber,
                    Name = name ?? "New Contact",
                    HasOptedIn = true,
                    OptInDate = DateTime.UtcNow,
                    LastContactDate = DateTime.UtcNow,
                    Tags = "auto-created"
                };

                await _contactRepository.AddAsync(newContact);

                return ApiResponse<(bool IsNew, Contact Contact)>.SuccessResponse((true, newContact));
            }
            catch (Exception ex)
            {
                return ApiResponse<(bool IsNew, Contact Contact)>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get all unique tags from contacts
        /// </summary>
        public async Task<ApiResponse<IEnumerable<string>>> GetAllTagsAsync()
        {
            try
            {
                var contacts = await _contactRepository.FindAsync(c => c.Tags != null && c.Tags != "");

                // Extract and flatten tags
                var allTags = contacts
                    .SelectMany(c => c.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                return ApiResponse<IEnumerable<string>>.SuccessResponse(allTags);
            }
            catch (Exception ex)
            {
                return ApiResponse<IEnumerable<string>>.ErrorResponse(ex.Message);
            }
        }

        /// <summary>
        /// Import contacts from invoices
        /// </summary>
        public async Task<ApiResponse<int>> ImportFromInvoicesAsync()
        {
            try
            {
                // Get all invoices
                var invoices = await _invoiceRepository.GetAllAsync();

                // Group by phone number to avoid duplicates
                var uniqueCustomers = invoices
                    .Where(i => !string.IsNullOrEmpty(i.PhoneNumber) && !string.IsNullOrEmpty(i.CustomerName))
                    .GroupBy(i => i.PhoneNumber)
                    .Select(g => g.First())
                    .ToList();

                int importedCount = 0;

                foreach (var invoice in uniqueCustomers)
                {
                    // Check if contact already exists
                    bool exists = await _contactRepository.PhoneNumberExistsAsync(invoice.PhoneNumber);
                    if (!exists)
                    {
                        // Create new contact
                        var contact = new Contact
                        {
                            PhoneNumber = invoice.PhoneNumber,
                            Name = invoice.CustomerName,
                            HasOptedIn = true,  // Assume opt-in for existing customers
                            OptInDate = DateTime.UtcNow,
                            LastContactDate = invoice.InitDate,
                            CustomerId = invoice.InvoiceId,
                            Tags = "imported,customer"
                        };

                        await _contactRepository.AddAsync(contact);
                        importedCount++;
                    }
                }

                return ApiResponse<int>.SuccessResponse(importedCount, $"Successfully imported {importedCount} contacts");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.ErrorResponse(ex.Message);
            }
        }


        /// <summary>
        /// Import contacts from an Excel file and save them with tags.
        /// </summary>
      

        public async Task<ApiResponse<int>> ImportContactsAsync(IList<Contact> contacts)
        {
            try
            {
                int importedCount = 0;

                foreach (var contact in contacts)
                {
                    // Check if the contact already exists
                    var existingContact = await _contactRepository.GetByPhoneNumberAsync(contact.PhoneNumber);
                    if (existingContact == null)
                    {
                        // New contact: Save it with the tag from Excel
                        contact.LastContactDate = DateTime.UtcNow;
                        if (contact.HasOptedIn && !contact.OptInDate.HasValue)
                        {
                            contact.OptInDate = DateTime.UtcNow;
                        }

                        await _contactRepository.AddAsync(contact);
                        importedCount++;
                    }
                    else
                    {
                        // Update existing contact with new tags (append if necessary)
                        if (!string.IsNullOrWhiteSpace(contact.Tags))
                        {
                            var existingTags = existingContact.Tags?.Split(',').Select(t => t.Trim()).ToList() ?? new List<string>();
                            var newTags = contact.Tags.Split(',').Select(t => t.Trim()).ToList();

                            foreach (var tag in newTags)
                            {
                                if (!existingTags.Contains(tag))
                                {
                                    existingTags.Add(tag);
                                }
                            }

                            existingContact.Tags = string.Join(", ", existingTags);
                            await _contactRepository.UpdateAsync(existingContact);
                        }
                    }
                }

                return ApiResponse<int>.SuccessResponse(importedCount, $"Successfully imported {importedCount} new contacts.");
            }
            catch (Exception ex)
            {
                return ApiResponse<int>.ErrorResponse(ex.Message);
            }
        }
    }
}