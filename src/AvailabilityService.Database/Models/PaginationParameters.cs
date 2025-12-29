using System.ComponentModel.DataAnnotations;

namespace AvailabilityService.Database.Models;

public class PaginationParameters
{
    private const int MaxPageSize = 100;
    private int _limit = 100;

    [Range(0, int.MaxValue, ErrorMessage = "Offset must be greater than or equal to 0")]
    public int Offset { get; set; } = 0;

    [Range(1, 100, ErrorMessage = "Limit must be between 1 and 100")]
    public int Limit
    {
        get => _limit;
        set => _limit = (value > MaxPageSize) ? MaxPageSize : value;
    }
}