using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Category
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Agrega esta línea
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; }

    public int DisplayOrder { get; set; }
}