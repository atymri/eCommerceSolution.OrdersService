namespace eCommerce.BLL.DTOs;

public class UserValidationResult
{
    public bool IsSuccess => InvalidGuids.Count == 0;
    public List<Guid> InvalidGuids { get; set; } = new List<Guid>();
    public List<UserRersponse> Users { get; set; } = new List<UserRersponse>();
}
