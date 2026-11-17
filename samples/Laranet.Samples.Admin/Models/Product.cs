using System.ComponentModel.DataAnnotations;

namespace Laranet.Samples.Admin.Models;

public sealed class Product
{
    // Field inference (ADR-0015): annotations keep the panel's labels and
    // read-only key exactly as they were declared explicitly before.
    [Editable(false)]
    [Display(Name = "ID")]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
