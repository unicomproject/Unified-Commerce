using E_POS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace E_POS.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(EPosDbContext))]
    [Migration("20260702143000_UpdateDevelopmentPlatformAdminPassword")]
    public partial class UpdateDevelopmentPlatformAdminPassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DevelopmentPlatformAdminSeedData.UpSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE platform_users
                SET password_hash = 'PBKDF2-SHA256:100000:zG7O+AY1EJBG5+sCXDBinA==:weI+nABmBRNW19gQODOHn5D2q8SUQ0rVJy0NITO/Qyo=',
                    updated_at = now()
                WHERE normalized_email = 'POSUNIQUE001@GMAIL.COM';
                """);
        }
    }
}
