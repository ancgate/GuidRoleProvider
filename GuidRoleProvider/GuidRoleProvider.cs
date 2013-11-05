using GuidRoleProvider.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;

namespace GuidRoleProvider
{
    /// <summary>
    /// In general, all methods are case insensitive
    /// </summary>
    public sealed class GuidRoleProvider : RoleProvider
    {
        public override string ApplicationName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// If user or role is not found, it is ignored
        /// </summary>
        /// <param name="usernames"></param>
        /// <param name="roleNames"></param>
        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            using (var db = new RoleProviderContext())
            {
                foreach (string username in usernames)
                {
                    User user = db.Users.FirstOrDefault(x => x.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (user != null)
                    {
                        var AllDbRoles = db.Roles.ToList();

                        foreach (var role in AllDbRoles)
                        {
                            foreach (string roleName in roleNames)
                            {
                                if (role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase)
                                    && user.Roles.Any(x => x.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    user.Roles.Add(role);
                                }
                            }
                        }
                    }
                }
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Will not create duplicates
        /// </summary>
        /// <param name="roleName"></param>
        public override void CreateRole(string roleName)
        {
            using (var db = new RoleProviderContext())
            {
                if (!db.Roles.Any(x => x.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                {
                    Role role = new Role();
                    role.RoleName = roleName;

                    db.Roles.Add(role);
                    db.SaveChanges();
                }
            }
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            bool ret = false;

            using (var db = new RoleProviderContext())
            {
                try
                {
                    Role role = db.Roles.SingleOrDefault(x => x.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));
                    if (role != null)
                    {
                        db.Roles.Remove(role);
                        db.SaveChanges();
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

            using (var db = new RoleProviderContext())
            {
                Role role = db.Roles.SingleOrDefault(x => x.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));

                if (role != null)
                {
                    foreach (var user in role.Users)
                    {
                        if (user.UserName.Equals(usernameToMatch, StringComparison.OrdinalIgnoreCase))
                        {
                            users.Add(user.UserName);
                        }
                    }
                }
            }
            return users.ToArray();
        }

        public override string[] GetAllRoles()
        {
            List<string> roles = new List<string>();

            using (var db = new RoleProviderContext())
            {
                foreach (var role in db.Roles)
                {
                    roles.Add(role.RoleName);
                }
            }

            return roles.ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            List<string> roles = new List<string>();

            using (var db = new RoleProviderContext())
            {
                var user = db.Users.SingleOrDefault(x => x.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user != null)
                {
                    foreach (var role in user.Roles)
                    {
                        roles.Add(role.RoleName);
                    }
                }
            }
            return roles.ToArray();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            List<string> users = new List<string>();

            using (var db = new RoleProviderContext())
            {
                var role = db.Roles.SingleOrDefault(x => x.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));

                if (role != null)
                {
                    foreach (var user in role.Users)
                    {
                        users.Add(user.UserName);
                    }
                }
            }
            return users.ToArray();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            bool isValid = false;

            using (var db = new RoleProviderContext())
            {
                var user = db.Users.SingleOrDefault(x => x.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user != null)
                {
                    if (user.Roles.Any(x => x.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        isValid = true;
                    }
                }
            }

            return isValid;
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            using (var db = new RoleProviderContext())
            {
                foreach (string username in usernames)
                {
                    User user = db.Users.SingleOrDefault(x => x.UserName.Equals(username, StringComparison.OrdinalIgnoreCase));

                    if (user != null)
                    {
                        var AllDbRoles = db.Roles.ToList();

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
                db.SaveChanges();
            }
        }

        public override bool RoleExists(string roleName)
        {
            bool isValid = false;

            using(var db = new RoleProviderContext())
            {
                if (db.Roles.Any(x => x.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase)))
                {
                    isValid = true;
                }
            }

            return isValid;
        }
    }
}
