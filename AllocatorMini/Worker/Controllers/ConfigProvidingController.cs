using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Worker.Controllers
{
    public abstract class ConfigProvidingController<T> : ApiController where T : HttpConfiguration
    {
        protected T GetConfiguration()
        {
            return this.Configuration as T;
        }
    }
}
