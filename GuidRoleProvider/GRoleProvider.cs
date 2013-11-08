using GuidRoleProvider.Models;
using System;
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
                    DataRow user = context.db.Tables["Users"].AsEnumerable().FirstOrDefault(x => x.Field<string>("UserName").Equals(username, StringComparison.OrdinalIgnoreCase));

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
                                    context.db.Tables["UserRoles"].Rows.Add(user["UserId"], role["RoleId"]);
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
                    context.db.Tables["Roles"].Rows.Add(null, roleName); // null is for identity column
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

        ///// <summary>
        ///// WTH is the point of this method? Maybe I'm not understanding it correctly
        ///// </summary>
        ///// <param name="roleName"></param>
        ///// <param name="usernameToMatch"></param>
        ///// <returns></returns>
        //public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        //{
        //    List<string> users = new List<string>();

        //    using (var context = new RoleProviderContext())
        //    {
        //        Role role = context.Roles.SingleOrDefault(x => x.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));

        //        if (role != null)
        //        {
        //            foreach (var user in role.Users)
        //            {
        //                if (user.UserName.Equals(usernameToMatch, StringComparison.OrdinalIgnoreCase))
        //                {
        //                    users.Add(user.UserName);
        //                }
        //            }
        //        }
        //    }
        //    return users.ToArray();
        //}

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
                var user = context.db.Tables["Junction"].AsEnumerable().Where(x => x.Field<string>("UserName").Equals(username, StringComparison.OrdinalIgnoreCase));

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
                DataRow user = context.db.Tables["Junction"].AsEnumerable().Where(x => x.Field<string>("UserName").Equals(username, StringComparison.OrdinalIgnoreCase))
                    .SingleOrDefault(y => y.Field<string>("RoleName").Equals(roleName, StringComparison.OrdinalIgnoreCase));

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
                    DataRow user = context.db.Tables["Users"].AsEnumerable().SingleOrDefault(x => x.Field<string>("UserName").Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (user != null)
                    {
                        var AllDbRoles = context.db.Tables["Roles"].AsEnumerable();

                        foreach (var role in AllDbRoles)
                        {
                            foreach (string roleName in roleNames)
                            {
                                if (role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase)
                                && role.Users.Any(x => x.UserName.Equals(username, StringComparison.OrdinalIgnoreCase)))
                                {
                                    role.Users.Remove(user);
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

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }
    }
}
