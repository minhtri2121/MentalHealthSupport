namespace MentalHealthSupport.Models.ViewModel
{
    public class ChatbotConsultantItemViewModel
    {
        public int ConsultantId { get; set; }
        public string FullName { get; set; } = "";
        public string Specialty { get; set; } = "";
        public int ExperienceYears { get; set; }
    }
}