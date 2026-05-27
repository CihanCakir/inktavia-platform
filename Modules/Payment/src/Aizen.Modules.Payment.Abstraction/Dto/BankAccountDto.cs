using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Payment.Abstraction.Dto
{
public class BankAccountDto
{
    public required string IBAN { get; set; }
    public required string AccountHolderName { get; set; }
    public required string BankName { get; set; }
}

}