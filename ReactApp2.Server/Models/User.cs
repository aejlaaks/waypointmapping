using Microsoft.AspNetCore.Identity;

namespace KarttaBackEnd2.Server.Models
{
    public class User : IdentityUser
    {
        // Voit lisätä käyttäjään liittyviä lisäominaisuuksia tähän, jos tarpeen
        // Esimerkiksi:
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
