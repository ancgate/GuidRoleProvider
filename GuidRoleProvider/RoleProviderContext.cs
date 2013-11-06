using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace GuidRoleProvider
{
    internal sealed class RoleProviderContext : IDisposable
    {
        private SqlConnection sqlConn = new SqlConnection();
        public DataSet db = new DataSet("RoleProvider");
        private SqlDataAdapter userAdapter;
        private SqlDataAdapter roleAdapter;
        private SqlDataAdapter junctionAdapter;

        public RoleProviderContext()
        {
            sqlConn.ConnectionString = ConfigurationManager.ConnectionStrings["RoleProviderContext"].ConnectionString;
            sqlConn.Open();

            userAdapter = new SqlDataAdapter("select * from Users", sqlConn);
            userAdapter.FillSchema(db, SchemaType.Source, "Users");
            userAdapter.Fill(db, "Users");

            roleAdapter = new SqlDataAdapter("select * from Roles", sqlConn);
            roleAdapter.FillSchema(db, SchemaType.Source, "Roles");
            roleAdapter.Fill(db, "Roles");

            junctionAdapter = new SqlDataAdapter("select * from UserRoles", sqlConn);
            junctionAdapter.FillSchema(db, SchemaType.Source, "UserRoles");
            junctionAdapter.Fill(db, "UserRoles");
        }

        public void SaveChanges()
        {
            userAdapter.UpdateCommand = new SqlCommandBuilder(userAdapter).GetUpdateCommand();
            userAdapter.Update(db, "Users");

            roleAdapter.UpdateCommand = new SqlCommandBuilder(roleAdapter).GetUpdateCommand();
            roleAdapter.Update(db, "Roles");

            junctionAdapter.UpdateCommand = new SqlCommandBuilder(junctionAdapter).GetUpdateCommand();
            junctionAdapter.Update(db, "UserRoles");
        }

        public void Dispose()
        {
            db.Dispose();
            roleAdapter.Dispose();
            userAdapter.Dispose();
            junctionAdapter.Dispose();
            sqlConn.Dispose();
        }
    }
}
