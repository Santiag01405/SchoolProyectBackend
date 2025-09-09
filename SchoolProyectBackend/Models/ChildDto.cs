namespace SchoolProyectBackend.Models
{
    public class ChildDto
    {
        public int RelationID { get; set; }
        public int StudentUserID { get; set; }
        public string StudentName { get; set; } = "";
        public string? Email { get; set; }
        public int SchoolID { get; set; }
        public string? SchoolName { get; set; }
    }

}
