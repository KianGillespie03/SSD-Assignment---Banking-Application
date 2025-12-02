using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;

namespace SSD_Assignment___Banking_Application
{
    public class AuthenticationService
    {
        private const string DOMAIN = "ITSLIGO.LAN";
        private const string GROUP_TELLER = "Bank Teller";
        private const string GROUP_ADMIN = "Bank Teller Administrator";

        public static bool AuthenticateUser(string username, string password)
        {
            try
            {
                using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, DOMAIN))
                {
                    bool isValid = pc.ValidateCredentials(username, password);
                    Logging.LogLoginAttempt(username, isValid, DateTime.Now, appMetadata: "SSD Assignment Banking Application v1.0.0");
                    return isValid;
                }
            }
            catch (Exception ex)
            {
                Logging.LogLoginAttempt(username, false, DateTime.Now, appMetadata: "SSD Assignment Banking Application v1.0.0", "AUTHENTICATION ERROR: " + ex.Message);
                return false;
            }
        }

        public static bool IsUserInTellerGroup(string username)
        {
            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, DOMAIN))
            using (UserPrincipal user = UserPrincipal.FindByIdentity(pc, username))
            {
                if (user == null) return false;

                foreach (var group in user.GetAuthorizationGroups())
                {
                    if (group.SamAccountName == GROUP_TELLER)
                        return true;
                }
                return false;
            }
        }

        public static bool IsUserAdmin(string username)
        {
            using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, DOMAIN))
            using (UserPrincipal user = UserPrincipal.FindByIdentity(pc, username))
            {
                if (user == null) return false;

                foreach (var group in user.GetAuthorizationGroups())
                {
                    if (group.SamAccountName == GROUP_ADMIN)
                        return true;
                }
                return false;
            }
        }
    }
}

