using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFilteringLibrary.Enums
{
    public enum RequestValidationStatus
    {
        InvalidFields = 1,
        InvalidFilter = 2,
        InvalidSort = 3,
        InvalidSkip = 4,
        InvalidTake = 5,
        Valid = 6
    }
}
