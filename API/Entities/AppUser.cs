namespace API.Entities
{
    public class AppUser
    {
        // will be the primary key of our database
        // make sure you call it "Id"
        public int Id { get; set; }
        public string UserName { get; set; }
    }
}