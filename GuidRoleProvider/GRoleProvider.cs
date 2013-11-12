using ActiveDirectoryCommunicator;
using GuidRoleProvider.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// If user or role is not found, it is ignored
        /// </summary>
        /// <param name="usernames"></param>
        /// <param name="roleNames"></param>
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            using (var context = new RoleProviderContext())
            {
                foreach (string username in usernames)
                {
                    DataRow user = ResolveUserRow(username, context,
                        (c) => { return c.db.Tables["Users"].AsEnumerable().SingleOrDefault(x => x.Field<string>("UserName").Equals(username, StringComparison.OrdinalIgnoreCase)); }
                        );

                    if (user != null)
                    {
                        var AllDbRoles = context.db.Tables["Roles"].AsEnumerable().ToList();

                        foreach (var role in AllDbRoles)
                        {
                            foreach (string roleName in roleNames)
                            {
                                // Don't apply duplicates
                                if (role["RoleName"].ToString().Equals(roleName, StringComparison.OrdinalIgnoreCase)
                                    && !user.GetChildRows("UserJunction").Any(x => x.Field<string>("RoleName").Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    DataRow newRow = context.db.Tables["UserRoles"].NewRow();
                                    newRow["UserId"] = user["UserId"];
                                    newRow["RoleId"] = role["RoleId"];
                                    context.db.Tables["UserRoles"].Rows.Add(newRow);
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
                if (!context.db.Tables["Roles"].AsEnumerable().Any(x => x.Field<string>("RoleName")
                    .Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                {
                    DataRow newRow = context.db.Tables["Roles"].NewRow();
                    newRow["RoleId"] = null; // null is for identity column (will be auto assigned)
                    newRow["RoleName"] = roleName;
                    context.db.Tables["Roles"].Rows.Add(newRow);
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
                    DataRow role = context.db.Tables["Roles"].AsEnumerable().SingleOrDefault(x => x.Field<string>("RoleName").Equals(roleName, StringComparison.OrdinalIgnoreCase));
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
            List<string> users = new List<string>();

            using (var context = new RoleProviderContext())
            {
                var matches = ResolveUserRow(usernameToMatch, context,
                        (c) =>
                        {
                            return c.db.Tables["Junction"].AsEnumerable().Where(x => x.Field<string>("RoleName").Equals(roleName, StringComparison.OrdinalIgnoreCase))
                                .Where(y => y.Field<string>("UserName").Equals(usernameToMatch, StringComparison.OrdinalIgnoreCase));
                        }
                        );

                foreach (var row in matches)
                {
                    users.Add(row["UserName"].ToString());
                }
            }
            return users.ToArray();
        }

        public override string[] GetAllRoles()
        {
            List<string> roles = new List<string>();

            using (var context = new RoleProviderContext())
            {
                foreach (DataRow row in context.db.Tables["Roles"].Rows)
                {
                    roles.Add(row["RoleName"].ToString());
                }
            }

            return roles.ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            List<string> roles = new List<string>();

            using (var context = new RoleProviderContext())
            {
                var user = ResolveUserRow(username, context,
                    (c) =>
                    {
                        return c.db.Tables["Junction"].AsEnumerable().Where(x => x.Field<string>("UserName").Equals(username, StringComparison.OrdinalIgnoreCase));
                    }
                    );

                if (user != null)
                {
                    foreach (DataRow row in user)
                    {
                        roles.Add(row["RoleName"].ToString());
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
                var role = context.db.Tables["Junction"].AsEnumerable().Where(x => x.Field<string>("RoleName").Equals(roleName, StringComparison.OrdinalIgnoreCase));

                if (role != null)
                {
                    foreach (DataRow row in role)
                    {
                        users.Add(row["UserName"].ToString());
                    }
                }
            }
            return users.ToArray();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            bool isValid = false;

            using (var context = new RoleProviderContext())
            {
                var user = ResolveUserRow(username, context,
                    (c) =>
                    {
                        return c.db.Tables["Junction"].AsEnumerable().Where(x => x.Field<string>("UserName").Equals(username, StringComparison.OrdinalIgnoreCase))
                                    .SingleOrDefault(y => y.Field<string>("RoleName").Equals(roleName, StringComparison.OrdinalIgnoreCase));
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
            using (var context = new RoleProviderContext())
            {
                foreach (string username in usernames)
                {
                    DataRow user = ResolveUserRow(username, context,
                        (c) => { return c.db.Tables["Users"].AsEnumerable().SingleOrDefault(x => x.Field<string>("UserName").Equals(username, StringComparison.OrdinalIgnoreCase)); }
                        );

                    if (user != null)
                    {
                        var AllDbRoles = context.db.Tables["Roles"].AsEnumerable();

                        foreach (DataRow role in AllDbRoles)
                        {
                            foreach (string roleName in roleNames)
                            {
                                if (role.Field<string>("RoleName").Equals(roleName, StringComparison.OrdinalIgnoreCase))
                                {
                                    var removeRow = user.GetChildRows("UserKey").SingleOrDefault(x => x.Field<int>("RoleId") == role.Field<int>("RoleId"));

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
                if (context.db.Tables["Roles"].AsEnumerable().Any(x => x.Field<string>("RoleName").Equals(roleName, StringComparison.OrdinalIgnoreCase)))
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
                var user = (ActiveDirectoryCommunicator.User)adUsers.SingleOrDefault(x => ("hai-mke\\" + x.LoginName).Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user != null)
                {
                    var row = context.db.Tables["Users"].AsEnumerable().SingleOrDefault(x => x.Field<Guid>("UserGuid").Equals(user.Guid.Value));

                    if (row != null)
                    {
                        row["UserName"] = username;
                    }
                    else // Add new user
                    {
                        DataRow newRow = context.db.Tables["Users"].NewRow();
                        newRow["UserGuid"] = user.Guid.Value;
                        newRow["FirstName"] = user.Name.Substring(0, user.Name.IndexOf(' '));
                        newRow["LastName"] = user.Name.Substring(user.Name.IndexOf(' ') + 1);
                        newRow["UserName"] = username;
                        newRow["Email"] = user.Email;
                        newRow["Phone"] = user.Phone;
                        context.db.Tables["Users"].Rows.Add(newRow);
                    }
                    context.SaveChanges();

                    context.Dispose();

                    using(context = new RoleProviderContext())
                    {
                        // Second try with updated db
                        result = linq(context);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }

            // Username is not permissioned
            return default(T);

        }
    }
}
