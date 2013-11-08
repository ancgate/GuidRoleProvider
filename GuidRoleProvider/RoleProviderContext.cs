using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace GuidRoleProvider
{
    /* Required Table Schema (Column order and contraint names don't matter)
    
    CREATE TABLE [dbo].[Users]  ( 
        [UserGuid] 	uniqueidentifier NOT NULL,
        [LastName] 	varchar(25) NULL,
        [UserName] 	varchar(50) NULL,
        [Email]    	varchar(50) NULL,
        [Phone]    	varchar(25) NULL,
        [FirstName]	varchar(25) NULL,
        [UserId]   	int IDENTITY(1,1) NOT NULL,
        CONSTRAINT [UserPKey] PRIMARY KEY CLUSTERED([UserId])
    )

    CREATE TABLE [dbo].[Roles]  ( 
        [RoleId]  	int IDENTITY(1,1) NOT NULL,
        [RoleName]	varchar(50) NOT NULL,
        CONSTRAINT [PKeyRole] PRIMARY KEY CLUSTERED([RoleId])
    )

    CREATE TABLE [dbo].[UserRoles]  ( 
        [RoleId]	int NOT NULL,
        [UserId]	int NOT NULL,
        CONSTRAINT [UserRoleKey] PRIMARY KEY CLUSTERED([RoleId],[UserId])
    )
    ALTER TABLE [dbo].[UserRoles]
        ADD CONSTRAINT [UserFKey]
        FOREIGN KEY([UserId])
        REFERENCES [dbo].[Users]([UserId])
        ON DELETE NO ACTION 
        ON UPDATE NO ACTION 
    ALTER TABLE [dbo].[UserRoles]
        ADD CONSTRAINT [RoleFKey]
        FOREIGN KEY([RoleId])
        REFERENCES [dbo].[Roles]([RoleId])
        ON DELETE NO ACTION 
        ON UPDATE NO ACTION 
     
     */
    internal sealed class RoleProviderContext : IDisposable
    {
        private SqlConnection sqlConn = new SqlConnection();
        public DataSet db = new DataSet("RoleProvider");
        private SqlDataAdapter userAdapter;
        private SqlDataAdapter roleAdapter;
        private SqlDataAdapter userRoleAdapter;

        public RoleProviderContext()
        {
            sqlConn.ConnectionString = ConfigurationManager.ConnectionStrings["RoleProviderContext"].ConnectionString;
            sqlConn.Open();

            // User table
            userAdapter = new SqlDataAdapter("select * from Users", sqlConn);
            userAdapter.FillSchema(db, SchemaType.Source, "Users");
            userAdapter.Fill(db, "Users");

            // Role table
            roleAdapter = new SqlDataAdapter("select * from Roles", sqlConn);
            roleAdapter.FillSchema(db, SchemaType.Source, "Roles");
            roleAdapter.Fill(db, "Roles");

            // UserRole join table
            userRoleAdapter = new SqlDataAdapter("select * from UserRoles", sqlConn);
            userRoleAdapter.FillSchema(db, SchemaType.Source, "UserRoles");
            userRoleAdapter.Fill(db, "UserRoles");

            db.Relations.Add("UserKey", db.Tables["Users"].Columns["UserId"], db.Tables["UserRoles"].Columns["UserId"]);
            db.Relations.Add("RoleKey", db.Tables["Roles"].Columns["RoleId"], db.Tables["UserRoles"].Columns["RoleId"]);

            // Read only join of User and Role tables based on UserRole table.
            using(SqlDataAdapter junctionAdapter = new SqlDataAdapter("select u.*, r.* from UserRoles ur join Users u on ur.UserId = u.UserId join Roles r on ur.RoleId = r.RoleId", sqlConn))
            {
                junctionAdapter.FillSchema(db, SchemaType.Source, "Junction");
                junctionAdapter.Fill(db, "Junction");
            }

            db.Relations.Add("UserJunction", db.Tables["Users"].Columns["UserId"], db.Tables["Junction"].Columns["UserId"]);
            db.Relations.Add("RoleJunction", db.Tables["Roles"].Columns["RoleId"], db.Tables["Junction"].Columns["RoleId"]);
        }

        public void SaveChanges()
        {
            userAdapter.UpdateCommand = new SqlCommandBuilder(userAdapter).GetUpdateCommand();
            userAdapter.Update(db, "Users");

            roleAdapter.UpdateCommand = new SqlCommandBuilder(roleAdapter).GetUpdateCommand();
            roleAdapter.Update(db, "Roles");

            userRoleAdapter.UpdateCommand = new SqlCommandBuilder(userRoleAdapter).GetUpdateCommand();
            userRoleAdapter.Update(db, "UserRoles");
        }

        public void Dispose()
        {
            db.Dispose();
            roleAdapter.Dispose();
            userAdapter.Dispose();
            userRoleAdapter.Dispose();
            sqlConn.Dispose();
        }
    }
}
