using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SklepInternetowyDocker.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceCity",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceCountry",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoicePostalCode",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceRecipient",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceStreet",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceTaxNumber",
                table: "AspNetUsers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InvoiceUsesShippingAddress",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCity",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingCountry",
                table: "AspNetUsers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingPostalCode",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingRecipient",
                table: "AspNetUsers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingStreet",
                table: "AspNetUsers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceCity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InvoiceCountry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InvoicePostalCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InvoiceRecipient",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InvoiceStreet",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InvoiceTaxNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InvoiceUsesShippingAddress",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ShippingCity",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ShippingCountry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ShippingPostalCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ShippingRecipient",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ShippingStreet",
                table: "AspNetUsers");
        }
    }
}
