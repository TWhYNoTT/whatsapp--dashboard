using Labys.Domain.Common;
using Labys.Domain.Entities;

namespace Labys.Application.Services.Interfaces
{
    /// <summary>
    /// Contact service interface
    /// </summary>
    public interface IContactService
    {
        Task<ApiResponse<Contact>> GetContactAsync(int id);
        Task<ApiResponse<Contact>> GetContactByPhoneNumberAsync(string phoneNumber);
        Task<PaginatedResponse<Contact>> GetContactsAsync(int page, int pageSize, string searchTerm = null, string tag = null);
        Task<ApiResponse<Contact>> CreateContactAsync(Contact contact);
        Task<ApiResponse<Contact>> UpdateContactAsync(int id, Contact contact);
        Task<ApiResponse<bool>> DeleteContactAsync(int id);
        Task<ApiResponse<(bool IsNew, Contact Contact)>> EnsureContactExistsAsync(string phoneNumber, string name = null);
        Task<ApiResponse<IEnumerable<string>>> GetAllTagsAsync();
        Task<ApiResponse<int>> ImportFromInvoicesAsync();
    }
}