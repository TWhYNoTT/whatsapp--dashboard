using System;
using System.Collections.Generic;

namespace Labys.Domain.Common
{
    /// <summary>
    /// Base response type for consistent API responses
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public T? Data { get; set; }

        public string? ErrorCode { get; set; }

        // Factory methods for creating responses
        public static ApiResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, string errorCode = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// Pagination support for collections
    /// </summary>
    public class PaginatedResponse<T> : ApiResponse<IEnumerable<T>>
    {
        public int TotalCount { get; set; }

        public int PageSize { get; set; }

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public bool HasPrevious => CurrentPage > 1;

        public bool HasNext => CurrentPage < TotalPages;

        public static PaginatedResponse<T> CreatePaginated(
            IEnumerable<T> data,
            int totalCount,
            int pageSize,
            int currentPage)
        {
            return new PaginatedResponse<T>
            {
                Success = true,
                Data = data,
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}