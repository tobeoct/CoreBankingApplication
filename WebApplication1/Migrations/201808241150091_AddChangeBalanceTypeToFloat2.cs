namespace WebApplication1.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddChangeBalanceTypeToFloat2 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.CustomerAccounts", "AccountBalance", c => c.Single(nullable: false));
        }

        
        public override void Down()
        {
            AlterColumn("dbo.CustomerAccounts", "AccountBalance", c => c.Long(nullable: false));
        }
    }
}
