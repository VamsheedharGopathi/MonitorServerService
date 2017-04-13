using System;
using System.Diagnostics.Contracts;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Linq;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Newtonsoft.Json;
using MonitorStorage;
using MonitorStorage.Models;
using System.Configuration;
using Microsoft.WindowsAzure.Storage;
using System.Text;
using System.Collections.Generic;

namespace ApplicationWatcher.Controllers
{
    /// <summary>
    /// ApplicationAuthenticationFilter
    /// </summary>
    public class ApplicationAuthorizationFilterAttribute : AuthorizationFilterAttribute
    {
        private string Storage
        {
            get { return ConfigurationManager.AppSettings["storage"] != null ? ConfigurationManager.AppSettings["storage"].ToString() : ""; }
        }

        /// <summary>
        /// OnAuthorization
        /// </summary>
        /// <param name="actionContext"></param>
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            //Task.Run(async () =>
            //{
            if (SkipAuthorization(actionContext))
            {
                return;
            }
            string username = string.Empty;
            string password = string.Empty;
            if (GetUserNameAndPassword(actionContext, out username, out password))
            {
                if (ValidateUser(username, password))
                {
                   // actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
                    //if (!isUserAuthorized(username))
                    //actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden);
                    //}
                    // else
                    // {
                    // actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
                }
                else
                {
                    actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.ProxyAuthenticationRequired);
                }
            }
            else
            {
                actionContext.Response = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            }
            // });
        }

        private bool GetUserNameAndPassword(HttpActionContext actionContext, out string username, out string password)
        {
            try
            {
                string requestParameter = actionContext.Request.Headers.Authorization.Parameter;
                byte[] resultuser = Convert.FromBase64String(requestParameter);
                string returnValue = ASCIIEncoding.ASCII.GetString(resultuser);
                var data = new
                {
                    EmailId = string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    ProjectName = string.Empty,
                    ExpriyDays = 0,
                    PhoneNumber = string.Empty,
                    Password = string.Empty
                };
                var result = JsonConvert.DeserializeAnonymousType(returnValue, data);
                username = result.EmailId;
                password = ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(result.Password));

                return true;
            }
            catch (Exception ex)
            {
                username = string.Empty;
                password = string.Empty;
                return false;
            }
        }

        private bool ValidateUser(string userName, string password)
        {
            CloudStorageAccount _cloudStorageAccount = CloudStorageAccount.Parse(Storage);
            Storage<User> userStorage = new Storage<User>(_cloudStorageAccount);
            User user = new User();
            user._userStorage = userStorage;
            Task<bool>[] tasks = new Task<bool>[1];
            tasks[0]= Task.Run(async () =>
            {
                IEnumerable<User> userData = await user.ReadUser($"EmailID eq '{userName}' and Password eq '{password}'");
               return userData.Any();
            });
            Task.WaitAll(tasks);
            return tasks[0].Result;
        }

        private static bool SkipAuthorization(HttpActionContext actionContext)
        {
            Contract.Assert(actionContext != null);
            return actionContext.ActionDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Count > 0
                   || actionContext.ControllerContext.ControllerDescriptor.GetCustomAttributes<AllowAnonymousAttribute>().Count > 0;
        }



    }
}