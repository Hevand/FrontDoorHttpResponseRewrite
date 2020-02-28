using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace AFD.Samples.HttpResponseRewrite.IISModule
{ 
    /*
     * The module is a .NET class that implements the ASP.NET System.Web.IHttpModule interface, and uses the APIs in the System.Web namespace to participate in one or more of ASP.NET's request processing stages.
     */
    public class ContentRewriteHttpModule : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += AddRewriteFilter;
        }

        private string customer_base_uri;
        private const string CUSTOMER_HOST_NAME = "CUSTOMER_HOST_NAME";

        private void AddRewriteFilter(object sender, EventArgs args)
        {
            HttpApplication app = (HttpApplication)sender;
            HttpRequest req = app.Request;

            if (req.Headers.AllKeys.Contains(CUSTOMER_HOST_NAME))
            {
                this.customer_base_uri = req.Headers[CUSTOMER_HOST_NAME];

                HttpResponse response = app.Response;
                var filter = new ResponseRewriteFilter(response.Filter);
                filter.TransformString += Filter_TransformString;
                response.Filter = filter;
            }
        }

        private string Filter_TransformString(string arg)
        {
            return arg.Replace("//localhost/", $"//{customer_base_uri}/");
        }
    }
}
