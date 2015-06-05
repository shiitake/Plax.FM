using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaxFm.Core.CustomExceptions
{
    public class IncompleteConfiguration : Exception
    {
        public override string Message
        {
            get { return "Configuration is incomplete."; }
        }
    }

    public class IncompleteAuthorization : Exception
    {
        public override string Message
        {
            get
            {
                return "Authorization is incomplete. The service will not work until the account has been authorized.";
            }
        }
    }
}
