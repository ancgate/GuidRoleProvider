using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Web;

namespace GuidRoleProvider
{
    /* Declare connection string with name "RoleProviderContext" and role provider as 

    <roleManager enabled="true" cacheRolesInCookie="false" defaultProvider="GuidRoleProvider">
        <providers>
            <clear />
            <add name="GRoleProvider" type="GuidRoleProvider.GRoleProvider, GuidRoleProvider" connectionStringName="RoleProviderContext" />
        </providers>
    </roleManager>

    Required Table Schema (Column order and contraint names don't matter)
    
    CREATE TABLE [dbo].[users]  ( 
        [ad_user_guid] 	uniqueidentifier NOT NULL,
        [last_name] 	varchar(25) NULL,
        [user_name] 	varchar(50) NULL,
        [email]    	    varchar(50) NULL,
        [first_name]	varchar(25) NULL,
        [user_id]   	int IDENTITY(1,1) NOT NULL,
        [insert_dt]     datetime NOT NULL,
        [insert_by]     varchar(25) NOT NULL,
        [update_dt]     datetime NOT NULL,
        [update_by]     varchar(25) NOT NULL,
        CONSTRAINT [UserPKey] PRIMARY KEY CLUSTERED([user_id])
    )

    CREATE TABLE [dbo].[roles]  ( 
        [role_id]  	int IDENTITY(1,1) NOT NULL,
        [role_name]	varchar(50) NOT NULL,
        [insert_dt]     datetime NOT NULL,
        [insert_by]     varchar(25) NOT NULL,
        [update_dt]     datetime NOT NULL,
        [update_by]     varchar(25) NOT NULL,
        CONSTRAINT [PKeyRole] PRIMARY KEY CLUSTERED([role_id])
    )

    CREATE TABLE [dbo].[user_roles]  ( 
        [role_id]	int NOT NULL,
        [user_id]	int NOT NULL,
        [update_dt]     datetime NOT NULL,
        [update_by]     varchar(25) NOT NULL,
        CONSTRAINT [UserRoleKey] PRIMARY KEY CLUSTERED([role_id],[user_id])
    )
    ALTER TABLE [dbo].[user_roles]
        ADD CONSTRAINT [UserFKey]
        FOREIGN KEY([user_id])
        REFERENCES [dbo].[users]([user_id])
        ON DELETE NO ACTION 
        ON UPDATE NO ACTION 
    ALTER TABLE [dbo].[user_roles]
        ADD CONSTRAINT [RoleFKey]
        FOREIGN KEY([role_id])
        REFERENCES [dbo].[roles]([role_id])
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

        public readonly string userTable = "users";
        public readonly string userIdCol = "user_id";
        public readonly string userNameCol = "user_name";
        public readonly string userGuidCol = "ad_user_guid";
        public readonly string userFNameCol = "first_name";
        public readonly string userLNameCol = "last_name";
        public readonly string userEmailCol = "email";

        public readonly string roleTable = "roles";
        public readonly string roleIdCol = "role_id";
        public readonly string roleNameCol = "role_name";

        public readonly string userRoleTable = "user_roles";
        public readonly string userFKeyRelation = "user_key";
        public readonly string roleFKeyRelation = "role_key";

        public readonly string junctionTable = "junction";
        public readonly string userJuncRelation = "user_junction";
        public readonly string roleJuncRelation = "role_junction";

        public readonly string insertDtCol = "insert_dt";
        public readonly string insertByCol = "insert_by";
        public readonly string updateDtCol = "update_dt";
        public readonly string updateByCol = "update_by";

        public RoleProviderContext()
        {
            sqlConn.ConnectionString = ConfigurationManager.ConnectionStrings["RoleProviderContext"].ConnectionString;
            sqlConn.Open();

            // User table
            userAdapter = new SqlDataAdapter(string.Format("select * from {0}", userTable), sqlConn);
            userAdapter.FillSchema(db, SchemaType.Source, userTable);
            userAdapter.Fill(db, userTable);

            // Role table
            roleAdapter = new SqlDataAdapter(string.Format("select * from {0}", roleTable), sqlConn);
            roleAdapter.FillSchema(db, SchemaType.Source, roleTable);
            roleAdapter.Fill(db, roleTable);

            // UserRole join table
            userRoleAdapter = new SqlDataAdapter(string.Format("select * from {0}", userRoleTable), sqlConn);
            userRoleAdapter.FillSchema(db, SchemaType.Source, userRoleTable);
            userRoleAdapter.Fill(db, userRoleTable);

            db.Relations.Add(userFKeyRelation, db.Tables[userTable].Columns[userIdCol], db.Tables[userRoleTable].Columns[userIdCol]);
            db.Relations.Add(roleFKeyRelation, db.Tables[roleTable].Columns[roleIdCol], db.Tables[userRoleTable].Columns[roleIdCol]);

            // Read only join of User and Role tables based on UserRole table.
            using (SqlDataAdapter junctionAdapter = new SqlDataAdapter(
                string.Format("select u.*, r.* from {0} ur join {1} u on ur.{2} = u.{2} join {3} r on ur.{4} = r.{4}",
                userRoleTable, userTable, userIdCol, roleTable, roleIdCol), sqlConn))
            {
                junctionAdapter.FillSchema(db, SchemaType.Source, junctionTable);
                junctionAdapter.Fill(db, junctionTable);
            }

            db.Relations.Add(userJuncRelation, db.Tables[userTable].Columns[userIdCol], db.Tables[junctionTable].Columns[userIdCol]);
            db.Relations.Add(roleJuncRelation, db.Tables[roleTable].Columns[roleIdCol], db.Tables[junctionTable].Columns[roleIdCol]);
        }

        public void SaveChanges()
        {
            // Update last update date
            DataSet changes = db.GetChanges();

            if (changes != null)
            {
                foreach (DataRow row in changes.Tables[userTable].Rows)
                {
                    if (row.RowState == DataRowState.Added || row.RowState == DataRowState.Modified)
                    {
                        row[updateDtCol] = DateTime.Now;
                        row[updateByCol] = HttpContext.Current.User.Identity.Name;
                    }
                }
                userAdapter.UpdateCommand = new SqlCommandBuilder(userAdapter).GetUpdateCommand();
                userAdapter.Update(changes, userTable);

                foreach (DataRow row in changes.Tables[roleTable].Rows)
                {
                    if (row.RowState == DataRowState.Added || row.RowState == DataRowState.Modified)
                    {
                        row[updateDtCol] = DateTime.Now;
                        row[updateByCol] = HttpContext.Current.User.Identity.Name;
                    }
                }
                roleAdapter.UpdateCommand = new SqlCommandBuilder(roleAdapter).GetUpdateCommand();
                roleAdapter.Update(changes, roleTable);

                foreach (DataRow row in changes.Tables[userRoleTable].Rows)
                {
                    if (row.RowState == DataRowState.Added || row.RowState == DataRowState.Modified)
                    {
                        row[updateDtCol] = DateTime.Now;
                        row[updateByCol] = HttpContext.Current.User.Identity.Name;
                    }
                }
                userRoleAdapter.UpdateCommand = new SqlCommandBuilder(userRoleAdapter).GetUpdateCommand();
                userRoleAdapter.Update(changes, userRoleTable);

                db.AcceptChanges(); // Don't double apply if savechanges is called again
            }
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
