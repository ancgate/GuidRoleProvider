using ActiveDirectoryCommunicator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;

namespace GuidRoleProvider
{
    /// <summary>
    /// In general, all methods are case insensitive
    /// </summary>
    public sealed class GRoleProvider : RoleProvider
    {
        public override string ApplicationName { get; set; }

        /// <summary>
        /// If role is not found, it is ignored
        /// </summary>
        /// <param name="usernames"></param>
        /// <param name="roleNames"></param>
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            usernames = StripDomains(usernames);

            using (var context = new RoleProviderContext())
            {
                foreach (string username in usernames)
                {
                    DataRow user = ResolveUserRow(username, context,
                        (c) => { return c.db.Tables[context.userTable].AsEnumerable().SingleOrDefault(x => x.Field<string>(context.userNameCol).Equals(username, StringComparison.OrdinalIgnoreCase)); }
                        );

                    if (user != null)
                    {
                        var AllDbRoles = context.db.Tables[context.roleTable].AsEnumerable().ToList();

                        foreach (var role in AllDbRoles)
                        {
                            foreach (string roleName in roleNames)
                            {
                                // Don't apply duplicates
                                if (role[context.roleNameCol].ToString().Equals(roleName, StringComparison.OrdinalIgnoreCase)
                                    && !user.GetChildRows(context.userJuncRelation).Any(x => x.Field<string>(context.roleNameCol).Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    DataRow newRow = context.db.Tables[context.userRoleTable].NewRow();
                                    newRow[context.userIdCol] = user[context.userIdCol];
                                    newRow[context.roleIdCol] = role[context.roleIdCol];
                                    context.db.Tables[context.userRoleTable].Rows.Add(newRow);
                                }
                            }
                        }
                    }
                }
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Will not create duplicates
        /// </summary>
        /// <param name="roleName"></param>
        public override void CreateRole(string roleName)
        {
            using (var context = new RoleProviderContext())
            {
                if (!context.db.Tables[context.roleTable].AsEnumerable().Any(x => x.Field<string>(context.roleNameCol)
                    .Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                {
                    DataRow newRow = context.db.Tables[context.roleTable].NewRow();
                    newRow[context.roleIdCol] = DBNull.Value; // null is for identity column (will be auto assigned)
                    newRow[context.roleNameCol] = roleName;
                    newRow[context.insertByCol] = HttpContext.Current.User.Identity.Name;
                    newRow[context.insertDtCol] = HttpContext.Current.User.Identity.Name;
                    context.db.Tables[context.roleTable].Rows.Add(newRow);
                    context.SaveChanges();
                }
            }
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            bool ret = false;

            using (var context = new RoleProviderContext())
            {
                try
                {
                    DataRow role = context.db.Tables[context.roleTable].AsEnumerable().SingleOrDefault(x => x.Field<string>(context.roleNameCol).Equals(roleName, StringComparison.OrdinalIgnoreCase));
                    if (role != null)
                    {
                        role.Delete();
                        context.SaveChanges();
                        ret = true;
                    }
                }
                catch
                {
                    ret = false;
                }
            }

            return ret;
        }

        /// <summary>
        /// WTH is the point of this method? Maybe I'm not understanding it correctly
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="usernameToMatch"></param>
        /// <returns></returns>
        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            usernameToMatch = StripDomain(usernameToMatch);
            List<string> users = new List<string>();

            using (var context = new RoleProviderContext())
            {
                var matches = ResolveUserRow(usernameToMatch, context,
                        (c) =>
                        {
                            return c.db.Tables[context.junctionTable].AsEnumerable().Where(x => x.Field<string>(context.roleNameCol).Equals(roleName, StringComparison.OrdinalIgnoreCase))
                                .Where(y => y.Field<string>(context.userNameCol).Equals(usernameToMatch, StringComparison.OrdinalIgnoreCase));
                        }
                        );

                foreach (var row in matches)
                {
                    users.Add(row[context.userNameCol].ToString());
                }
            }
            return users.ToArray();
        }

        public override string[] GetAllRoles()
        {
            List<string> roles = new List<string>();

            using (var context = new RoleProviderContext())
            {
                foreach (DataRow row in context.db.Tables[context.roleTable].Rows)
                {
                    roles.Add(row[context.roleNameCol].ToString());
                }
            }

            return roles.ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            username = StripDomain(username);
            List<string> roles = new List<string>();

            using (var context = new RoleProviderContext())
            {
                var user = ResolveUserRow(username, context,
                    (c) =>
                    {
                        return c.db.Tables[context.junctionTable].AsEnumerable().Where(x => x.Field<string>(context.userNameCol).Equals(username, StringComparison.OrdinalIgnoreCase));
                    }
                    );

                if (user != null)
                {
                    foreach (DataRow row in user)
                    {
                        roles.Add(row[context.roleNameCol].ToString());
                    }
                }
            }
            return roles.ToArray();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            List<string> users = new List<string>();

            using (var context = new RoleProviderContext())
            {
                var role = context.db.Tables[context.junctionTable].AsEnumerable().Where(x => x.Field<string>(context.roleNameCol).Equals(roleName, StringComparison.OrdinalIgnoreCase));

                if (role != null)
                {
                    foreach (DataRow row in role)
                    {
                        users.Add(row[context.userNameCol].ToString());
                    }
                }
            }
            return users.ToArray();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            username = StripDomain(username);
            bool isValid = false;

            using (var context = new RoleProviderContext())
            {
                var user = ResolveUserRow(username, context,
                    (c) =>
                    {
                        return c.db.Tables[context.junctionTable].AsEnumerable().Where(x => x.Field<string>(context.userNameCol).Equals(username, StringComparison.OrdinalIgnoreCase))
                                    .SingleOrDefault(y => y.Field<string>(context.roleNameCol).Equals(roleName, StringComparison.OrdinalIgnoreCase));
                    }
                    );

                if (user != null)
                {
                    isValid = true;
                }
            }

            return isValid;
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            usernames = StripDomains(usernames);
            using (var context = new RoleProviderContext())
            {
                foreach (string username in usernames)
                {
                    DataRow user = ResolveUserRow(username, context,
                        (c) => { return c.db.Tables[context.userTable].AsEnumerable().SingleOrDefault(x => x.Field<string>(context.userNameCol).Equals(username, StringComparison.OrdinalIgnoreCase)); }
                        );

                    if (user != null)
                    {
                        var AllDbRoles = context.db.Tables[context.roleTable].AsEnumerable();

                        foreach (DataRow role in AllDbRoles)
                        {
                            foreach (string roleName in roleNames)
                            {
                                if (role.Field<string>(context.roleNameCol).Equals(roleName, StringComparison.OrdinalIgnoreCase))
                                {
                                    var removeRow = user.GetChildRows(context.userFKeyRelation).SingleOrDefault(x => x.Field<int>(context.roleIdCol) == role.Field<int>(context.roleIdCol));

                                    if (removeRow != null)
                                    {
                                        removeRow.Delete();
                                    }
                                }
                            }
                        }
                    }
                }
                context.SaveChanges();
            }
        }

        public override bool RoleExists(string roleName)
        {
            bool isValid = false;

            using (var context = new RoleProviderContext())
            {
                if (context.db.Tables[context.roleTable].AsEnumerable().Any(x => x.Field<string>(context.roleNameCol).Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                {
                    isValid = true;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Retrieves user's role even if username has changed (performs GUID lookup)
        /// </summary>
        /// <param name="username">Username to resolve (current username)</param>
        /// <param name="context">Databse context to use</param>
        /// <param name="linq">LINQ query to search user tables for matching username. Return null if empty</param>
        /// <returns></returns>
        private T ResolveUserRow<T>(string username, RoleProviderContext context, Func<RoleProviderContext, T> linq)
        {
            username = StripDomain(username);

            // First try with current db
            T result = linq(context);
            if (result != null)
            {
                var emptyTest = result as EnumerableRowCollection<DataRow>;
                if (emptyTest == null || emptyTest.Any())
                {
                    return result;
                }
            }

            // Check with AD for updated username
            using (ActiveDirectoryComm adConn = new ActiveDirectoryComm())
            {
                UserCollection adUsers = adConn.GetAllUsersSimple();
                var user = (ActiveDirectoryCommunicator.User)adUsers.SingleOrDefault(x => x.LoginName.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user != null)
                {
                    var row = context.db.Tables[context.userTable].AsEnumerable().SingleOrDefault(x => x.Field<Guid>(context.userGuidCol).Equals(user.Guid.Value));

                    int nameSep = user.Name.IndexOf(' ');
                    string firstName, lastName;
                    if (nameSep > 0)
                    {
                        firstName = user.Name.Substring(0, nameSep);
                        lastName = user.Name.Substring(nameSep + 1);
                    }
                    else
                    {
                        firstName = user.Name;
                        lastName = string.Empty;
                    }

                    if (row != null)
                    {
                        row[context.userNameCol] = username;
                        row[context.userFNameCol] = firstName;
                        row[context.userLNameCol] = lastName;
                        row[context.userEmailCol] = user.Email;
                    }
                    else // Add new user
                    {
                        DataRow newRow = context.db.Tables[context.userTable].NewRow();
                        newRow[context.userGuidCol] = user.Guid.Value;
                        newRow[context.userFNameCol] = firstName;
                        newRow[context.userLNameCol] = lastName;
                        newRow[context.userNameCol] = username;
                        newRow[context.userEmailCol] = user.Email;
                        newRow[context.insertByCol] = HttpContext.Current.User.Identity.Name;
                        newRow[context.insertDtCol] = DateTime.Now;
                        context.db.Tables[context.userTable].Rows.Add(newRow);
                    }
                    context.SaveChanges();

                    // Second try with updated db
                    result = linq(context);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            // Username is not permissioned
            return default(T);
        }

        private string StripDomain(string username)
        {
            int domainSep = username.IndexOf('\\');
            if (domainSep < 0)
            {
                return username;
            }
            else
            {
                return username.Substring(domainSep + 1);
            }
        }

        private string[] StripDomains(string[] usernames)
        {
            string[] domainless = new string[usernames.Length];
            for (int i = 0; i < usernames.Length; i++)
            {
                domainless[i] = StripDomain(usernames[i]);
            }
            return domainless;
        }
    }
}
