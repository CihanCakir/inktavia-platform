namespace Aizen.Core.Domain
{
    public class AizenBaseDto
    {
        public int Id { get; set; }

        public string? ModifyHost { get; set; }

        public int? ModifyUserId { get; set; }

        public DateTime? ModifyDate { get; set; }

        public int? CreateUserId { get; set; }

        public string? CreateHost { get; set; }

        public DateTime? CreateDate { get; set; }
    }
}
