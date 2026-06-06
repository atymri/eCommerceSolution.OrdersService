using MongoDB.Bson.Serialization.IdGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.BLL.DTOs;

public record UserRersponse(Guid userId, string? Email, string? PersonName, string? Gender)
{
    public UserRersponse() : this(default, default, default, default)
    {
        
    }
}
