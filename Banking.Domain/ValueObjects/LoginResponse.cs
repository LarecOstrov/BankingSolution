using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banking.Domain.ValueObjects
{
    public record LoginResponse(string AccessToken, string RefreshToken);
}
