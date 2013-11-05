using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GuidRoleProvider.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public virtual ICollection<Role> Roles { get; set; }
    }
}
