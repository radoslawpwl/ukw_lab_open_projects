using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SklepInternetowyDocker.Models;

public class ApplicationUser : IdentityUser
{
    [PersonalData, MaxLength(150)]
    public string? ShippingRecipient { get; set; }

    [PersonalData, MaxLength(200)]
    public string? ShippingStreet { get; set; }

    [PersonalData, MaxLength(20)]
    public string? ShippingPostalCode { get; set; }

    [PersonalData, MaxLength(100)]
    public string? ShippingCity { get; set; }

    [PersonalData, MaxLength(100)]
    public string? ShippingCountry { get; set; }

    [PersonalData, MaxLength(200)]
    public string? InvoiceRecipient { get; set; }

    [PersonalData, MaxLength(32)]
    public string? InvoiceTaxNumber { get; set; }

    [PersonalData]
    public bool InvoiceUsesShippingAddress { get; set; } = true;

    [PersonalData, MaxLength(200)]
    public string? InvoiceStreet { get; set; }

    [PersonalData, MaxLength(20)]
    public string? InvoicePostalCode { get; set; }

    [PersonalData, MaxLength(100)]
    public string? InvoiceCity { get; set; }

    [PersonalData, MaxLength(100)]
    public string? InvoiceCountry { get; set; }
}